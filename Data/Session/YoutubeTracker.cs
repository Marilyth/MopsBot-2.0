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
using MopsBot.Data.Session.APIResults;
using System.Xml;

namespace MopsBot.Data.Session
{
    public class YoutubeTracker : ITracker
    {
        bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        private System.Threading.Timer checkForChange;
        private string id;
        private string lastTime;

        public YoutubeTracker(string channelId)
        {
            id = channelId;
            lastTime = XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc);
            ChannelIds = new HashSet<ulong>();

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6, 59) * 1000, 300000);
        }

        public YoutubeTracker(string[] initArray)
        {
            ChannelIds = new HashSet<ulong>();

            id = initArray[0];
            lastTime = initArray[1];
            foreach (string channel in initArray[2].Split(new char[] { '{', '}', ';' }))
            {
                if (channel != "")
                    ChannelIds.Add(ulong.Parse(channel));
            }

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6, 59) * 1000, 300000);
        }

        private YoutubeResult fetchVideos()
        {
            string query = MopsBot.Module.Information.readURL($"https://www.googleapis.com/youtube/v3/search?key={Program.youtubeKey}&channelId={id}&part=snippet,id&order=date&maxResults=20&publishedAfter={lastTime}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            YoutubeResult tmpResult = JsonConvert.DeserializeObject<YoutubeResult>(query, _jsonWriter);

            return tmpResult;
        }

        private YoutubeChannelResult fetchChannel()
        {
            string query = MopsBot.Module.Information.readURL($"https://www.googleapis.com/youtube/v3/channels?part=snippet&id={id}&key={Program.youtubeKey}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Error = HandleDeserializationError
            };

            YoutubeChannelResult tmpResult = JsonConvert.DeserializeObject<YoutubeChannelResult>(query, _jsonWriter);

            return tmpResult;
        }

        public void HandleDeserializationError(object sender, EventArgs errorArgs){}

        protected override void CheckForChange_Elapsed(object stateinfo)
        {
            YoutubeResult curStats = fetchVideos();
            try
            {
                APIResults.Item[] newVideos = curStats.items.ToArray();

                if (newVideos.Length > 1)
                {
                    lastTime = XmlConvert.ToString(newVideos[0].snippet.publishedAt, XmlDateTimeSerializationMode.Utc);
                    StaticBase.YoutubeTracks.writeList();
                }

                foreach (APIResults.Item video in newVideos)
                {
                    if (video != newVideos[newVideos.Length - 1])
                    {
                        foreach (ulong channel in ChannelIds)
                        {
                            OnMajorChangeTracked(channel, createYoutubeEmbed(video), "New Video");
                            System.Threading.Thread.Sleep(2000);
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }

        private EmbedBuilder createYoutubeEmbed(Item result)
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
            author.IconUrl = fetchChannel().items[0].snippet.thumbnails.medium.url;
            e.Author = author;

            e.ThumbnailUrl = fetchChannel().items[0].snippet.thumbnails.medium.url;
            e.ImageUrl = result.snippet.thumbnails.high.url;
            e.Description = result.snippet.description;

            return e;
        }

        public override string[] GetInitArray()
        {
            string[] informationArray = new string[2 + ChannelIds.Count];
            informationArray[0] = id;
            informationArray[1] = lastTime;
            informationArray[2] = "{" + string.Join(";", ChannelIds) + "}";

            return informationArray;
        }


        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                checkForChange.Dispose();
            }

            disposed = true;
        }
    }
}