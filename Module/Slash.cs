using Discord.WebSocket;
using Discord;
using Discord.Interactions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MopsBot.Module.Preconditions;
using System.Text.RegularExpressions;
using MopsBot.Data.Tracker;
using Discord.Interactions;
using static MopsBot.StaticBase;
using Discord.Addons.Interactive;
using MopsBot.Data.Entities;
using static MopsBot.Data.Tracker.BaseTracker;

namespace MopsBot.Module
{
    public class Slash : InteractionModuleBase<IInteractionContext>
    {
        [Group("twitch", "Commands for Twitch tracking")]
        [RequireBotPermission(ChannelPermission.ManageRoles)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitch : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified Streamer.", runMode: RunMode.Async)]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Twitch)]
            public async Task trackStreamer(string streamerName, string notificationMessage = "Stream went live!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    streamerName = streamerName.ToLower();
                    await Trackers[BaseTracker.TrackerType.Twitch].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                    await FollowupAsync("Keeping track of " + streamerName + "'s streams, from now on!");
                }
            }
        }
    }
}
