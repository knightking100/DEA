using DEA.Common;
using DEA.Common.Data;
using DEA.Database.Repositories;
using MongoDB.Driver;
using Discord.Commands;
using System.Threading.Tasks;
using DEA.Common.Preconditions;
using System.Collections.Generic;
using DEA.Common.Extensions.DiscordExtensions;
using System;
using System.Linq;
using DEA.Services;
using Discord;

namespace DEA.Modules
{
    public class Polls : DEAModule
    {
        private readonly ModerationService _moderationService;
        private readonly PollRepository _pollRepo;

        public Polls(ModerationService moderationService, PollRepository pollRepo)
        {
            _moderationService = moderationService;
            _pollRepo = pollRepo;
        }

        [Command("CreatePoll")]
        [Summary("Creates a poll.")]
        public async Task AddPoll([Summary("Are you white?")] string poll, [Summary("HELL YEA~No~Maybe")] string choices, double daysToLast = 1, bool elderOnly = false, bool modOnly = false)
        {
            var isMod = _moderationService.GetPermLevel(Context, Context.GUser) > 0;

            if (modOnly && !isMod)
            {
                ReplyError("Only moderators may create mod only polls.");
            }

            var choicesArray = choices.Split('~');

            if (choicesArray.Distinct().Count() != choicesArray.Length)
            {
                ReplyError("You may not have multiple choices that are identicle.");
            }

            await _pollRepo.CreatePollAsync(Context, poll, choicesArray, TimeSpan.FromDays(daysToLast), elderOnly, modOnly, isMod);

            await ReplyAsync($"You have successfully created poll #{await _pollRepo.Collection.CountAsync(y => y.GuildId == Context.Guild.Id)}.");
        }

        [Command("RemovePoll")]
        [Summary("Removes a poll.")]
        [Require(Attributes.Moderator)]
        public async Task RemovePoll(int index)
        {
            var poll = await _pollRepo.GetPollAsync(index, Context.Guild.Id);
            await _pollRepo.RemovePollAsync(index, Context.Guild.Id);

            await ReplyAsync($"You have successfully removed the \"{poll.Name}\" poll!");
        }

        [Command("Polls")]
        [Alias("Indexes", "Index")]
        [Summary("Sends you a list of all polls in progress.")]
        public async Task Indexes()
        {
            var polls = await (await _pollRepo.Collection.FindAsync(y => y.GuildId == Context.Guild.Id)).ToListAsync();

            polls = polls.OrderBy(x => x.CreatedAt).ToList();

            if (polls.Count == 0)
            {
                ReplyError("There are no polls in progress.");
            }

            List<string> elements = new List<string>();

            if (polls.Any(x => x.CreatedByMod))
            {
                elements.Add("Polls created by moderators:\n");
            }

            for (int i = 0; i < polls.Count; i++)
            {
                if (polls[i].CreatedByMod)
                {
                    elements.Add($"{i + 1}. {polls[i].Name}\n");
                }
            }

            if (polls.Any(x => !x.CreatedByMod))
            {
                elements.Add("User polls:\n");
            }

            for (int i = 0; i < polls.Count; i++)
            {
                if (!polls[i].CreatedByMod)
                {
                    elements.Add($"{i + 1}. {polls[i].Name}\n");
                }
            }

            var channel = await Context.User.CreateDMChannelAsync();
            await channel.SendCodeAsync(elements, "Poll Indexes");

            await ReplyAsync("You have been DMed with all polls in progress.");
        }

        [Command("Poll")]
        [Summary("View the information of any poll.")]
        public async Task PollInfo(int index)
        {
            var poll = await _pollRepo.GetPollAsync(index, Context.Guild.Id);
            string description = string.Empty;

            var votes = poll.Votes();
            for (int i = 0; i < poll.Choices.Length; i++)
            {
                var choice = poll.Choices[i];
                var percentage = (votes[choice] / (double)poll.VotesDocument.ElementCount);
                if (double.IsNaN(percentage))
                {
                    percentage = 0;
                }

                description += $"{i + 1}. **{choice}:** {votes[choice]} Vote(s) ({percentage.ToString("P")})\n";
            }

            var timeRemaining = TimeSpan.FromMilliseconds(poll.Length).Subtract(DateTime.UtcNow.Subtract(poll.CreatedAt));
            if (timeRemaining.Ticks > 0)
            {
                description += $"\nEnding in: Days: {timeRemaining.Days}, Hours: {timeRemaining.Hours}, Minutes: {timeRemaining.Minutes}, Seconds: {timeRemaining.Seconds}";
            }
            else
            {
                description += $"This poll will end very soon!";
            }

            if (poll.ModOnly)
            {
                description += "\n\n**Only moderators may vote on this poll.**";
            }
            else if (poll.ElderOnly)
            {
                description += $"\n\n**Only users that have been in this server for at least {Config.ELDER_TIME_REQUIRED.TotalHours} hours may vote on this poll.**";
            }

            var creator = await (Context.Guild as IGuild).GetUserAsync(poll.CreatorId);

            description += $"\n\nCreator: {creator}";

            await SendAsync(description, poll.Name);
        }

        [Command("Vote")]
        [Summary("Vote on any poll.")]
        public async Task Vote(int pollIndex, int choiceIndex)
        {
            var poll = await _pollRepo.GetPollAsync(pollIndex, Context.Guild.Id);

            if (poll.VotesDocument.Any(x => x.Name == Context.User.Id.ToString()))
            {
                ReplyError("You have already voted on this poll.");
            }
            else if (poll.ElderOnly && DateTime.UtcNow.Subtract((Context.GUser).JoinedAt.Value.UtcDateTime).TotalMilliseconds <
                Config.ELDER_TIME_REQUIRED.TotalMilliseconds)
            {
                ReplyError($"You must have been in this server for more than {Config.ELDER_TIME_REQUIRED.TotalDays} days to vote on this poll.");
            }
            else if (poll.ModOnly && _moderationService.GetPermLevel(Context, Context.GUser) == 0)
            {
                ReplyError("Only a moderator may vote on this poll.");
            }

            string choice = null;
            try
            {
                choice = poll.Choices[choiceIndex - 1];
            }
            catch (IndexOutOfRangeException)
            {
                ReplyError("This poll choice index does not exist.");
            }

            await _pollRepo.ModifyAsync(poll, x => x.VotesDocument.Add(Context.User.Id.ToString(), choice));

            await ReplyAsync($"You have successfully voted on the \"{poll.Name}\" poll.");
        }

    }
}
