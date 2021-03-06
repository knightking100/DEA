﻿using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using DEA.Database.Repositories;
using MongoDB.Driver;
using DEA.Services.Handlers;
using DEA.Common;
using DEA.Common.Preconditions;
using DEA.Common.Extensions;

namespace DEA.Modules
{
    [Require(Attributes.ServerOwner)]
    public class Owners : DEAModule
    {
        private readonly GuildRepository _guildRepo;
        private readonly GangRepository _gangRepo;
        private readonly UserRepository _userRepo;
        private readonly RankHandler _rankHandler;

        public Owners(GuildRepository guildRepo, UserRepository userRepo, GangRepository gangRepo, RankHandler rankHandler)
        {
            _guildRepo = guildRepo;
            _gangRepo = gangRepo;
            _userRepo = userRepo;
            _rankHandler = rankHandler;
        }

        [Command("ResetUser")]
        [Summary("Resets all data for a specific user.")]
        public async Task ResetUser([Remainder] IGuildUser user = null)
        {
            user = user ?? Context.GUser;

            await _userRepo.Collection.DeleteOneAsync(y => y.UserId == user.Id && y.GuildId == user.GuildId);
            await _rankHandler.HandleAsync(Context.Guild, user, Context.DbGuild, await _userRepo.GetUserAsync(user));

            await SendAsync($"Successfully reset {user.Boldify()}'s data.");
        }

        [Command("100k")]
        [Summary("Sets the user's balance to $100,000.00.")]
        public async Task HundredK([Remainder] IGuildUser user = null)
        {
            user = user ?? Context.GUser;

            var dbUser = user.Id == Context.User.Id ? Context.DbUser : await _userRepo.GetUserAsync(user);
            await _userRepo.ModifyAsync(dbUser, x => x.Cash = 100000);
            await _rankHandler.HandleAsync(Context.Guild, user, Context.DbGuild, await _userRepo.GetUserAsync(user));

            await SendAsync($"Successfully set {user.Boldify()}'s balance to $100,000.00.");
        }

        [Command("Add")]
        [Summary("Add cash into a user's balance.")]
        public async Task Add(decimal money, [Remainder] IGuildUser user)
        {
            if (money < 0)
            {
                ReplyError("You may not add negative money to a user's balance.");
            }

            var dbUser = user.Id == Context.User.Id ? Context.DbUser : await _userRepo.GetUserAsync(user);
            await _userRepo.EditCashAsync(user, Context.DbGuild, dbUser, money);

            await SendAsync($"Successfully added {money.USD()} to {user.Boldify()}'s balance.");
        }

        [Command("AddTo")]
        [Summary("Add cash to every users balance in a specific role.")]
        public async Task AddTo(decimal money, [Remainder] IRole role)
        {
            if (money < 0)
            {
                ReplyError("You may not add negative money to these users's balances.");
            }

            await ReplyAsync("The addition of cash has commenced...");
            foreach (var user in (await (Context.Guild as IGuild).GetUsersAsync()).Where(x => x.RoleIds.Any(y => y == role.Id)))
            {
                await _userRepo.EditCashAsync(user, Context.DbGuild, await _userRepo.GetUserAsync(user), money);
            }

            await SendAsync($"Successfully added {money.USD()} to the balance of every user in the {role.Mention} role.");
        }

        [Command("Remove")]
        [Summary("Remove cash from a user's balance.")]
        public async Task Remove(decimal money, [Remainder] IGuildUser user)
        {
            if (money < 0)
            {
                ReplyError("You may not remove a negative amount of money from a user's balance.");
            }

            var dbUser = user.Id == Context.User.Id ? Context.DbUser : await _userRepo.GetUserAsync(user);
            await _userRepo.EditCashAsync(user, Context.DbGuild, dbUser, -money);

            await SendAsync($"Successfully removed {money.USD()} from {user.Boldify()}'s balance.");
        }

        [Command("RemoveFrom")]
        [Summary("Remove cash to every users balance in a specific role.")]
        public async Task Remove(decimal money, [Remainder] IRole role)
        {
            if (money < 0)
            {
                ReplyError("You may not remove negative money from these users's balances.");
            }

            await ReplyAsync("The cash removal has commenced...");
            foreach (var user in (await (Context.Guild as IGuild).GetUsersAsync()).Where(x => x.RoleIds.Any(y => y == role.Id)))
            {
                await _userRepo.EditCashAsync(user, Context.DbGuild, await _userRepo.GetUserAsync(user), -money);
            }

            await SendAsync($"Successfully removed {money.USD()} from the balance of every user in the {role.Mention} role.");
        }

        [Command("Reset")]
        [Summary("Resets all user data for the entire server or a specific role.")]
        public async Task Remove([Remainder] IRole role = null)
        {
            if (role == null)
            {
                await _userRepo.Collection.DeleteManyAsync(x => x.GuildId == Context.Guild.Id);
                await _gangRepo.Collection.DeleteManyAsync(y => y.GuildId == Context.Guild.Id);

                await ReplyAsync("Successfully reset all data in your server!");
            }
            else
            {
                foreach (var user in (await (Context.Guild as IGuild).GetUsersAsync()).Where(x => x.RoleIds.Any(y => y == role.Id)))
                {
                    _userRepo.Collection.DeleteOne(y => y.UserId == user.Id && y.GuildId == user.Guild.Id);
                }

                await ReplyAsync($"Successfully reset all users with the {role.Mention} role!");
            }
        }

        [Command("AddModRole")]
        [Summary("Adds a moderator role.")]
        public async Task AddModRole(IRole modRole, int permissionLevel = 1)
        {
            if (permissionLevel < 1 || permissionLevel > 3)
            {
                ReplyError("Permission levels:\nModeration: 1\nAdministration: 2\nServer Owner: 3");
            }

            if (Context.DbGuild.ModRoles.ElementCount == 0)
            {
                await _guildRepo.ModifyAsync(Context.DbGuild, x => x.ModRoles.Add(modRole.Id.ToString(), permissionLevel));
            }
            else
            {
                if (Context.DbGuild.ModRoles.Any(x => x.Name == modRole.Id.ToString()))
                {
                    ReplyError("You have already set this mod role.");
                }

                await _guildRepo.ModifyAsync(Context.DbGuild, x => x.ModRoles.Add(modRole.Id.ToString(), permissionLevel));
            }

            await ReplyAsync($"You have successfully added {modRole.Mention} as a moderation role with a permission level of {permissionLevel}.");
        }

        [Command("RemoveModRole")]
        [Summary("Removes a moderator role.")]
        public async Task RemoveModRole([Remainder] IRole modRole)
        {
            if (Context.DbGuild.ModRoles.ElementCount == 0)
            {
                ReplyError("There are no moderator roles yet!");
            }
            else if (!Context.DbGuild.ModRoles.Any(x => x.Name == modRole.Id.ToString()))
            {
                ReplyError("This role is not a moderator role!");
            }

            await _guildRepo.ModifyAsync(Context.DbGuild, x => x.ModRoles.Remove(modRole.Id.ToString()));

            await ReplyAsync($"You have successfully removed the {modRole.Mention} moderator role.");
        }

        [Command("ModifyModRole")]
        [Summary("Modfies a moderator role.")]
        public async Task ModifyRank(IRole modRole, int permissionLevel)
        {
            if (Context.DbGuild.ModRoles.ElementCount == 0)
            {
                ReplyError("There are no moderator roles yet!");
            }
            else if (!Context.DbGuild.ModRoles.Any(x => x.Name == modRole.Id.ToString()))
            {
                ReplyError("This role is not a moderator role!");
            }
            else if (Context.DbGuild.ModRoles.First(x => x.Name == modRole.Id.ToString()).Value == permissionLevel)
            {
                ReplyError($"This mod role already has a permission level of {permissionLevel}");
            }

            await _guildRepo.ModifyAsync(Context.DbGuild, x => x.ModRoles[Context.DbGuild.ModRoles.IndexOfName(modRole.Id.ToString())] = permissionLevel);

            await ReplyAsync($"You have successfully set the permission level of the {modRole.Mention} moderator role to {permissionLevel}.");
        }

        [Command("SetGlobalMultiplier")]
        [Summary("Sets the global chatting multiplier.")]
        public async Task SetGlobalMultiplier(decimal globalMultiplier){
            if (globalMultiplier < 0)
            {
                ReplyError("The global multiplier may not be negative.");
            }

            await _guildRepo.ModifyAsync(Context.DbGuild, x => x.GlobalChattingMultiplier = globalMultiplier);

            await ReplyAsync($"You have successfully set the global chatting multiplier to {globalMultiplier.ToString("N2")}.");
        }
        
        [Command("SetRate")]
        [Summary("Sets the global temporary multiplier increase rate.")]
        public async Task SetMultiplierIncrease(decimal interestRate){
            if (interestRate < 0)
            {
                ReplyError("The temporary multiplier increase rate may not be negative.");
            }

            await _guildRepo.ModifyAsync(Context.DbGuild, x => x.TempMultiplierIncreaseRate = interestRate);

            await ReplyAsync($"You have successfully set the global temporary multiplier increase rate to {interestRate.ToString("N2")}.");
        }

    }
}
