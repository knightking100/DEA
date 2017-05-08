using DEA.Database.Models;
using DEA.Database.Repositories;
using DEA.Services.Static;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DEA.Events
{
    /// <summary>
    /// An event that is run every time a guild gets updated.
    /// </summary>
    class GuildUpdated
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _client;
        private readonly BlacklistRepository _blaclistRepo;

        public GuildUpdated(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _client = _serviceProvider.GetService<DiscordSocketClient>();
            _blaclistRepo = _serviceProvider.GetService<BlacklistRepository>();
            _client.GuildUpdated += HandleGuildUpdated;
        }

        private Task HandleGuildUpdated(SocketGuild guildBefore, SocketGuild guildAfter)
        {
            return Task.Run(async () =>
            {
                Logger.Log(LogSeverity.Debug, $"Event", "Guild Updated");
                var blacklists = await _blaclistRepo.AllAsync();

                var isBlacklistedOwner = blacklists.Any(x => x.UserId == guildAfter.OwnerId);

                var isBlacklistedGuild = blacklists.Any(x => x.GuildIds.Any(y => y == guildAfter.Id));

                if (isBlacklistedOwner && !isBlacklistedGuild)
                {
                    await _blaclistRepo.AddGuildAsync(guildAfter.OwnerId, guildAfter.Id);
                    await guildAfter.LeaveAsync();
                }
                else if (isBlacklistedGuild && !isBlacklistedOwner)
                {
                    await _blaclistRepo.InsertAsync(new Blacklist(guildAfter.OwnerId, guildAfter.Owner.Username, guildAfter.Owner.GetAvatarUrl()));
                    await guildAfter.LeaveAsync();
                }
                else if (isBlacklistedOwner && isBlacklistedGuild)
                {
                    await guildAfter.LeaveAsync();
                }
            });
        }
    }
}
