using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MopsBot.Module.Preconditions;
using System.Text.RegularExpressions;
using static MopsBot.StaticBase;
using MopsBot.Data.Tracker;
using Discord.Addons.Interactive;

namespace MopsBot.Module
{
    public class VoterBeta : ModuleBase
    {
        [Group("YoutubeLive")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class YoutubeLive : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Youtubers livestreams, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(2)]
            public async Task trackYoutube(string channelID, [Remainder]string notificationMessage = "New Stream")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.YoutubeLive].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                        await ReplyAsync("Keeping track of " + channelID + "'s streams, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtubers streams, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(string channelID)
            {
                if (await Trackers[BaseTracker.TrackerType.YoutubeLive].TryRemoveTrackerAsync(channelID, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + channelID + "'s streams!");
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n" +
                                     $"Currently tracked Youtubers are:", embed: StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:", embed: StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new stream goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string channelID, [Remainder]string notification = "")
            {
                if (await StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].TrySetNotificationAsync(channelID, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{channelID}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n" +
                                     $"Currently tracked channels are:", embed: StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].GetTrackersEmbed(Context.Channel.Id));
            }
        }
    }
}
