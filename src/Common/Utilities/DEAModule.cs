﻿using DEA.Common.Extensions.DiscordExtensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace DEA.Common
{
    /// <summary>
    /// Custom module base using all IMessageChannel extensions with an error reply method.
    /// </summary>
    public abstract class DEAModule : ModuleBase<DEAContext>
    {

        /// <summary>
        /// Replies to the context user, starting the message with their username, discriminator and a comma.
        /// </summary>
        /// <param name="description">The content of the embed.</param>
        /// <param name="title">The title of the embed.</param>
        /// <param name="color">The color of the embed.</param>
        /// <returns>Task returning the sent message.</returns>
        public Task<IUserMessage> ReplyAsync(string message, string title = null, Color color = default(Color))
        {
            return Context.Channel.ReplyAsync(Context.User, message, title, color);
        }

        /// <summary>
        /// Sends a embedded message.
        /// </summary>
        /// <param name="description">The content of the embed.</param>
        /// <param name="title">The title of the embed.</param>
        /// <param name="color">The color of the embed.</param>
        /// <returns>Task returning the sent message.</returns>
        public Task<IUserMessage> SendAsync(string description, string title = null, Color color = default(Color))
        {
            return Context.Channel.SendAsync(description, title, color);
        }

        /// <summary>
        /// Throws a DEAException which will get caught by the error handler.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public void ReplyError(string message)
        {
            throw new DEAException(message);
        }
    }
}
