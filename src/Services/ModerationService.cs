using DEA.Common;
using DEA.Common.Extensions.DiscordExtensions;
using DEA.Database.Models;
using DEA.Database.Repositories;
using Discord;
using System.Linq;
using System.Threading.Tasks;

namespace DEA.Services
{
    /// <summary>
    /// All moderation utilies.
    /// </summary>
    public class ModerationService
    {
        private readonly GuildRepository _guildRepo;

        public ModerationService(GuildRepository guildRepo)
        {
            _guildRepo = guildRepo;
        }

        /// <summary>
        /// Checks whether a user is a moderator.
        /// </summary>
        /// <param name="context">The context to get the guild data information.</param>
        /// <param name="user">The user in question.</param>
        /// <returns>The permission level of the user.</returns>
        public int GetPermLevel(DEAContext context, IGuildUser user)
        {
            var permLevel = 0;

            if (context.DbGuild.ModRoles.ElementCount != 0)
            {
                foreach (var role in context.DbGuild.ModRoles.OrderBy(x => x.Value))
                {
                    if (user.Guild.GetRole(ulong.Parse(role.Name)) != null)
                    {
                        if (user.RoleIds.Any(x => x.ToString() == role.Name))
                        {
                            permLevel = role.Value.AsInt32;
                        }
                    }
                }
            }

            return user.GuildPermissions.Administrator && permLevel < 2 ? 2 : permLevel;
        }

        /// <summary>
        /// Checks whether a user is a moderator.
        /// </summary>
        /// <param name="context">The context to get the guild data information.</param>
        /// <param name="user">The user in question.</param>
        /// <returns>The permission level of the user.</returns>
        public int GetPermLevel(Guild dbGuild, IGuildUser user)
        {
            var permLevel = 0;

            if (dbGuild.ModRoles.ElementCount != 0)
            {
                foreach (var role in dbGuild.ModRoles.OrderBy(x => x.Value))
                {
                    if (user.Guild.GetRole(ulong.Parse(role.Name)) != null)
                    {
                        if (user.RoleIds.Any(x => x.ToString() == role.Name))
                        {
                            permLevel = role.Value.AsInt32;
                        }
                    }
                }
            }

            return user.GuildPermissions.Administrator && permLevel < 2 ? 2 : permLevel;
        }

        /// <summary>
        /// Informs a user of an action regarding them including the responsible moderator.
        /// </summary>
        /// <param name="moderator">The moderator in question.</param>
        /// <param name="action">The action.</param>
        /// <param name="subject">The user in question.</param>
        /// <param name="reason">The reason for the action.</param>
        public async Task InformSubjectAsync(IUser moderator, string action, IUser subject, string reason = null)
        {
            try
            {
                var channel = await subject.CreateDMChannelAsync();
                var message = $"{moderator} has attempted to {action.ToLower()} you.";
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    message = message.Remove(message.Length - 1) + $" for the following reason: \"{reason}\".";
                }

                await channel.SendAsync(message);
            }
            catch { }
        }

        /// <summary>
        /// If the moderation log channel exists, it will log all moderation commands.
        /// </summary>
        /// <param name="context">The context of the command use.</param>
        /// <param name="action">The action that was taken.</param>
        /// <param name="color">The color of the embed.</param>
        /// <param name="reason">The reason for the action.</param>
        /// <param name="subject">The user in question.</param>
        /// <param name="extra">An extra line for more information.</param>
        public async Task ModLogAsync(DEAContext context, string action, Color color, string reason, IUser subject = null, string extra = "")
        {
            var channel = context.Guild.GetTextChannel(context.DbGuild.ModLogChannelId);

            if (channel == null)
            {
                return;
            }

            EmbedFooterBuilder footer = new EmbedFooterBuilder()
            {
                IconUrl = "http://i.imgur.com/BQZJAqT.png",
                Text = $"Case #{context.DbGuild.CaseNumber}"
            };
            EmbedAuthorBuilder author = new EmbedAuthorBuilder()
            {
                IconUrl = context.User.GetAvatarUrl(),
                Name = $"{context.User.Username}#{context.User.Discriminator}"
            };

            string userText = string.Empty;
            if (subject != null)
            {
                userText = $"\n**User:** {subject} ({subject.Id})";
            }

            var description = $"**Action:** {action}{extra}{userText}";
            if (reason != null)
            {
                description += $"\n**Reason:** {reason}";
            }

            var builder = new EmbedBuilder()
            {
                Author = author,
                Color = color,
                Description = description,
                Footer = footer
            }.WithCurrentTimestamp();

            try
            {
                await channel.SendMessageAsync(string.Empty, embed: builder);
                await _guildRepo.ModifyAsync(context.DbGuild, x => x.CaseNumber++);
            }
            catch { }
        }
    }
}
