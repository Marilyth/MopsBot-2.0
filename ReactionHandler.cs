using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Diagnostics;

namespace MopsBot
{
    public class ReactionHandlerContext
    {
        public ISocketMessageChannel Channel;
        public Cacheable<IUserMessage, ulong> MessageCache;
        public IUserMessage Message { get { return MessageCache.GetOrDownloadAsync().Result; } }
        public IUserMessage DownloadMessage { get { return MessageCache.DownloadAsync().Result; } }
        public IEmote Emote;
        public SocketReaction Reaction;
    }
    public class ReactionHandler
    {
        private DiscordSocketClient client;
        private IServiceProvider _provider;
        private Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>> emojiAddedFunctions;
        private Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>> emojiRemovedFunctions;
        public static IEmote DefaultEmote = new Emoji("DEFAULT");

        /// <summary>
        /// Subscribes to the ReactionAdded event of the client 
        /// </summary>
        /// <param name="provider">The service provider to get the client from</param>
        public void Install(IServiceProvider provider)
        {
            _provider = provider;
            client = _provider.GetService<DiscordSocketClient>();

            client.ReactionAdded += Client_ReactionAdded;
            client.ReactionRemoved += Client_ReactionRemoved;

            emojiAddedFunctions = new Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>>();
            emojiRemovedFunctions = new Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>>();
        }

        /// <summary>
        /// Handles calling the reactions corresponding functions, if they exist.
        /// </summary>
        /// <param name="messageCache"></param>
        /// <param name="channel"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> messageCache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await HandleReaction(messageCache, channel, reaction, false);
        }

        private async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> messageCache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await HandleReaction(messageCache, channel, reaction, true);
        }

        public async Task HandleReaction(Cacheable<IUserMessage, ulong> messageCache, ISocketMessageChannel channel, SocketReaction reaction, bool wasRemoved)
        {
            Task.Run(() =>
            {
                try
                {
                    if (!reaction.UserId.Equals(client.CurrentUser.Id) && wasRemoved ? emojiRemovedFunctions.Any(x => x.Key.Id == messageCache.Id) : emojiAddedFunctions.Any(x => x.Key.Id == messageCache.Id))
                    {
                        if (reaction.UserId.Equals(client.CurrentUser.Id))
                            return;

                        IUserMessage message = messageCache.GetOrDownloadAsync().Result;
                        ReactionHandlerContext context = new ReactionHandlerContext();
                        context.Channel = channel;
                        context.MessageCache = messageCache;
                        context.Emote = reaction.Emote;
                        context.Reaction = reaction;

                        if (wasRemoved)
                        {
                            if (emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.ContainsKey(reaction.Emote))
                                emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[reaction.Emote](context);
                            else if (emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.ContainsKey(DefaultEmote))
                                emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[DefaultEmote](context);
                        }
                        else
                        {
                            if (emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.ContainsKey(reaction.Emote))
                                emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[reaction.Emote](context);
                            else if (emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.ContainsKey(DefaultEmote))
                                emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[DefaultEmote](context);
                        }
                    }
                }
                catch (Exception e)
                {
                    Program.Client_Log(new LogMessage(LogSeverity.Error, this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name, $"message {messageCache.Id} emote {reaction.Emote.Name} threw an error", e)).Wait();
                }
            });
        }

        /// <summary>
        /// Adds a function for the default emote for the specified message to the handler.
        /// </summary>
        /// <param name="message">The message to listen to</param>
        /// <param name="function">The function to call</param>
        /// <param name="clear"></param>
        /// <returns></returns>
        public async Task AddHandler(IUserMessage message, Func<ReactionHandlerContext, Task> function, bool clear = false)
        {
            await AddHandler(message, DefaultEmote, function, clear);
        }

        /// <summary>
        /// Adds a function for the specified emote, for the specified message to the handler.
        /// </summary>
        /// <param name="message">The message to listen to</param>
        /// <param name="emote">The emote to listen for</param>
        /// <param name="function">The function to execute</param>
        /// <param name="clear"></param>
        /// <returns></returns>
        public async Task AddHandler(IUserMessage message, IEmote emote, Func<ReactionHandlerContext, Task> function, bool onRemove = false)
        {
            if (onRemove)
            {
                if (emojiRemovedFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                    emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[emote] = function;
                else
                    emojiRemovedFunctions.Add(message, new Dictionary<IEmote, Func<ReactionHandlerContext, Task>> { { emote, function } });
            }
            else
            {
                if (emojiAddedFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                    emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[emote] = function;
                else
                    emojiAddedFunctions.Add(message, new Dictionary<IEmote, Func<ReactionHandlerContext, Task>> { { emote, function } });
            }

            if (!emote.Equals(DefaultEmote) && !message.Reactions.ContainsKey(emote) && !onRemove)
            {
                await message.AddReactionAsync(emote);
            }
        }

        public async Task AddHandlers(IUserMessage message, params Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>[] handlers)
        {
            foreach (var handler in handlers)
            {
                if (handler.Item3)
                {
                    if (emojiRemovedFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                        emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[handler.Item1] = handler.Item2;
                    else
                        emojiRemovedFunctions.Add(message, new Dictionary<IEmote, Func<ReactionHandlerContext, Task>> { { handler.Item1, handler.Item2 } });
                }
                else
                {
                    if (emojiAddedFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                        emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[handler.Item1] = handler.Item2;
                    else
                        emojiAddedFunctions.Add(message, new Dictionary<IEmote, Func<ReactionHandlerContext, Task>> { { handler.Item1, handler.Item2 } });
                }
            }
            
            await message.AddReactionsAsync(handlers.Select(x => x.Item1).ToArray());
        }

        /// <summary>
        /// Removes the message from the handler.
        /// 
        /// Also removes all reactions.
        /// </summary>
        /// <param name="message">The message to remove from the handler.</param>
        /// <returns></returns>
        public async Task ClearHandler(IUserMessage message)
        {
            try
            {
                if (emojiRemovedFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                    emojiRemovedFunctions.Remove(emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Key);

                if (emojiAddedFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                    emojiAddedFunctions.Remove(emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Key);

                await message.RemoveAllReactionsAsync();
            }
            catch (Exception e)
            {
                await Program.Client_Log(new LogMessage(LogSeverity.Error, this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name, $"Tried to delete message {message.Id} but it did not exist.", e));
            }
        }

        /// <summary>
        /// Removes an emote-function combination from a message.
        /// </summary>
        /// <param name="message">The message to remove from</param>
        /// <param name="emote">The emote to remove</param>
        /// <returns></returns>
        public async Task RemoveHandler(IUserMessage message, IEmote emote)
        {
            if (emojiAddedFunctions.Any(x => x.Key.Id.Equals(message.Id)))
            {
                emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.Remove(emote);
                emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.Remove(emote);

                if (!emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.Any())
                {
                    emojiRemovedFunctions.Remove(message);
                    emojiAddedFunctions.Remove(message);
                }

                await message.RemoveReactionAsync(emote, client.CurrentUser);
            }
        }

        /// <summary>
        /// Returns all handler entries for the given message.
        /// </summary>
        /// <param name="message">The message to get information from</param>
        /// <returns>A dictionary consisting of all emotes and their corresponding functions</returns>
        public Dictionary<IEmote, Func<ReactionHandlerContext, Task>> GetHandler(IUserMessage message, bool onRemove = false)
        {
            return onRemove ? emojiRemovedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value : emojiAddedFunctions.First(x => x.Key.Id.Equals(message.Id)).Value;
        }
    }
}
