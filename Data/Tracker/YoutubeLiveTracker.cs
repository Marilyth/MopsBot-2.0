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
        public bool IsThumbnailLarge;
        private LiveVideoItem liveStatus;

        public YoutubeLiveTracker() : base(300000, ExistingTrackers * 2000)
        {
        }

        public YoutubeLiveTracker(string channelId) : base(300000)
        {
            Name = channelId;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var checkExists = fetchChannel().Result;
                Name = checkExists.id;
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"Channel {TrackerUrl()} could not be found on Youtube!\nPerhaps you used the channel-name instead?");
            }
        }

        public async override void PostInitialisation()
        {
            if(ViewerGraph != null)
                ViewerGraph.InitPlot();

            if(VideoId != null)
                checkForChange.Change(60000, 60000);

            foreach (var channelMessage in ToUpdate ?? new Dictionary<ulong, ulong>())
            {
                try
                {
                    await setReaction((IUserMessage)((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value).Result);
                }
                catch
                {
                }
            }
        }

        public async override Task setReaction(IUserMessage message)
        {
            await Program.ReactionHandler.AddHandler(message, new Emoji("🖌"), recolour);
            await Program.ReactionHandler.AddHandler(message, new Emoji("🔄"), switchThumbnail);
        }

        private async Task<string> fetchLivestreamId()
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://www.googleapis.com/youtube/v3/search?part=snippet&channelId={Name}&eventType=live&type=video&key={Program.Config["YoutubeLive"]}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            Live tmpResult = JsonConvert.DeserializeObject<Live>(query, _jsonWriter);

            if (tmpResult.items.Count > 0)
            {
                return tmpResult.items.FirstOrDefault()?.id?.videoId;
            }

            return null;
        }

        private async Task<LiveVideoItem> fetchLiveVideoContent()
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://www.googleapis.com/youtube/v3/videos?part=snippet%2C+liveStreamingDetails&id={VideoId}&key={Program.Config["YoutubeLive"]}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            LiveVideo tmpResult = JsonConvert.DeserializeObject<LiveVideo>(query, _jsonWriter);

            if (tmpResult.items.Count > 0)
            {
                return tmpResult.items.FirstOrDefault();
            }

            return null;
        }

        private async Task<ChannelItem> fetchChannel()
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://www.googleapis.com/youtube/v3/channels?part=contentDetails,snippet&id={Name}&key={Program.Config["YoutubeLive"]}");

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
                if (VideoId == null)
                {
                    VideoId = await fetchLivestreamId();

                    //Not live
                    if (VideoId == null)
                        return;

                    //New livestream
                    else
                    {
                        ViewerGraph = new DatePlot(Name, "Time since start", "Viewers");

                        foreach (ulong channel in ChannelMessages.Keys.ToList())
                            await OnMinorChangeTracked(channel, ChannelMessages[channel]);
                        
                        checkForChange.Change(60000, 60000);

                        IconUrl = (await fetchChannel()).snippet.thumbnails.medium.url;
                    }
                }

                liveStatus = await fetchLiveVideoContent();

                bool isStreaming = liveStatus.snippet.liveBroadcastContent.Equals("live");

                if (!isStreaming)
                {
                    VideoId = null;
                    checkForChange.Change(300000, 300000);
                    Console.WriteLine("\n" + $"{DateTime.Now} {liveStatus.snippet.channelTitle} went Offline (Youtube)");
                    ViewerGraph.Dispose();
                    ViewerGraph = null;

                    foreach (var channelMessage in ToUpdate)
                        await Program.ReactionHandler.ClearHandler((IUserMessage)await ((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value));

                    ToUpdate = new Dictionary<ulong, ulong>();

                    foreach (ulong channel in ChannelMessages.Keys.ToList())
                        await OnMinorChangeTracked(channel, $"{liveStatus.snippet.channelTitle} went Offline!");
                }
                else
                {
                    ViewerGraph.AddValue("Viewers", double.Parse(liveStatus.liveStreamingDetails.concurrentViewers));

                    foreach (ulong channel in ChannelMessages.Keys.ToList())
                        await OnMajorChangeTracked(channel, createEmbed());
                }

                await StaticBase.Trackers[TrackerType.YoutubeLive].UpdateDBAsync(this);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + $"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        public Embed createEmbed()
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0xFF0000);
            e.Title = liveStatus.snippet.title;
            e.Url = $"https://www.youtube.com/watch?v={VideoId}";
            e.Description = "**For people with manage channel permission**:\n🖌: Change chart colour\n🔄: Switch thumbnail and chart position";

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = liveStatus.snippet.channelTitle;
            author.Url = $"https://www.youtube.com/channel/{liveStatus.snippet.channelId}";
            author.IconUrl = IconUrl ?? liveStatus.snippet.thumbnails.standard.url;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "http://www.stickpng.com/assets/images/580b57fcd9996e24bc43c545.png";
            footer.Text = "Youtube-Live";
            e.Footer = footer;

            e.ThumbnailUrl = IsThumbnailLarge ? ViewerGraph.DrawPlot() : $"{liveStatus.snippet.thumbnails.medium.url}?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = IsThumbnailLarge ? $"{liveStatus.snippet.thumbnails.maxres?.url ?? liveStatus.snippet.thumbnails.medium.url}?rand={StaticBase.ran.Next(0, 99999999)}" : ViewerGraph.DrawPlot();

            e.AddField("Viewers", liveStatus.liveStreamingDetails.concurrentViewers, true);

            return e.Build();
        }

        private async Task recolour(ReactionHandlerContext context)
        {
            if (((IGuildUser)await context.Reaction.Channel.GetUserAsync(context.Reaction.UserId)).GetPermissions((IGuildChannel)context.Channel).ManageChannel)
            {
                ViewerGraph.Recolour();

                foreach (ulong channel in ChannelMessages.Keys.ToList())
                    await OnMajorChangeTracked(channel, createEmbed());
            }
        }

        private async Task switchThumbnail(ReactionHandlerContext context)
        {
            if (((IGuildUser)await context.Reaction.Channel.GetUserAsync(context.Reaction.UserId)).GetPermissions((IGuildChannel)context.Channel).ManageChannel)
            {
                IsThumbnailLarge = !IsThumbnailLarge;
                await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);

                foreach (ulong channel in ChannelMessages.Keys.ToList())
                    await OnMajorChangeTracked(channel, createEmbed());
            }
        }

        public override Dictionary<string, object> GetParameters(ulong guildId)
        {
            var parentParameters = base.GetParameters(guildId);
            (parentParameters["Parameters"] as Dictionary<string, object>)["IsThumbnailLarge"] = new bool[]{true, false};
            return parentParameters;
        }

        public override object GetAsScope(ulong channelId){
            return new ContentScope(){
                Name = this.Name,
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId,
                IsThumbnailLarge = this.IsThumbnailLarge
            };
        }

        public override void Update(params string[] args){
            var channelId = ulong.Parse(args[2].Split(":")[1]);
            ChannelMessages[channelId] = args[1];
            IsThumbnailLarge = bool.Parse(args[3]);
        }

        public new struct ContentScope
        {
            public string Name;
            public string Notification;
            public string Channel;
            public bool IsThumbnailLarge;
        }

        public override string TrackerUrl()
        {
            return "https://www.youtube.com/channel/" + Name;
        }
    }
}