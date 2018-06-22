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
                trackers["twitter"].AddTracker(twitterUser, Context.Channel.Id, tweetNotification.Contains('|') ? tweetNotification : tweetNotification + "|~Tweet Tweet~");

                await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter(string twitterUser)
            {
                if(trackers["twitter"].TryRemoveTracker(twitterUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + twitterUser + "'s tweets!");
                else
                    await ReplyAsync($"Could not find tracker for `{twitterUser}`\n"+
                                     $"Currently tracked Twitter Users are: ``{StaticBase.trackers["twitter"].GetTracker(Context.Channel.Id)}``");
            }

            [Command("GetTracks")]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following twitters are currently being tracked:\n``" + StaticBase.trackers["twitter"].GetTracker(Context.Channel.Id) + "``");
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
                trackers["osu"].AddTracker(OsuUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + OsuUser + "'s plays, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Osu player, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder]string OsuUser)
            {
                if(trackers["osu"].TryRemoveTracker(OsuUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + OsuUser + "'s plays!");
                else
                    await ReplyAsync($"Could not find tracker for `{OsuUser}`\n"+
                                     $"Currently tracked osu! players are: ``{StaticBase.trackers["osu"].GetTracker(Context.Channel.Id)}``");
            }

            [Command("GetTracks")]
            [Summary("Returns the Osu players that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following Osu players are currently being tracked:\n``" + StaticBase.trackers["osu"].GetTracker(Context.Channel.Id) + "``");
            }
        }

        [Group("Youtube")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Youtube : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackTwitter(string channelID)
            {
                trackers["youtube"].AddTracker(channelID, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + channelID + "'s videos, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(string channelID)
            {
                if(trackers["youtube"].TryRemoveTracker(channelID, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + channelID + "'s videos!");
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n"+
                                     $"Currently tracked Youtubers are: ``{StaticBase.trackers["youtube"].GetTracker(Context.Channel.Id)}``");
            }

            [Command("GetTracks")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:\n``" + StaticBase.trackers["youtube"].GetTracker(Context.Channel.Id) + "``");
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
                trackers["twitch"].AddTracker(streamerName, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(string streamerName)
            {
                if(trackers["twitch"].TryRemoveTracker(streamerName, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName + "'s streams!");
                else
                    await ReplyAsync($"Could not find tracker for `{streamerName}`\n"+
                                     $"Currently tracked Streamers are: ``{StaticBase.trackers["twitch"].GetTracker(Context.Channel.Id)}``");
            }

            [Command("GetTracks")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following streamers are currently being tracked:\n``" + StaticBase.trackers["twitch"].GetTracker(Context.Channel.Id) + "``");
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
                trackers["reddit"].AddTracker(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                await ReplyAsync("Keeping track of " + subreddit + $"'s posts, from now on, using {query}!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified Subreddit.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackSubreddit(string subreddit, string query = null)
            {
                if(trackers["reddit"].TryRemoveTracker(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + subreddit + "'s posts!");
                else
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n"+
                                     $"Currently tracked Subreddits are: ``{StaticBase.trackers["reddit"].GetTracker(Context.Channel.Id)}``");
            }

            [Command("GetTracks")]
            [Summary("Returns the subreddits that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following subreddits are currently being tracked:\n``" + StaticBase.trackers["reddit"].GetTracker(Context.Channel.Id) + "``");
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
                trackers["overwatch"].AddTracker(owUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOW(string owUser)
            {
                if(trackers["overwatch"].TryRemoveTracker(owUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + owUser + "'s stats!");
                else
                    await ReplyAsync($"Could not find tracker for `{owUser}`\n"+
                                     $"Currently tracked players are: ``{StaticBase.trackers["overwatch"].GetTracker(Context.Channel.Id)}``");
            }

            [Command("GetStats")]
            [Summary("Returns an embed representing the stats of the specified Overwatch player")]
            public async Task GetStats(string owUser)
            {
                await ReplyAsync("Stats fetched:", false, await Data.Tracker.OverwatchTracker.overwatchInformation(owUser));
            }

            [Command("GetTracks")]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following players are currently being tracked:\n``" + StaticBase.trackers["overwatch"].GetTracker(Context.Channel.Id) + "``");
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
                trackers["news"].AddTracker(String.Join("|", new string[] { source, query }), Context.Channel.Id);
                await ReplyAsync($"Keeping track of `{source}`'s articles {(query.Equals("") ? "" : $"including `{query}` from now on!")}");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking articles with the specified query.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackNews([Remainder]string articleQuery)
            {
                if(trackers["news"].TryRemoveTracker(articleQuery, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of articles including " + articleQuery + "!");
                else
                    await ReplyAsync($"Could not find tracker for `{articleQuery}`\n"+
                                     $"Currently tracked article queries are: ``{StaticBase.trackers["news"].GetTracker(Context.Channel.Id)}``");
            }

            [Command("GetTracks")]
            [Summary("Returns the article queries that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following article queries are currently being tracked:\n``" + StaticBase.trackers["news"].GetTracker(Context.Channel.Id) + "``");
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