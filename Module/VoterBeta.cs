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
using static MopsBot.Data.Tracker.BaseTracker;

namespace MopsBot.Module
{
    public class VoterBeta : ModuleBase<ShardedCommandContext>
    {
        [Group("YoutubeLive")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class YoutubeLive : InteractiveBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Youtubers livestreams, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(2)]
            [TrackerLimit(20, TrackerType.YoutubeLive)]
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
                if (await Trackers[BaseTracker.TrackerType.YoutubeLive].TryRemoveTrackerAsync(channelID.Name, channelID.LastCalledChannelPerGuild[Context.Guild.Id]))
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
                channelID.ChannelConfig[channelID.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].UpdateDBAsync(channelID);
                await ReplyAsync($"Changed notification for `{channelID.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [Hide]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker ChannelID)
            {
                await Tracking.ModifyConfig(this, ChannelID, BaseTracker.TrackerType.YoutubeLive);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel){
                await Tracking.ChangeChannelAsync(Name, FromChannel, TrackerType.YoutubeLive, Context);
            }
        }

        [Group("Mixer")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Mixer : InteractiveBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(2)]
            [TrackerLimit(20, TrackerType.Mixer)]
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
                if (await Trackers[BaseTracker.TrackerType.Mixer].TryRemoveTrackerAsync(streamerName.Name, streamerName.LastCalledChannelPerGuild[Context.Guild.Id]))
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
                streamer.ChannelConfig[streamer.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Mixer].UpdateDBAsync(streamer);
                await ReplyAsync($"Changed notification for `{streamer.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [Hide]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker streamerName)
            {
                await Tracking.ModifyConfig(this, streamerName, BaseTracker.TrackerType.Mixer);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel){
                await Tracking.ChangeChannelAsync(Name, FromChannel, TrackerType.Mixer, Context);
            }
        }
    }
}
