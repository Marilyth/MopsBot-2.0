using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace MopsBot.Data
{
    public class ReactionPoll
    {
        //Key: Channel ID, Value: Message IDs
        public Dictionary<ulong, List<Poll>> Polls = new Dictionary<ulong, List<Poll>>();

        public ReactionPoll()
        {
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//ReactionPoll.json", FileMode.OpenOrCreate)))
            {
                try
                {
                    Polls = JsonConvert.DeserializeObject<Dictionary<ulong, List<Poll>>>(read.ReadToEnd());
                    if (Polls == null)
                    {
                        Polls = new Dictionary<ulong, List<Poll>>();
                    }
                    foreach (var channel in Polls)
                    {
                        foreach (var poll in channel.Value)
                        {
                            poll.CreateChart();
                            var textmessage = (IUserMessage)((ITextChannel)Program.Client.GetChannel(channel.Key)).GetMessageAsync(poll.MessageID).Result;

                            for (int i = 0; i < poll.Options.Length; i++)
                            {
                                var option = poll.Options[i];
                                Program.ReactionHandler.AddHandler(textmessage, EmojiDict[i], x => AddVote(x, option)).Wait();
                            }

                            Program.ReactionHandler.AddHandler(textmessage, new Emoji("🗑"), DeletePoll).Wait();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + e.StackTrace);
                }
            }
        }

        public void SaveJson()
        {
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//ReactionPoll.json", FileMode.Create)))
                write.Write(JsonConvert.SerializeObject(Polls, Formatting.Indented));
        }

        public async Task AddPoll(ITextChannel channel, Poll poll)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Title = poll.Question + $" Poll";
            e.Description = $"To vote for an option, press the corresponding digit reactions.\n" +
                            "If you can manage channels, you may delete this poll by pressing the 🗑 Icon.";
            e.Color = Color.Blue;
            e.WithCurrentTimestamp();
            e.WithFooter(x => x.WithIconUrl("http://thebullelephant.com/wp-content/uploads/2016/10/poll-box-1.png").WithText("Poll"));
            //e.WithImageUrl(poll.GetChartURI());

            StringBuilder optionText = new StringBuilder();
            for (int i = 0; i < poll.Options.Length; i++)
            {
                optionText.AppendLine(EmojiDict[i].Name + " : " + poll.Options[i]);
            }
            e.AddField("Options", optionText);

            var message = await channel.SendMessageAsync("", embed: e.Build());
            for (int i = 0; i < poll.Options.Length; i++)
            {
                var option = poll.Options[i];
                Program.ReactionHandler.AddHandler(message, EmojiDict[i], x => AddVote(x, option)).Wait();
            }
            await Program.ReactionHandler.AddHandler(message, new Emoji("🗑"), DeletePoll);

            if (Polls.ContainsKey(channel.Id))
                Polls[channel.Id].Add(poll);
            else
            {
                Polls.Add(channel.Id, new List<Poll> { poll });
            }

            poll.MessageID = message.Id;
            poll.CreateChart(false);
            await updateMessage(message, poll);

            SaveJson();
        }

        public async Task AddVote(ReactionHandlerContext context, string option)
        {
            var poll = Polls[context.channel.Id].First(x => x.MessageID.Equals(context.message.Id));
            if(!poll.Voters.ContainsKey(context.reaction.UserId)){
                poll.AddValue(option, 1);
                poll.Voters.Add(context.reaction.UserId, option);
            }
            else{
                poll.AddValue(option, 1);
                poll.AddValue(poll.Voters[context.reaction.UserId], -1);
                poll.Voters[context.reaction.UserId] = option;
            }

            SaveJson();
            await updateMessage(context, poll);
        }

        private async Task DeletePoll(ReactionHandlerContext context)
        {
            var user = await ((ITextChannel)context.channel).Guild.GetUserAsync(context.reaction.UserId);
            if (user.GuildPermissions.ManageChannels)
            {
                await Program.ReactionHandler.ClearHandler(context.message);

                if (Polls[context.channel.Id].Count > 1)
                    Polls[context.channel.Id].RemoveAll(x => x.MessageID == context.message.Id);
                else
                    Polls.Remove(context.channel.Id);

                SaveJson();
            }
        }

        private async Task updateMessage(ReactionHandlerContext context, Poll poll)
        {
            var e = context.message.Embeds.First().ToEmbedBuilder();

            e.WithImageUrl(poll.GetChartURI());
            

            await context.message.ModifyAsync(x =>
            {
                x.Embed = e.Build();
            });
        }

        private async Task updateMessage(IUserMessage message, Poll poll)
        {
            var e = message.Embeds.First().ToEmbedBuilder();

            e.WithImageUrl(poll.GetChartURI());
            

            await message.ModifyAsync(x =>
            {
                x.Embed = e.Build();
            });
        }

        public static Dictionary<int, Emoji> EmojiDict = new Dictionary<int, Emoji>{
            {0, new Emoji("\u0030\u20E3")},
            {1, new Emoji("\u0031\u20E3")},
            {2, new Emoji("\u0032\u20E3")},
            {3, new Emoji("\u0033\u20E3")},
            {4, new Emoji("\u0034\u20E3")},
            {5, new Emoji("\u0035\u20E3")},
            {6, new Emoji("\u0036\u20E3")},
            {7, new Emoji("\u0037\u20E3")},
            {8, new Emoji("\u0038\u20E3")},
            {9, new Emoji("\u0039\u20E3")},
        };
    }

    public class Poll
    {
        public Dictionary<ulong, string> Voters;
        public string[] Options;
        public string Question;
        public ulong MessageID;
        private BarPlot chart;

        public Poll(string question, params string[] options)
        {
            Options = options;
            Question = question;
            Voters = new Dictionary<ulong, string>();
        }

        public void CreateChart(bool alreadyExists = true){
            chart = new BarPlot(Question+MessageID, alreadyExists, Options);
        }

        public void AddValue(string option, double value){
            chart.AddValue(option, value);
        }

        public string GetChartURI(){
            return chart.DrawPlot();
        }
    }
}