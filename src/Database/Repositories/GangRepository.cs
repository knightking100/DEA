using DEA.Common;
using DEA.Common.Data;
using DEA.Database.Models;
using Discord;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DEA.Database.Repositories
{
    public class GangRepository : BaseRepository<Gang>
    {
        public GangRepository(IMongoCollection<Gang> gangs) : base(gangs) { }

        /// <summary>
        /// Modifies a gang.
        /// </summary>
        /// <param name="member">Member/leader in the gang.</param>
        /// <param name="function">Modification on the gang.</param>
        public Task ModifyGangAsync(IGuildUser member, Action<Gang> function)
        {
            return ModifyAsync(c => (c.LeaderId == member.Id || c.Members.Any(x => x == member.Id)) && c.GuildId == member.GuildId, function);
        }

        /// <summary>
        /// Modifies a gang.
        /// </summary>
        /// <param name="gangName">Gang name to find the gang.</param>
        /// <param name="guildId">Guild Id of the gang.</param>
        /// <param name="function">>Modification on the gang.</param>
        public Task ModifyGangAsync(string gangName, ulong guildId, Action<Gang> function)
        {
            return ModifyAsync(c => c.Name.ToLower() == gangName.ToLower() && c.GuildId == guildId, function);
        }

        /// <summary>
        /// Gets a gang by member/leader.
        /// </summary>
        /// <param name="user">Member/leader of the gang.</param>
        /// <returns>A task returning a gang.</returns>
        public async Task<Gang> GetGangAsync(IGuildUser user)
        {
            var gang = await GetAsync(c => (c.LeaderId == user.Id || c.Members.Any(x => x == user.Id)) && c.GuildId == user.GuildId);
            if (gang == default(Gang))
            {
                throw new DEAException("This user is not in a gang.");
            }

            return gang;
        }

        /// <summary>
        /// Gets a gang by gang name and Guild Id.
        /// </summary>
        /// <param name="gangName">Gang name to find the gang.</param>
        /// <param name="guildId">Guild Id of the gang.</param>
        /// <returns></returns>
        public async Task<Gang> GetGangAsync(string gangName, ulong guildId)
        {
            var gang = await GetAsync(c => c.Name.ToLower() == gangName.ToLower() && c.GuildId == guildId);
            if (gang == default(Gang))
            {
                throw new DEAException("This gang does not exist.");
            }

            return gang;
        }

        /// <summary>
        /// Creates a gang.
        /// </summary>
        /// <param name="context">Context of the command use.</param>
        /// <param name="name">Name of the gang.</param>
        /// <returns>A task returning the created gang.</returns>
        public async Task<Gang> CreateGangAsync(DEAContext context, string name)
        {
            if (await ExistsAsync(x => x.Name.ToLower() == name.ToLower() && x.GuildId == context.Guild.Id))
            {
                throw new DEAException($"There is already a gang by the name {name}.");
            }
            else if (name.Length > Config.GANG_NAME_CHAR_LIMIT)
            {
                throw new DEAException($"The length of a gang name may not be longer than {Config.GANG_NAME_CHAR_LIMIT} characters.");
            }

            var createdGang = new Gang(context.User.Id, context.Guild.Id, name);
            await InsertAsync(createdGang);
            return createdGang;
        }

        /// <summary>
        /// Destroys the gang by member/leader.
        /// </summary>
        /// <param name="user">Member/leader</param>
        public Task DestroyGangAsync(IGuildUser user)
        {
            return DeleteAsync(c => (c.LeaderId == user.Id || c.Members.Any(x => x == user.Id)) && c.GuildId == user.GuildId);
        }

        /// <summary>
        /// Checks whether a user is in a gang.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <returns>A boolean of whether the user is in a gang.</returns>
        public Task<bool> InGangAsync(IGuildUser user)
        {
            return ExistsAsync(c => (c.LeaderId == user.Id || c.Members.Any(x => x == user.Id)) && c.GuildId == user.GuildId);
        }
        
        /// <summary>
        /// Checks whether a user is a member of a specific gang.
        /// </summary>
        /// <param name="gang"></param>
        /// <param name="userId"></param>
        /// <returns>A boolean of whether the user is a member of the gang.</returns>
        public bool IsMemberOfAsync(Gang gang, ulong userId)
        {
            if (gang.LeaderId == userId || gang.Members.Any(x => x == userId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a member from a gang.
        /// </summary>
        /// <param name="gang">Gang to remove the user from.</param>
        /// <param name="memberId">The user to remove.</param>
        public Task RemoveMemberAsync(Gang gang, ulong memberId)
        {
            var builder = Builders<Gang>.Update;
            return Collection.UpdateOneAsync(c => c.Id == gang.Id, builder.Pull(x => x.Members, memberId));
        }

        /// <summary>
        /// Adds a member to a gang.
        /// </summary>
        /// <param name="gang">The gang to add the member to.</param>
        /// <param name="newMemberId">The member to add.</param>
        public Task AddMemberAsync(Gang gang, ulong newMemberId)
        {
            var builder = Builders<Gang>.Update;
            return Collection.UpdateOneAsync(c => c.Id == gang.Id, builder.Push(x => x.Members, newMemberId));
        }

    }
}