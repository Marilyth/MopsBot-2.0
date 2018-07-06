using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace MopsBot.Module.Preconditions{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireBotManageAttribute : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if(new List<string>(Program.Config["BotManager"].Split(":")).Contains(context.User.Id.ToString())||context.User.Id.Equals((await Program.Client.GetApplicationInfoAsync()).Owner.Id))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError("");
        }
    }
}
