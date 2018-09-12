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

namespace MopsBot.Data.Tracker
{
    public class YoutubeTracker : ITracker
    {
        public string LastTime;
        private string channelThumbnailUrl, uploadPlaylistId;

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
                Name = checkExists.id;
                uploadPlaylistId = checkExists.contentDetails.relatedPlaylists.uploads;
                channelThumbnailUrl = checkExists.snippet.thumbnails.medium.url;
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"Channel-ID `{Name}` could not be found on Youtube!\nPerhaps you used the channel-name instead?");
            }
        }

        public async override void PostInitialisation()
        {
            try{
                ChannelItem channel = await fetchChannel();
            
                uploadPlaylistId = channel.contentDetails.relatedPlaylists.uploads;
                channelThumbnailUrl = channel.snippet.thumbnails.medium.url;
            } catch {
                
            }
        }

        private async Task<Video[]> fetchPlaylist()
        {
            var lastDateTime = DateTime.Parse(LastTime).ToUniversalTime();
            var lastStringDateTime = XmlConvert.ToString(lastDateTime.AddSeconds(1), XmlDateTimeSerializationMode.Utc);
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&playlistId={uploadPlaylistId}&key={Program.Config["Youtube"]}");

            var tmp = Program.Config["Youtube"];
            Program.Config["Youtube"] = Program.Config["Youtube2"];
            Program.Config["Youtube2"] = tmp;

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            Playlist tmpResult = JsonConvert.DeserializeObject<Playlist>(query, _jsonWriter);

            return tmpResult.items.Where(x => x.snippet.publishedAt > lastDateTime).OrderByDescending(x => x.snippet.publishedAt).ToArray();
        }

        private async Task<ChannelItem> fetchChannel()
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&id={Name}&key={Program.Config["Youtube"]}");
            var tmp = Program.Config["Youtube"];
            Program.Config["Youtube"] = Program.Config["Youtube2"];
            Program.Config["Youtube2"] = tmp;

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            Channel tmpResult = JsonConvert.DeserializeObject<Channel>(query, _jsonWriter);

            return tmpResult.items.First();
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var newVideos = await fetchPlaylist();

                foreach (Video video in newVideos)
                {
                    foreach (ulong channel in ChannelIds.ToList())
                    {
                        await OnMajorChangeTracked(channel, await createEmbed(video), ChannelMessages[channel]);
                    }
                }

                if (newVideos.Length > 0)
                {
                    LastTime = XmlConvert.ToString(newVideos[0].snippet.publishedAt, XmlDateTimeSerializationMode.Utc);
                    await StaticBase.Trackers["youtube"].UpdateDBAsync(this);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" +  $"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<Embed> createEmbed(Video video)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0xFF0000);
            e.Title = video.snippet.title;
            e.Url = $"https://www.youtube.com/watch?v={video.id}";
            e.Timestamp = video.snippet.publishedAt;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "http://www.stickpng.com/assets/images/580b57fcd9996e24bc43c545.png";
            footer.Text = "Youtube";
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = video.snippet.channelTitle;
            author.Url = $"https://www.youtube.com/channel/{video.snippet.channelId}";
            author.IconUrl = channelThumbnailUrl;
            e.Author = author;

            e.ThumbnailUrl = channelThumbnailUrl;
            e.ImageUrl = video.snippet.thumbnails.high.url;
            e.Description = video.snippet.description.Length > 300 ? video.snippet.description.Substring(0, 300) + " [...]" : video.snippet.description;

            return e.Build();
        }
    }
}