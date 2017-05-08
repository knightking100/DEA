using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using DEA.Database.Repositories;
using DEA.Common;
using DEA.Common.Preconditions;
using DEA.Services;
using DEA.Common.Extensions;
using DEA.Common.Data;

namespace DEA.Modules
{
    [RequireCooldown]
    public class Crime : DEAModule
    {
        private readonly UserRepository _userRepo;
        private readonly GangRepository _gangRepo;
        private readonly ModerationService _moderationService;

        private readonly Item[] _items;
        public Crime(UserRepository userRepo, GangRepository gangRepo, ModerationService moderationService, Item[] _items)
        {
            _userRepo = userRepo;
            _gangRepo = gangRepo;
            _moderationService = moderationService;
            _items = items;
        }

        [Command("Whore")]
        [Summary("Sell your body for some quick cash.")]
        public async Task Whore()
        {
            await _userRepo.ModifyAsync(Context.DbUser, x => x.Whore = DateTime.UtcNow);

            int roll = Config.RAND.Next(1, 101);
            if (roll > Config.WHORE_ODDS)
            {
                await _userRepo.EditCashAsync(Context, -Config.WHORE_FINE);

                await ReplyAsync($"What are the fucking odds that one of your main clients was a cop... " +
                            $"You are lucky you only got a {Config.WHORE_FINE.USD()} fine. Balance: {Context.Cash.USD()}.");
            }
            else
            {
                decimal moneyWhored = (Config.RAND.Next((int)(Config.MIN_WHORE) * 100, (int)(Config.MAX_WHORE) * 100)) / 100m;
                await _userRepo.EditCashAsync(Context, moneyWhored);

                await ReplyAsync($"You whip it out and manage to rake in {moneyWhored.USD()}. Balance: {Context.Cash.USD()}.");
            }
        }

        [Command("Jump")]
        [Require(Attributes.Jump)]
        [Summary("Jump some random nigga in the hood.")]
        public async Task Jump()
        {
            await _userRepo.ModifyAsync(Context.DbUser, x => x.Jump = DateTime.UtcNow);

            int roll = Config.RAND.Next(1, 101);
            if (roll > Config.JUMP_ODDS)
            {
                await _userRepo.EditCashAsync(Context, -Config.JUMP_FINE);

                await ReplyAsync($"Turns out the nigga was a black belt, whooped your ass, and brought you in. " +
                            $"Court's final ruling was a {Config.JUMP_FINE.USD()} fine. Balance: {Context.Cash.USD()}.");
            }
            else
            {
                decimal moneyJumped = (Config.RAND.Next((int)(Config.MIN_JUMP) * 100, (int)(Config.MAX_JUMP) * 100)) / 100m;
                await _userRepo.EditCashAsync(Context, moneyJumped);

                await ReplyAsync($"You jump some random nigga on the streets and manage to get {moneyJumped.USD()}. Balance: {Context.Cash.USD()}.");
            }
        }

        [Command("Steal")]
        [Require(Attributes.Steal)]
        [Summary("Snipe some goodies from your local stores.")]
        public async Task Steal()
        {
            await _userRepo.ModifyAsync(Context.DbUser, x => x.Steal = DateTime.UtcNow);

            int roll = Config.RAND.Next(1, 101);
            if (roll > Config.STEAL_ODDS)
            {
                await _userRepo.EditCashAsync(Context, -Config.STEAL_FINE);
                await ReplyAsync($"You were on your way out with the cash, but then some hot chick asked you if you " +
                            $"wanted to bust a nut. Turns out she was cop, and raped you before turning you in. Since she passed on some " +
                            $"nice words to the judge about you not resisting arrest, you managed to walk away with only a " +
                            $"{Config.STEAL_FINE.USD()} fine. Balance: {Context.Cash.USD()}.");
            }
            else
            {
                decimal moneyStolen = (Config.RAND.Next((int)(Config.MIN_STEAL) * 100, (int)(Config.MAX_STEAL) * 100)) / 100m;
                await _userRepo.EditCashAsync(Context, moneyStolen);

                string randomStore = Config.STORES[Config.RAND.Next(1, Config.STORES.Length) - 1];
                await ReplyAsync($"You walk in to your local {randomStore}, point a fake gun at the clerk, and manage to walk away " +
                            $"with {moneyStolen.USD()}. Balance: {Context.Cash.USD()}.");
            }
        }

        [Command("Bully")]
        [Require(Attributes.Bully)]
        [Summary("Bully anyone's nickname to whatever you please.")]
        [RequireBotPermission(GuildPermission.ManageNicknames)]
        public async Task Bully(IGuildUser userToBully, [Remainder] string nickname)
        {
            if (nickname.Length > 32)
            {
                ReplyError("The length of a nickname may not be longer than 32 characters.");
            }
            else if (_moderationService.GetPermLevel(Context, userToBully) > 0)
            {
                ReplyError("You may not bully a moderator.");
            }
            else if ((await _userRepo.GetUserAsync(userToBully)).Cash >= Context.Cash)
            {
                ReplyError("You may not bully a user with more money than you.");
            }

            await userToBully.ModifyAsync(x => x.Nickname = nickname);
            await SendAsync($"{userToBully.Boldify()} just got ***BULLIED*** by {Context.User.Boldify()} with his new nickname: \"{nickname}\".");
        }

        [Command("Rob")]
        [Require(Attributes.Rob)]
        [Summary("Slam anyone's bank account.")]
        public async Task Rob(decimal resources, [Remainder] IGuildUser user)
        {
            if (user.Id == Context.User.Id)
            {
                ReplyError("Only the *retards* try to rob themselves. Are you a retard?");
            }
            else if (resources < Config.MIN_RESOURCES)
            {
                ReplyError($"The minimum amount of money to spend on resources for a robbery is {Config.MIN_RESOURCES.USD()}.");
            }
            else if (Context.Cash < resources)
            {
                ReplyError($"You don't have enough money. Balance: {Context.Cash.USD()}.");
            }

            var raidedDbUser = await _userRepo.GetUserAsync(user);
            if (resources > Math.Round(raidedDbUser.Cash * Config.MAX_ROB_PERCENTAGE / 2, 2))
            {
                ReplyError($"You are overkilling it. You only need {(raidedDbUser.Cash * Config.MAX_ROB_PERCENTAGE / 2).USD()} " +
                           $"to rob {Config.MAX_ROB_PERCENTAGE.ToString("P")} of their cash, that is {(raidedDbUser.Cash * Config.MAX_ROB_PERCENTAGE).USD()}.");
            }

            var stolen = resources * 2;

            int roll = Config.RAND.Next(1, 101);

            var successOdds = await _gangRepo.InGangAsync(Context.GUser) ? Config.ROB_SUCCESS_ODDS - 5 : Config.ROB_SUCCESS_ODDS;

            if (successOdds > roll)
            {
                await _userRepo.EditCashAsync(user, Context.DbGuild, raidedDbUser, -stolen);
                await _userRepo.EditCashAsync(Context, stolen);

                await user.Id.DMAsync(Context.Client, $"{Context.User} just robbed you and managed to walk away with {stolen.USD()}.");

                await ReplyAsync($"With a {successOdds}.00% chance of success, you successfully stole {stolen.USD()}. Balance: {Context.Cash.USD()}.");
            }
            else
            {
                await _userRepo.EditCashAsync(Context, -resources);

                await user.Id.DMAsync(Context.Client, $"{Context.User} tried to rob your sweet cash, but the nigga slipped on a banana peel and got arrested :joy: :joy: :joy:.");

                await ReplyAsync($"With a {successOdds}.00% chance of success, you failed to steal {stolen.USD()} " +
                                 $"and lost all resources in the process. Balance: {Context.Cash.USD()}.");
            }
            await _userRepo.ModifyAsync(Context.DbUser, x => x.Rob = DateTime.UtcNow);
        }
        [Command("Shop")]
        [Summary("List of available shop items.")]
        public async Task Shop([Summary("Bullets")][Remainder]string item = null)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                string description = string.Empty;
                foreach (var kv in _items)
                {
                    description += $"**Cost: {kv.Price}$** | Command: `{Config.Prefix}shop {kv.Name} | Description: {kv.Description}\n";
                }
                await SendAsync(description, "Available Shop Items");
            }
            else if (_items.Any(x => x.Name == item))
            {
                var element = _items.First(x => x.Name == item);
                if (element.Price > Context.Cash)
                {
                    ReplyError($"You do not have enough money. Balance: {Context.Cash.USD()}.");
                }
                if (Context.DbUser.Inventory.Contains(element.Name))
                {
                    await _userRepo.ModifyAsync(Context.DbUser, x => x.Inventory[element.Name] += 1);
                }
                else
                {
                    await _userRepo.ModifyAsync(Context.DbUser, x => x.Inventory.Add(element.Name, 1));
                }
                await ReplyAsync($"Successfully purchased {element.Name}!");
            }
        }        
    }
}
