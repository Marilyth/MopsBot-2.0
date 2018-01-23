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
    class StaticBase
    {
        public static Module.Data.Statistics stats = new Module.Data.Statistics();
        public static Module.Data.UserScore people = new Module.Data.UserScore();
        public static Module.Data.MeetUps meetups = new Module.Data.MeetUps();
        public static Random ran = new Random();
        public static List<Module.Data.Session.IdleDungeon> dungeonCrawler = new List<Module.Data.Session.IdleDungeon>();
        public static List<ulong> BotManager = new List<ulong>();
        public static List<string> playlist = new List<string>();
        public static Module.Data.Session.Poll poll;
        public static Module.Data.Session.Blackjack blackjack;
        public static Module.Data.ClipTracker ClipTracker;
        public static Module.Data.OsuTracker osuTracker;
        public static Module.Data.StreamerList streamTracks;
        public static Module.Data.TwitterList twitterTracks;
        public static Module.Data.OverwatchList OverwatchTracks;
        
        public static bool init = false;

        public static void initTracking()
        {
            streamTracks = new Module.Data.StreamerList();
            twitterTracks = new Module.Data.TwitterList();
            ClipTracker = new Module.Data.ClipTracker();
            OverwatchTracks = new Module.Data.OverwatchList();
            //osuTracker = new Module.Data.OsuTracker();        

            init = true;
        }

        public static void disconnected(){
            if(init){
                streamTracks.Dispose();
                twitterTracks.Dispose();
                ClipTracker.Dispose();
                OverwatchTracks.Dispose();
            }
        }

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