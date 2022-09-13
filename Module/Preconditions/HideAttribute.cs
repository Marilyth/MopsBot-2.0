using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;

namespace MopsBot.Module.Preconditions{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class HideHelpAttribute : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return PreconditionResult.FromSuccess();
        }

        public override string ToString(){
            return $"Hidden command";
        }
    }
}