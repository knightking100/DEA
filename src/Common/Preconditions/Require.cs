using DEA.Common.Data;
using DEA.Common.Extensions;
using DEA.Common.Utilities;
using DEA.Database.Repositories;
using DEA.Services;
using Discord;
using Discord.Commands;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace DEA.Common.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireAttribute : PreconditionAttribute
    {
        private IServiceProvider _serviceProvider;
        
        private Credentials _credentials;
        private UserRepository _userRepo;
        private ModerationService _moderationService;
        private GuildRepository _guildRepo;
        private GangRepository _gangRepo;

        private Attributes[] _attributes;

        /// <summary>
        /// Requires the user to comply with all listed attributes before executing the command.
        /// </summary>
        /// <param name="attributes">Attribute precondition.</param>
        public RequireAttribute(params Attributes[] attributes)
        {
            _attributes = attributes;
        }
        
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _credentials = _serviceProvider.GetService<Credentials>();
            _userRepo = _serviceProvider.GetService<UserRepository>();
            _moderationService = serviceProvider.GetService<ModerationService>();
            _guildRepo = _serviceProvider.GetService<GuildRepository>();
            _gangRepo = _serviceProvider.GetService<GangRepository>();

            var guildUser = context.User as IGuildUser;
            var DbUser = await _userRepo.GetUserAsync(guildUser);
            var DbGuild = await _guildRepo.GetGuildAsync(context.Guild.Id);
            foreach (var attribute in _attributes)
            {
                switch (attribute)
                {
                    case Attributes.BotOwner:
                        if (!_credentials.OwnerIds.Any(x => x == context.User.Id))
                        {
                            return PreconditionResult.FromError("Only an owner of this bot may use this command.");
                        }

                        break;
                    case Attributes.ServerOwner:
                        if (context.Guild.OwnerId != guildUser.Id && DbGuild.ModRoles.ElementCount == 0)
                        {
                            return PreconditionResult.FromError("Only the owners of this server may use this command.");
                        }
                        else if (guildUser.Guild.OwnerId != context.User.Id && DbGuild.ModRoles != null && !guildUser.RoleIds.Any(x => DbGuild.ModRoles.Any(y => y.Name == x.ToString() && y.Value.AsInt32 >= 3)))
                        {
                            return PreconditionResult.FromError("Only the owners of this server may use this command.");
                        }

                        break;
                    case Attributes.Admin:
                        if (!guildUser.GuildPermissions.Administrator && DbGuild.ModRoles.ElementCount == 0)
                        {
                            return PreconditionResult.FromError("The administrator permission is required to use this command.");
                        }
                        else if (!guildUser.GuildPermissions.Administrator && DbGuild.ModRoles.ElementCount != 0 && !guildUser.RoleIds.Any(x => DbGuild.ModRoles.Any(y => y.Name == x.ToString() && y.Value.AsInt32 >= 2)))
                        {
                            return PreconditionResult.FromError("The administrator permission is required to use this command.");
                        }

                        break;
                    case Attributes.Moderator:
                        if (_moderationService.GetPermLevel(DbGuild, context.User as IGuildUser) == 0)
                        {
                            return PreconditionResult.FromError("Only a moderator may use this command.");
                        }
                        break;
                    case Attributes.Nsfw:
                        if (!DbGuild.Nsfw)
                        {
                            return PreconditionResult.FromError($"This command may not be used while NSFW is disabled. An administrator may enable with the " +
                                                                $"`{DbGuild.Prefix}ChangeNSFWSettings` command.");
                        }

                        var nsfwChannel = await context.Guild.GetChannelAsync(DbGuild.NsfwChannelId);
                        if (nsfwChannel != null && context.Channel.Id != DbGuild.NsfwChannelId)
                        {
                            return PreconditionResult.FromError($"You may only use this command in {(nsfwChannel as ITextChannel).Mention}.");
                        }

                        break;
                    case Attributes.InGang:
                        if (!await _gangRepo.InGangAsync(guildUser))
                        {
                            return PreconditionResult.FromError("You must be in a gang to use this command.");
                        }

                        break;
                    case Attributes.NoGang:
                        if (await _gangRepo.InGangAsync(guildUser))
                        {
                            return PreconditionResult.FromError("You may not use this command while in a gang.");
                        }

                        break;
                    case Attributes.GangLeader:
                        if ((await _gangRepo.GetGangAsync(guildUser)).LeaderId != context.User.Id)
                        {
                            return PreconditionResult.FromError("You must be the leader of a gang to use this command.");
                        }

                        break;
                    case Attributes.Jump:
                        if (DbUser.Cash < Config.JUMP_REQUIREMENT)
                        {
                            return PreconditionResult.FromError($"You do not have the permission to use this command.\nRequired cash: {Config.JUMP_REQUIREMENT.USD()}.");
                        }

                        break;
                    case Attributes.Steal:
                        if (DbUser.Cash < Config.STEAL_REQUIREMENT)
                        {
                            return PreconditionResult.FromError($"You do not have the permission to use this command.\nRequired cash: {Config.STEAL_REQUIREMENT.USD()}.");
                        }

                        break;
                    
                    case Attributes.Rob:
                        if (DbUser.Cash < Config.ROB_REQUIREMENT)
                        {
                            return PreconditionResult.FromError($"You do not have the permission to use this command.\nRequired cash: {Config.ROB_REQUIREMENT.USD()}.");
                        }

                        break;
                    case Attributes.Bully:
                        if (DbUser.Cash < Config.BULLY_REQUIREMENT)
                        {
                            return PreconditionResult.FromError($"You do not have the permission to use this command.\nRequired cash: {Config.BULLY_REQUIREMENT.USD()}.");
                        }

                        break;
                    case Attributes.FiftyX2:
                        if (DbUser.Cash < Config.FIFTYX2_REQUIREMENT)
                        {
                            return PreconditionResult.FromError($"You do not have the permission to use this command.\nRequired cash: {Config.FIFTYX2_REQUIREMENT.USD()}.");
                        }

                        break;
                    default:
                        return PreconditionResult.FromError($"ERROR: The {attribute} attribute is not being handled!");
                }
            }

            return PreconditionResult.FromSuccess();
        }
    }
}