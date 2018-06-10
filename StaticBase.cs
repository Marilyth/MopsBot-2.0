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
        public static List<string> playlist = new List<string>();
        public static HashSet<ulong> MemberSet;
        public static Dictionary<ulong, string> guildPrefix;
        public static Giveaway Giveaways = new Giveaway();
        public static ReactionGiveaway ReactGiveaways;
        public static Poll poll;
        public static Blackjack blackjack;
        public static Crosswords crosswords;
        public static ClipTracker ClipTracker;
        public static Dictionary<string, TrackerWrapper> trackers;
        public static MuteTimeHandler MuteHandler;

        public static bool init = false;

        /// <summary>
        /// Initialises the Twitch, Twitter and Overwatch trackers
        /// </summary>
        public static void initTracking()
        {
            if (!init)
            {
                ReactGiveaways = new ReactionGiveaway();

                Auth.SetUserCredentials(Program.Config["TwitterKey"], Program.Config["TwitterSecret"],
                                        Program.Config["TwitterToken"], Program.Config["TwitterAccessSecret"]);
                TweetinviConfig.CurrentThreadSettings.TweetMode = TweetMode.Extended;
                TweetinviConfig.ApplicationSettings.TweetMode = TweetMode.Extended;
                gfy = new Gfycat.GfycatClient(Program.Config["GfyID"], Program.Config["GfySecret"]);

                ClipTracker = new ClipTracker();

                using (StreamReader read = new StreamReader(new FileStream($"mopsdata//MuteTimerHandler.json", FileMode.OpenOrCreate)))
                {
                    try{
                        MuteHandler = Newtonsoft.Json.JsonConvert.DeserializeObject<MuteTimeHandler>(read.ReadToEnd());
                        
                        if(MuteHandler == null)
                            MuteHandler = new MuteTimeHandler();
                        
                        MuteHandler.SetTimers();
                    } catch(Exception e){
                        Console.WriteLine(e.Message + e.StackTrace);
                    }
                }
                trackers = new Dictionary<string, Data.TrackerWrapper>();
                trackers["osu"] = new TrackerHandler<OsuTracker>();
                //trackers["overwatch"] = new TrackerHandler<OverwatchTracker>();
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

        public static async Task UpdateGameAsync()
        {
            MemberSet = new HashSet<ulong>();
            await Program.client.DownloadUsersAsync(Program.client.Guilds);
            foreach (SocketGuild curGuild in Program.client.Guilds)
            {
                foreach (SocketGuildUser curUser in curGuild.Users)
                {
                    if (!curUser.IsBot)
                        MemberSet.Add(curUser.Id);
                }
            }
            await Program.client.SetActivityAsync(new Game($"{MemberSet.Count} people", ActivityType.Listening));
        }
    }
}
