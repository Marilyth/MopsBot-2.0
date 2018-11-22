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
    public class Tracking : ModuleBase
    {
        [Group("Twitter")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitter : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackTwitter(string twitterUser, [Remainder]string tweetNotification = "~Tweet Tweet~")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.Twitter].AddTrackerAsync(twitterUser, Context.Channel.Id, tweetNotification + "|" + tweetNotification);

                        await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, replies and retweets, from now on!\nTo disable replies and retweets, please use the `Twitter DisableNonMain` subcommand!");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter(string twitterUser)
            {
                if (await Trackers[ITracker.TrackerType.Twitter].TryRemoveTrackerAsync(twitterUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + twitterUser + "'s tweets!");
                else
                    await ReplyAsync($"Could not find tracker for `{twitterUser}`\n" +
                                     $"Currently tracked Twitter Users are:", embed: StaticBase.Trackers[ITracker.TrackerType.Twitter].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following twitters are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.Twitter].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new Main-Tweet is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string TwitterName, [Remainder]string notification = "")
            {
                var twitter = StaticBase.Trackers[ITracker.TrackerType.Twitter].GetTracker(Context.Channel.Id, TwitterName);
                try
                {
                    var nonMainNotification = twitter.ChannelMessages[Context.Channel.Id].Split("|")[1];
                    nonMainNotification = $"{notification}|{nonMainNotification}";
                    await StaticBase.Trackers[ITracker.TrackerType.Twitter].TrySetNotificationAsync(TwitterName, Context.Channel.Id, nonMainNotification);
                    await ReplyAsync($"Set notification for main tweets, for `{TwitterName}`, to {notification}!");
                }
                catch
                {
                    await ReplyAsync($"Could not find tracker for `{TwitterName}`\n" +
                                     $"Currently tracked Twitter Users are:", embed: StaticBase.Trackers[ITracker.TrackerType.Twitter].GetTrackersEmbed(Context.Channel.Id));
                }
            }

            [Command("SetNonMainNotification")]
            [Summary("Sets the notification text that is used each time a new retweet or reply is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNonMainNotification(string TwitterName, [Remainder]string notification = "~Tweet Tweet~")
            {
                var twitter = StaticBase.Trackers[ITracker.TrackerType.Twitter].GetTracker(Context.Channel.Id, TwitterName);
                try
                {
                    var mainNotification = twitter.ChannelMessages[Context.Channel.Id].Split("|")[0];
                    mainNotification += $"|{notification}";
                    await StaticBase.Trackers[ITracker.TrackerType.Twitter].TrySetNotificationAsync(TwitterName, Context.Channel.Id, mainNotification);
                    await ReplyAsync($"Set notification for retweets and replies, for `{TwitterName}`, to {notification}!");
                }
                catch
                {
                    await ReplyAsync($"Could not find tracker for `{TwitterName}`\n" +
                                     $"Currently tracked Twitter Users are:", embed: StaticBase.Trackers[ITracker.TrackerType.Twitter].GetTrackersEmbed(Context.Channel.Id));
                }
            }

            [Command("DisableNonMain")]
            [Alias("DisableReplies", "DisableRetweets")]
            [Summary("Disables tracking for the retweets and replies of the specified Twitter account.")]
            public async Task DisableRetweets(string TwitterName)
            {
                var twitter = StaticBase.Trackers[ITracker.TrackerType.Twitter].GetTracker(Context.Channel.Id, TwitterName);
                try
                {
                    var notification = twitter.ChannelMessages[Context.Channel.Id].Split("|")[0];
                    notification += "|NONE";
                    await StaticBase.Trackers[ITracker.TrackerType.Twitter].TrySetNotificationAsync(TwitterName, Context.Channel.Id, notification);
                    await ReplyAsync($"Disabled retweets and replies for `{TwitterName}`!\nTo reenable retweets and replies, please provide a notification via the `Twitter SetNonMainNotification` subcommand!");
                }
                catch
                {
                    await ReplyAsync($"Could not find tracker for `{TwitterName}`\n" +
                                     $"Currently tracked Twitter Users are:", embed: StaticBase.Trackers[ITracker.TrackerType.Twitter].GetTrackersEmbed(Context.Channel.Id));
                }
            }
        }

        [Group("Osu")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Osu : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Osu player, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackOsu([Remainder]string OsuUser)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.Osu].AddTrackerAsync(OsuUser, Context.Channel.Id);

                        await ReplyAsync("Keeping track of " + OsuUser + "'s plays above `0.1pp` gain, from now on!\nYou can change the lower pp boundary by using the `Osu SetPPBounds` subcommand!");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Osu player, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder]string OsuUser)
            {
                if (await Trackers[ITracker.TrackerType.Osu].TryRemoveTrackerAsync(OsuUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + OsuUser + "'s plays!");
                else
                    await ReplyAsync($"Could not find tracker for `{OsuUser}`\n" +
                                     $"Currently tracked osu! players are:", embed: StaticBase.Trackers[ITracker.TrackerType.Osu].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the Osu players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Osu players are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.Osu].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetPPBounds")]
            [Summary("Sets the lower bounds of pp gain that must be reached, to show a notification.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetPPBounds(string osuUser, double threshold)
            {
                var tracker = (OsuTracker)StaticBase.Trackers[ITracker.TrackerType.Osu].GetTracker(Context.Channel.Id, osuUser);
                if (tracker != null)
                {
                    if (threshold > 0.1)
                    {
                        tracker.PPThreshold = threshold;
                        await StaticBase.Trackers[ITracker.TrackerType.Osu].UpdateDBAsync(tracker);
                        await ReplyAsync($"Changed threshold for `{osuUser}` to `{threshold}`");
                    }
                    else
                        await ReplyAsync("Threshold must be above 0.1!");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{osuUser}`\n" +
                                     $"Currently tracked Osu Players are:", embed: StaticBase.Trackers[ITracker.TrackerType.Osu].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a player gained pp.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string osuUser, [Remainder]string notification)
            {
                if (await StaticBase.Trackers[ITracker.TrackerType.Osu].TrySetNotificationAsync(osuUser, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{osuUser}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{osuUser}`\n" +
                                     $"Currently tracked Osu Players are:", embed: StaticBase.Trackers[ITracker.TrackerType.Osu].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Youtube")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Youtube : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackYoutube(string channelID, [Remainder]string notificationMessage = "New Video")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.Youtube].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                        await ReplyAsync("Keeping track of " + channelID + "'s videos, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(string channelID)
            {
                if (await Trackers[ITracker.TrackerType.Youtube].TryRemoveTrackerAsync(channelID, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + channelID + "'s videos!");
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n" +
                                     $"Currently tracked Youtubers are:", embed: StaticBase.Trackers[ITracker.TrackerType.Youtube].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.Youtube].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new video appears.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string channelID, [Remainder]string notification = "")
            {
                if (await StaticBase.Trackers[ITracker.TrackerType.Youtube].TrySetNotificationAsync(channelID, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{channelID}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n" +
                                     $"Currently tracked channels are:", embed: StaticBase.Trackers[ITracker.TrackerType.Youtube].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Twitch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitch : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage = "Stream went live!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.Twitch].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                        await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(string streamerName)
            {
                if (await Trackers[ITracker.TrackerType.Twitch].TryRemoveTrackerAsync(streamerName, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName + "'s streams!");
                else
                    await ReplyAsync($"Could not find tracker for `{streamerName}`\n" +
                                     $"Currently tracked Streamers are:", embed: StaticBase.Trackers[ITracker.TrackerType.Twitch].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.Twitch].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a streamer goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string streamer, [Remainder]string notification)
            {
                if (await StaticBase.Trackers[ITracker.TrackerType.Twitch].TrySetNotificationAsync(streamer, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{streamer}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n" +
                                     $"Currently tracked streamers are:", embed: StaticBase.Trackers[ITracker.TrackerType.Twitch].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("TwitchClips")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class TwitchClips : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified streamer's top clips every 30 minutes, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackClips(string streamerName, [Remainder]string notificationMessage = "New trending clip found!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.TwitchClip].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                        await ReplyAsync("Keeping track of " + streamerName + "'s top clips above **2** views every 30 minutes, from now on!\nUse the `SetViewThreshold` subcommand to change the threshold.");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer's clips.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(string streamerName)
            {
                if (await Trackers[ITracker.TrackerType.TwitchClip].TryRemoveTrackerAsync(streamerName, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName + "'s streams!");
                else
                    await ReplyAsync($"Could not find tracker for `{streamerName}`\n" +
                                     $"Currently tracked Streamers are:", embed: StaticBase.Trackers[ITracker.TrackerType.TwitchClip].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.TwitchClip].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new clip is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string streamer, [Remainder]string notification = "")
            {
                if (await StaticBase.Trackers[ITracker.TrackerType.TwitchClip].TrySetNotificationAsync(streamer, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{streamer}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n" +
                                     $"Currently tracked streamers are:", embed: StaticBase.Trackers[ITracker.TrackerType.TwitchClip].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetViewThreshold")]
            [Summary("Sets the minimum views a top clip must have to be shown.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetViewThreshold(string streamer, uint threshold)
            {
                var tracker = (TwitchClipTracker)StaticBase.Trackers[ITracker.TrackerType.TwitchClip].GetTracker(Context.Channel.Id, streamer);
                if (tracker != null)
                {
                    tracker.ViewThreshold = threshold;
                    await StaticBase.Trackers[ITracker.TrackerType.TwitchClip].UpdateDBAsync(tracker);
                    await ReplyAsync($"Will only notify about clips equal or above **{threshold}** views for `{streamer}` now.");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n" +
                                     $"Currently tracked streamers are:", embed: StaticBase.Trackers[ITracker.TrackerType.TwitchClip].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Reddit")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Reddit : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Subreddit, in the Channel you are calling this command right now.\nRequires Manage channel permissions."
            + "\n queries MUST look something like this: `title:mei+title:hanzo`")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackSubreddit(string subreddit, string query = null)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.Reddit].AddTrackerAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                        await ReplyAsync("Keeping track of " + subreddit + $"'s posts, from now on, using {query}!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified Subreddit.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackSubreddit(string subreddit, string query = null)
            {
                if (await Trackers[ITracker.TrackerType.Reddit].TryRemoveTrackerAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + subreddit + "'s posts!");
                else
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n" +
                                     $"Currently tracked Subreddits are:", embed: StaticBase.Trackers[ITracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the subreddits that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following subreddits are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string subreddit, string notification = "", string query = null)
            {
                if (await StaticBase.Trackers[ITracker.TrackerType.Reddit].TrySetNotificationAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{subreddit}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n" +
                                     $"Currently tracked subreddits are:", embed: StaticBase.Trackers[ITracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Overwatch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Overwatch : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackOW(string owUser)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        owUser = owUser.Replace("#", "-");
                        await Trackers[ITracker.TrackerType.Overwatch].AddTrackerAsync(owUser, Context.Channel.Id);

                        await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOW(string owUser)
            {
                owUser = owUser.Replace("#", "-");
                if (await Trackers[ITracker.TrackerType.Overwatch].TryRemoveTrackerAsync(owUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + owUser + "'s stats!");
                else
                    await ReplyAsync($"Could not find tracker for `{owUser}`\n" +
                                     $"Currently tracked players are:", embed: StaticBase.Trackers[ITracker.TrackerType.Overwatch].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetStats")]
            [Summary("Returns an embed representing the stats of the specified Overwatch player")]
            public async Task GetStats(string owUser)
            {
                await ReplyAsync("Stats fetched:", false, await Data.Tracker.OverwatchTracker.overwatchInformation(owUser));
            }

            [Command("GetTrackers")]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.Overwatch].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a players' stats changed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string owUser, [Remainder]string notification = "")
            {
                owUser = owUser.Replace("#", "-");
                if (await StaticBase.Trackers[ITracker.TrackerType.Overwatch].TrySetNotificationAsync(owUser, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{owUser}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{owUser}`\n" +
                                     $"Currently tracked players are:", embed: StaticBase.Trackers[ITracker.TrackerType.Overwatch].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("News")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class News : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of articles from the specified source.\n" +
                     "Here is a list of possible sources: https://newsapi.org/sources")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackNews(string source, [Remainder]string query = "")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.News].AddTrackerAsync(String.Join("|", new string[] { source, query }), Context.Channel.Id);
                        await ReplyAsync($"Keeping track of `{source}`'s articles {(query.Equals("") ? "" : $"including `{query}` from now on!")}");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking articles with the specified query.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackNews([Remainder]string articleQuery)
            {
                if (await Trackers[ITracker.TrackerType.News].TryRemoveTrackerAsync(articleQuery, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of articles including " + articleQuery + "!");
                else
                    await ReplyAsync($"Could not find tracker for `{articleQuery}`\n" +
                                     $"Currently tracked article queries are:", embed: StaticBase.Trackers[ITracker.TrackerType.News].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the article queries that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following article queries are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.News].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a article was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string articleQuery, [Remainder]string notification = "")
            {
                if (await StaticBase.Trackers[ITracker.TrackerType.News].TrySetNotificationAsync(articleQuery, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{articleQuery}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{articleQuery}`\n" +
                                     $"Currently tracked article queries are:", embed: StaticBase.Trackers[ITracker.TrackerType.News].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("OSRS")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class OSRS : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the stats of the OSRS player.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task Track(string name, [Remainder]string notification = "")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.OSRS].AddTrackerAsync(name, Context.Channel.Id);
                        await ReplyAsync($"Keeping track of `{name}` stats after each playsession, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the player with the specified name.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrack([Remainder]string name)
            {
                if (await Trackers[ITracker.TrackerType.OSRS].TryRemoveTrackerAsync(name, Context.Channel.Id))
                    await ReplyAsync($"Stopped keeping track of {name}!");
                else
                    await ReplyAsync($"Could not find tracker for `{name}`\n" +
                                     $"Currently tracked players are:", embed: StaticBase.Trackers[ITracker.TrackerType.OSRS].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetStats")]
            [Summary("Gets all top 2kk stats of the specified player.")]
            public async Task GetStats([Remainder]string name)
            {
                await ReplyAsync("", embed: await OSRSTracker.GetStatEmbed(name));
            }

            [Command("Compare")]
            [Summary("Compares the stats of 2 players.")]
            public async Task Compare(string name1, string name2)
            {
                await ReplyAsync("", embed: await OSRSTracker.GetCompareEmbed(name1, name2));
            }

            [Command("GetItem")]
            [Summary("Gets information on an Item")]
            public async Task GetItem([Remainder]string name)
            {
                await ReplyAsync("", embed: await OSRSTracker.GetItemEmbed(name));
            }

            [Command("GetTrackers")]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.OSRS].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a level up takes place.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string name, [Remainder]string notification = "")
            {
                if (await StaticBase.Trackers[ITracker.TrackerType.OSRS].TrySetNotificationAsync(name, Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{name}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{name}`\n" +
                                     $"Currently tracked players are:", embed: StaticBase.Trackers[ITracker.TrackerType.OSRS].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("HTML")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Ratelimit(1, 60, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
        public class HTML : InteractiveBase
        {
            [Command("TrackRegex", RunMode = RunMode.Async)]
            [Summary("Tracks regex on a webpage. Use () around the text you want to track to signify a match.")]
            public async Task TrackRegex(string website, string scrapeRegex)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.HTML].AddTrackerAsync(website + "|||" + scrapeRegex, Context.Channel.Id);
                        await ReplyAsync($"Keeping track of `{website}` data using ```html\n{scrapeRegex}```, from now on!\n\nInitial value was: **{await HTMLTracker.FetchData(website + "|||" + scrapeRegex)}**");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Tracks plain text on a webpage, and notifies whenever that text changes.\nThis command will guide you through the process.")]
            public async Task TrackText(string website, string textToTrack, int leftContextLength = 4, int rightContextLength = 1)
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (leftContextLength > 0 && rightContextLength > 0)
                    {
                        string escapedTextToTrack = textToTrack.Replace("?", @"\?").Replace("*", @"\*").Replace(".", @"\.").Replace("+", @"\+").Replace(")", @"\)").Replace("(", @"\(").Replace("[", @"\[").Replace("]", @"\]");

                        MatchCollection matches = await HTMLTracker.FetchAllData(website + "|||" + $"(<[^<>]*?>[^<>]*?){{{leftContextLength}}}({escapedTextToTrack})[^<>]*?(<[^<>]*?>[^<>]*?){{{rightContextLength}}}");
                        await Data.Interactive.MopsPagniator.CreatePagedMessage(Context.Channel, matches.Select(x => $"**{textToTrack}** in context\n\n```html\n{x.Value}```"));

                        await ReplyAsync("Which page is the one you want to track?\nIf none are specific enough, consider extending the context, or writing your own regex using the `TrackRegex` subcommand.");
                        int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;

                        //Escape regex symbols
                        string unescapedMatchString = matches[page].Value.Replace(escapedTextToTrack, textToTrack);

                        //Find out position of text, and replace it with wild characters
                        var match = Regex.Match(unescapedMatchString, $@">[^<>]*?({escapedTextToTrack})[^<>]*?<", RegexOptions.Singleline);
                        int position = match.Groups.First(x => x.Value.Equals(textToTrack)).Index;
                        string scrapeRegex = unescapedMatchString.Remove(position, textToTrack.Length).Insert(position, $@"\(\[^<>\]\*\?\)");

                        //Make any additional occurences of text in context wild characters
                        scrapeRegex = scrapeRegex.Replace(escapedTextToTrack, @"\[^<>\]\*\?");
                        scrapeRegex = scrapeRegex.Replace("?", @"\?").Replace("*", @"\*").Replace(".", @"\.").Replace("+", @"\+").Replace(")", @"\)").Replace("(", @"\(").Replace("[", @"\[").Replace("]", @"\]");
                        scrapeRegex = scrapeRegex.Replace("\\\\?", @"?").Replace("\\\\*", @"*").Replace("\\\\.", @".").Replace("\\\\+", @"+").Replace("\\\\)", @")").Replace("\\\\(", @"(").Replace("\\\\[", @"[").Replace("\\\\]", @"]");

                        await ReplyAsync($"Is there anything, for the sake of context, that you want to have removed (e.g. tracking highest level, but don't want it to be bound to a certain name)?\n\n```html\n{scrapeRegex}```\n\nIf so, please enter the exact texts you want to be generic instead of fixed in a **comma seperated list**.");
                        string result = (await NextMessageAsync(timeout: new TimeSpan(0, 1, 0)))?.Content;

                        if (result != null)
                        {
                            foreach (string value in result?.Split(","))
                            {
                                if (value.ToLower().Equals("no") || value.ToLower().Equals("n") || value.ToLower().Equals("nope"))
                                    break;
                                string toRemove = value.Trim();
                                scrapeRegex = scrapeRegex.Replace(toRemove, "[^<>]*?");
                            }
                        }

                        await TrackRegex(website, scrapeRegex);
                    }
                }
            }

            [Command("TestRegex", RunMode = RunMode.Async)]
            [Summary("Tests the regex and returns it's value. Handy if you want to check your regex before tracking with it!")]
            public async Task Test(string website, string scrapeRegex)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await ReplyAsync($"Regex returned value: {await HTMLTracker.FetchData(website + "|||" + scrapeRegex)}");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack", RunMode = RunMode.Async)]
            [Summary("Creates a paginator of all trackers, out of which you have to choose one.")]
            public async Task UnTrack()
            {
                using (Context.Channel.EnterTypingState())
                {
                    var trackers = Trackers[ITracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList();
                    await Data.Interactive.MopsPagniator.CreatePagedMessage(Context.Channel, trackers.Select(x => $"```html\n{x.Name}```"));
                    await ReplyAsync("Which tracker do you want to delete?\nPlease enter the page number");

                    int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;
                    if (await Trackers[ITracker.TrackerType.HTML].TryRemoveTrackerAsync(trackers[page].Name, Context.Channel.Id))
                        await ReplyAsync($"Stopped keeping track of result {page + 1} of paginator!");
                }
            }

            [Command("UnTrackAll")]
            [Summary("Untracks all trackers in the current channel.")]
            public async Task UnTrackAll()
            {
                foreach (var tracker in Trackers[ITracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList())
                {
                    if (await Trackers[ITracker.TrackerType.HTML].TryRemoveTrackerAsync(tracker.Name, Context.Channel.Id))
                        await ReplyAsync($"Stopped keeping track of {tracker.Name.Split("|||")[0]}!");
                }
            }

            [Command("SetNotification", RunMode = RunMode.Async)]
            [Summary("Sets the notification for when the text of a regex match changes.\nRequires only the notification, paginator will guide you.")]
            public async Task SetNotification([Remainder]string notification = "")
            {
                using (Context.Channel.EnterTypingState())
                {
                    var trackers = Trackers[ITracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList();
                    await Data.Interactive.MopsPagniator.CreatePagedMessage(Context.Channel, trackers.Select(x => $"```html\n{x.Name}```"));
                    await ReplyAsync("Which tracker do you want to set the notification for?\nPlease enter the page number");

                    int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;
                    if (await Trackers[ITracker.TrackerType.HTML].TrySetNotificationAsync(trackers[page].Name, Context.Channel.Id, notification))
                        await ReplyAsync($"Set notification for result {page + 1} of paginator to `{notification}`!");
                }
            }
        }

        [Group("WoW")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class WoW : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of changes in stats of the specified WoW player.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task Track(string Region, string Realm, string Name)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[ITracker.TrackerType.WoW].AddTrackerAsync(String.Join("|", new string[] { Region, Realm, Name }), Context.Channel.Id);
                        await ReplyAsync($"Keeping track of `{Name}`'s stats in `{Realm}` from now on.");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking stats of the specified player.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrack(string Region, string Realm, string Name)
            {
                if (await Trackers[ITracker.TrackerType.WoW].TryRemoveTrackerAsync(string.Join("|", Region, Realm, Name), Context.Channel.Id))
                    await ReplyAsync($"Stopped keeping track of `{Region} {Realm} {Name}`'s stats!");
                else
                    await ReplyAsync($"Could not find tracker for `{Region} {Realm} {Name}`\n" +
                                     $"Currently tracked WoW players are:", embed: StaticBase.Trackers[ITracker.TrackerType.WoW].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the WoW players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:", embed: StaticBase.Trackers[ITracker.TrackerType.WoW].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetStats")]
            [Summary("Returns the WoW players' stats.")]
            public async Task getStats(string Region, string Realm, string Name)
            {
                await ReplyAsync("Stats for player:", embed: WoWTracker.createStatEmbed(Region, Realm, Name));
            }

            /*[Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a change in stats was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string Region, string Realm, string Name, [Remainder]string notification)
            {
                if(StaticBase.Trackers[ITracker.TrackerType.WoW].TrySetNotification(string.Join("|", Region, Realm, Name), Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{Region} {Realm} {Name}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{Region} {Realm} {Name}`\n"+
                                     $"Currently tracked players are: ``{String.Join(", ", StaticBase.Trackers[ITracker.TrackerType.WoW].GetTrackers(Context.Channel.Id).Select(x => x.Name.Replace("|", " ")))}``");
            }*/

            [Command("ChangeEQTrack")]
            [Summary("Notifies on change of equipment.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableEQTrack(string Region, string Realm, string Name)
            {
                WoWTracker tracker = (WoWTracker)StaticBase.Trackers[ITracker.TrackerType.WoW].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackEquipment = !tracker.trackEquipment;

                await StaticBase.Trackers[ITracker.TrackerType.WoW].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed EQTrack for `{Region} {Realm} {Name}` to `{tracker.trackEquipment}`");
            }

            [Command("ChangeStatTrack")]
            [Summary("Notifies on change of stats.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableStatTrack(string Region, string Realm, string Name)
            {
                WoWTracker tracker = (WoWTracker)StaticBase.Trackers[ITracker.TrackerType.WoW].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackStats = !tracker.trackStats;

                await StaticBase.Trackers[ITracker.TrackerType.WoW].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed StatTrack for `{Region} {Realm} {Name}` to `{tracker.trackStats}`");
            }

            [Command("ChangeFeedTrack")]
            [Summary("Notifies on change of feed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableFeedTrack(string Region, string Realm, string Name)
            {
                WoWTracker tracker = (WoWTracker)StaticBase.Trackers[ITracker.TrackerType.WoW].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackFeed = !tracker.trackFeed;

                await StaticBase.Trackers[ITracker.TrackerType.WoW].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed FeedTrack for `{Region} {Realm} {Name}` to `{tracker.trackFeed}`");
            }
        }

        /*[Group("WoWGuild")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class WoWGuild : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of changes in news of the specified WoW guild.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Track(string Region, string Realm, string Name)
            {
                await Trackers[ITracker.TrackerType.WoWGuild].AddTrackerAsync(String.Join("|", new string[] { Region, Realm, Name }), Context.Channel.Id);
                await ReplyAsync($"Keeping track of `{Name}`'s news in `{Realm}` from now on.");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking stats of the specified Guild.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrack(string Region, string Realm, string Name)
            {
                if(await Trackers[ITracker.TrackerType.WoWGuild].TryRemoveTrackerAsync(string.Join("|", Region, Realm, Name), Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + string.Join("|", Region, Realm, Name) + "'s news!");
                else
                    await ReplyAsync($"Could not find tracker for `{Region} {Realm} {Name}`\n"+
                                     $"Currently tracked WoW Guilds are:", embed:StaticBase.Trackers[ITracker.TrackerType.WoWGuild].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the WoW Guilds that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following guilds are currently being tracked:", embed:StaticBase.Trackers[ITracker.TrackerType.WoWGuild].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a change in news was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string Region, string Realm, string Name, [Remainder]string notification)
            {
                if(StaticBase.Trackers[ITracker.TrackerType.WoWGuild].TrySetNotification(string.Join("|", Region, Realm, Name), Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{Region} {Realm} {Name}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{Region} {Realm} {Name}`\n"+
                                     $"Currently tracked guilds are: ``{String.Join(", ", StaticBase.Trackers[ITracker.TrackerType.WoWGuild].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("ChangeLootTrack")]
            [Summary("Notifies when member gains loot.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableEQTrack(string Region, string Realm, string Name)
            {
                WoWGuildTracker tracker = (WoWGuildTracker)StaticBase.Trackers[ITracker.TrackerType.WoWGuild].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackLoot = !tracker.trackLoot;
                
                await StaticBase.Trackers[ITracker.TrackerType.WoWGuild].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed EQTrack for `{Region} {Realm} {Name}` to `{tracker.trackLoot}`");
            }

            [Command("ChangeAchievementTrack")]
            [Summary("Notifies on gained achievements.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableStatTrack(string Region, string Realm, string Name)
            {
                WoWGuildTracker tracker = (WoWGuildTracker)StaticBase.Trackers[ITracker.TrackerType.WoWGuild].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackAchievements = !tracker.trackAchievements;
                
                await StaticBase.Trackers[ITracker.TrackerType.WoWGuild].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed StatTrack for `{Region} {Realm} {Name}` to `{tracker.trackAchievements}`");
            }
        }*/
        /*[Command("trackClips")]
        [Summary("Keeps track of clips from streams of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task trackClips(string streamerName)
        {
            ClipTracker.AddTracker(streamerName, Context.Channel.Id);

            await ReplyAsync("Keeping track of clips of " + streamerName + "'s streams, from now on!");
        }*/

        [Command("PruneTrackers", RunMode = RunMode.Async)]
        [RequireBotManage()]
        [Hide]
        public async Task PruneTrackers(bool testing = true)
        {
            using (Context.Channel.EnterTypingState())
            {
                Dictionary<string, int> pruneCount = new Dictionary<string, int>();

                foreach (var trackerHandler in StaticBase.Trackers)
                {
                    pruneCount[trackerHandler.Key.ToString()] = 0;
                    foreach (var tracker in trackerHandler.Value.GetTrackerSet())
                    {
                        foreach (var channel in tracker.ChannelMessages.Keys.ToList())
                        {
                            if (Program.Client.GetChannel(channel) == null)
                            {
                                if (!testing)
                                    await trackerHandler.Value.TryRemoveTrackerAsync(tracker.Name, channel);

                                pruneCount[trackerHandler.Key.ToString()]++;
                            }
                        }
                    }
                }

                await ReplyAsync($"```{"TrackerType",-20}{"PruneCount"}\n{string.Join("\n", pruneCount.Select(x => $"{x.Key,-20}{x.Value,-3}"))}```");
            }
        }
    }
}
