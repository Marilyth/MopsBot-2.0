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

        [Group("Steam")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Steam : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified steam user, in the Channel you are calling this command in.\nWill notify on game changes and achievements.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(2)]
            public async Task Track([Remainder]string SteamNameOrId)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        SteamNameOrId = SteamNameOrId.ToLower();
                        await Trackers[BaseTracker.TrackerType.Steam].AddTrackerAsync(SteamNameOrId, Context.Channel.Id);
                        var worked = long.TryParse(SteamNameOrId, out long test);

                        await ReplyAsync("Keeping track of " + SteamNameOrId + $"'s Achievements and playing status from now on.");
                        if(!worked) await ReplyAsync($"Make sure this is you: https://steamcommunity.com/id/{SteamNameOrId}\nOtherwise use your steamid instead of steam name");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Steam user, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder]string SteamNameOrId)
            {
                SteamNameOrId = SteamNameOrId.ToLower();
                if (await Trackers[BaseTracker.TrackerType.Steam].TryRemoveTrackerAsync(SteamNameOrId, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + SteamNameOrId + "'s Steam data!");
                else
                    await ReplyAsync($"Could not find tracker for `{SteamNameOrId}`\n" +
                                     $"Currently tracked Steam users are:", embed: StaticBase.Trackers[BaseTracker.TrackerType.Steam].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the Steam users that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Steam users are currently being tracked:", embed: StaticBase.Trackers[BaseTracker.TrackerType.Steam].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new achievement was achieved.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string SteamNameOrId, [Remainder]string notification)
            {
                SteamNameOrId = SteamNameOrId.ToLower();
                if (await StaticBase.Trackers[BaseTracker.TrackerType.Steam].TrySetNotificationAsync(SteamNameOrId, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{SteamNameOrId}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{SteamNameOrId}`\n" +
                                     $"Currently tracked Steam users are:", embed: StaticBase.Trackers[BaseTracker.TrackerType.Steam].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        /*[Group("TwitterRealtime")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitter : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireUserVotepoints(2)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackTwitter(string twitterUser, [Remainder]string tweetNotification = "~Tweet Tweet~")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.TwitterRealtime].AddTrackerAsync(twitterUser, Context.Channel.Id, tweetNotification + "|" + tweetNotification);

                        await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, replies and retweets, from now on!\nTo disable replies and retweets, please use the `Twitter DisableNonMain` subcommand!");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter(string twitterUser)
            {
                if (await Trackers[BaseTracker.TrackerType.TwitterRealtime].TryRemoveTrackerAsync(twitterUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + twitterUser + "'s tweets!");
                else
                    await ReplyAsync($"Could not find tracker for `{twitterUser}`\n" +
                                     $"Currently tracked Twitter Users are:", embed: StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following twitters are currently being tracked:", embed: StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new Main-Tweet is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string TwitterName, [Remainder]string notification = "")
            {
                var twitter = StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTracker(Context.Channel.Id, TwitterName);
                try
                {
                    var nonMainNotification = twitter.ChannelMessages[Context.Channel.Id].Split("|")[1];
                    nonMainNotification = $"{notification}|{nonMainNotification}";
                    await StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].TrySetNotificationAsync(TwitterName, Context.Channel.Id, nonMainNotification);
                    await ReplyAsync($"Set notification for main tweets, for `{TwitterName}`, to {notification}!");
                }
                catch
                {
                    await ReplyAsync($"Could not find tracker for `{TwitterName}`\n" +
                                     $"Currently tracked Twitter Users are:", embed: StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTrackersEmbed(Context.Channel.Id));
                }
            }

            [Command("SetNonMainNotification")]
            [Summary("Sets the notification text that is used each time a new retweet or reply is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNonMainNotification(string TwitterName, [Remainder]string notification = "")
            {
                var twitter = StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTracker(Context.Channel.Id, TwitterName);
                try
                {
                    var mainNotification = twitter.ChannelMessages[Context.Channel.Id].Split("|")[0];
                    mainNotification += $"|{notification}";
                    await StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].TrySetNotificationAsync(TwitterName, Context.Channel.Id, mainNotification);
                    await ReplyAsync($"Set notification for retweets and replies, for `{TwitterName}`, to {notification}!");
                }
                catch
                {
                    await ReplyAsync($"Could not find tracker for `{TwitterName}`\n" +
                                     $"Currently tracked Twitter Users are:", embed: StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTrackersEmbed(Context.Channel.Id));
                }
            }

            [Command("DisableNonMain")]
            [Alias("DisableReplies", "DisableRetweets")]
            [Summary("Disables tracking for the retweets and replies of the specified Twitter account.")]
            public async Task DisableRetweets(string TwitterName)
            {
                var twitter = StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTracker(Context.Channel.Id, TwitterName);
                try
                {
                    var notification = twitter.ChannelMessages[Context.Channel.Id].Split("|")[0];
                    notification += "|NONE";
                    await StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].TrySetNotificationAsync(TwitterName, Context.Channel.Id, notification);
                    await ReplyAsync($"Disabled retweets and replies for `{TwitterName}`!\nTo reenable retweets and replies, please provide a notification via the `Twitter SetNonMainNotification` subcommand!");
                }
                catch
                {
                    await ReplyAsync($"Could not find tracker for `{TwitterName}`\n" +
                                     $"Currently tracked Twitter Users are:", embed: StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTrackersEmbed(Context.Channel.Id));
                }
            }

            [Command("Prune")]
            [Hide]
            [RequireBotManage]
            public async Task PruneTrackers(int failThreshold, bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var allTrackers = StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].GetTrackers();
                    Dictionary<string, int> pruneCount = new Dictionary<string, int>();
                    int totalCount = 0;

                    foreach (var tracker in allTrackers.Where(x => (x.Value as TwitterTracker).FailCount >= failThreshold))
                    {
                        totalCount++;
                        pruneCount[tracker.Key] = (tracker.Value as TwitterTracker).FailCount;
                        if(!testing){
                            foreach(var channel in tracker.Value.ChannelMessages.Keys.ToList())
                                await StaticBase.Trackers[BaseTracker.TrackerType.TwitterRealtime].TryRemoveTrackerAsync(tracker.Key, channel);
                        }
                    }
                    var result = $"{"Twitter User",-20}{"Fail count"}\n{string.Join("\n", pruneCount.Select(x => $"{x.Key,-20}{x.Value,-3}"))}";
                    if(result.Length < 2040)
                        await ReplyAsync($"```{result}```");
                    else
                        await ReplyAsync($"```Pruned {totalCount} trackers```");
                }
            }
        }*/
    }
}
