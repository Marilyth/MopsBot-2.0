using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MopsBot.Module.Preconditions
{
    /// <summary> Sets that a user must have voted for the bot to use the command </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireVoter : PreconditionAttribute
    {
        private VoterFlag votePeriod;
        private  int invokes;
        private TimeSpan period;
        private readonly Dictionary<(ulong, ulong?), RatelimitAttribute.CommandTimeout> _invokeTracker = new Dictionary<(ulong, ulong?), RatelimitAttribute.CommandTimeout>();
        public RequireVoter(VoterFlag flag = VoterFlag.Month, int invokesWithoutVote = 0, double period = 0, Measure measure = Measure.Minutes)
        {
            votePeriod = flag;
            invokes = invokesWithoutVote;

            switch (measure)
            {
                case Measure.Days:
                    this.period = TimeSpan.FromDays(period);
                    break;
                case Measure.Hours:
                    this.period = TimeSpan.FromHours(period);
                    break;
                case Measure.Minutes:
                    this.period = TimeSpan.FromMinutes(period);
                    break;
                case Measure.Seconds:
                    this.period = TimeSpan.FromSeconds(period);
                    break;
            }
        }

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            bool isVoter = votePeriod.Equals(VoterFlag.HalfDay) ? await StaticBase.DiscordBotList.HasVoted(context.User.Id) : (await StaticBase.DiscordBotList.GetVotersAsync()).Any(x => x.Id == context.User.Id);

            if (!isVoter)
            {
                var now = DateTime.UtcNow;
                ulong scopeId = 0;
                ulong userId = context.User.Id;
                var key = (userId, scopeId);

                var timeout = (_invokeTracker.TryGetValue(key, out var t)
                    && ((now - t.FirstInvoke) < period))
                        ? t : new RatelimitAttribute.CommandTimeout(now);

                if (++timeout.TimesInvoked <= invokes)
                {
                    _invokeTracker[key] = timeout;
                    return PreconditionResult.FromSuccess();
                }
                else
                {
                    var timeLeft = period - (now - t.FirstInvoke);

                    if (invokes > 0){
                        return PreconditionResult.FromError($"You can only use this command {invokes} times without having voted for Mops{(votePeriod.Equals(VoterFlag.HalfDay) ? " in the past 12h" : " this month")}!\nYou are currently in Timeout\nPlease try again in: `{timeLeft.Hours}h {timeLeft.Minutes}m {timeLeft.Seconds}s` or vote for Mops here: https://discordbots.org/bot/{Program.Client.CurrentUser.Id}/vote\nIt might take a few minutes to recieve the vote.");
                    }
                    else
                        return PreconditionResult.FromError($"You need to have voted for Mops in the past {(votePeriod.Equals(VoterFlag.HalfDay) ? "12h " : "month ")}to use this command!\nhttps://discordbots.org/bot/{Program.Client.CurrentUser.Id}/vote\nIt might take a few minutes to recieve the vote.");
                }
            }

            else
                return PreconditionResult.FromSuccess();
        }

        public override string ToString(){
            return $"Can be used {invokes}x within {period}\nOr infinite usage if voted in the past {votePeriod}\n";
        }
    }

    public enum VoterFlag
    {
        HalfDay,
        Month
    }
}