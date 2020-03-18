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
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class YoutubeLiveTracker : BaseUpdatingTracker
    {
        //Gets set automatically by batch channel fetching
        private LiveVideoItem StreamInfo;
        public string VideoId, IconUrl;
        private string channelThumbnailUrl;
        public DatePlot ViewerGraph;
        public static readonly string SHOWEMBED = "ShowEmbed", THUMBNAIL = "LargeThumbnail", OFFLINE = "NotifyOnOffline", ONLINE = "NotifyOnOnline", SHOWCHAT = "ShowChat", SENDGRAPH = "SendGraphAfterOffline";

        public YoutubeLiveTracker() : base()
        {
        }

        public YoutubeLiveTracker(string channelId) : base()
        {
            Name = channelId;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var checkExists = fetchChannel().Result;
                Name = checkExists.id;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Channel {TrackerUrl()} could not be found on Youtube!\nPerhaps you used the channel-name instead?", e);
            }
        }

        public async override void PostInitialisation(object info = null)
        {
            if (ViewerGraph != null)
                ViewerGraph.InitPlot();

            if (VideoId != null)
            {
                //SetTimer(120000);
            }
        }

        public async override void PostChannelAdded(ulong channelId)
        {
            base.PostChannelAdded(channelId);

            var config = ChannelConfig[channelId];
            config[SHOWEMBED] = true;
            config[THUMBNAIL] = false;
            config[OFFLINE] = true;
            config[ONLINE] = true;
            config[SHOWCHAT] = false;
            config[SENDGRAPH] = false;
        }

        public override async void Conversion(object obj = null)
        {
            bool save = false;
            foreach (var channel in ChannelConfig.Keys.ToList())
            {
                if (!ChannelConfig[channel].ContainsKey(SENDGRAPH))
                {
                    ChannelConfig[channel][SENDGRAPH] = false;
                    save = true;
                }
            }
            if (save)
                await UpdateTracker();
        }

        private async Task<string> fetchLivestreamId()
        {
            var tmpResult = await FetchJSONDataAsync<Live>($"https://www.googleapis.com/youtube/v3/search?part=snippet&channelId={Name}&eventType=live&type=video&key={Program.Config["Youtube"]}");

            if (tmpResult.items.Count > 0)
            {
                return tmpResult.items.FirstOrDefault()?.id?.videoId;
            }

            return null;
        }

        //Because Youtubes search endpoint is doing not needed work and uses too much quota for any reasonable application, we search ourselves.
        //Also note, that the endpoint Youtube provided to search for live videos has shown to not work and return false data, very often.
        //This was a last resort. If you update your endpoint, we will return to our method above (fetchLivestreamId).
        public static async Task<string> scrapeLivestreamId(string channelId)
        {
            var html = await MopsBot.Module.Information.GetURLAsync($"https://www.youtube.com/channel/{channelId}/videos");
            var videoSegment = html.Split("Jetzt live").FirstOrDefault();
            if (videoSegment == null || html.Length == videoSegment.Length)
            {
                return null;
            }
            else
            {
                videoSegment = videoSegment.Split("/watch?v=").LastOrDefault();
                var videoId = videoSegment.Split("\"").FirstOrDefault();
                return videoId;
            }
        }

        public static async Task fetchChannelsBatch()
        {
            while (true)
            {
                var liveTrackers = StaticBase.Trackers[TrackerType.YoutubeLive].GetTrackers().Where(x => ((YoutubeLiveTracker)x.Value).VideoId != null).ToDictionary(x => x.Key, v => v.Value);
                var liveTrackersList = liveTrackers.Values.ToList();
                for (int i = 0; i < liveTrackers.Count; i += 50)
                {
                    try
                    {
                        var currentBatch = liveTrackersList.Skip(i).Take(50).ToList();
                        var tmpResult = await FetchJSONDataAsync<LiveVideo>($"https://www.googleapis.com/youtube/v3/videos?part=snippet%2C+liveStreamingDetails&maxResults=50&id={String.Join(",", currentBatch.Select(x => (x as YoutubeLiveTracker).VideoId))}&key={Program.Config["Youtube"]}");

                        var nullValues = currentBatch.Select(x => x.Name).ToHashSet();

                        foreach (var video in tmpResult.items)
                        {
                            (liveTrackers[video.snippet.channelId] as YoutubeLiveTracker).StreamInfo = video;
                            nullValues.Remove(video.snippet.channelId);
                        }
                        foreach (var nullChannel in nullValues)
                        {
                            (liveTrackers[nullChannel] as YoutubeLiveTracker).StreamInfo = null;
                        }

                        await Task.Delay(5000);

                    }
                    catch (Exception e)
                    {
                        await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"Error loading channel caches, repeating", e));
                        await Task.Delay(5000);
                    }
                }

                await Task.Delay(60000);
            }
        }

        private async Task<ChannelItem> fetchChannel()
        {
            var tmpResult = await FetchJSONDataAsync<Channel>($"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&id={Name}&key={Program.Config["Youtube"]}");

            return tmpResult.items.First();
        }

        private async Task<List<Message>> fetchChat()
        {
            if (StreamInfo.liveStreamingDetails.activeLiveChatId == null) return new List<Message>();
            var tmpResult = await FetchJSONDataAsync<ChatMessages>($"https://www.googleapis.com/youtube/v3/liveChat/messages?liveChatId={StreamInfo.liveStreamingDetails.activeLiveChatId}&part=snippet,authorDetails&key={Program.Config["Youtube"]}");
            return tmpResult.items;
        }

        public async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                if (VideoId == null)
                {
                    VideoId = await scrapeLivestreamId(Name);

                    //Not live
                    if (VideoId == null)
                        return;

                    //New livestream
                    else
                    {
                        while (StreamInfo == null) await Task.Delay(60000);

                        ViewerGraph = new DatePlot(Name, "Time since start", "Viewers");

                        ViewerGraph.AddValue("Viewers", 0, StreamInfo.liveStreamingDetails.actualStartTime);

                        foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][ONLINE]).ToList())
                            await OnMinorChangeTracked(channel, (string)ChannelConfig[channel]["Notification"]);

                        //SetTimer(120000, 120000);

                        IconUrl = (await fetchChannel()).snippet.thumbnails.medium.url;
                    }
                }

                if (StreamInfo == null) await Task.Delay(120000);
                bool isStreaming = StreamInfo?.snippet?.liveBroadcastContent?.Equals("live") ?? false;

                if (!isStreaming)
                {
                    VideoId = null;
                    //SetTimer(900000);
                    var png = ViewerGraph.DrawPlot(false, $"{Name}-{DateTime.UtcNow.ToString("MM-dd-yy_hh-mm")}", true);
                    foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][SENDGRAPH]).ToList())
                        await (Program.Client.GetChannel(channel) as SocketTextChannel).SendFileAsync(png, "Graph for personal use:");
                    //File.Delete(png);

                    ViewerGraph?.Dispose();
                    ViewerGraph = null;

                    ToUpdate = new Dictionary<ulong, ulong>();

                    foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][OFFLINE]).ToList())
                        await OnMinorChangeTracked(channel, $"{StreamInfo?.snippet?.channelTitle ?? "Streamer"} went Offline!");

                    StreamInfo = null;
                }
                else
                {
                    if(StreamInfo.liveStreamingDetails?.concurrentViewers != null)
                        ViewerGraph.AddValue("Viewers", double.Parse(StreamInfo.liveStreamingDetails.concurrentViewers));
                    else
                        ViewerGraph.AddValue("Viewers", ViewerGraph.PlotDataPoints.Last().Value.Value);

                    foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][SHOWEMBED]).ToList())
                        await OnMajorChangeTracked(channel, await createEmbed((bool)ChannelConfig[channel][THUMBNAIL], (bool)ChannelConfig[channel][SHOWCHAT]));
                }

                await UpdateTracker();
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        public async Task<Embed> createEmbed(bool largeThumbnail = false, bool showChat = false)
        {
            ViewerGraph.SetMaximumLine();
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0xFF0000);
            e.Title = StreamInfo.snippet.title;
            e.Url = $"https://www.youtube.com/watch?v={VideoId}";
            e.WithCurrentTimestamp();

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = StreamInfo.snippet.channelTitle;
            author.Url = $"https://www.youtube.com/channel/{StreamInfo.snippet.channelId}";
            author.IconUrl = IconUrl ?? StreamInfo.snippet.thumbnails.standard.url;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "http://www.stickpng.com/assets/images/580b57fcd9996e24bc43c545.png";
            footer.Text = "Youtube-Live";
            e.Footer = footer;

            e.ThumbnailUrl = largeThumbnail ? ViewerGraph.DrawPlot() : $"{StreamInfo.snippet.thumbnails.medium.url}?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = largeThumbnail ? $"{StreamInfo.snippet.thumbnails.maxres?.url ?? StreamInfo.snippet.thumbnails.medium.url}?rand={StaticBase.ran.Next(0, 99999999)}" : ViewerGraph.DrawPlot();

            e.AddField("Viewers", StreamInfo.liveStreamingDetails.concurrentViewers, true);
            var liveTime = DateTime.UtcNow - StreamInfo.liveStreamingDetails.actualStartTime;
            e.AddField("Runtime", (int)liveTime.TotalHours + "h " + liveTime.ToString(@"mm\m"), true);

            if (showChat)
            {
                var chat = await fetchChat();
                chat.Reverse();

                if (chat.Count > 5) chat = chat.Take(5).ToList();

                string chatPreview = "```asciidoc\n";
                for (int i = chat.Count - 1; i >= 0; i--)
                {
                    if (chat[i].snippet.displayMessage.Length > 100)
                        chatPreview += chat[i].authorDetails.displayName + ":: " + string.Join("", chat[i].snippet.displayMessage.Take(100)) + " [...]\n";
                    else
                        chatPreview += chat[i].authorDetails.displayName + ":: " + chat[i].snippet.displayMessage + "\n";
                }
                if (chatPreview.Equals("```asciidoc\n")) chatPreview += "Could not fetch chat messages. Is it empty or private?";
                chatPreview += "```";

                e.AddField("Chat Preview", chatPreview);
            }

            return e.Build();
        }

        public override async Task UpdateTracker()
        {
            await StaticBase.Trackers[TrackerType.YoutubeLive].UpdateDBAsync(this);
        }

        public override string TrackerUrl()
        {
            return "https://www.youtube.com/channel/" + Name;
        }
    }
}