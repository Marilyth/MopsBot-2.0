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
        public Cacheable<IUserMessage, ulong> messageCache;
        public IUserMessage cachedMessage {get {return messageCache.GetOrDownloadAsync().Result;}}
        public IUserMessage message {get {return messageCache.DownloadAsync().Result;}}
        public IEmote emote;
        public SocketReaction reaction;
    }
    public class ReactionHandler{
        private DiscordSocketClient client;
        private IServiceProvider _provider;
        private Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>> messageFunctions;
        public static IEmote defaultEmote = new Emoji("DEFAULT");

        public async Task Install(IServiceProvider provider){
            _provider = provider;
            client = _provider.GetService<DiscordSocketClient>();
            
            client.ReactionAdded += Client_ReactionAdded;

            messageFunctions = new Dictionary<IUserMessage, Dictionary<IEmote, Func<ReactionHandlerContext, Task>>>();
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> messageCache, ISocketMessageChannel channel, SocketReaction reaction){
            if(reaction.UserId.Equals(client.CurrentUser.Id))
                return;
            
            IUserMessage message = await messageCache.DownloadAsync();
            ReactionHandlerContext context = new ReactionHandlerContext();
            context.channel = channel;
            context.messageCache = messageCache;
            context.emote = reaction.Emote;
            context.reaction = reaction;
            Task.Run(async () => {
            if(messageFunctions.Any(x => x.Key.Id.Equals(message.Id))){
                if(messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.ContainsKey(reaction.Emote))
                    await messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[reaction.Emote](context);
                else if(messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.ContainsKey(defaultEmote))
                    await messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[defaultEmote](context);
                await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }});
        }

        public async Task addHandler(IUserMessage message, Func<ReactionHandlerContext, Task> function, bool clear=false){
            await addHandler(message, defaultEmote, function, clear);
        }

        public async Task addHandler(IUserMessage message, IEmote emote, Func<ReactionHandlerContext, Task> function, bool clear=false){
            if(clear)
                await clearHandler(message);
            if(messageFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[emote]=function;
            else   
                messageFunctions.Add(message, new Dictionary<IEmote, Func<ReactionHandlerContext, Task>>{{emote, function}});
            // await populate(message);
            if(!emote.Equals(defaultEmote))
                await message.AddReactionAsync(emote);
        }

        public async Task addHandler(IUserMessage message, Dictionary<IEmote, Func<ReactionHandlerContext, Task>> functions, bool clear=false){
            if(clear)
                await clearHandler(message);
            if(messageFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                foreach(var pair in functions)
                    messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value[pair.Key]=pair.Value;
            else   
                messageFunctions.Add(message, functions);
            // await populate(message);
            foreach(var pair in functions){
                await message.AddReactionAsync(pair.Key);
            }
        }

        public async Task clearHandler(IUserMessage message){
            if(messageFunctions.Any(x => x.Key.Id.Equals(message.Id)))
                messageFunctions.Remove(messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Key);
            await message.RemoveAllReactionsAsync();
        }

        public async Task removeHandler(IUserMessage message, IEmote emote){
            if(messageFunctions.Any(x => x.Key.Id.Equals(message.Id))){
                messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.Remove(emote);
                if(!messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value.Any())
                    messageFunctions.Remove(message);
                await message.RemoveReactionAsync(emote, client.CurrentUser);
            }
        }

        public Dictionary<IEmote, Func<ReactionHandlerContext, Task>> getHandler(IUserMessage message){
            return messageFunctions.First(x => x.Key.Id.Equals(message.Id)).Value;
        }

        // public async Task populate(IUserMessage message){
        //     if(messageFunctions.Any(x => x.Key.Id.Equals(message.Id))){
        //         IEnumerable<IEmote> remove = message.Reactions.Where(x => messageFunctions.First(w => w.Key.Id.Equals(message.Id)).Value.Any(y => x.Key.Name == y.Key.Name)).Select(z=>z.Key);
        //         IEnumerable<IEmote> add = messageFunctions.First(w => w.Key.Id.Equals(message.Id)).Value.Where(x => message.Reactions.Any(y => x.Key.Name == y.Key.Name)).Select(z=>z.Key);

        //         foreach(var item in remove){
        //             await message.RemoveReactionAsync(item, client.CurrentUser);
        //         }

        //         foreach(var item in add){
        //             await message.AddReactionAsync(item);
        //         }
        //     }
        // }
    }
}