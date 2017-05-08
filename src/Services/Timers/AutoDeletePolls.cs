using DEA.Common.Data;
using DEA.Common.Extensions;
using DEA.Database.Repositories;
using DEA.Services.Static;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DEA.Services.Timers
{
    /// <summary>
    /// Periodically delets all finished polls and informs the creator of the results.
    /// </summary>
    class AutoDeletePolls
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _client;
        private readonly PollRepository _pollRepo;

        private readonly Timer _timer;

        public AutoDeletePolls(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _pollRepo = _serviceProvider.GetService<PollRepository>();
            _client = _serviceProvider.GetService<DiscordSocketClient>();

            ObjectState StateObj = new ObjectState();

            TimerCallback TimerDelegate = new TimerCallback(DeletePolls);

            _timer = new Timer(TimerDelegate, StateObj, TimeSpan.FromMilliseconds(500), Config.AUTO_DELETE_POLLS_COOLDOWN);

            StateObj.TimerReference = _timer;
        }

        private void DeletePolls(object stateObj)
        {
            Task.Run(async () =>
            {
                Logger.Log(LogSeverity.Debug, $"Timers", "Auto Delete Polls");

                foreach (var poll in await _pollRepo.AllAsync())
                {
                    if (TimeSpan.FromMilliseconds(poll.Length).Subtract(DateTime.UtcNow.Subtract(poll.CreatedAt)).TotalMilliseconds < 0)
                    {
                        var description = string.Empty;
                        var votes = poll.Votes();

                        for (int j = 0; j < poll.Choices.Length; j++)
                        {
                            var choice = poll.Choices[j];
                            var percentage = (votes[choice] / (double)poll.VotesDocument.ElementCount);
                            if (double.IsNaN(percentage))
                            {
                                percentage = 0;
                            }

                            description += $"{j + 1}. {choice}: {votes[choice]} Votes ({percentage.ToString("P")})\n";
                        }

                        await poll.CreatorId.DMAsync(_client, description, $"Final Poll Results of \"{poll.Name}\" Poll");

                        await _pollRepo.DeleteAsync(y => y.Id == poll.Id);
                    }
                }
            });
        }
    }
}