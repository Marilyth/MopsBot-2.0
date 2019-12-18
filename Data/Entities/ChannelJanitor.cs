using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using Discord.WebSocket;
using Discord;

namespace MopsBot.Data.Entities
{
    public class ChannelJanitor
    {
        public TimeSpan MessageDuration;
        [BsonId]
        public ulong ChannelId;
        private System.Threading.Timer checkMessages;
        public DateTime NextCheck;
        public DateTime JanitorBegin;

        public ChannelJanitor()
        {
            Task.Run(() =>
            {
                Task.Delay(1000).Wait();
                var nextCheck = NextCheck - DateTime.UtcNow;
                if (nextCheck > TimeSpan.FromMilliseconds(0))
                    checkMessages = new System.Threading.Timer(CheckMessages, null, nextCheck, TimeSpan.FromMilliseconds(-1));
                else
                    checkMessages = new System.Threading.Timer(CheckMessages, null, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(-1));
            });
        }

        public ChannelJanitor(ulong channelId, TimeSpan messageDuration)
        {
            JanitorBegin = DateTime.UtcNow;
            MessageDuration = messageDuration;
            ChannelId = channelId;
            NextCheck = DateTime.UtcNow.AddMinutes(1);

            checkMessages = new System.Threading.Timer(CheckMessages, null, NextCheck - DateTime.UtcNow, TimeSpan.FromMilliseconds(-1));
        }

        public async void CheckMessages(object state)
        {
            try
            {
                while (true)
                {
                    var messages = await (Program.Client.GetChannel(ChannelId) as ITextChannel).GetMessagesAsync().Flatten().Where(x => GetMessageTime(x) > JanitorBegin).OrderByDescending(x => GetMessageTime(x)).ToArray();
                    var messagesToDelete = messages.Where(x => DateTimeOffset.UtcNow - GetMessageTime(x) > MessageDuration);
                    foreach (var message in messagesToDelete)
                    {
                        try
                        {
                            await message.DeleteAsync();
                        }
                        catch (Exception e)
                        {
                            await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {ChannelId} message deletion: {message.Id}", e));
                        }

                        await Task.Delay(1000);
                    }
                    if (messagesToDelete.Count() == 100) continue;

                    if (messages.Count() == 0 || (messagesToDelete.FirstOrDefault()?.Equals(messages.First()) ?? false))
                    {
                        NextCheck = (DateTime.UtcNow + MessageDuration).AddMinutes(1);
                        break;
                    }
                    else if (!messagesToDelete.FirstOrDefault()?.Equals(messages.First()) ?? true)
                    {
                        NextCheck = GetMessageTime(messages[messages.Count() - messagesToDelete.Count() - 1]) + MessageDuration;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {ChannelId}", e));
                NextCheck = DateTime.UtcNow + MessageDuration;
            }

            await SetTimer();
        }

        private static DateTime GetMessageTime(IMessage message)
        {
            return message.EditedTimestamp?.UtcDateTime ?? message.Timestamp.UtcDateTime;
        }

        public async Task SetTimer()
        {
            await UpdateDBAsync(this);
            var nextCheck = NextCheck - DateTime.UtcNow;
            if (nextCheck > TimeSpan.FromMilliseconds(0))
                checkMessages.Change(nextCheck, TimeSpan.FromMilliseconds(-1));
            else
                checkMessages.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(-1));
        }

        public static async Task UpdateDBAsync(ChannelJanitor janitor)
        {
            await StaticBase.Database.GetCollection<ChannelJanitor>("ChannelJanitor").ReplaceOneAsync(x => x.ChannelId.Equals(janitor.ChannelId), janitor);
        }

        public static async Task InsertToDBAsync(ChannelJanitor janitor)
        {
            await StaticBase.Database.GetCollection<ChannelJanitor>("ChannelJanitor").InsertOneAsync(janitor);
        }

        public static async Task RemoveFromDBAsync(ChannelJanitor janitor)
        {
            await StaticBase.Database.GetCollection<ChannelJanitor>("ChannelJanitor").DeleteOneAsync(x => x.ChannelId.Equals(janitor.ChannelId));
        }

        public static async Task<Dictionary<ulong, ChannelJanitor>> GetJanitors()
        {
            return (await StaticBase.Database.GetCollection<ChannelJanitor>("ChannelJanitor").FindAsync(x => true)).ToEnumerable().ToDictionary(x => x.ChannelId) ?? new Dictionary<ulong, MopsBot.Data.Entities.ChannelJanitor>();
        }
    }
}