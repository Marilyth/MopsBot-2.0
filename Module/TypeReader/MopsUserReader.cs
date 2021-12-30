using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MopsBot.Data.Tracker;
using Discord.Interactions;
using static MopsBot.Data.Tracker.BaseTracker;

namespace MopsBot.Module.TypeReader
{
    public class MopsUserReader : Discord.Commands.TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var id = context.Message.MentionedUserIds.ToList().FirstOrDefault();

            return TypeReaderResult.FromSuccess(await MopsBot.Data.Entities.User.GetUserAsync(id));
        }
    }

    public class MopsUserConverter : Discord.Interactions.TypeConverter{
        public override async Task<Discord.Interactions.TypeConverterResult> ReadAsync(IInteractionContext context, IApplicationCommandInteractionDataOption input, IServiceProvider services)
        {
            return TypeConverterResult.FromSuccess(await MopsBot.Data.Entities.User.GetUserAsync((input.Value as IUser).Id));
        }

        public override bool CanConvertTo(Type type)
        {
            return true;
        }

        public override ApplicationCommandOptionType GetDiscordType()
        {
            return ApplicationCommandOptionType.User;
        }
    }
}