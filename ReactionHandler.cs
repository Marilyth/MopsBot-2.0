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
        private Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>> messageFunctions;
        public static IEmote DefaultEmote = new Emoji("DEFAULT");
        private Dictionary<ulong, int> stackLength;

        /// <summary>
        /// Subscribes to the ReactionAdded event of the client 
        /// </summary>
        /// <param name="provider">The service provider to get the client from</param>
        public void Install(IServiceProvider provider)
        {
            _provider = provider;
            client = _provider.GetService<DiscordSocketClient>();

            client.ReactionAdded += Client_ReactionAdded;

            messageFunctions = new Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>>();
            stackLength = new Dictionary<ulong, int>();
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
            if (!reaction.UserId.Equals(client.CurrentUser.Id) && messageFunctions.Any(x => x.Key.Id == messageCache.Id))
            {
                if (!stackLength.ContainsKey(reaction.Channel.Id))
                    stackLength[reaction.Channel.Id] = 0;

                stackLength[reaction.Channel.Id]++;

                await Task.Delay(2000 * (int)stackLength[reaction.Channel.Id]);

                Task.Run(() =>
                {
                    if (reaction.UserId.Equals(client.CurrentUser.Id))
                        return;

                    IUserMessage message = messageCache.GetOrDownloadAsync().Result;
                    ReactionHandlerContext context = new ReactionHandlerContext();
                    context.Channel = channel;
                    context.MessageCache = messageCache;
                    context.Emote = reaction.Emote;
                    context.Reaction = reaction;


                    if (messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.ContainsKey(reaction.Emote))
                        messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[reaction.Emote](context);
                    else if (messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.ContainsKey(DefaultEmote))
                        messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[DefaultEmote](context);
                    message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);

                    stackLength[context.Channel.Id]--;
                });
            }
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
        public async Task AddHandler(IUserMessage message, IEmote emote, Func<ReactionHandlerContext, Task> function, bool clear = false)
        {
            if (!stackLength.ContainsKey(message.Channel.Id))
                stackLength[message.Channel.Id] = 0;

            stackLength[message.Channel.Id]++;

            await Task.Delay(2000 * (int)stackLength[message.Channel.Id]);

            if (clear)
                await ClearHandler(message);
            if (messageFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[emote] = function;
            else
                messageFunctions.Add(message, new Dictionary<IEmote, Func<ReactionHandlerContext, Task>> { { emote, function } });
            // await populate(message);
            if (!emote.Equals(DefaultEmote)){
                await message.AddReactionAsync(emote);
            }
            
            stackLength[message.Channel.Id]--;
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
                if (messageFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                    messageFunctions.Remove(messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Key);
                await message.RemoveAllReactionsAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Tried to delete message {message.Id} but it did not exist.");
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
            if (messageFunctions.Any(x => x.Key.Id.Equals(message.Id)))
            {
                messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.Remove(emote);
                if (!messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.Any())
                    messageFunctions.Remove(message);
                await message.RemoveReactionAsync(emote, client.CurrentUser);
            }
        }

        /// <summary>
        /// Returns all handler entries for the given message.
        /// </summary>
        /// <param name="message">The message to get information from</param>
        /// <returns>A dictionary consisting of all emotes and their corresponding functions</returns>
        public Dictionary<IEmote, Func<ReactionHandlerContext, Task>> GetHandler(IUserMessage message)
        {
            return messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value;
        }
    }
}