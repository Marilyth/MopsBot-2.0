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
    public class WelcomeMessage
    {
        [BsonId]
        public ulong GuildId;
        public string Name, AvatarUrl, WebhookToken, Notification;
        public bool IsWebhook;
        public ulong WebhookId, ChannelId;
        private Discord.Webhook.DiscordWebhookClient Client;

        public async Task SendWelcomeMessageAsync(SocketGuildUser User)
        {
            Task.Run(() => {
            if (IsWebhook)
            {
                if (Client == null)
                {
                    Client = new Discord.Webhook.DiscordWebhookClient(WebhookId, WebhookToken);
                }

                Client.SendMessageAsync(Notification.Replace("{User.Mention}", User.Mention).Replace("{User.Username}", User.Username), username: Name, avatarUrl: AvatarUrl ?? Program.Client.CurrentUser.GetAvatarUrl());
            }

            else
            {
                ((ITextChannel)Program.Client.GetChannel(ChannelId)).SendMessageAsync(Notification.Replace("{User.Mention}", User.Mention).Replace("{User.Username}", User.Username));
            }
            });
        }

        public WelcomeMessage(ulong guildId, ulong channelId, string notification, ulong webhookId, string webhookToken, string username = null, string avatarUrl = null)
        {
            IsWebhook = true;
            ChannelId = channelId;
            GuildId = guildId;
            Notification = notification;
            WebhookId = webhookId;
            WebhookToken = webhookToken;
            Name = username ?? "Mops";
            AvatarUrl = avatarUrl;
        }

        public WelcomeMessage(ulong guildId, ulong channelId, string notification)
        {
            IsWebhook = false;
            GuildId = guildId;
            ChannelId = channelId;
            Notification = notification;
        }

        public async Task RemoveWebhookAsync()
        {
            if (IsWebhook)
            {
                Client = Client ?? new Discord.Webhook.DiscordWebhookClient(WebhookId, WebhookToken);
                await Client.DeleteWebhookAsync();
                Client.Dispose();
                Client = null;
            }
        }
    }
}
