using DEA.Common.Data;
using DEA.Database.Repositories;
using DEA.Services.Static;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DEA.Services.Timers
{
    /// <summary>
    /// Periodically unmutes users who's mute length has ran out.
    /// </summary>
    class AutoUnmute
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _client;
        private readonly GuildRepository _guildRepo;
        private readonly MuteRepository _muteRepo;

        private readonly Timer _timer;

        public AutoUnmute(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _client = _serviceProvider.GetService<DiscordSocketClient>();
            _guildRepo = _serviceProvider.GetService<GuildRepository>();
            _muteRepo = _serviceProvider.GetService<MuteRepository>();

            ObjectState StateObj = new ObjectState();

            TimerCallback TimerDelegate = new TimerCallback(Unmute);

            _timer = new Timer(TimerDelegate, StateObj, TimeSpan.FromMilliseconds(1000), Config.AUTO_UNMUTE_COOLDOWN);

            StateObj.TimerReference = _timer;
        }

        private void Unmute(object stateObj)
        {
            Task.Run(async () =>
            {
                Logger.Log(LogSeverity.Debug, $"Timers", "Auto Unmute");

                foreach (var mute in await _muteRepo.AllAsync())
                {
                    if (DateTime.UtcNow.Subtract(mute.MutedAt).TotalMilliseconds > mute.MuteLength)
                    {
                        var guild = await (_client as IDiscordClient).GetGuildAsync(mute.GuildId);
                        var user = await guild.GetUserAsync(mute.UserId);
                        if (guild != null && user != null)
                        {
                            var guildData = await _guildRepo.GetGuildAsync(guild.Id);
                            var mutedRole = guild.GetRole(guildData.MutedRoleId);
                            if (mutedRole != null && user.RoleIds.Any(x => x == mutedRole.Id))
                            {
                                var channel = await guild.GetTextChannelAsync(guildData.ModLogChannelId);
                                ChannelPermissions? perms = null;
                                var currentUser = await guild.GetCurrentUserAsync();
                                if (channel != null)
                                    perms = currentUser.GetPermissions(channel as SocketTextChannel);
                                if (channel != null && currentUser.GuildPermissions.EmbedLinks && perms.Value.SendMessages && perms.Value.EmbedLinks)
                                {
                                    await user.RemoveRoleAsync(mutedRole);
                                    var footer = new EmbedFooterBuilder()
                                    {
                                        IconUrl = "http://i.imgur.com/BQZJAqT.png",
                                        Text = $"Case #{guildData.CaseNumber}"
                                    };
                                    var embedBuilder = new EmbedBuilder()
                                    {
                                        Color = new Color(12, 255, 129),
                                        Description = $"**Action:** Automatic Unmute\n**User:** {user} ({mute.UserId})",
                                        Footer = footer
                                    }.WithCurrentTimestamp();
                                    await _guildRepo.ModifyAsync(guildData, x => x.CaseNumber++);
                                    await channel.SendMessageAsync(string.Empty, embed: embedBuilder);
                                }
                            }
                        }
                        await _muteRepo.RemoveMuteAsync(mute.UserId, mute.GuildId);
                    }
                }
            });
        }
    }
}
