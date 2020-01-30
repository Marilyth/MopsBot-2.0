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
        public string VideoId, IconUrl;
        private string channelThumbnailUrl;
        public DatePlot ViewerGraph;
        private LiveVideoItem liveStatus;
        public static readonly string SHOWEMBED = "ShowEmbed", THUMBNAIL = "LargeThumbnail", OFFLINE = "NotifyOnOffline", ONLINE = "NotifyOnOnline", SHOWCHAT = "ShowChat";

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
            if(ViewerGraph != null)
                ViewerGraph.InitPlot();

            if(VideoId != null){
                SetTimer(300000);
            } else {
                SetTimer(900000);
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

            await UpdateTracker();
        }

        public override async void Conversion(object obj = null)
        {
            bool save = false;
            foreach (var channel in ChannelConfig.Keys.ToList())
            {
                if (!ChannelConfig[channel].ContainsKey(SHOWCHAT))
                {
                    ChannelConfig[channel][SHOWCHAT] = false;
                    save = true;
                }
            }
            if (save)
                await UpdateTracker();
        }

        private async Task<string> fetchLivestreamId()
        {
            var tmpResult = await FetchJSONDataAsync<Live>($"https://www.googleapis.com/youtube/v3/search?part=snippet&channelId={Name}&eventType=live&type=video&key={Program.Config["YoutubeLive"]}");

            if (tmpResult.items.Count > 0)
            {
                return tmpResult.items.FirstOrDefault()?.id?.videoId;
            }

            return null;
        }

        public static async Task<string> scrapeLivestreamId(string channelId){
            var html = await MopsBot.Module.Information.GetURLAsync($"https://www.youtube.com/channel/{channelId}/videos");
            var videoSegment = html.Split(">Jetzt live</span>").FirstOrDefault();
            if(videoSegment == null || html.Length == videoSegment.Length){
                return null;
            } else {
                videoSegment = videoSegment.Split("/watch?v=").LastOrDefault();
                var videoId = videoSegment.Split("\"").FirstOrDefault();
                return videoId;
            }
        }

        private async Task<LiveVideoItem> fetchLiveVideoContent()
        {
            var tmpResult = await FetchJSONDataAsync<LiveVideo>($"https://www.googleapis.com/youtube/v3/videos?part=snippet%2C+liveStreamingDetails&id={VideoId}&key={Program.Config["YoutubeLive"]}");

            if (tmpResult.items.Count > 0)
            {
                return tmpResult.items.FirstOrDefault();
            }

            return null;
        }

        private async Task<ChannelItem> fetchChannel()
        {
            var tmpResult = await FetchJSONDataAsync<Channel>($"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&id={Name}&key={Program.Config["YoutubeLive"]}");
            
            return tmpResult.items.First();
        }

        private async Task<List<Message>> fetchChat(){
            if(liveStatus.liveStreamingDetails.activeLiveChatId == null) return new List<Message>();
            var tmpResult = await FetchJSONDataAsync<ChatMessages>($"https://www.googleapis.com/youtube/v3/liveChat/messages?liveChatId={liveStatus.liveStreamingDetails.activeLiveChatId}&part=snippet,authorDetails&key={Program.Config["YoutubeLive"]}");
            return tmpResult.items;
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
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
                        ViewerGraph = new DatePlot(Name, "Time since start", "Viewers");
                        liveStatus = await fetchLiveVideoContent();
                        ViewerGraph.AddValue("Viewers", 0, liveStatus.liveStreamingDetails.actualStartTime);

                        foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][ONLINE]).ToList())
                            await OnMinorChangeTracked(channel, (string)ChannelConfig[channel]["Notification"]);
                        
                        SetTimer(300000, 300000);

                        IconUrl = (await fetchChannel()).snippet.thumbnails.medium.url;
                    }
                }

                liveStatus = await fetchLiveVideoContent();

                bool isStreaming = liveStatus?.snippet?.liveBroadcastContent?.Equals("live") ?? false;

                if (!isStreaming)
                {
                    VideoId = null;
                    SetTimer(900000);
                    ViewerGraph.Dispose();
                    ViewerGraph = null;

                    foreach (var channelMessage in ToUpdate)
                        await Program.ReactionHandler.ClearHandler((IUserMessage)await ((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value));

                    ToUpdate = new Dictionary<ulong, ulong>();

                    foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][OFFLINE]).ToList())
                        await OnMinorChangeTracked(channel, $"{liveStatus.snippet.channelTitle} went Offline!");
                }
                else
                {
                    ViewerGraph.AddValue("Viewers", double.Parse(liveStatus.liveStreamingDetails.concurrentViewers));

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
            e.Title = liveStatus.snippet.title;
            e.Url = $"https://www.youtube.com/watch?v={VideoId}";
            e.WithCurrentTimestamp();

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = liveStatus.snippet.channelTitle;
            author.Url = $"https://www.youtube.com/channel/{liveStatus.snippet.channelId}";
            author.IconUrl = IconUrl ?? liveStatus.snippet.thumbnails.standard.url;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "http://www.stickpng.com/assets/images/580b57fcd9996e24bc43c545.png";
            footer.Text = "Youtube-Live";
            e.Footer = footer;

            e.ThumbnailUrl = largeThumbnail ? ViewerGraph.DrawPlot() : $"{liveStatus.snippet.thumbnails.medium.url}?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = largeThumbnail ? $"{liveStatus.snippet.thumbnails.maxres?.url ?? liveStatus.snippet.thumbnails.medium.url}?rand={StaticBase.ran.Next(0, 99999999)}" : ViewerGraph.DrawPlot();

            e.AddField("Viewers", liveStatus.liveStreamingDetails.concurrentViewers, true);
            var liveTime = DateTime.UtcNow - liveStatus.liveStreamingDetails.actualStartTime;
            e.AddField("Runtime", (int)liveTime.TotalHours + "h " + liveTime.ToString(@"mm\m"), true);

            if (showChat)
                {
                    var chat = await fetchChat();
                    chat.Reverse();

                    if(chat.Count > 5) chat = chat.Take(5).ToList();

                    string chatPreview = "```asciidoc\n";
                    for (int i = chat.Count - 1; i >= 0; i--)
                    {
                        if (chat[i].snippet.displayMessage.Length > 100)
                            chatPreview += chat[i].authorDetails.displayName + ":: " + string.Join("", chat[i].snippet.displayMessage.Take(100)) + " [...]\n";
                        else
                            chatPreview += chat[i].authorDetails.displayName + ":: " + chat[i].snippet.displayMessage + "\n";
                    }
                    if(chatPreview.Equals("```asciidoc\n")) chatPreview += "Could not fetch chat messages. Is it empty or private?";
                    chatPreview += "```";

                    e.AddField("Chat Preview", chatPreview);
                }

            return e.Build();
        }

        public override async Task UpdateTracker(){
            await StaticBase.Trackers[TrackerType.YoutubeLive].UpdateDBAsync(this);
        }

        public override string TrackerUrl()
        {
            return "https://www.youtube.com/channel/" + Name;
        }
    }
}