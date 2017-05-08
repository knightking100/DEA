using DEA.Database.Repositories;
using Discord;
using System;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using DEA.Services;
using DEA.Common;
using DEA.Common.Preconditions;
using DEA.Common.Extensions;
using DEA.Common.Data;

namespace DEA.Modules
{
    [Require(Attributes.Moderator)]
    public class Moderation : DEAModule
    {
        private readonly MuteRepository _muteRepo;
        private readonly ModerationService _moderationService;

        public Moderation(MuteRepository muteRepo, ModerationService moderationService)
        {
            _muteRepo = muteRepo;
            _moderationService = moderationService;
        }

        [Command("Ban")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Bans a user.")]
        public async Task Ban(IGuildUser userToBan, [Remainder] string reason = null)
        {
            if (_moderationService.GetPermLevel(Context, Context.GUser) <= _moderationService.GetPermLevel(Context, userToBan))
            {
                ReplyError("You cannot ban another mod with a permission level higher or equal to your own.");
            }

            await _moderationService.InformSubjectAsync(Context.User, "Ban", userToBan, reason);
            await Context.Guild.AddBanAsync(userToBan);

            await SendAsync($"{Context.User.Boldify()} has successfully banned {userToBan.Boldify()}.");

            await _moderationService.ModLogAsync(Context, "Ban", Config.ERROR_COLOR, reason, userToBan);
        }

        [Command("Unban")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Unban a user.")]
        public async Task Unban([Summary("Billy Steve#4821")] string username, [Remainder] string reason = null)
        {
            var guildBans = await Context.Guild.GetBansAsync();

            var match = guildBans.Where(x => x.User.ToString().ToLower().Contains(username.ToLower()));

            var count = match.Count();

            if (count >= 2)
            {
                var matches = string.Empty;
                foreach (var restBan in match)
                {
                    matches += $"{restBan.User}\n";
                }

                ReplyError($"There are multiple matches to your unban request:\n{matches}");
            }
            else if (count == 0)
            {
                ReplyError("You may not unban someone who isn't banned.");
            }

            var user = match.First().User;

            await Context.Guild.RemoveBanAsync(user);

            await SendAsync($"{Context.User.Boldify()} has successfully unbanned {user.Boldify()}.");

            await _moderationService.InformSubjectAsync(Context.User, "Unban", user, reason);
            await _moderationService.ModLogAsync(Context, "Unban", new Color(0, 255, 0), reason, user);
        }

        [Command("Kick")]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [Summary("Kicks a user.")]
        public async Task Kick(IGuildUser userToKick, [Remainder] string reason = null)
        {
            if (_moderationService.GetPermLevel(Context, userToKick) > 0)
            {
                ReplyError("You cannot kick another mod!");
            }

            await _moderationService.InformSubjectAsync(Context.User, "Kick", userToKick, reason);
            await userToKick.KickAsync();

            await SendAsync($"{Context.User.Boldify()} has successfully kicked {userToKick.Boldify()}.");

            await _moderationService.ModLogAsync(Context, "Kick", new Color(255, 114, 14), reason, userToKick);
        }

        [Command("Mute")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [Summary("Permanently mutes a user.")]
        public async Task Mute(IGuildUser userToMute, [Remainder] string reason = null)
        {
            var mutedRole = Context.Guild.GetRole(Context.DbGuild.MutedRoleId);

            if (mutedRole == null)
            {
                ReplyError($"You may not mute users if the muted role is not valid.\nPlease use the " +
                                 $"`{Context.Prefix}SetMutedRole` command to change that.");
            }
            else if (_moderationService.GetPermLevel(Context, userToMute) > 0)
            {
                ReplyError("You cannot mute another mod.");
            }

            await userToMute.AddRoleAsync(mutedRole);
            await _muteRepo.InsertMuteAsync(userToMute, TimeSpan.FromDays(365));

            await SendAsync($"{Context.User.Boldify()} has successfully muted {userToMute.Boldify()}.");

            await _moderationService.InformSubjectAsync(Context.User, "Mute", userToMute, reason);
            await _moderationService.ModLogAsync(Context, "Mute", new Color(255, 114, 14), reason, userToMute, null);
        }

        [Command("CustomMute")]
        [Alias("CMute")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [Summary("Temporarily mutes a user for x amount of hours.")]
        public async Task CustomMute([Summary("2")] double hours, IGuildUser userToMute, [Remainder] string reason = null)
        {
            if (hours > 168)
            {
                ReplyError("You may not mute a user for more than a week.");
            }
            else if (hours < 1)
            {
                ReplyError("You may not mute a user for less than 1 hour.");
            }

            string time = (hours == 1) ? "hour" : "hours";
            var mutedRole = Context.Guild.GetRole(Context.DbGuild.MutedRoleId);

            if (mutedRole == null)
            {
                ReplyError($"You may not mute users if the muted role is not valid.\nPlease use the " +
                           $"{Context.DbGuild.Prefix}SetMutedRole command to change that.");
            }
            else if (_moderationService.GetPermLevel(Context, userToMute) > 0)
            {
                ReplyError("You cannot mute another mod.");
            }

            await userToMute.AddRoleAsync(mutedRole);
            await _muteRepo.InsertMuteAsync(userToMute, TimeSpan.FromHours(hours));

            await SendAsync($"{Context.User.Boldify()} has successfully muted {userToMute.Boldify()} for {hours} {time}.");

            await _moderationService.InformSubjectAsync(Context.User, "Mute", userToMute, reason);
            await _moderationService.ModLogAsync(Context, "Mute", new Color(255, 114, 14), reason, userToMute, $"\n**Length:** {hours} {time}");
        }

        [Command("Unmute")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [Summary("Unmutes a muted user.")]
        public async Task Unmute(IGuildUser userToUnmute, [Remainder] string reason = null)
        {
            if (!userToUnmute.RoleIds.Any(x => x == Context.DbGuild.MutedRoleId))
            {
                ReplyError("You cannot unmute a user who isn't muted.");
            }

            await userToUnmute.RemoveRoleAsync(Context.Guild.GetRole(Context.DbGuild.MutedRoleId));
            await _muteRepo.RemoveMuteAsync(userToUnmute.Id, userToUnmute.GuildId);

            await SendAsync($"{Context.User.Boldify()} has successfully unmuted {userToUnmute.Boldify()}.");

            await _moderationService.InformSubjectAsync(Context.User, "Unmute", userToUnmute, reason);
            await _moderationService.ModLogAsync(Context, "Unmute", new Color(12, 255, 129), reason, userToUnmute);
        }

        [Command("Clear")]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Summary("Deletes x amount of messages.")]
        public async Task CleanAsync(int quantity = 25, [Remainder] string reason = null)
        {
            if (quantity < Config.MIN_CLEAR)
            {
                ReplyError($"You may not clear less than {Config.MIN_CLEAR} messages.");
            }
            else if (quantity > Config.MAX_CLEAR)
            {
                ReplyError($"You may not clear more than {Config.MAX_CLEAR} messages.");
            }
            else if (Context.Channel.Id == Context.DbGuild.ModLogChannelId)
            {
                ReplyError("For security reasons, you may not use this command in the mod log channel.");
            }

            var messages = await Context.Channel.GetMessagesAsync(quantity).Flatten();
            await Context.Channel.DeleteMessagesAsync(messages);

            var msg = await ReplyAsync($"Messages deleted: **{quantity}**.");

            await _moderationService.ModLogAsync(Context, "Clear", new Color(34, 59, 255), reason, null, $"\n**Quantity:** {quantity}");

            await Task.Delay(2500);
            await msg.DeleteAsync();
        }

        [Command("Chill")]
        [RequireBotPermission(GuildPermission.Administrator)]
        [Summary("Prevents users from talking in a specific channel for x amount of seconds.")]
        public async Task Chill(int seconds = 30, [Remainder] string reason = null)
        {
            if (seconds < Config.MIN_CHILL.TotalSeconds)
            {
                ReplyError($"You may not chill for less than {Config.MIN_CHILL.TotalSeconds} seconds.");
            }
            else if (seconds > Config.MAX_CHILL.TotalSeconds)
            {
                ReplyError($"You may not chill for more than {Config.MAX_CHILL.TotalSeconds} seconds.");
            }

            var channel = Context.Channel as SocketTextChannel;
            var nullablePermOverwrites = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole);

            var perms = nullablePermOverwrites ?? new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit);

            if (perms.SendMessages == PermValue.Deny)
            {
                ReplyError("This chat is already chilled.");
            }

            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions().Modify(perms.CreateInstantInvite, perms.ManageChannel, perms.AddReactions, perms.ReadMessages, PermValue.Deny));

            await ReplyAsync($"Chat just got cooled down. Won't heat up until at least {seconds} seconds have passed.");

            await Task.Delay(seconds * 1000);
            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions().Modify(perms.CreateInstantInvite, perms.ManageChannel, perms.AddReactions, perms.ReadMessages, perms.SendMessages));

            await _moderationService.ModLogAsync(Context, "Chill", new Color(34, 59, 255), reason, null, $"\n**Length:** {seconds} seconds");
        }

    }
}