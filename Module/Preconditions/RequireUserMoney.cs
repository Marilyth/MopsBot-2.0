using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using MopsBot.Data.Entities;

namespace MopsBot.Module.Preconditions{
    /// <summary>
    /// Sets that the user must pay any amount of VP to use the command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireUserVotepoints : PreconditionAttribute
    {
        private int amount;
        public RequireUserVotepoints(int amount = 1){
            this.amount = amount;
        }

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var user = await User.GetUserAsync(context.User.Id);

            if(user.Money >= amount){
                await User.ModifyUserAsync(context.User.Id, x => x.Money -= amount);
                return PreconditionResult.FromSuccess();
            } else {
                return PreconditionResult.FromError($"This command requires {amount} **Votepoints** to use, but you only have {user.Money} in your balance!\nVote for Mops to recieve 10 **Votepoints**\nhttps://discordbots.org/bot/{Program.Client.CurrentUser.Id}/vote");
            }
        }
    }
}