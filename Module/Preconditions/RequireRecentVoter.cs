using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace MopsBot.Module.Preconditions{
    /// <summary> Sets that a user must have voted for the bot to use the command </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireVoter : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            bool isVoter = (await StaticBase.DiscordBotList.GetVotersAsync()).Any(x => x.Id == context.User.Id);

            if(isVoter)
                return PreconditionResult.FromSuccess(); 

            return PreconditionResult.FromError("You need to have voted for Mops in the past 12h to use this command!");
        }
    }
}