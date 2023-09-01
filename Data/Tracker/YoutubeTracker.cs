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
        [MongoDB.Bson.Serialization.Attributes.BsonSerializer(typeof(MopsBot.Utils.MongoDictionarySerializer<string, DateTime>))]
        public Dictionary<string, DateTime> PastVideoIds = new Dictionary<string, DateTime>();

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
            }
        }

        public static async Task pushSubscribe(string channelId, bool subscribe = true)
        {
            try
            {
                var callBackUrl = $"{Program.Config["ServerAddress"]}:{Program.Config["Port"]}/api/webhook/youtube";
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

        /// <summary>
        /// Fetches the channel information from the Youtube API.
        /// If the name start with an @, it will be treated as a handle and the channel id will first be fetched by that.
        /// </summary>
        /// <returns></returns>
        private async Task<ChannelItem> fetchChannel()
        {
            // Convert handle to channel id.
            if(Name.StartsWith("@")){
                var handleResponse = await FetchJSONDataAsync<Channel>($"https://yt.lemnoslife.com/channels?handle={Name}");

                if(handleResponse.items.FirstOrDefault()?.id is null){
                    throw new Exception($"Failed to fetch channel by handle {Name}");
                }

                Name = handleResponse.items.FirstOrDefault().id;
            }

            Channel channel = await FetchJSONDataAsync<Channel>($"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&id={Name}&key={Program.Config["YoutubeKey"]}");
            
            return channel.items?.FirstOrDefault();
        }

        public async Task CheckInfoAsync(YoutubeNotification push)
        {
            if (push.IsNewVideo && !PastVideoIds.ContainsKey(push.VideoId))
            {
                PastVideoIds[push.VideoId] = DateTime.UtcNow;
                PastVideoIds = PastVideoIds.Where(x => (DateTime.UtcNow - x.Value).Days <= 3).ToDictionary(x => x.Key, x => x.Value);

                foreach (ulong channel in ChannelConfig.Keys.ToList())
                    await OnMinorChangeTracked(channel, (string)ChannelConfig[channel]["Notification"] + "\nhttps://www.youtube.com/watch?v=" + push.VideoId);
            }
        }

        public async override void CheckForChange_Elapsed(object stateinfo)
        {
            if ((WebhookExpire - DateTime.Now).TotalMinutes <= 60)
            {
                await pushSubscribe(Name);
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