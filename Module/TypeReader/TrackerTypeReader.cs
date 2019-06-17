using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MopsBot.Data.Tracker;
using static MopsBot.Data.Tracker.BaseTracker;

namespace MopsBot.Module.TypeReader
{
    public class TrackerTypeReader : Discord.Commands.TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var command = context.Message.Content;
            var prefix = await StaticBase.GetGuildPrefixAsync(context.Guild.Id);
            if(!command.StartsWith(prefix))
                prefix = Program.Client.CurrentUser.Mention;
            var module = command.Remove(0, prefix.Length).Split(" ").First(x => x.Length > 0);
            var worked = Enum.TryParse<TrackerType>(module, true, out TrackerType type);
            
            if(!new List<TrackerType>{TrackerType.HTML, TrackerType.JSON, TrackerType.Overwatch, TrackerType.RSS, TrackerType.Youtube, TrackerType.YoutubeLive}.Any(x => x == type))
                input = input.ToLower();

            var result = StaticBase.Trackers[type].GetTracker(context.Channel.Id, input);

            if (result != null)
                return TypeReaderResult.FromSuccess(result);

            await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(context.Channel.Id, StaticBase.Trackers[type].GetTrackersEmbed(context.Channel.Id));
            return TypeReaderResult.FromError(CommandError.ParseFailed, $"Could not find a {module}-tracker for {input}.");
        }
    }
}