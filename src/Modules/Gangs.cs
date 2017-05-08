using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using DEA.Database.Repositories;
using System.Linq;
using DEA.Database.Models;
using MongoDB.Driver;
using DEA.Services;
using DEA.Common;
using DEA.Common.Preconditions;
using DEA.Services.Static;
using DEA.Common.Extensions;
using DEA.Common.Extensions.DiscordExtensions;
using DEA.Common.Data;

namespace DEA.Modules
{
    public class Gangs : DEAModule
    {
        private readonly GangRepository _gangRepo;
        private readonly UserRepository _userRepo;
        private readonly InteractiveService _interactiveService;

        public Gangs(GangRepository gangRepo, UserRepository userRepo, InteractiveService interactiveService)
        {
            _gangRepo = gangRepo;
            _userRepo = userRepo;
            _interactiveService = interactiveService;
        }

        [Command("CreateGang")]
        [Require(Attributes.NoGang)]
        [Summary("Allows you to create a gang at a hefty price.")]
        public async Task CreateGang([Summary("SLAM EM BOYS")] [Remainder] string name)
        {
            if (Context.Cash < Config.GANG_CREATION_COST)
            {
                ReplyError($"You do not have {Config.GANG_CREATION_COST.USD()}. Balance: {Context.Cash.USD()}.");
            }
            else if (!Config.ALPHANUMERICAL.IsMatch(name))
            {
                ReplyError("Gang names may not contain any non alphanumeric characters.");
            }

            var gang = await _gangRepo.CreateGangAsync(Context, name);
            await _userRepo.EditCashAsync(Context, -Config.GANG_CREATION_COST);

            await ReplyAsync($"You have successfully created the {gang.Name} gang.");
        }

        [Command("JoinGang")]
        [Require(Attributes.NoGang)]
        [Summary("Sends a request to join a gang.")]
        public async Task AddToGang([Summary("SLAM EM BOYS")] [Remainder] string gangName)
        {
            var gang = await _gangRepo.GetGangAsync(gangName, Context.Guild.Id);
            if (gang.Members.Length == 4)
            {
                ReplyError("This gang is already full!");
            }

            var leader = await (Context.Guild as IGuild).GetUserAsync(gang.LeaderId);

            if (leader != null)
            {
                var leaderDM = await leader.CreateDMChannelAsync();

                var key = Config.RAND.Next();
                await leaderDM.SendAsync($"{Context.User} has requested to join your gang. Please respond with \"{key}\" within 5 minutes to add this user to your gang.");

                await ReplyAsync($"The leader of {gang.Name} has been informed of your request to join their gang.");

                var answer = await _interactiveService.WaitForMessage(leaderDM, x => x.Content == key.ToString(), TimeSpan.FromMinutes(5));
                if (answer != null)
                {
                    if (await _gangRepo.InGangAsync(Context.GUser))
                    {
                        await leaderDM.SendAsync("This user has already joined a different gang.");
                    }
                    else if ((await _gangRepo.GetGangAsync(leader)).Members.Length == 4)
                    {
                        await leaderDM.SendAsync("Your gang is already full.");
                    }
                    else
                    {
                        await _gangRepo.AddMemberAsync(gang, Context.User.Id);

                        await leaderDM.SendAsync($"You have successfully added {Context.User} as a member of your gang.");

                        await Context.User.Id.DMAsync(Context.Client, $"Congrats! {leader} has accepted your request to join {gang.Name}!");
                    }
                }
            }
            else
            {
                await ReplyAsync("The leader of that gang is no longer in this server. ***RIP GANG ROFL***");
            }
        }

        [Command("Gang")]
        [Summary("Gives you all the info about any gang.")]
        public async Task GangInfo([Summary("SLAM EM BOYS")] [Remainder] string gangName = null)
        {
            Gang gang;
            if (gangName == null)
            {
                gang = Context.Gang;
            }
            else
            {
                gang = await _gangRepo.GetGangAsync(gangName, Context.Guild.Id);
            }

            if (gang == null && gangName == null)
            {
                ReplyError("You are not in a gang.");
            }

            var members = string.Empty;
            var guildInterface = Context.Guild as IGuild;
            foreach (var member in gang.Members)
            {
                var user = await guildInterface.GetUserAsync(member);
                if (user != null)
                {
                    members += $"{user.Boldify()}, ";
                }
            }

            if (members.Length != 0)
            {
                members = $"__**Members:**__ {members.Substring(0, members.Length - 2)}\n";
            }

            var leader = await guildInterface.GetUserAsync(gang.LeaderId);
            if (leader != null)
            {
                members = $"__**Leader:**__ {leader.Boldify()}\n" + members;
            }

            var description = members + $"__**Wealth:**__ {gang.Wealth.USD()}\n" +
                              $"__**Interest rate:**__ {InterestRate.Calculate(gang.Wealth).ToString("P")}";

            await SendAsync(description, gang.Name);
        }

        [Command("GangLb")]
        [Alias("gangs")]
        [Summary("Shows the wealthiest gangs.")]
        public async Task Ganglb()
        {
            var gangs = await (await _gangRepo.Collection.FindAsync(y => y.GuildId == Context.Guild.Id)).ToListAsync();

            if (gangs.Count == 0)
            {
                ReplyError("There aren't any gangs yet.");
            }

            var sortedGangs = gangs.OrderByDescending(x => x.Wealth).ToList();
            string description = string.Empty;

            for (int i = 0; i < sortedGangs.Count(); i++)
            {
                if (i + 1 > Config.GANGSLB_CAP)
                {
                    break;
                }

                description += $"{i + 1}. {sortedGangs[i].Name.Boldify()}: {sortedGangs[i].Wealth.USD()}\n";
            }

            await SendAsync(description, "The Wealthiest Gangs");
        }

        [Command("LeaveGang")]
        [Require(Attributes.InGang)]
        [Summary("Allows you to break all ties with a gang.")]
        public async Task LeaveGang()
        {
            if (Context.Gang.LeaderId == Context.User.Id)
            {
                ReplyError($"You may not leave a gang if you are the owner. Either destroy the gang with the `{Context.Prefix}DestroyGang` command, or " +
                                    $"transfer the ownership of the gang to another member with the `{Context.Prefix}TransferLeadership` command.");
            }

            await _gangRepo.RemoveMemberAsync(Context.Gang, Context.User.Id);
            await ReplyAsync($"You have successfully left {Context.Gang.Name}.");

            await Context.Gang.LeaderId.DMAsync(Context.Client, $"{Context.User} has left {Context.Gang.Name}.");
        }

        [Command("KickGangMember")]
        [Require(Attributes.InGang, Attributes.GangLeader)]
        [Summary("Kicks a user from your gang.")]
        public async Task KickFromGang([Remainder] IGuildUser gangMember)
        {
            if (gangMember.Id == Context.User.Id)
            {
                ReplyError("You may not kick yourself!");
            }
            else if (!_gangRepo.IsMemberOfAsync(Context.Gang, gangMember.Id))
            {
                ReplyError("This user is not a member of your gang!");
            }

            await _gangRepo.RemoveMemberAsync(Context.Gang, gangMember.Id);
            await ReplyAsync($"You have successfully kicked {gangMember} from {Context.Gang.Name}.");

            await gangMember.Id.DMAsync(Context.Client, $"You have been kicked from {Context.Gang.Name}.");
        }

        [Command("DestroyGang")]
        [Require(Attributes.InGang, Attributes.GangLeader)]
        [Summary("Destroys a gang entirely taking down all funds with it.")]
        public async Task DestroyGang()
        {
            await _gangRepo.DestroyGangAsync(Context.GUser);
            await ReplyAsync($"You have successfully destroyed your gang.");
        }

        [Command("ChangeGangName")]
        [Alias("ChangeName")]
        [Require(Attributes.InGang, Attributes.GangLeader)]
        [Summary("Changes the name of your gang.")]
        public async Task ChangeGangName([Summary("JERK EM OFF BOYS")] [Remainder] string newName)
        {
            if (Context.Cash < Config.GANG_NAME_CHANGE_COST)
            {
                ReplyError($"You do not have {Config.GANG_NAME_CHANGE_COST.USD()}. Balance: {Context.Cash.USD()}.");
            }

            var gangs = await (await _gangRepo.Collection.FindAsync(y => y.GuildId == Context.Guild.Id)).ToListAsync();

            if (gangs.Any(x => x.Name.ToLower() == newName.ToLower()))
            {
                ReplyError($"There is already a gang by the name {newName}.");
            }

            if (!Config.ALPHANUMERICAL.IsMatch(newName))
            {
                ReplyError("Gang names may not contain any non alphanumeric characters.");
            }

            await _userRepo.EditCashAsync(Context, -Config.GANG_NAME_CHANGE_COST);
            await _gangRepo.ModifyAsync(Context.Gang, x => x.Name = newName);

            await ReplyAsync($"You have successfully changed your gang name to {newName} at the cost of {Config.GANG_NAME_CHANGE_COST.USD()}.");
        }

        [Command("Deposit")]
        [Require(Attributes.InGang)]
        [Summary("Deposit cash into your gang's funds.")]
        public async Task Deposit(decimal cash)
        {
            if (cash < Config.MIN_DEPOSIT)
            {
                ReplyError($"The lowest deposit is {Config.MIN_DEPOSIT.USD()}.");
            }
            else if (Context.Cash < cash)
            {
                ReplyError($"You do not have enough money. Balance: {Context.Cash.USD()}.");
            }

            await _userRepo.EditCashAsync(Context, -cash);
            await _gangRepo.ModifyAsync(Context.Gang, x => x.Wealth = Context.Gang.Wealth + cash);

            await ReplyAsync($"You have successfully deposited {cash.USD()}. " +
                        $"{Context.Gang.Name}'s Wealth: {Context.Gang.Wealth.USD()}");

            await Context.Gang.LeaderId.DMAsync(Context.Client, $"{Context.User} deposited {cash.USD()} into your gang's wealth.");
        }

        [Command("Withdraw")]
        [Require(Attributes.InGang)]
        [RequireCooldown]
        [Summary("Withdraw cash from your gang's funds.")]
        public async Task Withdraw(decimal cash)
        {
            if (cash < Config.MIN_WITHDRAW)
            {
                ReplyError($"The minimum withdrawal is {Config.MIN_WITHDRAW.USD()}.");
            }
            else if (cash > Math.Round(Context.Gang.Wealth * Config.WITHDRAW_CAP, 2))
            {
                ReplyError($"You may only withdraw {Config.WITHDRAW_CAP.ToString("P")} of your gang's wealth, " +
                                    $"that is {(Context.Gang.Wealth * Config.WITHDRAW_CAP).USD()}.");
            }

            await _userRepo.ModifyAsync(Context.DbUser, x => x.Withdraw = DateTime.UtcNow);
            await _gangRepo.ModifyAsync(Context.Gang, x => x.Wealth = Context.Gang.Wealth - cash);
            await _userRepo.EditCashAsync(Context, +cash);

            await ReplyAsync($"You have successfully withdrawn {cash.USD()}. " +
                             $"{Context.Gang.Name}'s Wealth: {Context.Gang.Wealth.USD()}.");

            await Context.Gang.LeaderId.DMAsync(Context.Client, $"{Context.User} has withdrawn {cash.USD()} from your gang's wealth.");
        }

        [Command("Raid")]
        [Require(Attributes.InGang)]
        [RequireCooldown]
        [Summary("Raid another gang in attempt to steal some of their wealth.")]
        public async Task Raid(decimal resources, [Summary("SLAM EM BOYS")] [Remainder] string gangName)
        {
            if (resources < Config.MIN_RESOURCES)
            {
                ReplyError($"The minimum amount of money to spend on resources for a raid is {Config.MIN_RESOURCES.USD()}.");
            }
            else if (Context.Gang.Wealth < resources)
            {
                ReplyError($"Your gang does not have enough money. {Context.Gang.Name}'s Wealth {Context.Gang.Wealth.USD()}.");
            }

            var raidedGang = await _gangRepo.GetGangAsync(gangName, Context.Guild.Id);
            if (Math.Round(resources, 2) > Math.Round(raidedGang.Wealth * Config.MAX_RAID_PERCENTAGE / 2, 2))
            {
                ReplyError($"You are overkilling it. You only need {(raidedGang.Wealth * Config.MAX_RAID_PERCENTAGE / 2).USD()} " +
                           $"to steal {Config.MAX_RAID_PERCENTAGE.ToString("P")} of their cash, that is {(raidedGang.Wealth * Config.MAX_RAID_PERCENTAGE).USD()}.");
            }

            var stolen = resources * 2;

            int roll = Config.RAND.Next(1, 101);
            if (Config.RAID_SUCCESS_ODDS > roll)
            {
                await _gangRepo.ModifyGangAsync(gangName, Context.Guild.Id, x => x.Wealth = raidedGang.Wealth - stolen);
                await _gangRepo.ModifyAsync(Context.Gang, x => x.Wealth = Context.Gang.Wealth + stolen);
                await _gangRepo.ModifyAsync(Context.Gang, x => x.Raid = DateTime.UtcNow);

                await raidedGang.LeaderId.DMAsync(Context.Client, $"{Context.Gang.Name} just raided your gang's wealth and managed to walk away with {stolen.USD()}.");

                await ReplyAsync($"With a {Config.RAID_SUCCESS_ODDS}.00% chance of success, you successfully stole {stolen.USD()}. " +
                                 $"{Context.Gang.Name}'s Wealth {Context.Gang.Wealth.USD()}.");
            }
            else
            {
                await _gangRepo.ModifyAsync(Context.Gang, x => x.Wealth = Context.Gang.Wealth - resources);
                await _gangRepo.ModifyAsync(Context.Gang, x => x.Raid = DateTime.UtcNow);

                await raidedGang.LeaderId.DMAsync(Context.Client, $"{Context.Gang.Name} tried to raid your gang's stash, but one of your loyal sicarios gunned them out.");

                await ReplyAsync($"With a {Config.RAID_SUCCESS_ODDS}.00% chance of success, you failed to steal {stolen.USD()} " +
                                 $"and lost all resources in the process.");
            }
        }

    }
}
