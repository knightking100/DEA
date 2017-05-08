using DEA.Database.Models;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace DEA.Database.Repositories
{
    public class GuildRepository : BaseRepository<Guild>
    {
        public GuildRepository(IMongoCollection<Guild> guilds) : base(guilds) { }

        /// <summary>
        /// Gets a guild by Guild Id.
        /// </summary>
        /// <param name="guildId">The Guild Id.</param>
        /// <returns>A task returning a guild.</returns>
        public async Task<Guild> GetGuildAsync(ulong guildId)
        {
            var dbGuild = await GetAsync(x => x.GuildId == guildId);
            if (dbGuild == default(Guild))
            {
                var createdGuild = new Guild(guildId);
                await InsertAsync(createdGuild);
                return createdGuild;
            }
            return dbGuild;
        }

    }
}