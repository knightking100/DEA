using DEA.Services.Static;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;

namespace DEA.Events
{
    /// <summary>
    /// An event that is run when DEA is ready to access the Discord API Client.
    /// </summary>
    class Ready
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _client;

        public Ready(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _client = _serviceProvider.GetService<DiscordSocketClient>();
            _client.Ready += HandleReady;
        }

        private Task HandleReady()
        {
            return Task.Run(async () =>
            {
                Logger.Log(LogSeverity.Debug, $"Event", "Ready");
                await _client.SetGameAsync("USE $help");            
            });
        }
    }
}
