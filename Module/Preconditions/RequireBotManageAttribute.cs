using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MopsBot.Module.Preconditions{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireBotManageAttribute : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var owners = Program.Config["BotManager"].Split(":").Select(x => ulong.Parse(x)).ToHashSet();
            owners.Add((await Program.Client.GetApplicationInfoAsync()).Owner.Id);
            bool isOwner = owners.Contains(context.User.Id) || owners.Contains(Moderation.CustomCaller[context.Channel.Id]);
            Moderation.CustomCaller.Remove(context.Channel.Id);

            if(isOwner)
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError("");
        }

        public override string ToString(){
            return $"Requires user to be a bot dev";
        }
    }
}
