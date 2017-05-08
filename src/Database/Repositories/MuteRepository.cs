using DEA.Database.Models;
using Discord;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace DEA.Database.Repositories
{
    public class MuteRepository : BaseRepository<Mute>
    {
        public MuteRepository(IMongoCollection<Mute> mutes) : base(mutes) { }

        /// <summary>
        /// Inserts a mute into the mute collection.
        /// </summary>
        /// <param name="user">The muted user.</param>
        /// <param name="muteLength">The length of the mute.</param>
        public Task InsertMuteAsync(IGuildUser user, TimeSpan muteLength)
        {
            return InsertAsync(new Mute(user.Id, user.GuildId, muteLength.TotalMilliseconds));
        }

        /// <summary>
        /// Checks whether a user is muted.
        /// </summary>
        /// <param name="userId">Id of the user.</param>
        /// <param name="guildId">Id of the guild.</param>
        /// <returns>A task returning a boolean whether the user is muted or not.</returns>
        public Task<bool> IsMutedAsync(ulong userId, ulong guildId)
        {
            return ExistsAsync(y => y.UserId == userId && y.GuildId == guildId);
        }

        /// <summary>
        /// Removes a mute from the mute collection.
        /// </summary>
        /// <param name="userId">Id of the muted user.</param>
        /// <param name="guildId">Id of the guild.</param>
        public Task RemoveMuteAsync(ulong userId, ulong guildId)
        {
            return DeleteAsync(y => y.UserId == userId && y.GuildId == guildId);
        }
    }
}