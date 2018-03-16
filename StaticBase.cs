using System;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MopsBot
{
    public class StaticBase
    {
        public static Data.Statistics stats = new Data.Statistics();
        public static Data.UserScore people = new Data.UserScore();
        public static Data.MeetUps meetups = new Data.MeetUps();
        public static Random ran = new Random();
        public static List<Data.Session.IdleDungeon> dungeonCrawler = new List<Data.Session.IdleDungeon>();
        public static List<ulong> BotManager = new List<ulong>();
        public static List<string> playlist = new List<string>();
        public static Dictionary<ulong, string> guildPrefix;
        public static Data.Session.Poll poll;
        public static Data.Session.Blackjack blackjack;
        public static Data.Session.Crosswords crosswords;
        public static Data.ClipTracker ClipTracker;
        //public static Data.OsuTracker osuTracker;
        public static Data.StreamerList streamTracks;
        public static Data.TwitterList twitterTracks;
        public static Data.OverwatchList OverwatchTracks;
        public static Data.TrackerHandler TrackerHandle;

        public static bool init = false;

        /// <summary>
        /// Initialises the Twitch, Twitter and Overwatch trackers
        /// </summary>
        public static void initTracking()
        {
            if (!init)
            {
                TrackerHandle = new Data.TrackerHandler();
                streamTracks = new Data.StreamerList();
                twitterTracks = new Data.TwitterList();
                ClipTracker = new Data.ClipTracker();
                OverwatchTracks = new Data.OverwatchList();
                //osuTracker = new Data.OsuTracker();        

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
    }
}
