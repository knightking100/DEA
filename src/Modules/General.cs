using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using DEA.Database.Models;
using DEA.Database.Repositories;
using System.Linq;
using MongoDB.Driver;
using System.Collections.Generic;
using DEA.Services.Handlers;
using DEA.Common;
using DEA.Common.Data;
using DEA.Common.Extensions;
using DEA.Common.Utilities;

namespace DEA.Modules
{
    public class General : DEAModule
    {
        private readonly UserRepository _userRepo;
        private readonly GuildRepository _guildRepo;
        private readonly GangRepository _gangRepo;
        private readonly RankHandler _rankHandler;
        private readonly Item[] _items;

        public General(UserRepository userRepo, GuildRepository guildRepo, GangRepository gangRepo, RankHandler rankHandler, Item[] items)
        {
            _userRepo = userRepo;
            _guildRepo = guildRepo;
            _gangRepo = gangRepo;
            _rankHandler = rankHandler;
            _items = items;
        }

        [Command("Leaderboards")]
        [Alias("lb", "rankings", "highscores")]
        [Summary("View the richest Drug Traffickers.")]
        public async Task Leaderboards()
        {
            var users = await _userRepo.AllAsync(y => y.GuildId == Context.Guild.Id);
            var sorted = users.OrderByDescending(x => x.Cash);
            string description = string.Empty;
            int position = 1;

            if (users.Count == 0)
            {
                ReplyError("There is nobody on the leaderboards yet.");
            }

            var guildInterface = Context.Guild as IGuild;

            foreach (User dbUser in sorted)
            {
                var user = await guildInterface.GetUserAsync(dbUser.UserId);
                if (user == null)
                {
                    continue;
                }

                description += $"{position}. {user.Boldify()}: {dbUser.Cash.USD()}\n";
                if (position >= Config.LEADERBOARD_CAP)
                {
                    break;
                }

                position++;
            }

            await SendAsync(description, "The Richest Traffickers");
        }

        [Command("Donate")]
        [Alias("Sauce")]
        [Summary("Sauce some cash to one of your mates.")]
        public async Task Donate(decimal money, [Remainder] IGuildUser user)
        {
            if (user.Id == Context.User.Id)
            {
                ReplyError("Hey kids! Look at that retard, he is trying to give money to himself!");
            }
            else if (money < Config.DONATE_MIN)
            {
                ReplyError($"Lowest donation is {Config.DONATE_MIN}$.");
            }
            else if (Context.Cash < money)
            {
                ReplyError($"You do not have enough money. Balance: {Context.Cash.USD()}.");
            }

            await _userRepo.EditCashAsync(Context, -money);
            decimal deaMoney = money * Config.DEA_CUT / 100;

            var otherDbUser = await _userRepo.GetUserAsync(user);
            await _userRepo.EditCashAsync(user, Context.DbGuild, otherDbUser,  money - deaMoney);

            await ReplyAsync($"Successfully donated {(money - deaMoney).USD()} to {user.Boldify()}.\nDEA has taken a {deaMoney.USD()} cut out of this donation. Balance: {Context.Cash.USD()}.");
        }

        [Command("Rank")]
        [Summary("View the detailed ranking information of any user.")]
        public async Task Rank([Remainder] IGuildUser user = null)
        {
            user = user ?? Context.GUser;

            var dbUser = user.Id == Context.User.Id ? Context.DbUser : await _userRepo.GetUserAsync(user);
            var users = await (await _userRepo.Collection.FindAsync(y => y.GuildId == Context.Guild.Id)).ToListAsync();
            var sorted = users.OrderByDescending(x => x.Cash).ToList();

            IRole rank = await _rankHandler.GetRankAsync(Context, dbUser);
            var description = $"Balance: {dbUser.Cash.USD()}\n" +
                              $"Position: #{sorted.FindIndex(x => x.UserId == user.Id) + 1}\n";
            if (rank != null)
            {
                description += $"Rank: {rank.Mention}";
            }

            await SendAsync(description, $"Ranking of {user}");
        }

        [Command("Money")]
        [Alias("Cash", "Balance", "Bal")]
        [Summary("View the wealth of anyone.")]
        public async Task Money([Remainder] IGuildUser user = null)
        {
            user = user ?? Context.GUser;
            var dbUser = user.Id == Context.User.Id ? Context.DbUser : await _userRepo.GetUserAsync(user);

            await SendAsync($"{user.Boldify()}'s balance: {dbUser.Cash.USD()}.");
        }

        [Command("Ranks")]
        [Summary("View all ranks.")]
        public async Task Ranks()
        {
            if (Context.DbGuild.RankRoles.ElementCount == 0)
            {
                ReplyError("There are no ranks yet!");
            }

            var description = string.Empty;
            foreach (var rank in Context.DbGuild.RankRoles.OrderBy(x => x.Value.AsDouble))
            {
                var role = Context.Guild.GetRole(ulong.Parse(rank.Name));
                if (role == null)
                {
                    continue;
                }
                description += $"{((decimal)rank.Value.AsDouble).USD()}: {role.Mention}\n";
            }

            await SendAsync(description, "Ranks");
        }

        [Command("ModRoles")]
        [Alias("ModeratorRoles", "ModRole", "PermLevels", "PermissionLevels")]
        [Summary("View all the moderator roles.")]
        public async Task ModRoles()
        {
            if (Context.DbGuild.ModRoles.ElementCount == 0)
            {
                ReplyError("There are no moderator roles yet!");
            }

            var description = "**Moderation Roles:**\n";
            foreach (var modRole in Context.DbGuild.ModRoles.OrderBy(x => x.Value))
            {
                var role = Context.Guild.GetRole(ulong.Parse(modRole.Name));
                if (role == null)
                {
                    continue;
                }
                description += $"{role.Mention}: {modRole.Value}\n";
            }

            await SendAsync(description + "\n**Permission Levels:**\n1: Moderator\n2: Administrator\n3: Owner");
        }
        [Command("Inventory")]
        [Alias("Inv")]
        [Summary("Check the inventory of any user.")]
        public async Task Inventory([Remainder]IGuildUser user = null)
        {
            user = user ?? Context.GUser;
            var dbUser = user.Id == Context.User.Id ? Context.DbUser : await _userRepo.GetUserAsync(user);

            if (dbUser.Inventory.ElementCount == 0)
            {
                if (dbUser.UserId == Context.User.Id)
                {
                    ReplyError("You have nothing in your inventory.");
                }
                else
                {
                    ReplyError("This user has nothing in their inventory.");
                }
                
            }
            var description = string.Empty;
            
            foreach(var item in dbUser.Inventory.Elements)
            {
                if (item.Value.AsInt32 == 1)
                {
                    description += $"1 {item.Name}.";
                }
                else
                {
                    description += $"{item.Value} {item.Name}s.";
                }
            }
            await SendAsync(description, $"Inventory of {user.Boldify()}");
        }
        [Command("Cooldowns")]
        [Summary("Check when you can sauce out more cash.")]
        public async Task Cooldowns([Remainder] IGuildUser user = null)
        {
            user = user ?? Context.GUser;
            var dbUser = Context.User.Id == user.Id ? Context.DbUser : await _userRepo.GetUserAsync(user);

            var cooldowns = new Dictionary<String, TimeSpan>
            {
                { "Whore", Config.WHORE_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(dbUser.Whore)) },
                { "Jump", Config.JUMP_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(dbUser.Jump)) },
                { "Steal", Config.STEAL_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(dbUser.Steal)) },
                { "Rob", Config.ROB_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(dbUser.Rob)) },
                { "Withdraw", Config.WITHDRAW_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(dbUser.Withdraw)) }
            };

            if (await _gangRepo.InGangAsync(user))
            {
                cooldowns.Add("Raid", Config.RAID_COOLDOWN.Subtract(DateTime.UtcNow.Subtract(Context.User.Id == user.Id ? Context.Gang.Raid : (await _gangRepo.GetGangAsync(user)).Raid)));
            }

            var description = string.Empty;
            foreach (var cooldown in cooldowns)
            {
                if (cooldown.Value.TotalMilliseconds > 0)
                {
                    description += $"{cooldown.Key}: {cooldown.Value.Hours}:{cooldown.Value.Minutes.ToString("D2")}:{cooldown.Value.Seconds.ToString("D2")}\n";
                }
            }

            if (description.Length == 0)
            {
                ReplyError("All commands are available for use!");
            }

            await SendAsync(description, $"All cooldowns for {user}");
        }

    }
}
