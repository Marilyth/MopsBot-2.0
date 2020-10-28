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
    public class RestUserReader : Discord.Commands.TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var command = context.Message.Content;
            var prefix = await StaticBase.GetGuildPrefixAsync(context.Guild.Id);
            var id = context.Message.MentionedUserIds.ToList().FirstOrDefault();

            return TypeReaderResult.FromSuccess(Program.Client.Rest.GetGuildUserAsync(context.Guild.Id, id));
        }
    }
}