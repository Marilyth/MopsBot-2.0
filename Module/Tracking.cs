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
                trackers["twitter"].addTracker(twitterUser, Context.Channel.Id, tweetNotification.Contains('|') ? tweetNotification : tweetNotification + "|~Tweet Tweet~");

                await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter(string twitterUser)
            {
                trackers["twitter"].removeTracker(twitterUser, Context.Channel.Id);

                await ReplyAsync("Stopped keeping track of " + twitterUser + "'s tweets!");
            }

            [Command("GetTracks")]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following twitters are currently being tracked:\n``" + StaticBase.trackers["twitter"].getTracker(Context.Channel.Id) + "``");
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
                trackers["osu"].addTracker(OsuUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + OsuUser + "'s plays, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Osu player, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder]string OsuUser)
            {
                trackers["osu"].removeTracker(OsuUser, Context.Channel.Id);

                await ReplyAsync("Stopped keeping track of " + OsuUser + "'s plays!");
            }

            [Command("GetTracks")]
            [Summary("Returns the Osu players that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following Osu players are currently being tracked:\n``" + StaticBase.trackers["osu"].getTracker(Context.Channel.Id) + "``");
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
                trackers["youtube"].addTracker(channelID, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + channelID + "'s videos, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(string channelID)
            {
                trackers["youtube"].removeTracker(channelID, Context.Channel.Id);

                await ReplyAsync("Stopped keeping track of " + channelID + "'s videos!");
            }

            [Command("GetTracks")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:\n``" + StaticBase.trackers["youtube"].getTracker(Context.Channel.Id) + "``");
            }
        }
        [Group("Twitch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public class Twitch : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage = "Stream went live!")
            {
                trackers["twitch"].addTracker(streamerName, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(string streamerName)
            {
                trackers["twitch"].removeTracker(streamerName, Context.Channel.Id);

                await ReplyAsync("Stopped tracking " + streamerName + "'s streams!");
            }

            [Command("GetTracks")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following streamers are currently being tracked:\n``" + StaticBase.trackers["twitch"].getTracker(Context.Channel.Id) + "``");
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
            [Hide]
            public async Task trackSubreddit(string subreddit, string query = null)
            {
                trackers["reddit"].addTracker(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                await ReplyAsync("Keeping track of " + subreddit + $"'s posts, from now on, using {query}!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified Subreddit.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackSubreddit(string subreddit, string query = null)
            {
                trackers["reddit"].removeTracker(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                await ReplyAsync("Stopped tracking " + subreddit + "'s posts!");
            }

            [Command("GetTracks")]
            [Summary("Returns the subreddits that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following subreddits are currently being tracked:\n``" + StaticBase.trackers["reddit"].getTracker(Context.Channel.Id) + "``");
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
                trackers["overwatch"].addTracker(owUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOW(string owUser)
            {
                trackers["overwatch"].removeTracker(owUser, Context.Channel.Id);

                await ReplyAsync("Stopped keeping track of " + owUser + "'s stats!");
            }

            [Command("GetStats")]
            [Summary("Returns an embed representating the stats of the specified Overwatch player")]
            public async Task GetStats(string owUser)
            {
                await ReplyAsync("Stats fetched:", false, await Data.Tracker.OverwatchTracker.overwatchInformation(owUser));
            }

            [Command("GetTracks")]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following players are currently being tracked:\n``" + StaticBase.trackers["overwatch"].getTracker(Context.Channel.Id) + "``");
            }
        }

        
        [Command("trackClips")]
        [Summary("Keeps track of clips from streams of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task trackClips(string streamerName)
        {
            ClipTracker.addTracker(streamerName, Context.Channel.Id);

            await ReplyAsync("Keeping track of clips of " + streamerName + "'s streams, from now on!");
        }
    }
}