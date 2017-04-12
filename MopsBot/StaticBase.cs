using System;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot
{
    class StaticBase
    {
        public static Module.Data.Statistics stats;
        public static Module.Data.TextInformation info;
        public static Module.Data.UserScore people;
        public static Module.Data.Session.Poll poll;

        public StaticBase()
        {
            stats = new Module.Data.Statistics();
            info = new Module.Data.TextInformation();
            people = new Module.Data.UserScore();

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
    }
}
