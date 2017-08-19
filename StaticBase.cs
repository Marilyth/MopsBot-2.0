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
        public static Module.Data.Statistics stats;
        public static Module.Data.UserScore people;
        public static Module.Data.Session.Poll poll;
        public static Module.Data.Session.Blackjack blackjack;
        public static Module.Data.StreamerList streamTracks;
        public static Module.Data.MeetUps meetups;
        public static Random ran = new Random();
        public static Module.Data.ClipTracker ClipTracker;
        public static Module.Data.OsuTracker osuTracker;
        public static List<Module.Data.Session.IdleDungeon> dungeonCrawler;
        public static List<ulong> BotManager;
        public static List<string> playlist;
        
        public StaticBase()
        {
            stats = new Module.Data.Statistics();
            people = new Module.Data.UserScore();
            streamTracks = new Module.Data.StreamerList();
            ClipTracker = new Module.Data.ClipTracker();
            osuTracker = new Module.Data.OsuTracker();
            dungeonCrawler = new List<Module.Data.Session.IdleDungeon>();
            
            meetups = new Module.Data.MeetUps();
            BotManager = new List<ulong>();
            playlist = new List<string>();

            Program.client.UserJoined += Client_UserJoined;
            Program.client.MessageReceived += Client_MessageReceived;
            
        }

        
        private async Task Client_MessageReceived(SocketMessage arg)
        {
            //Poll
            if (arg.Channel.Name.Contains((arg.Author.Username)) && poll != null)
            {
                if (poll.participants.ToList().Select(x => x.Id).ToArray().Contains(arg.Author.Id))
                {
                    poll.results[int.Parse(arg.Content) - 1]++;
                    await arg.Channel.SendMessageAsync("Vote accepted!");
                    poll.participants.RemoveAll(x => x.Id == arg.Author.Id);
                }

            }

            //Daily Statistics & User Experience
            if (!arg.Author.IsBot && !arg.Content.StartsWith("!"))
            {
                people.addStat(arg.Author.Id, arg.Content.Length, "experience");
                stats.addValue(arg.Content.Length);
            }
        }

        private async Task Client_UserJoined(SocketGuildUser User)
        {
            //PhunkRoyalServer Begruessung
            if (User.Guild.Id.Equals(205130885337448469))
                await User.Guild.GetTextChannel(305443055396192267).SendMessageAsync($"Willkommen im **{User.Guild.Name}** Server, {User.Mention}!" +
                $"\n\nBevor Du vollen Zugriff auf den Server hast, möchten wir Dich auf die Regeln des Servers hinweisen, die Du hier findest:" +
                $" {User.Guild.GetTextChannel(305443033296535552).Mention}\nSobald Du fertig bist, kannst Du Dich an einen unserer Moderatoren zu Deiner" +
                $" rechten wenden, die Dich alsbald zum Mitglied ernennen.\n\nHave a very mopsig day\nDein heimlicher Verehrer Mops");
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
