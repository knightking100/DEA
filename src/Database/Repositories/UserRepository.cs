using DEA.Common;
using DEA.Database.Models;
using DEA.Services.Handlers;
using Discord;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace DEA.Database.Repositories
{
    public class UserRepository : BaseRepository<User>
    {
        private readonly RankHandler _rankHandler;

        public UserRepository(IMongoCollection<User> users, RankHandler rankHandler) : base (users)
        {
            _rankHandler = rankHandler;
        }

        /// <summary>
        /// Gets a user by IGuildUser object.
        /// </summary>
        /// <param name="user">The user to get.</param>
        /// <returns>A task returning a user.</returns>
        public async Task<User> GetUserAsync(IGuildUser user)
        {
            var dbUser = await GetAsync(x => x.UserId == user.Id && x.GuildId == user.GuildId);

            if (dbUser == default(User))
            {
                var createdUser = new User(user.Id, user.GuildId);
                await InsertAsync(createdUser);
                return createdUser;
            }
            return dbUser;
        }

        /// <summary>
        /// Gets the user by user Id and guild Id.
        /// </summary>
        /// <param name="userId">The user Id.</param>
        /// <param name="guildId">The guild Id.</param>
        /// <returns>A task returning the user.</returns>
        public async Task<User> GetUserAsync(ulong userId, ulong guildId)
        {
            var dbUser = await GetAsync(x => x.UserId == userId && x.GuildId == guildId);

            if (dbUser == default(User))
            {
                var createdUser = new User(userId, guildId);
                await InsertAsync(createdUser);
                return createdUser;
            }
            return dbUser;
        }

        /// <summary>
        /// Finds and modifies a user document.
        /// </summary>
        /// <param name="user">The user to modify.</param>
        /// <param name="function">Modification of the user.</param>
        public Task ModifyUserAsync(IGuildUser user, Action<User> function)
        {
            return ModifyAsync(y => y.UserId == user.Id && y.GuildId == user.GuildId, function);
        }

        /// <summary>
        /// Finds and modifies a user document.
        /// </summary>
        /// <param name="userId">The Id of the user to modify.</param>
        /// <param name="guildId">The Id of the guild the user is in.</param>
        /// <param name="function">Modification of the user.</param>
        public Task ModifyUserAsync(ulong userId, ulong guildId, Action<User> function)
        {
            return ModifyAsync(y => y.UserId == userId && y.GuildId == guildId, function);
        }

        /// <summary>
        /// Modifies a user's cash.
        /// </summary>
        /// <param name="context">The context of the command use.</param>
        /// <param name="change">The +/- change on the user's cash.</param>
        public async Task EditCashAsync(DEAContext context, decimal change)
        {
            var newCash = Math.Round(context.Cash + change, 2);
            context.Cash = newCash;
            context.DbUser.Cash = newCash; 
            await UpdateAsync(context.DbUser);
            await _rankHandler.HandleAsync(context.Guild, context.User as IGuildUser, context.DbGuild, context.DbUser);
        }

        /// <summary>
        /// Modifies a user's cash.
        /// </summary>
        /// <param name="user">IGuildUser object of the user.</param>
        /// <param name="dbGuild">Guild document.</param>
        /// <param name="dbUser">User document.</param>
        /// <param name="change">The +/- change on the user's cash.</param>
        public async Task EditCashAsync(IGuildUser user, Guild dbGuild, User dbUser, decimal change)
        {
            dbUser.Cash = Math.Round(dbUser.Cash + change, 2);
            await UpdateAsync(dbUser);
            await _rankHandler.HandleAsync(user.Guild, user, dbGuild, dbUser);
        }

        /// <summary>
        /// Provides the user in question with money and an increased rate if the cooldown has finished.
        /// </summary>
        /// <param name="userRepo">The user repository object to modify the user's cash.</param>
        /// <param name="context">The context to get the user's data information.</param>
        /// <returns></returns>
        public async Task ApplyCash(IGuildUser user, User dbUser, Guild dbGuild)
        {
            if (DateTime.UtcNow.Subtract(dbUser.Message).TotalMilliseconds > dbUser.MessageCooldown)
            {
                await ModifyAsync(dbUser, x =>
                {
                    x.TemporaryMultiplier = dbUser.TemporaryMultiplier + dbGuild.TempMultiplierIncreaseRate;
                    x.Message = DateTime.UtcNow;
                    x.Cash += dbGuild.GlobalChattingMultiplier * dbUser.TemporaryMultiplier * dbUser.InvestmentMultiplier;
                });
                await _rankHandler.HandleAsync(user.Guild, user, dbGuild, dbUser);
            }
        }

    }
}