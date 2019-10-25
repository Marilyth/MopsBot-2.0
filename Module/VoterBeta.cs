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
        public class YoutubeLive : InteractiveBase
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
                    await Trackers[BaseTracker.TrackerType.YoutubeLive].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                    await ReplyAsync("Keeping track of " + channelID + "'s streams, from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtubers streams, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(BaseTracker channelID)
            {
                if (await Trackers[BaseTracker.TrackerType.YoutubeLive].TryRemoveTrackerAsync(channelID.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + channelID.Name + "'s streams!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new stream goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker channelID, [Remainder]string notification = "")
            {
                channelID.ChannelConfig[Context.Channel.Id]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].UpdateDBAsync(channelID);
                await ReplyAsync($"Changed notification for `{channelID.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [Hide]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[Context.Channel.Id].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker ChannelID)
            {
                await Tracking.ModifyConfig(this, ChannelID, BaseTracker.TrackerType.YoutubeLive);
            }
        }

        [Group("Mixer")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Mixer : InteractiveBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(2)]
            public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage = "Stream went live!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    streamerName = streamerName.ToLower();
                    await Trackers[BaseTracker.TrackerType.Mixer].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                    await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(BaseTracker streamerName)
            {
                if (await Trackers[BaseTracker.TrackerType.Mixer].TryRemoveTrackerAsync(streamerName.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName.Name + "'s streams!");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Mixer].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a streamer goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker streamer, [Remainder]string notification = "")
            {
                streamer.ChannelConfig[Context.Channel.Id]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Mixer].UpdateDBAsync(streamer);
                await ReplyAsync($"Changed notification for `{streamer.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [Hide]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[Context.Channel.Id].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker streamerName)
            {
                await Tracking.ModifyConfig(this, streamerName, BaseTracker.TrackerType.Mixer);
            }
        }
    }
}
