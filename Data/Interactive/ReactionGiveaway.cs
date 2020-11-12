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
using MopsBot.Api;
using MongoDB.Driver;

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
                Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"", e)).Wait();
            }
            //}
            Giveaways = Giveaways ?? new Dictionary<ulong, Dictionary<ulong, List<ulong>>>();
            bool doPrune = false;

            foreach (var channel in Giveaways.ToList())
            {
                foreach (var message in channel.Value.ToList())
                {
                    try
                    {
                        var textmessage = (IUserMessage)((ITextChannel)Program.Client.GetChannel(channel.Key)).GetMessageAsync(message.Key).Result;

                        var join = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("✅"), JoinGiveaway, false);
                        var leave = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("✅"), LeaveGiveaway, true);
                        var draw = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("🎁"), DrawGiveaway, false);

                        if(textmessage == null) throw new Exception("Message could not be loaded!");

                        Program.ReactionHandler.AddHandlers(textmessage, join, leave, draw).Wait();
                    }
                    catch (Exception e)
                    {
                        doPrune = true;
                        Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by [{channel.Key}][{message.Key}]", e)).Wait();

                        if (e.Message.Contains("Message could not be loaded") && Program.Client.Shards.All(x => x.ConnectionState.Equals(ConnectionState.Connected)))
                        {
                            Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"Removing [{channel.Key}][{message.Key}] due to missing message.")).Wait();
                        }
                    }
                }
            }

            if(doPrune){
                TryPruneAsync(false).Wait();
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
            e.Description = "To join/leave the giveaway, add/remove the ✅ Icon below this message!\n" +
                            "The Creator may draw a winner at any time, by pressing the 🎁 Icon.";
            e.Color = new Color(100, 100, 0);

            var author = new EmbedAuthorBuilder();
            author.Name = creator.Username;
            author.IconUrl = creator.GetAvatarUrl();

            e.Author = author;
            e.AddField("Participants", 0, true);
            e.AddField("Chance to win", Double.NaN, true);

            Dictionary<ulong, List<ulong>> messages = new Dictionary<ulong, List<ulong>>();
            List<ulong> participants = new List<ulong>();
            participants.Add(creator.Id);

            var message = await channel.SendMessageAsync("", embed: e.Build());
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

            var join = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("✅"), JoinGiveaway, false);
            var leave = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("✅"), LeaveGiveaway, true);
            var draw = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("🎁"), DrawGiveaway, false);

            await Program.ReactionHandler.AddHandlers(message, join, leave, draw);
        }

        private async Task JoinGiveaway(ReactionHandlerContext context)
        {
            await JoinGiveaway(context.Reaction.UserId, context.Message);
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
            await LeaveGiveaway(context.Reaction.UserId, context.Message);
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
            await DrawGiveaway(context.Reaction.UserId, context.Message);
        }

        private async Task DrawGiveaway(ulong userId, IUserMessage message)
        {
            if (userId.Equals(Giveaways[message.Channel.Id][message.Id].First()))
            {
                var limit = message.Reactions[new Emoji("✅")].ReactionCount;
                var participants = message.GetReactionUsersAsync(new Emoji("✅"), limit).FlattenAsync().Result.Where(x => !x.IsBot);
                await Program.ReactionHandler.ClearHandler(message);

                int.TryParse(message.Embeds.First().Title.Split("x")[0], out int winnerCount);
                string winnerDescription = "";

                //Remove any duplicates to race-conditions
                var participantsDraw = participants.ToHashSet().Select(x => x.Id).ToList();

                if (winnerCount <= 0) winnerCount = 1;
                if (winnerCount > participantsDraw.Count) winnerCount = participantsDraw.Count;

                for (int i = 0; i < winnerCount; i++)
                {
                    var index = StaticBase.ran.Next(0, participantsDraw.Count);

                    ulong winnerId = participantsDraw[index];

                    var winner = await Program.Client.Rest.GetUserAsync(winnerId);
                    winnerDescription += $"{winner.Mention} won the "
                                       + $"`{message.Embeds.First().Title}`\n";

                    participantsDraw.RemoveAt(index);
                }

                if(string.IsNullOrEmpty(winnerDescription))
                    winnerDescription = "No winners could be drawn.";
                
                var winners = winnerDescription.Split("\n");
                var messageText = "";
                for(int i = 0; i < winners.Count(); i++){
                    if(messageText.Count() + winners[i].Count() <= 2000)
                        messageText += winners[i] + "\n";
                    else{
                        await message.Channel.SendMessageAsync(messageText);
                        messageText = "";
                    }
                }
                if(messageText.Count() > 0)
                    await message.Channel.SendMessageAsync(messageText);

                var embed = message.Embeds.First().ToEmbedBuilder().WithDescription("Giveaway has ended");
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

        private Dictionary<ulong, bool> updating = new Dictionary<ulong, bool>();
        private async Task updateMessage(IUserMessage message)
        {
            if(!updating.ContainsKey(message.Id)) updating.Add(message.Id, false);

            if (!updating[message.Id])
            {
                updating[message.Id] = true;
                await Task.Delay(10000);
                updating[message.Id] = false;
                var e = message.Embeds.First().ToEmbedBuilder();

                e.Color = new Color(100, 100, 0);

                var participants = message.GetReactionUsersAsync(new Emoji("✅"), message.Reactions[new Emoji("✅")].ReactionCount).FlattenAsync().Result.Where(x => !x.IsBot);
                var participantsCount = participants.Count();
                foreach (EmbedFieldBuilder field in e.Fields)
                {
                    if (field.Name.Equals("Participants"))
                        field.Value = participantsCount;
                    else
                    {
                        int.TryParse(message.Embeds.First().Title.Split("x")[0], out int winnerCount);

                        if (winnerCount == 0) winnerCount = 1;
                        if (winnerCount > participantsCount) winnerCount = participantsCount;

                        double probability = 1;
                        if (participantsCount != 0)
                            probability = (1.0 / participantsCount) * winnerCount;

                        field.Value = Math.Round(probability * 100, 2) + "%";
                    }
                }

                await message.ModifyAsync(x =>
                {
                    x.Embed = e.Build();
                });
            }
        }

        public async Task<List<KeyValuePair<ulong, ulong>>> TryPruneAsync(bool testing = true)
        {
            var pruneList = new List<KeyValuePair<ulong, ulong>>();

            foreach (var channel in Giveaways.ToList())
            {
                foreach (var message in channel.Value.ToList())
                {
                    try
                    {
                        var curChannel = (ITextChannel)Program.Client.GetChannel(channel.Key);
                        if (curChannel != null)
                        {
                            var curMessage = await curChannel.GetMessageAsync(message.Key);
                            if (curMessage != null){
                                var daysSinceEdit = (DateTime.UtcNow - (curMessage.EditedTimestamp.HasValue ? curMessage.EditedTimestamp.Value : curMessage.Timestamp).UtcDateTime).TotalDays;
                                if(daysSinceEdit <= 30) continue;
                            }
                            else if(Program.Client.Shards.All(x => x.ConnectionState.Equals(ConnectionState.Connected))){
                                pruneList.Add(KeyValuePair.Create<ulong, ulong>(channel.Key, message.Key));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Contains("50001"))
                            pruneList.Add(KeyValuePair.Create<ulong, ulong>(channel.Key, message.Key));

                        else if(Program.Client.Shards.All(x => x.ConnectionState.Equals(ConnectionState.Connected)))
                            pruneList.Add(KeyValuePair.Create<ulong, ulong>(channel.Key, message.Key));
                    }
                }
            }

            if (!testing)
            {
                foreach (var channel in pruneList)
                {
                    if (Giveaways[channel.Key].Count > 1)
                    {
                        Giveaways[channel.Key].Remove(channel.Value);
                        await UpdateDBAsync(channel.Key);
                    }
                    else
                    {
                        Giveaways.Remove(channel.Key);
                        await RemoveFromDBAsync(channel.Key);
                    }
                }
            }

            return pruneList;
        }
    }
}
