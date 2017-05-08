using DEA.Common;
using DEA.Common.Data;
using DEA.Database.Models;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DEA.Database.Repositories
{
    public class PollRepository : BaseRepository<Poll>
    {
        public PollRepository(IMongoCollection<Poll> polls) : base(polls) { }

        /// <summary>
        /// Gets a poll by index.
        /// </summary>
        /// <param name="index">Index of the poll.</param>
        /// <param name="guildId">Guild Id of where the poll was created.</param>
        /// <returns></returns>
        public async Task<Poll> GetPollAsync(int index, ulong guildId)
        {
            var polls = await AllAsync(y => y.GuildId == guildId);

            polls = polls.OrderBy(y => y.CreatedAt).ToList();

            try
            {
                return polls[index - 1];
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new DEAException("This poll index does not exist.");
            }
        }

        /// <summary>
        /// Creates a poll.
        /// </summary>
        /// <param name="context">Context of the command use.</param>
        /// <param name="name">Name of the poll.</param>
        /// <param name="choices">Choices in the poll.</param>
        /// <param name="length">Length of the poll before deletion.</param>
        /// <param name="elderOnly">Whether the poll can only be voted on by elder users.</param>
        /// <param name="modOnly">Whether the poll can only be voted on moderators.</param>
        /// <param name="createdByMod">Whether the poll was created by a moderator.</param>
        /// <returns>A task returning a poll.</returns>
        public async Task<Poll> CreatePollAsync(DEAContext context, string name, string[] choices, TimeSpan? length = null, bool elderOnly = false, bool modOnly = false, bool createdByMod = false)
        {
            if (await ExistsAsync(x => x.Name.ToLower() == name.ToLower() && x.GuildId == context.Guild.Id))
            {
                throw new DEAException($"There is already a poll by the name \"{name}\".");
            }
            else if (name.Length > Config.MAX_POLL_SIZE)
            {
                throw new DEAException($"The poll name may not be larger than {Config.MAX_POLL_SIZE} characters.");
            }
            else if (length.HasValue && length.Value.TotalMilliseconds > Config.MAX_POLL_LENGTH.TotalMilliseconds)
            {
                throw new DEAException($"The poll length may not be longer than one week.");
            }

            var createdPoll = new Poll(context.User.Id, context.Guild.Id, name, choices)
            {
                ElderOnly = elderOnly,
                ModOnly = modOnly,
                CreatedByMod = createdByMod,
            };

            if (length.HasValue)
            {
                createdPoll.Length = length.Value.TotalMilliseconds;
            }

            await InsertAsync(createdPoll);
            return createdPoll;
        }

        /// <summary>
        /// Removes a poll by index.
        /// </summary>
        /// <param name="index">Index of the poll.</param>
        /// <param name="guildId">Guild Id of where the poll was created.</param>
        public async Task RemovePollAsync(int index, ulong guildId)
        {
            var polls = (await AllAsync(y => y.GuildId == guildId)).OrderBy(y => y.CreatedAt).ToList();

            try
            {
                var poll = polls[index - 1];
                await DeleteAsync(y => y.Id == poll.Id);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new DEAException("This poll index does not exist.");
            }
        }

    }
}
