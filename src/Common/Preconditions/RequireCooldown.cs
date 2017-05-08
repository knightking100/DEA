using System;
using System.Threading.Tasks;
using Discord.Commands;
using DEA.Common.Extensions.DiscordExtensions;
using DEA.Common.Extensions;
using DEA.Common.Data;

namespace DEA.Common.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireCooldownAttribute : PreconditionAttribute
    {
        /// <summary>
        /// If the name of the command matches a command with a cooldown, it will check if the user's cooldown has finished. 
        /// If it has, the command will execute normally, if not, it will reply with the time remaining on the cooldown.
        /// </summary>
        public RequireCooldownAttribute() { }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider serviceProvider)
        {
            TimeSpan cooldown;
            DateTime lastUse;
            var deaContext = context as DEAContext;
            switch (command.Name.ToLower())
            {
                case "whore":
                    cooldown = Config.WHORE_COOLDOWN;
                    lastUse = deaContext.DbUser.Whore;
                    break;
                case "jump":
                    cooldown = Config.JUMP_COOLDOWN;
                    lastUse = deaContext.DbUser.Jump;
                    break;
                case "steal":
                    cooldown = Config.STEAL_COOLDOWN;
                    lastUse = deaContext.DbUser.Steal;
                    break;
                case "rob":
                    cooldown = Config.ROB_COOLDOWN;
                    lastUse = deaContext.DbUser.Rob;
                    break;
                case "withdraw":
                    cooldown = Config.WITHDRAW_COOLDOWN;
                    lastUse = deaContext.DbUser.Withdraw;
                    break;
                case "raid":
                    cooldown = Config.RAID_COOLDOWN;
                    lastUse = deaContext.Gang.Raid;
                    break;
                default:
                    return PreconditionResult.FromSuccess();
            }
            if (DateTime.UtcNow.Subtract(lastUse).TotalMilliseconds > cooldown.TotalMilliseconds)
            {
                return PreconditionResult.FromSuccess();
            }
            else
            {
                var user = command.Name.ToLower() == "raid" ? deaContext.Gang.Name : context.User.ToString();
                var timeSpan = cooldown.Subtract(DateTime.UtcNow.Subtract(lastUse));
                await deaContext.Channel.SendAsync($"Hours: {timeSpan.Hours}\nMinutes: {timeSpan.Minutes}\nSeconds: {timeSpan.Seconds}", $"{command.Name.UpperFirstChar()} cooldown for {user}", Config.ERROR_COLOR);
                return PreconditionResult.FromError(string.Empty);
            }
        }
    }
}