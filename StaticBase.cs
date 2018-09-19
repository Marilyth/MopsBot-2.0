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
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;

namespace MopsBot
{
    public class StaticBase
    {
        public static MongoClient DatabaseClient = new MongoClient($"{Program.Config["DatabaseURL"]}");
        public static IMongoDatabase Database = DatabaseClient.GetDatabase("Mops");
        public static Data.UserHandler Users;
        public static Random ran = new Random();
        public static Gfycat.GfycatClient gfy;
        public static List<string> Playlist = new List<string>();

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public static Dictionary<ulong, string> GuildPrefix;
        
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public static Dictionary<ulong, Dictionary<string, string>> CustomCommands;
        public static ReactionGiveaway ReactGiveaways;
        public static ReactionRoleJoin ReactRoleJoin;
        public static ReactionPoll Poll;
        //public static Crosswords Crosswords;
        public static Dictionary<ITracker.TrackerType, TrackerWrapper> Trackers;
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
                    Users = new UserHandler();
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
                
                Trackers = new Dictionary<ITracker.TrackerType, Data.TrackerWrapper>();
                Trackers[ITracker.TrackerType.Osu] = new TrackerHandler<OsuTracker>();
                Trackers[ITracker.TrackerType.Overwatch] = new TrackerHandler<OverwatchTracker>();
                Trackers[ITracker.TrackerType.Twitch] = new TrackerHandler<TwitchTracker>();
                Trackers[ITracker.TrackerType.TwitchClips] = new TrackerHandler<TwitchClipTracker>();
                Trackers[ITracker.TrackerType.Twitter] = new TrackerHandler<TwitterTracker>();
                Trackers[ITracker.TrackerType.Youtube] = new TrackerHandler<YoutubeTracker>();
                Trackers[ITracker.TrackerType.Reddit] = new TrackerHandler<RedditTracker>();
                Trackers[ITracker.TrackerType.News] = new TrackerHandler<NewsTracker>();
                Trackers[ITracker.TrackerType.WoW] = new TrackerHandler<WoWTracker>();
                Trackers[ITracker.TrackerType.WoWGuild] = new TrackerHandler<WoWGuildTracker>();

                foreach (var tracker in Trackers)
                {
                    Task.Run(() => tracker.Value.PostInitialisation());
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
