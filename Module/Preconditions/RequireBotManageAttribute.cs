using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;

namespace MopsBot.Module.Preconditions{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireBotManageAttribute : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if(StaticBase.BotManager.Contains(context.User.Id)||context.User.Id.Equals((await Program.client.GetApplicationInfoAsync()).Owner.Id))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError("you need to develope the bot to use this command");
        }
    }
}