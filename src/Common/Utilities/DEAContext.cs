using DEA.Database.Models;
using DEA.Database.Repositories;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DEA.Common
{
    /// <summary>
    /// Custom context containing the guild user of the command user and the data information of the guild and the user.
    /// </summary>
    public class DEAContext : SocketCommandContext
    {
        public IGuildUser GUser { get; }
        public User DbUser { get; private set; }
        public Guild DbGuild { get; private set; }
        public Gang Gang { get; private set; }
        public string Prefix { get; private set; }
        public decimal Cash { get; set; }

        private readonly IServiceProvider _serviceProvider;
        private readonly UserRepository _userRepo;
        private readonly GuildRepository _guildRepo;
        private readonly GangRepository _gangRepo;

        public DEAContext(DiscordSocketClient client, SocketUserMessage msg, IServiceProvider serviceProvider) : base(client, msg)
        {
            _serviceProvider = serviceProvider;
            _userRepo = _serviceProvider.GetService<UserRepository>();
            _guildRepo = _serviceProvider.GetService<GuildRepository>();
            _gangRepo = _serviceProvider.GetService<GangRepository>();
            GUser = User as IGuildUser;
        }

        /// <summary>
        /// Getes the guild and user database information for the custom context object.
        /// </summary>
        public async Task InitializeAsync()
        {
            DbUser = await _userRepo.GetUserAsync(GUser);
            DbGuild = await _guildRepo.GetGuildAsync(Guild.Id);
            if (await _gangRepo.InGangAsync(GUser))
            {
                Gang = await _gangRepo.GetGangAsync(GUser);
            }
            Prefix = DbGuild.Prefix;
            Cash = DbUser.Cash;
        }
    }
}
