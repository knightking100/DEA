using DEA.Common.Data;
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
    /// Periodically sends out trivia messages and awaits for the answers in guilds with auto trivia enabled.
    /// </summary>
    class AutoTrivia
    { 
        private readonly IServiceProvider _serviceProvider;
        private readonly GuildRepository _guildRepo;
        private readonly DiscordSocketClient _client;
        private readonly UserRepository _userRepo;
        private readonly GameService _gameService;
        private readonly InteractiveService _interactiveService;

        private readonly Timer _timer;

        public AutoTrivia(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _guildRepo = _serviceProvider.GetService<GuildRepository>();
            _userRepo = serviceProvider.GetService<UserRepository>();
            _client = serviceProvider.GetService<DiscordSocketClient>();
            _gameService = serviceProvider.GetService<GameService>();
            _interactiveService = serviceProvider.GetService<InteractiveService>();

            ObjectState StateObj = new ObjectState();

            TimerCallback TimerDelegate = new TimerCallback(Trivia);

            _timer = new Timer(TimerDelegate, StateObj, TimeSpan.FromMilliseconds(750), Config.AUTO_TRIVIA_COOLDOWN);

            StateObj.TimerReference = _timer;
        }

        private void Trivia(object stateObj)
        {
            Task.Run(async () =>
            {
                Logger.Log(LogSeverity.Debug, $"Timers", "Auto Trivia");
                foreach (var dbGuild in await _guildRepo.AllAsync())
                {
                    if (dbGuild.AutoTrivia)
                    {
                        var guild = await (_client as IDiscordClient).GetGuildAsync(dbGuild.GuildId);
                        if (guild != null)
                        {
                            var defaultChannel = await guild.GetDefaultChannelAsync();
                            try
                            {
                                await _gameService.TriviaAsync(defaultChannel, dbGuild);
                            }
                            catch { }
                        }
                    }
                }
            });
        }
    }
}