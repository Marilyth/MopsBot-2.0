using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MopsBot.Module.Preconditions;

namespace MopsBot.Module{
    [Hide]
    public class Testing : ModuleBase{

        [Command("test")]
        public async Task test()
        {
            var message = ReplyAsync("test").Result;
            Program.reactionHandler.addHandler(message, x => (x.cachedMessage.ModifyAsync(y=>y.Content = x.emote.ToString())));
        }
        [Command("test2")]
        public async Task test2()
        {
            var message = ReplyAsync("💧").Result;
            Program.reactionHandler.addHandler(message, new Emoji("🔥"), test2Funciton);
        }

        private async Task test2Funciton(ReactionHandlerContext context){
            switch (context.emote.Name){
                case "🔥":await context.cachedMessage.ModifyAsync(x=> x.Content = "🔥");
                            Program.reactionHandler.addHandler(context.cachedMessage, new Emoji("💧"), test2Funciton, true);
                            break;
                case "💧": await context.cachedMessage.ModifyAsync(x=> x.Content = "💧");
                            Program.reactionHandler.addHandler(context.cachedMessage, new Emoji("🔥"), test2Funciton, true);
                            break;
            }
        } 

        [Command("test3")]
        public async Task test3(){
            var message = ReplyAsync("a").Result;
            Program.reactionHandler.addHandler(message, new Emoji("🔁"), test3Funciton);
        }

        private async Task test3Funciton(ReactionHandlerContext context){
            switch( context.message.Content){
                case "a": await context.cachedMessage.ModifyAsync(x => x.Content = "b");
                            Program.reactionHandler.addHandler(context.cachedMessage, new Emoji("🔁"), test3Funciton);
                            break;
                case "b": await context.cachedMessage.ModifyAsync(x => x.Content= "a");
                            break;
            }
        }

        [Command("test4")]
        public async Task test4(){
            var message = ReplyAsync("🇦").Result;
            Program.reactionHandler.addHandler(message, new Dictionary<IEmote, Func<ReactionHandlerContext, Task>>
            {
                { new Emoji("🇦"), test4A},
                { new Emoji("🇧"), test4B}
            });
        }

        private  async Task test4A(ReactionHandlerContext context){
            if(!context.message.Content.Equals("🇦"))
                await context.message.ModifyAsync(x => x.Content = "🇦");
        }
        private async Task test4B(ReactionHandlerContext context){
            if(!context.message.Content.Equals("🇧"))
                await context.message.ModifyAsync(x => x.Content = "🇧");
        }

        [Command("test5")]
        public async Task test5(){
            Program.reactionHandler.addHandler(ReplyAsync("a").Result, new Dictionary<IEmote, Func<ReactionHandlerContext, Task>>
            {
                { new Emoji("⏯"), test5Function},
            });
        }

        private async Task test5Function(ReactionHandlerContext context){
            if(Program.reactionHandler.getHandler(context.cachedMessage).Any(x => x.Key.ToString().Equals(new Emoji("🔁").ToString()) ))
                Program.reactionHandler.removeHandler(context.cachedMessage, new Emoji("🔁"));
            else
                Program.reactionHandler.addHandler(context.cachedMessage, new Emoji("🔁"), test3Funciton);
        }
    }
}