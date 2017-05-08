using DEA.Database.Repositories;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using DEA.Services.Handlers;
using Microsoft.Extensions.DependencyInjection;
using DEA.Common.Extensions.DiscordExtensions;
using DEA.Services.Static;
using System;

namespace DEA.Events
{
    /// <summary>
    /// An event that is run every time a user joins a guild.
    /// </summary>
    class UserJoined
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _client;
        private readonly UserRepository _userRepo;
        private readonly GuildRepository _guildRepo;
        private readonly MuteRepository _muteRepo;
        private readonly BlacklistRepository _blacklistRepo;
        private readonly RankHandler _rankHandler;

        public UserJoined(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _userRepo = _serviceProvider.GetService<UserRepository>();
            _guildRepo = serviceProvider.GetService<GuildRepository>();
            _muteRepo = serviceProvider.GetService<MuteRepository>();
            _blacklistRepo = serviceProvider.GetService<BlacklistRepository>();
            _rankHandler = serviceProvider.GetService<RankHandler>();
            _client = _serviceProvider.GetService<DiscordSocketClient>();
            _client.UserJoined += HandleUserJoined;
        }

        private Task HandleUserJoined(SocketGuildUser u)
        {
            return Task.Run(async () =>
            {
                Logger.Log(LogSeverity.Debug, $"Event", "User Joined");
                if ((await _blacklistRepo.AllAsync()).Any(x => x.UserId == u.Id))
                {
                    try
                    {
                        await u.Guild.AddBanAsync(u);
                    }
                    catch { }
                }

                var user = u as IGuildUser;
                var dbGuild = await _guildRepo.GetGuildAsync(user.Guild.Id);

                var mutedRole = user.Guild.GetRole((dbGuild.MutedRoleId));
                if (mutedRole != null && u.Guild.CurrentUser.GuildPermissions.ManageRoles &&
                mutedRole.Position < u.Guild.CurrentUser.Roles.OrderByDescending(x => x.Position).First().Position)
                {
                    await _rankHandler.HandleAsync(u.Guild, user, dbGuild, await _userRepo.GetUserAsync(user));
                    if (await _muteRepo.IsMutedAsync(user.Id, user.Guild.Id) && mutedRole != null && user != null)
                    {
                        await user.AddRoleAsync(mutedRole);
                    }
                }

                if (!string.IsNullOrWhiteSpace(dbGuild.WelcomeMessage))
                {
                    var channel = _client.GetChannel(dbGuild.WelcomeChannelId);
                    if (channel != null)
                    {
                        try
                        {
                            await (channel as ITextChannel).SendAsync($"{u}, " + dbGuild.WelcomeMessage);
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            var dmChannel = await u.CreateDMChannelAsync();
                            await dmChannel.SendAsync(dbGuild.WelcomeMessage);
                        }
                        catch { }
                    }
                }
            });
        }
    }
}
