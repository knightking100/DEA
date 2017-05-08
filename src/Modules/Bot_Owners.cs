using DEA.Common;
using DEA.Common.Extensions.DiscordExtensions;
using DEA.Common.Preconditions;
using DEA.Database.Models;
using DEA.Database.Repositories;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace DEA.Modules
{
    [Name("Bot Owners")]
    [Require(Attributes.BotOwner)]
    public class Bot_Owners : DEAModule
    {
        private readonly GuildRepository _guildRepo;
        private readonly BlacklistRepository _blacklistRepo;

        public Bot_Owners(GuildRepository guildRepo, BlacklistRepository blacklistRepo)
        {
            _guildRepo = guildRepo;
            _blacklistRepo = blacklistRepo;
        }

        [Command("SetGame")]
        [Summary("Sets the game of DEA.")]
        public async Task SetGame([Summary("boss froth")] [Remainder] string game)
        {
            await Context.Client.SetGameAsync(game);
            await ReplyAsync($"Successfully set the game to {game}.");
        }

        [Command("LeaveGuild")]
        [Summary("Leaves any guild by guild ID.")]
        public async Task LeaveGuild(ulong guildId)
        {
            var guild = await (Context.Client as IDiscordClient).GetGuildAsync(guildId);
            if (guild != null)
            {
                await guild.LeaveAsync();
                await ReplyAsync("DEA has successfully left this guild.");
            }
            else
            {
                await ReplyAsync("DEA is not in this guild.");
            }
        }

        [Command("InformOwners")]
        [Summary("Sends a message to all server owners.")]
        public async Task InformOwners([Remainder] string message)
        {
            await ReplyAsync("The inform owners process has started...");
            foreach (var guild in Context.Client.Guilds)
            {
                try
                {
                    var channel = await guild.Owner.CreateDMChannelAsync();

                    await channel.SendAsync(message);
                }
                catch { }

                await Task.Delay(1000);
            }
            await ReplyAsync("All owners have been informed.");
        }

        [Command("SendGlobalUpdate")]
        [Summary("Sends a global update message into all DEA Update channels.")]
        public async Task SendGlobalUpdate([Remainder] string updateMessage)
        {
            await ReplyAsync("The global update message process has started...");
            var dbGuilds = await _guildRepo.AllAsync();

            foreach (var guild in Context.Client.Guilds)
            {
                if (dbGuilds.Exists(x => x.GuildId == guild.Id))
                {
                    var dbGuild = dbGuilds.Find(x => x.GuildId == guild.Id);
                    try
                    {
                        var channel = guild.GetChannel(dbGuild.UpdateChannelId);
                        await (channel as ITextChannel).SendAsync(updateMessage);
                    }
                    catch { }
                }
            }
            await ReplyAsync("All global update messages have been sent.");
        }

        [Command("Blacklist")]
        [Summary("Blacklist a user from DEA entirely to the fullest extent.")]
        public async Task Blacklist(ulong userId)
        {
            var username = string.Empty;
            var avatarUrl = string.Empty;

            try
            {
                var user = await (Context.Client as IDiscordClient).GetUserAsync(userId);

                username = user.Username;
                avatarUrl = user.GetAvatarUrl();
            }
            catch { }

            var blacklist = new Blacklist(userId, username, avatarUrl);
            await _blacklistRepo.InsertAsync(blacklist);

            foreach (var guild in Context.Client.Guilds)
            {
                if (guild.OwnerId == userId)
                {
                    await _blacklistRepo.AddGuildAsync(userId, guild.Id);
                    await guild.LeaveAsync();
                }
            }

            await ReplyAsync($"You have successfully blacklisted the following ID: {userId}.");
        }

    }
}
