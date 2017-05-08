using DEA.Database.Models;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace DEA.Database.Repositories
{
    public class BlacklistRepository : BaseRepository<Blacklist>
    {
        public BlacklistRepository(IMongoCollection<Blacklist> blacklists) : base(blacklists) { }

        /// <summary>
        /// Adds a guild to a blacklist document found via user Id.
        /// </summary>
        /// <param name="userId">User Id of the blacklist document.</param>
        /// <param name="newGuildId">Guild Id to add.</param>
        public Task AddGuildAsync(ulong userId, ulong newGuildId)
        {
            var builder = Builders<Blacklist>.Update;
            return Collection.UpdateOneAsync(c => c.UserId == userId, builder.Push(x => x.GuildIds, newGuildId));
        }
    }
}
