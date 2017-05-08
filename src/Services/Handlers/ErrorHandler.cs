using DEA.Common;
using DEA.Common.Data;
using DEA.Common.Extensions;
using DEA.Common.Extensions.DiscordExtensions;
using DEA.Services.Static;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DEA.Services.Handlers
{
    /// <summary>
    /// Handles all errors.
    /// </summary>
    class ErrorHandler
    {
        private readonly CommandService _commandService;

        public ErrorHandler(CommandService commandService)
        {
            _commandService = commandService;
            _commandService.Log += HandleLog;
        }

        public Task HandleLog(LogMessage logMessage)
        {
            return Task.Run(async () =>
            {
                if (logMessage.Exception is CommandException cmdEx)
                {
                    if (cmdEx.InnerException is RateLimitedException)
                    {
                        return;
                    }
                    else if (cmdEx.InnerException is DEAException)
                    {
                        await cmdEx.Context.Channel.ReplyAsync(cmdEx.Context.User, cmdEx.InnerException.Message, null, Config.ERROR_COLOR);
                    }
                    else if (cmdEx.InnerException is HttpException httpEx)
                    {
                        var message = string.Empty;
                        switch (httpEx.DiscordCode)
                        {
                            case null:
                                switch (httpEx.HttpCode)
                                {
                                    case HttpStatusCode.BadRequest:
                                        message = "There seems to have been a bad request. Please report this issue with context at: " +
                                        "https://github.com/RealBlazeIt/DEA/issues.";
                                        break;
                                    case HttpStatusCode.BadGateway:
                                        message = "Something went wrong with the gateway connection. Try again in a bit.";
                                        break;
                                    case HttpStatusCode.Forbidden:
                                        message = "DEA does not have permission to do that. This issue may be fixed by moving the DEA role " +
                                        "to the top of the roles list, and giving DEA the \"Administrator\" server permission.";
                                        break;
                                    case HttpStatusCode.InternalServerError:
                                        message = "Looks like Discord fucked up. An interal server error has occured on Discord's part which is " +
                                        "entirely unrelated with DEA. Sorry, nothing I can do.";
                                        break;
                                    default:
                                        message = "Something went wrong. Please try again later. If this issue persists, please report it with " +
                                        "context at: https://github.com/RealBlazeIt/DEA/issues.";
                                        break;
                                }
                                break;
                            case 50013:
                                message = "DEA does not have permission to do that. This issue may be fixed by moving the DEA role " +
                                "to the top of the roles list, and giving DEA the \"Administrator\" server permission.";
                                break;
                            case 50007:
                                message = "DEA does not have permission to send messages to this user.";
                                break;
                            default:
                                message = httpEx.Message.Remove(0, 39) + ".";
                                break;
                        }
                        await cmdEx.Context.Channel.ReplyAsync(cmdEx.Context.User, message, null, Config.ERROR_COLOR);
                    }
                    else
                    {
                        var message = cmdEx.InnerException.Message;
                        if (cmdEx.InnerException.InnerException != null)
                        {
                            message += $"\n**Inner Exception:** {cmdEx.InnerException.InnerException.Message}";
                        }

                        await cmdEx.Context.Channel.ReplyAsync(cmdEx.Context.User, message, null, Config.ERROR_COLOR);

                        if ((await cmdEx.Context.Guild.GetCurrentUserAsync() as IGuildUser).GetPermissions(cmdEx.Context.Channel as SocketTextChannel).AttachFiles)
                        {
                            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(cmdEx.ToString() ?? string.Empty)))
                            {
                                await cmdEx.Context.Channel.SendFileAsync(ms, "Stack_Trace.txt");
                            }
                        }
                    }
                }
                else if (logMessage.Exception != null)
                {
                    Logger.Log(LogSeverity.Error, logMessage.Exception.Source, logMessage.Exception.StackTrace);
                }
            });
        }

        public Task HandleCommandFailureAsync(DEAContext context, IResult result, int argPos)
        {
            var args = context.Message.Content.Split(' ');
            var commandName = args.First().StartsWith(context.DbGuild.Prefix) ? args.First().Remove(0, context.DbGuild.Prefix.Length) : args[1];
            var message = string.Empty;

            switch (result.Error)
            {
                case CommandError.Exception:
                    return Task.CompletedTask; // Exceptions are handled by the log event from the command service.
                case CommandError.UnknownCommand:
                    foreach (var command in _commandService.Commands)
                    {
                        foreach (var alias in command.Aliases)
                        {
                            if (alias.Length < 5)
                            {
                                if (LevenshteinDistance.Compute(commandName, alias) == 1)
                                {
                                    message = $"Did you mean `{context.DbGuild.Prefix}{alias.UpperFirstChar()}`?";
                                }
                            }
                            else if (alias.Length < 10)
                            {
                                if (LevenshteinDistance.Compute(commandName, alias) <= 2)
                                {
                                    message = $"Did you mean `{context.DbGuild.Prefix}{alias.UpperFirstChar()}`?";
                                }
                            }
                            else
                            {
                                if (LevenshteinDistance.Compute(commandName, alias) <= 3)
                                {
                                    message = $"Did you mean `{context.DbGuild.Prefix}{alias.UpperFirstChar()}`?";
                                }
                            }
                        }
                    }
                    break;
                case CommandError.BadArgCount:
                    var cmd = _commandService.Search(context, argPos).Commands.First().Command;
                    var cmdNameUpperFirst = commandName.UpperFirstChar();
                    var example = cmd.Parameters.Count == 0 ? string.Empty : $"**Example:** `{context.DbGuild.Prefix}{cmdNameUpperFirst}{cmd.GetExample()}`";

                    message = $"You are incorrectly using this command. \n\n**Usage:** `{context.DbGuild.Prefix}{cmdNameUpperFirst}{cmd.GetUsage()}`\n\n" + example;   
                    break;
                case CommandError.ParseFailed:
                    message = $"Invalid number.";
                    break;
                case CommandError.UnmetPrecondition:
                    if (result.ErrorReason.StartsWith("Command requires guild permission "))
                    {
                        var permission = result.ErrorReason.Replace("Command requires guild permission ", string.Empty);
                        permission = Regex.Replace(permission, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled).Trim();
                        message = $"DEA requires the server pemission \"{permission}\" in order to be able to execute this command.";
                    }
                    else
                    {
                        message = result.ErrorReason;
                    }

                    break;
                default:
                    message = result.ErrorReason;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                return context.Channel.ReplyAsync(context.User, message, null, Config.ERROR_COLOR);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

    }
}