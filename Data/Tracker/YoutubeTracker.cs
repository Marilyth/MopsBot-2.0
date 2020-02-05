using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MopsBot.Data.Tracker.APIResults.Youtube;
using System.Xml;
using System.Web;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class YoutubeTracker : BaseTracker
    {
        public DateTime WebhookExpire = DateTime.Now;
        private string channelThumbnailUrl, uploadPlaylistId;
        private HashSet<string> pastVideoIds = new HashSet<string>();

        public YoutubeTracker() : base()
        {
        }

        public YoutubeTracker(string channelId) : base()
        {
            Name = channelId;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var checkExists = fetchChannel().Result;
                Name = checkExists.id;
                uploadPlaylistId = checkExists.contentDetails.relatedPlaylists.uploads;
                channelThumbnailUrl = checkExists.snippet.thumbnails.medium.url;
                SetTimer();
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Channel {TrackerUrl()} could not be found on Youtube!\nPerhaps you used the channel-name instead?", e);
            }
        }

        public async override void PostInitialisation(object info = null)
        {
            if ((WebhookExpire - DateTime.Now).TotalMinutes <= 60)
            {
                await pushSubscribe(Name);
                WebhookExpire = DateTime.Now.AddDays(4);
                await UpdateTracker();
            }

            SetTimer(600000);
        }

        public static async Task pushSubscribe(string channelId, bool subscribe = true)
        {
            try
            {
                var callBackUrl = $"{Program.Config["ServerAddress"]}:5000/api/webhook/youtube";
                var topicUrl = $"https://www.youtube.com/xml/feeds/videos.xml?channel_id={channelId}";
                var subscribeUrl = "https://pubsubhubbub.appspot.com/subscribe";
                string postDataStr = $"?hub.mode={(subscribe ? "subscribe" : "unsubscribe")}&" +
                                     $"hub.verify=async&hub.callback={HttpUtility.UrlEncode(callBackUrl)}&" +
                                     $"hub.topic={HttpUtility.UrlEncode(topicUrl)}";

                var test = await MopsBot.Module.Information.PostURLAsync(subscribeUrl + postDataStr);
            }
            catch (Exception ex)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"Subscribing didn't work", ex));
            }
        }

        private async Task<ChannelItem> fetchChannel()
        {
            var tmpResult = await FetchJSONDataAsync<Channel>($"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&id={Name}&key={Program.Config["Youtube"]}");
            var channel = tmpResult.items.First();
            return channel;
        }

        public async Task CheckInfoAsync(YoutubeNotification push)
        {
            if (push.IsNewVideo && !pastVideoIds.Contains(push.VideoId))
            {
                foreach (ulong channel in ChannelConfig.Keys.ToList())
                    await OnMinorChangeTracked(channel, (string)ChannelConfig[channel]["Notification"] + "\nhttps://www.youtube.com/watch?v=" + push.VideoId);

                pastVideoIds.Add(push.VideoId);
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            if ((WebhookExpire - DateTime.Now).TotalMinutes <= 60)
            {
                await pushSubscribe(Name);
                WebhookExpire = DateTime.Now.AddDays(4);
                await UpdateTracker();
            }
        }

        public override string TrackerUrl()
        {
            return "https://www.youtube.com/channel/" + Name;
        }

        public override async Task UpdateTracker()
        {
            await StaticBase.Trackers[TrackerType.Youtube].UpdateDBAsync(this);
        }

        public override void Dispose()
        {
            base.Dispose(true);
            GC.SuppressFinalize(this);
            pushSubscribe(Name, false).Wait();
        }
    }
}