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

namespace MopsBot.Module
{
    public class Tracking : ModuleBase
    {
        [Group("Twitter")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitter : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.\n"+
                      "You can specify the tweet notification like so: Normal tweet notification|Retweet or answer notification")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackTwitter(string twitterUser, [Remainder]string tweetNotification = "~Tweet Tweet~|~Tweet Tweet~")
            {
                Trackers["twitter"].AddTracker(twitterUser, Context.Channel.Id, tweetNotification.Contains('|') ? tweetNotification : tweetNotification + "|" + tweetNotification);

                await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter(string twitterUser)
            {
                if(Trackers["twitter"].TryRemoveTracker(twitterUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + twitterUser + "'s tweets!");
                else
                    await ReplyAsync($"Could not find tracker for `{twitterUser}`\n"+
                                     $"Currently tracked Twitter Users are: ``{String.Join(", ", StaticBase.Trackers["twitter"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("GetTrackers")]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following twitters are currently being tracked:\n``" + String.Join(", ", StaticBase.Trackers["twitter"].GetTrackers(Context.Channel.Id).Select(x => x.Name)) + "``");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new Tweet is found.\n"+
                     "To differentiate between main tweets and other tweets, use `<Main Tweet Notification>|<Other Tweet Notification>`\n"+
                     "To disable a kind of tweet, set notification to **NONE**")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string twitterUser, [Remainder]string notification)
            {
                notification = notification.Contains("|") ? notification : notification + "|" + notification;

                if(StaticBase.Trackers["twitter"].TrySetNotification(twitterUser, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{twitterUser}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{twitterUser}`\n"+
                                     $"Currently tracked Twitter Users are: ``{String.Join(", ", StaticBase.Trackers["twitter"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }
        }

        [Group("Osu")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Osu : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Osu player, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackOsu([Remainder]string OsuUser)
            {
                Trackers["osu"].AddTracker(OsuUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + OsuUser + "'s plays, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Osu player, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder]string OsuUser)
            {
                if(Trackers["osu"].TryRemoveTracker(OsuUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + OsuUser + "'s plays!");
                else
                    await ReplyAsync($"Could not find tracker for `{OsuUser}`\n"+
                                     $"Currently tracked osu! players are: ``{String.Join(", ", StaticBase.Trackers["osu"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("GetTrackers")]
            [Summary("Returns the Osu players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Osu players are currently being tracked:\n``" + String.Join(", ", StaticBase.Trackers["osu"].GetTrackers(Context.Channel.Id).Select(x => x.Name)) + "``");
            }

            [Command("SetPPBounds")]
            [Summary("Sets the lower bounds of pp gain that must be reached, to show a notification.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetPPBounds(string osuUser, double threshold)
            {
                var player = (OsuTracker)StaticBase.Trackers["osu"].GetTracker(Context.Channel.Id, osuUser);
                if(player != null){
                    if(threshold > 0.1){
                        player.PPThreshold = threshold;
                        StaticBase.Trackers["osu"].SaveJson();
                        await ReplyAsync($"Changed threshold for `{osuUser}` to `{threshold}`");
                    }
                    else
                        await ReplyAsync("Threshold must be above 0.1!");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{osuUser}`\n"+
                                     $"Currently tracked Osu Players are: ``{String.Join(", ", StaticBase.Trackers["osu"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a player gained pp.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string osuUser, [Remainder]string notification)
            {
                if(StaticBase.Trackers["osu"].TrySetNotification(osuUser, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{osuUser}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{osuUser}`\n"+
                                     $"Currently tracked Osu Players are: ``{String.Join(", ", StaticBase.Trackers["osu"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }
        }

        [Group("Youtube")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Youtube : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackYoutube(string channelID, [Remainder]string notificationMessage = "New Video")
            {
                Trackers["youtube"].AddTracker(channelID, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + channelID + "'s videos, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(string channelID)
            {
                if(Trackers["youtube"].TryRemoveTracker(channelID, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + channelID + "'s videos!");
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n"+
                                     $"Currently tracked Youtubers are: ``{String.Join(", ", StaticBase.Trackers["youtube"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("GetTrackers")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:\n``" + String.Join(", ", StaticBase.Trackers["youtube"].GetTrackers(Context.Channel.Id).Select(x => x.Name)) + "``");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new video appears.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string channelID, [Remainder]string notification)
            {
                if(StaticBase.Trackers["youtube"].TrySetNotification(channelID, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{channelID}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n"+
                                     $"Currently tracked channels are: ``{String.Join(", ", StaticBase.Trackers["youtube"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }
        }

        [Group("Twitch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitch : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage = "Stream went live!")
            {
                Trackers["twitch"].AddTracker(streamerName, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(string streamerName)
            {
                if(Trackers["twitch"].TryRemoveTracker(streamerName, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName + "'s streams!");
                else
                    await ReplyAsync($"Could not find tracker for `{streamerName}`\n"+
                                     $"Currently tracked Streamers are: ``{String.Join(", ", StaticBase.Trackers["twitch"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("GetTrackers")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:\n``" + String.Join(", ", StaticBase.Trackers["twitch"].GetTrackers(Context.Channel.Id).Select(x => x.Name)) + "``");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a streamer goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string streamer, [Remainder]string notification)
            {
                if(StaticBase.Trackers["twitch"].TrySetNotification(streamer, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{streamer}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n"+
                                     $"Currently tracked streamers are: ``{String.Join(", ", StaticBase.Trackers["twitch"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }
        }

        [Group("TwitchClips")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class TwitchClips : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified streamer's top clips every 30 minutes, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackClips(string streamerName, [Remainder]string notificationMessage = "New trending clip found!")
            {
                Trackers["twitchclips"].AddTracker(streamerName, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + streamerName + "'s top clips above **2** views every 30 minutes, from now on!\nUse the `SetViewThreshold` subcommand to change the threshold.");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer's clips.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(string streamerName)
            {
                if(Trackers["twitchclips"].TryRemoveTracker(streamerName, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName + "'s streams!");
                else
                    await ReplyAsync($"Could not find tracker for `{streamerName}`\n"+
                                     $"Currently tracked Streamers are: ``{String.Join(", ", StaticBase.Trackers["twitch"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("GetTrackers")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:\n``" + String.Join(", ", StaticBase.Trackers["twitchclips"].GetTrackers(Context.Channel.Id).Select(x => x.Name)) + "``");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new clip is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string streamer, [Remainder]string notification)
            {
                if(StaticBase.Trackers["twitchclips"].TrySetNotification(streamer, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{streamer}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n"+
                                     $"Currently tracked streamers are: ``{String.Join(", ", StaticBase.Trackers["twitchclips"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("SetViewThreshold")]
            [Summary("Sets the minimum views a top clip must have to be shown.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetViewThreshold(string streamer, uint threshold)
            {
                var tracker = (TwitchClipTracker)StaticBase.Trackers["twitchclips"].GetTracker(Context.Channel.Id, streamer);
                if(tracker != null){
                    tracker.ViewThreshold = threshold;
                    StaticBase.Trackers["twitchclips"].SaveJson();
                    await ReplyAsync($"Will only notify about clips equal or above **{threshold}** views for `{streamer}` now.");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n"+
                                     $"Currently tracked streamers are: ``{String.Join(", ", StaticBase.Trackers["twitchclips"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }
        }

        [Group("Reddit")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Reddit : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Subreddit, in the Channel you are calling this command right now.\nRequires Manage channel permissions."
            + "\n queries MUST look something like this: `title:mei+title:hanzo`")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackSubreddit(string subreddit, string query = null)
            {
                Trackers["reddit"].AddTracker(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                await ReplyAsync("Keeping track of " + subreddit + $"'s posts, from now on, using {query}!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified Subreddit.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackSubreddit(string subreddit, string query = null)
            {
                if(Trackers["reddit"].TryRemoveTracker(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + subreddit + "'s posts!");
                else
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n"+
                                     $"Currently tracked Subreddits are: ``{String.Join(", ", StaticBase.Trackers["reddit"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("GetTrackers")]
            [Summary("Returns the subreddits that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following subreddits are currently being tracked:\n``" + String.Join(", ", StaticBase.Trackers["reddit"].GetTrackers(Context.Channel.Id).Select(x => x.Name)) + "``");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string subreddit, string notification, string query = null)
            {
                if(StaticBase.Trackers["reddit"].TrySetNotification(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{subreddit}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n"+
                                     $"Currently tracked subreddits are: ``{String.Join(", ", StaticBase.Trackers["reddit"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }
        }

        [Group("Overwatch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Overwatch : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackOW(string owUser)
            {
                owUser = owUser.Replace("#", "-");
                Trackers["overwatch"].AddTracker(owUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOW(string owUser)
            {
                owUser = owUser.Replace("#", "-");
                if(Trackers["overwatch"].TryRemoveTracker(owUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + owUser + "'s stats!");
                else
                    await ReplyAsync($"Could not find tracker for `{owUser}`\n"+
                                     $"Currently tracked players are: ``{String.Join(", ", StaticBase.Trackers["overwatch"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
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
                await ReplyAsync("Following players are currently being tracked:\n``" + String.Join(", ", StaticBase.Trackers["overwatch"].GetTrackers(Context.Channel.Id).Select(x => x.Name)) + "``");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a players' stats changed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string owUser, [Remainder]string notification)
            {
                owUser = owUser.Replace("#", "-");
                if(StaticBase.Trackers["overwatch"].TrySetNotification(owUser, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{owUser}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{owUser}`\n"+
                                     $"Currently tracked players are: ``{String.Join(", ", StaticBase.Trackers["overwatch"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }
        }

        [Group("News")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class News : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of articles from the specified source.\n"+
                     "Here is a list of possible sources: https://newsapi.org/sources")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackNews(string source, [Remainder]string query = "")
            {
                Trackers["news"].AddTracker(String.Join("|", new string[] { source, query }), Context.Channel.Id);
                await ReplyAsync($"Keeping track of `{source}`'s articles {(query.Equals("") ? "" : $"including `{query}` from now on!")}");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking articles with the specified query.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackNews([Remainder]string articleQuery)
            {
                if(Trackers["news"].TryRemoveTracker(articleQuery, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of articles including " + articleQuery + "!");
                else
                    await ReplyAsync($"Could not find tracker for `{articleQuery}`\n"+
                                     $"Currently tracked article queries are: ``{String.Join(", ", StaticBase.Trackers["news"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }

            [Command("GetTrackers")]
            [Summary("Returns the article queries that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following article queries are currently being tracked:\n``" + String.Join(", ", StaticBase.Trackers["news"].GetTrackers(Context.Channel.Id).Select(x => x.Name)) + "``");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a article was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string articleQuery, [Remainder]string notification)
            {
                if(StaticBase.Trackers["news"].TrySetNotification(articleQuery, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{articleQuery}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{articleQuery}`\n"+
                                     $"Currently tracked article queries are: ``{String.Join(", ", StaticBase.Trackers["news"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }
        }
        
        /*[Command("trackClips")]
        [Summary("Keeps track of clips from streams of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task trackClips(string streamerName)
        {
            ClipTracker.AddTracker(streamerName, Context.Channel.Id);

            await ReplyAsync("Keeping track of clips of " + streamerName + "'s streams, from now on!");
        }*/
    }
}