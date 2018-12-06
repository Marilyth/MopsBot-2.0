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
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using MopsBot.Data.Entities;
using MongoDB.Driver;
using Accord.Statistics.Distributions.Univariate;

namespace MopsBot.Data.Interactive
{
    public class ReactionGiveaway
    {

        //Key: Channel ID, Value: (Key: Message ID, Value: User IDs)
        public Dictionary<ulong, Dictionary<ulong, List<ulong>>> Giveaways = new Dictionary<ulong, Dictionary<ulong, List<ulong>>>();

        public ReactionGiveaway()
        {
            //using (StreamReader read = new StreamReader(new FileStream($"mopsdata//ReactionGiveaways.json", FileMode.OpenOrCreate)))
            //{
            try
            {
                //Giveaways = JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<ulong, List<ulong>>>>(read.ReadToEnd());
                //StaticBase.Database.GetCollection<MongoKVP<ulong, List<KeyValuePair<ulong, List<ulong>>>>>(this.GetType().Name).InsertMany(MongoKVP<ulong, List<KeyValuePair<ulong, List<ulong>>>>.DictToMongoKVP(Giveaways.ToDictionary(x => x.Key, x=> x.Value.ToList())));
                Giveaways = new Dictionary<ulong, Dictionary<ulong, List<ulong>>>(StaticBase.Database.GetCollection<MongoKVP<ulong, List<KeyValuePair<ulong, List<ulong>>>>>(this.GetType().Name).FindSync(x => true).ToList().Select(x => new KeyValuePair<ulong, Dictionary<ulong, List<ulong>>>(x.Key, x.Value.ToDictionary(y => y.Key, y => y.Value))));
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message + e.StackTrace);
            }
            //}
            Giveaways = Giveaways ?? new Dictionary<ulong, Dictionary<ulong, List<ulong>>>();

            foreach (var channel in Giveaways.ToList())
            {
                foreach (var message in channel.Value.ToList())
                {
                    try
                    {
                        var textmessage = (IUserMessage)((ITextChannel)Program.Client.GetChannel(channel.Key)).GetMessageAsync(message.Key).Result;
                        Program.ReactionHandler.AddHandler(textmessage, new Emoji("✅"), JoinGiveaway).Wait();
                        Program.ReactionHandler.AddHandler(textmessage, new Emoji("❎"), LeaveGiveaway).Wait();
                        Program.ReactionHandler.AddHandler(textmessage, new Emoji("🎁"), DrawGiveaway).Wait();

                        //Task.Run(async () =>
                        //{
                        foreach (var user in textmessage.GetReactionUsersAsync(new Emoji("✅"), 100).First().Result.Where(x => !x.IsBot))
                        {
                            JoinGiveaway(user.Id, textmessage);
                            textmessage.RemoveReactionAsync(new Emoji("✅"), user);
                        }
                        foreach (var user in textmessage.GetReactionUsersAsync(new Emoji("❎"), 100).First().Result.Where(x => !x.IsBot))
                        {
                            LeaveGiveaway(user.Id, textmessage);
                            textmessage.RemoveReactionAsync(new Emoji("❎"), user);
                        }
                        foreach (var user in textmessage.GetReactionUsersAsync(new Emoji("🎁"), 100).First().Result.Where(x => !x.IsBot))
                        {
                            DrawGiveaway(user.Id, textmessage);
                            textmessage.RemoveReactionAsync(new Emoji("🎁"), user);
                        }
                        //});
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\n" + $"[ERROR] by ReactionGiveaway for [{channel.Key}][{message.Key}] at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");

                        if ((e.Message.Contains("Object reference not set to an instance of an object.") || e.Message.Contains("Value cannot be null."))
                            && Program.Client.ConnectionState.Equals(ConnectionState.Connected))
                        {
                            Console.WriteLine("\n" + $"Removing Giveaway due to missing message: [{channel.Key}][{message.Key}]");

                            if (channel.Value.Count > 1)
                                channel.Value.Remove(message.Key);
                            else
                                Giveaways.Remove(channel.Key);
                        }
                    }
                }
            }
        }

        public async Task InsertIntoDBAsync(ulong key)
        {
            await StaticBase.Database.GetCollection<MongoKVP<ulong, List<KeyValuePair<ulong, List<ulong>>>>>(this.GetType().Name).InsertOneAsync(new MongoKVP<ulong, List<KeyValuePair<ulong, List<ulong>>>>(key, Giveaways[key].ToList()));
        }

        public async Task UpdateDBAsync(ulong key)
        {
            await StaticBase.Database.GetCollection<MongoKVP<ulong, List<KeyValuePair<ulong, List<ulong>>>>>(this.GetType().Name).ReplaceOneAsync(x => x.Key == key, new MongoKVP<ulong, List<KeyValuePair<ulong, List<ulong>>>>(key, Giveaways[key].ToList()));
        }

        public async Task RemoveFromDBAsync(ulong key)
        {
            await StaticBase.Database.GetCollection<MongoKVP<ulong, List<KeyValuePair<ulong, List<ulong>>>>>(this.GetType().Name).DeleteOneAsync(x => x.Key == key);
        }

        public async Task AddGiveaway(IMessageChannel channel, string name, IUser creator)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Title = name + " Giveaway!";
            e.Description = "To join/leave the giveaway, press the ✅/❎ Icons below this message!\n" +
                            "The Creator may draw a winner at any time, by pressing the 🎁 Icon.";
            e.Color = new Color(100, 100, 0);

            var author = new EmbedAuthorBuilder();
            author.Name = creator.Username;
            author.IconUrl = creator.GetAvatarUrl();

            e.Author = author;
            e.AddField("Participants", 0, true);
            e.AddField("Chance to win", Double.NaN, true);

            var message = await channel.SendMessageAsync("", embed: e.Build());
            await Program.ReactionHandler.AddHandler(message, new Emoji("✅"), JoinGiveaway);
            await Program.ReactionHandler.AddHandler(message, new Emoji("❎"), LeaveGiveaway);
            await Program.ReactionHandler.AddHandler(message, new Emoji("🎁"), DrawGiveaway);

            Dictionary<ulong, List<ulong>> messages = new Dictionary<ulong, List<ulong>>();
            List<ulong> participants = new List<ulong>();
            participants.Add(creator.Id);

            messages.Add(message.Id, participants);
            if (Giveaways.ContainsKey(channel.Id))
            {
                Giveaways[channel.Id].Add(message.Id, participants);
                await UpdateDBAsync(channel.Id);
            }
            else
            {
                Giveaways.Add(channel.Id, messages);
                await InsertIntoDBAsync(channel.Id);
            }
        }

        private async Task JoinGiveaway(ReactionHandlerContext context)
        {
            if (!Giveaways[context.Channel.Id][context.Message.Id].First().Equals(context.Reaction.UserId)
                && !Giveaways[context.Channel.Id][context.Message.Id].Contains(context.Reaction.UserId))
            {
                Giveaways[context.Channel.Id][context.Message.Id].Add(context.Reaction.UserId);
                await UpdateDBAsync(context.Channel.Id);
                await updateMessage(context.Message);
            }
        }

        private async Task JoinGiveaway(ulong userId, IUserMessage message)
        {
            if (!Giveaways[message.Channel.Id][message.Id].First().Equals(userId)
               && !Giveaways[message.Channel.Id][message.Id].Contains(userId))
            {
                Giveaways[message.Channel.Id][message.Id].Add(userId);
                await UpdateDBAsync(message.Channel.Id);
                await updateMessage(message);
            }
        }

        private async Task LeaveGiveaway(ReactionHandlerContext context)
        {
            if (!Giveaways[context.Channel.Id][context.Message.Id].First().Equals(context.Reaction.UserId))
            {
                Giveaways[context.Channel.Id][context.Message.Id].Remove(context.Reaction.UserId);
                await UpdateDBAsync(context.Channel.Id);
                await updateMessage(context.Message);
            }
        }

        private async Task LeaveGiveaway(ulong userId, IUserMessage message)
        {
            if (!Giveaways[message.Channel.Id][message.Id].First().Equals(userId))
            {
                Giveaways[message.Channel.Id][message.Id].Remove(userId);
                await UpdateDBAsync(message.Channel.Id);
                await updateMessage(message);
            }
        }

        private async Task DrawGiveaway(ReactionHandlerContext context)
        {
            if (context.Reaction.UserId.Equals(Giveaways[context.Channel.Id][context.Message.Id].First()))
            {
                await Program.ReactionHandler.ClearHandler(context.Message);

                int.TryParse(context.Message.Embeds.First().Title.Split("x")[0], out int winnerCount);
                string winnerDescription = "";

                if (winnerCount == 0) winnerCount = 1;
                if (winnerCount > Giveaways[context.Channel.Id][context.Message.Id].Count) winnerCount = Giveaways[context.Channel.Id][context.Message.Id].Count;

                for (int i = 0; i < winnerCount; i++)
                {
                    ulong winnerId = Giveaways[context.Channel.Id][context.Message.Id].Count > 1 ? Giveaways[context.Channel.Id][context.Message.Id]
                                   [StaticBase.ran.Next(1, Giveaways[context.Channel.Id][context.Message.Id].Count)]
                                   : context.Reaction.UserId;

                    IUser winner = await context.Channel.GetUserAsync(winnerId);
                    winnerDescription += $"{winner.Mention} won the "
                                       + $"`{context.Message.Embeds.First().Title}`\n";
                    Giveaways[context.Channel.Id][context.Message.Id].Remove(winnerId);
                }

                await context.Channel.SendMessageAsync(winnerDescription);

                var embed = context.Message.Embeds.First().ToEmbedBuilder().WithDescription(winnerDescription);
                await context.Message.ModifyAsync(x => x.Embed = embed.Build());

                if (Giveaways[context.Channel.Id].Count == 1)
                {
                    Giveaways.Remove(context.Channel.Id);
                    await RemoveFromDBAsync(context.Channel.Id);
                }
                else
                {
                    Giveaways[context.Channel.Id].Remove(context.Message.Id);
                    await UpdateDBAsync(context.Channel.Id);
                }
            }
        }

        private async Task DrawGiveaway(ulong userId, IUserMessage message)
        {
            if (userId.Equals(Giveaways[message.Channel.Id][message.Id].First()))
            {
                await Program.ReactionHandler.ClearHandler(message);

                int.TryParse(message.Embeds.First().Title.Split("x")[0], out int winnerCount);
                string winnerDescription = "";

                if (winnerCount == 0) winnerCount = 1;
                if (winnerCount > Giveaways[message.Channel.Id][message.Id].Count) winnerCount = Giveaways[message.Channel.Id][message.Id].Count;

                for (int i = 0; i < winnerCount; i++)
                {
                    ulong winnerId = Giveaways[message.Channel.Id][message.Id].Count > 1 ? Giveaways[message.Channel.Id][message.Id]
                                   [StaticBase.ran.Next(1, Giveaways[message.Channel.Id][message.Id].Count)]
                                   : userId;

                    IUser winner = await message.Channel.GetUserAsync(winnerId);
                    winnerDescription += $"{winner.Mention} won the "
                                       + $"`{message.Embeds.First().Title}`\n";
                    Giveaways[message.Channel.Id][message.Id].Remove(winnerId);
                }

                await message.Channel.SendMessageAsync(winnerDescription);

                var embed = message.Embeds.First().ToEmbedBuilder().WithDescription(winnerDescription);
                await message.ModifyAsync(x => x.Embed = embed.Build());

                if (Giveaways[message.Channel.Id].Count == 1)
                {
                    Giveaways.Remove(message.Channel.Id);
                    await RemoveFromDBAsync(message.Channel.Id);
                }
                else
                {
                    Giveaways[message.Channel.Id].Remove(message.Id);
                    await UpdateDBAsync(message.Channel.Id);
                }
            }
        }

        private async Task updateMessage(IUserMessage message)
        {
            var e = message.Embeds.First().ToEmbedBuilder();

            e.Color = new Color(100, 100, 0);

            foreach (EmbedFieldBuilder field in e.Fields)
            {
                if (field.Name.Equals("Participants"))
                    field.Value = Giveaways[message.Channel.Id][message.Id].Count - 1;
                else{
                    int.TryParse(message.Embeds.First().Title.Split("x")[0], out int winnerCount);

                    if (winnerCount == 0) winnerCount = 1;
                    if (winnerCount > Giveaways[message.Channel.Id][message.Id].Count - 1) winnerCount = Giveaways[message.Channel.Id][message.Id].Count - 1;

                    double probability = 1;
                    if(Giveaways[message.Channel.Id][message.Id].Count - 1 != 0)
                        probability = (1.0 / (Giveaways[message.Channel.Id][message.Id].Count - 1)) * winnerCount;

                    field.Value = Math.Round(probability*100, 2) + "%";
                }
            }

            await message.ModifyAsync(x =>
            {
                x.Embed = e.Build();
            });
        }
    }
}