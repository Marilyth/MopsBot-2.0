using System;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using MopsBot.Data;
using MopsBot.Data.Tracker;
using MopsBot.Data.Updater;
using Tweetinvi;
using NewsAPI;
using WowDotNetAPI;

namespace MopsBot
{
    public class StaticBase
    {
        //public static Data.UserScore people = new Data.UserScore();
        public static Random ran = new Random();
        //public static List<IdleDungeon> DungeonCrawler = new List<IdleDungeon>();
        public static Gfycat.GfycatClient gfy;
        public static List<string> Playlist = new List<string>();
        public static HashSet<ulong> MemberSet;
        public static Dictionary<ulong, string> GuildPrefix;
        public static Dictionary<ulong, Dictionary<string, string>> CustomCommands;
        public static Giveaway Giveaways = new Giveaway();
        public static ReactionGiveaway ReactGiveaways;
        public static ReactionRoleJoin ReactRoleJoin;
        public static ReactionPoll Poll;
        //public static Crosswords Crosswords;
        public static Dictionary<string, TrackerWrapper> Trackers;
        public static MuteTimeHandler MuteHandler;
        public static NewsApiClient NewsClient;

        public static bool init = false;

        /// <summary>
        /// Initialises and loads all trackers
        /// </summary>
        public static void initTracking()
        {
            if (!init)
            {
                Task.Run(() =>
                {
                    ReactGiveaways = new ReactionGiveaway();
                    ReactRoleJoin = new ReactionRoleJoin();
                    Poll = new ReactionPoll();
                });

                Auth.SetUserCredentials(Program.Config["TwitterKey"], Program.Config["TwitterSecret"],
                                        Program.Config["TwitterToken"], Program.Config["TwitterAccessSecret"]);
                TweetinviConfig.CurrentThreadSettings.TweetMode = TweetMode.Extended;
                TweetinviConfig.ApplicationSettings.TweetMode = TweetMode.Extended;
                Tweetinvi.ExceptionHandler.SwallowWebExceptions = false;
                Tweetinvi.RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackOnly;
                TweetinviEvents.QueryBeforeExecute += Data.Tracker.TwitterTracker.QueryBeforeExecute;

                gfy = new Gfycat.GfycatClient(Program.Config["GfyID"], Program.Config["GfySecret"]);
                
                NewsClient = new NewsApiClient(Program.Config["NewsAPI"]);

                WoWTracker.WoWClient = new WowExplorer(Region.EU, Locale.en_GB, Program.Config["WoWKey"]);

                using (StreamReader read = new StreamReader(new FileStream($"mopsdata//MuteTimerHandler.json", FileMode.OpenOrCreate)))
                {
                    try
                    {
                        MuteHandler = Newtonsoft.Json.JsonConvert.DeserializeObject<MuteTimeHandler>(read.ReadToEnd());

                        if (MuteHandler == null)
                            MuteHandler = new MuteTimeHandler();

                        MuteHandler.SetTimers();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\n" +  e.Message + e.StackTrace);
                    }
                }
                Trackers = new Dictionary<string, Data.TrackerWrapper>();
                Trackers["osu"] = new TrackerHandler<OsuTracker>();
                Trackers["overwatch"] = new TrackerHandler<OverwatchTracker>();
                Trackers["twitch"] = new TrackerHandler<TwitchTracker>();
                Trackers["twitchclips"] = new TrackerHandler<TwitchClipTracker>();
                Trackers["twitter"] = new TrackerHandler<TwitterTracker>();
                Trackers["youtube"] = new TrackerHandler<YoutubeTracker>();
                Trackers["reddit"] = new TrackerHandler<RedditTracker>();
                Trackers["news"] = new TrackerHandler<NewsTracker>();
                Trackers["wow"] = new TrackerHandler<WoWTracker>();
                Trackers["wowguild"] = new TrackerHandler<WoWGuildTracker>();

                foreach (var tracker in Trackers)
                {
                    Task.Run(() => tracker.Value.postInitialisation());
                }

                init = true;

            }
        }

        /// <summary>
        /// Writes all guildprefixes into a file.
        /// </summary>
        public static void savePrefix()
        {
            using (StreamWriter write = new StreamWriter(new FileStream("mopsdata//guildprefixes.txt", FileMode.Create)))
            {
                write.AutoFlush = true;
                foreach (var kv in GuildPrefix)
                {
                    write.WriteLine($"{kv.Key}|{kv.Value}");
                }
            }
        }

        /// <summary>
        /// Writes all custom commands into a file.
        /// </summary>
        public static void saveCommand()
        {
            using (StreamWriter write = new StreamWriter(new FileStream("mopsdata//CustomCommands.json", FileMode.Create)))
            {
                write.WriteLine(JsonConvert.SerializeObject(CustomCommands, Formatting.Indented));
            }
        }

        /// <summary>
        /// Updates the guild count, by displaying it as an activity.
        /// </summary>
        /// <returns>A Task that sets the activity</returns>
        public static async Task UpdateGameAsync()
        {
            await Program.Client.SetActivityAsync(new Game($"{Program.Client.Guilds.Count} servers", ActivityType.Watching));
        }
    }
}
