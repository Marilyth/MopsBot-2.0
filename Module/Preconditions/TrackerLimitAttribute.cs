using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using static MopsBot.Data.Tracker.BaseTracker;

namespace MopsBot.Module.Preconditions{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class TrackerLimitAttribute : PreconditionAttribute
    {
        public int Limit;
        public TrackerType Type;

        public TrackerLimitAttribute(int limit, TrackerType type){
            Limit = limit;
            Type = type;
        }

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guildId = context.Guild.Id;
            var trackers = StaticBase.Trackers[Type].GetGuildTrackers(guildId);

            int countTrackers = 0;
            foreach(var tracker in trackers)
                countTrackers += tracker.ChannelConfig.Keys.Where(x => (context.Guild as SocketGuild).GetTextChannel(x) != null).Count();

            if(countTrackers < Limit)
                return PreconditionResult.FromSuccess();

            return PreconditionResult.FromError($"Your server exceeded the limit of {Limit} {Type.ToString()}-Trackers.\nTo add another tracker, get below the limit first.");
        }

        public override string ToString(){
            return $"Limit of {Limit} trackers per server.";
        }
    }
}
