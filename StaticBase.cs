using System;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MopsBot.Data;
using MopsBot.Data.Tracker;
using MopsBot.Data.Updater;
using Tweetinvi;

namespace MopsBot
{
    public class StaticBase
    {
        public static Data.Statistics stats = new Data.Statistics();
        public static Data.UserScore people = new Data.UserScore();
        public static Random ran = new Random();
        public static List<IdleDungeon> dungeonCrawler = new List<IdleDungeon>();
        public static Gfycat.GfycatClient gfy;
        public static List<ulong> BotManager = new List<ulong>();
        public static List<string> playlist = new List<string>();
        public static HashSet<ulong> MemberSet;
        public static Dictionary<ulong, string> guildPrefix;
        public static Dictionary<string, HashSet<ulong>> GiveAways = new Dictionary<string, HashSet<ulong>>();
        public static Poll poll;
        public static Blackjack blackjack;
        public static Crosswords crosswords;
        public static ClipTracker ClipTracker;/*
        public static TrackerHandler<OsuTracker> osuTracker;
        public static TrackerHandler<TwitchTracker> streamTracks;
        public static TrackerHandler<TwitterTracker> twitterTracks;
        public static TrackerHandler<OverwatchTracker> OverwatchTracks;
        public static TrackerHandler<YoutubeTracker> YoutubeTracks;
*/
        public static Dictionary<string, TrackerWrapper> trackers;

        public static bool init = false;

        /// <summary>
        /// Initialises the Twitch, Twitter and Overwatch trackers
        /// </summary>
        public static void initTracking()
        {
            if (!init)
            {
                Auth.SetUserCredentials(Program.twitterAuth[0], Program.twitterAuth[1], Program.twitterAuth[2], Program.twitterAuth[3]);
                TweetinviConfig.CurrentThreadSettings.TweetMode = TweetMode.Extended;
                TweetinviConfig.ApplicationSettings.TweetMode = TweetMode.Extended;
                StaticBase.gfy = new Gfycat.GfycatClient(Program.gfyAuth[0], Program.gfyAuth[1]);

/*
                OverwatchTracks = new TrackerHandler<OverwatchTracker>();
                streamTracks = new TrackerHandler<TwitchTracker>();
                twitterTracks = new TrackerHandler<TwitterTracker>();
                YoutubeTracks = new TrackerHandler<YoutubeTracker>();
                osuTracker = new TrackerHandler<OsuTracker>(); */
                ClipTracker = new ClipTracker();
                
                trackers = new Dictionary<string, Data.TrackerWrapper>();
                trackers["osu"] = new TrackerHandler<OsuTracker>();
                trackers["overwatch"] = new TrackerHandler<OverwatchTracker>();
                trackers["twitch"] = new TrackerHandler<TwitchTracker>();
                trackers["twitter"] = new TrackerHandler<TwitterTracker>();
                trackers["youtube"] = new TrackerHandler<YoutubeTracker>();
                trackers["reddit"] = new TrackerHandler<RedditTracker>();

                init = true;

            }
        }

        public static void savePrefix()
        {
            using (StreamWriter write = new StreamWriter(new FileStream("mopsdata//guildprefixes.txt", FileMode.Create)))
            {
                write.AutoFlush = true;
                foreach (var kv in guildPrefix)
                {
                    write.WriteLine($"{kv.Key}|{kv.Value}");
                }
            }
        }

        public static void disconnected()
        {
            /*
            if(init){
                streamTracks.Dispose();
                twitterTracks.Dispose();
                ClipTracker.Dispose();
                OverwatchTracks.Dispose();
            }*/
        }

        /// <summary>
        /// Finds out who was mentioned in a command
        /// </summary>
        /// <param name="Context">The CommandContext containing the command message </param>
        /// <returns>A List of IGuildUsers representing the mentioned Users</returns>
        public static List<IGuildUser> getMentionedUsers(CommandContext Context)
        {
            List<IGuildUser> user = Context.Message.MentionedUserIds.Select(id => Context.Guild.GetUserAsync(id).Result).ToList();

            foreach (var a in Context.Message.MentionedRoleIds.Select(id => Context.Guild.GetRole(id)))
            {
                user.AddRange(Context.Guild.GetUsersAsync().Result.Where(u => u.RoleIds.Contains(a.Id)));
            }

            if (Context.Message.Tags.Select(t => t.Type).Contains(TagType.EveryoneMention))
            {
                user.AddRange(Context.Guild.GetUsersAsync().Result);
            }
            if (Context.Message.Tags.Select(t => t.Type).Contains(TagType.HereMention))
            {
                user.AddRange(Context.Guild.GetUsersAsync().Result.Where(u => u.Status.Equals(UserStatus.Online)));
            }
            return new List<IGuildUser>(user.Distinct());
        }

        public static async Task UpdateGameAsync(){
            MemberSet = new HashSet<ulong>();
            await Program.client.DownloadUsersAsync(Program.client.Guilds);
            foreach(SocketGuild curGuild in Program.client.Guilds){
                foreach(SocketGuildUser curUser in curGuild.Users){
                    MemberSet.Add(curUser.Id);
                }
            }
            await Program.client.SetGameAsync($"{MemberSet.Count} people", null, StreamType.NotStreaming + 2);
        }
    }
}
