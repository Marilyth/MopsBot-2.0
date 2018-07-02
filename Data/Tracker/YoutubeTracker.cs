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
using MopsBot.Data.Tracker.APIResults;
using System.Xml;

namespace MopsBot.Data.Tracker
{
    public class YoutubeTracker : ITracker
    {
        public string LastTime;

        public YoutubeTracker() : base(1200000, (ExistingTrackers * 2000 + 500) % 1200000)
        {
        }

        public YoutubeTracker(string channelId) : base(1200000)
        {
            Name = channelId;
            LastTime = XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc);

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var checkExists = fetchChannel().Result;
                Name = checkExists.items[0].id;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Channel-ID `{Name}` could not be found on Youtube!\nPerhaps you used the channel-name instead?");
            }
        }

        private async Task<YoutubeResult> fetchVideos()
        {
            var lastDateTime = DateTime.Parse(LastTime);
            var lastStringDateTime = XmlConvert.ToString(lastDateTime.AddSeconds(1), XmlDateTimeSerializationMode.Utc);
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://www.googleapis.com/youtube/v3/search?key={Program.Config["Youtube"]}&channelId={Name}&part=snippet,id&order=date&maxResults=20&publishedAfter={lastStringDateTime}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            YoutubeResult tmpResult = JsonConvert.DeserializeObject<YoutubeResult>(query, _jsonWriter);

            return tmpResult;
        }

        private async Task<YoutubeChannelResult> fetchChannel()
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://www.googleapis.com/youtube/v3/channels?part=snippet&id={Name}&key={Program.Config["Youtube"]}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            YoutubeChannelResult tmpResult = JsonConvert.DeserializeObject<YoutubeChannelResult>(query, _jsonWriter);

            return tmpResult;
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                YoutubeResult curStats = await fetchVideos();
                APIResults.Item[] newVideos = curStats.items.ToArray();

                foreach (APIResults.Item video in newVideos)
                {
                    foreach (ulong channel in ChannelIds)
                    {
                        await OnMajorChangeTracked(channel, await createEmbed(video), ChannelMessages[channel]);
                    }
                }

                if (newVideos.Length > 0)
                {
                    LastTime = XmlConvert.ToString(newVideos[0].snippet.publishedAt, XmlDateTimeSerializationMode.Utc);
                    StaticBase.Trackers["youtube"].SaveJson();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<Embed> createEmbed(Item result)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0xFF0000);
            e.Title = result.snippet.title;
            e.Url = $"https://www.youtube.com/watch?v={result.id.videoId}";
            e.Timestamp = result.snippet.publishedAt;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "http://www.stickpng.com/assets/images/580b57fcd9996e24bc43c545.png";
            footer.Text = "Youtube";
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = result.snippet.channelTitle;
            author.Url = $"https://www.youtube.com/channel/{result.snippet.channelId}";
            var channelInformation = await fetchChannel();
            author.IconUrl = channelInformation.items[0].snippet.thumbnails.medium.url;
            e.Author = author;

            e.ThumbnailUrl = channelInformation.items[0].snippet.thumbnails.medium.url;
            e.ImageUrl = result.snippet.thumbnails.high.url;
            e.Description = result.snippet.description;

            return e.Build();
        }
    }
}