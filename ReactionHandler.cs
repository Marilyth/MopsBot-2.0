using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace MopsBot{
    public class ReactionHandlerContext{
        public ISocketMessageChannel channel;
        public IUserMessage message;
        public IEmote emote;
    }
    public class ReactionHandler{
        private DiscordSocketClient client;
        private IServiceProvider _provider;
        private Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>> messageFunctions;
        public static IEmote defaultEmote = new Emoji("DEFAULT");

        public async Task Install(IServiceProvider provider){
            _provider = provider;
            client = _provider.GetService<DiscordSocketClient>();
            
            client.ReactionAdded +=Client_ReactionAdded;
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> messageCache, ISocketMessageChannel channel, SocketReaction reaction){
            if(!messageCache.HasValue)
                await messageCache.DownloadAsync();

            IUserMessage message = messageCache.Value;
            ReactionHandlerContext context = new ReactionHandlerContext();
            context.channel=channel;
            context.message=message;
            context.emote=reaction.Emote;

            if(messageFunctions.ContainsKey(message))
                if(messageFunctions[message].ContainsKey(reaction.Emote))
                    await messageFunctions[message][reaction.Emote](context);
                else if(messageFunctions[message].ContainsKey(defaultEmote))
                    await messageFunctions[message][defaultEmote](context);

            await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
        }

        public void addHandler(IUserMessage message, Func<ReactionHandlerContext, Task> function, bool clear=false){
            if(clear)
                clearHandler(message);
            if(messageFunctions.ContainsKey(message))
                messageFunctions[message][defaultEmote]=function;
            else   
                messageFunctions[message]=new Dictionary<IEmote, Func<ReactionHandlerContext, Task>>{{defaultEmote, function}};
            populate(message);
        }

        public void addHandler(IUserMessage message, Dictionary<IEmote, Func<ReactionHandlerContext, Task>> functions, bool clear=false){
            if(clear)
                clearHandler(message);
            if(messageFunctions.ContainsKey(message))
                foreach(var pair in functions)
                    messageFunctions[message][pair.Key]=pair.Value;
            else   
                messageFunctions[message]=functions;
            populate(message);
        }

        public void clearHandler(IUserMessage message){
            messageFunctions.Remove(message);
            populate(message);
        }

        public void removeHander(IUserMessage message, IEmote emote){
            if(messageFunctions.ContainsKey(message)){
                messageFunctions[message].Remove(emote);
                if(!messageFunctions[message].Any())
                    messageFunctions.Remove(message);
            }
            populate(message);
        }

        public Dictionary<IEmote, Func<ReactionHandlerContext, Task>> getHandler(IUserMessage message){
            return messageFunctions[message];
        }

        public async Task populate(IUserMessage message){
            if(messageFunctions.ContainsKey(message)){
                await message.RemoveAllReactionsAsync();
                foreach(var pair in messageFunctions[message].Where(x => x.Key!=defaultEmote)){
                    await message.AddReactionAsync(pair.Key);
                }
        }
    }
}