using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;

namespace MopsBot.Module.Preconditions{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class DisableAttribute : PreconditionAttribute
    {
        private string reason;
        public DisableAttribute(string reason = "No reason given."){
            this.reason = reason;
        }

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return PreconditionResult.FromError("This command is currently disabled, reason:\n" + reason);
        }

        public override string ToString(){
            return $"Disabled command";
        }
    }
}