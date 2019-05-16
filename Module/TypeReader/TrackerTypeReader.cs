using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MopsBot.Data.Tracker;

namespace MopsBot.Module.TypeReader
{
    public class TrackerTypeReader : Discord.Commands.TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var command = context.Message.Content.Split(input)[0];
            input = input.ToLower();
            var prefix = await StaticBase.GetGuildPrefixAsync(context.Guild.Id);
            var module = command.Split(prefix)[1].Split(" ")[0];
            var worked = Enum.TryParse<BaseTracker.TrackerType>(module, true, out BaseTracker.TrackerType type);

            var result = StaticBase.Trackers[type].GetTracker(context.Channel.Id, input);

            if (result != null)
                return TypeReaderResult.FromSuccess(result);

            return TypeReaderResult.FromError(CommandError.ParseFailed, $"Could not find a tracker for {input}.\nExisting {module}-trackers are:\n{StaticBase.Trackers[type].GetTrackersEmbed(context.Channel.Id).Description}");
        }
    }
}