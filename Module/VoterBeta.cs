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
            [Summary("Keeps track of the specified Youtubers livestreams, in the Channel you are calling this command in.\nYoutubes Terms of Service and Googles privacy policy apply:\nhttps://www.youtube.com/t/terms \nhttp://www.google.com/policies/privacy")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(0)]
            [TrackerLimit(TrackerType.YoutubeLive)]
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
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel){
                await Tracking.ChangeChannelAsync(Name, FromChannel, TrackerType.YoutubeLive, Context);
            }
        }
    }
}
