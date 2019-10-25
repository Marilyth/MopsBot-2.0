using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults.Twitch;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;

namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public class TwitchTracker : BaseUpdatingTracker
    {
        private static KeyValuePair<string, string> acceptHeader = new KeyValuePair<string, string>("Accept", "application/vnd.twitchtv.v5+json");
        public event HostingEventHandler OnHosting;
        public event StatusEventHandler OnLive;
        public event StatusEventHandler OnOffline;
        public delegate Task HostingEventHandler(string hostName, string targetName, int viewers);
        public delegate Task StatusEventHandler(BaseTracker sender);
        private List<Comment> comments = new List<Comment>();
        public DatePlot ViewerGraph;
        private TwitchResult StreamerStatus;
        public Boolean IsOnline;
        public string CurGame, VodUrl;
        public bool IsHosting;
        public int TimeoutCount;
        public ulong TwitchId;
        public DateTime WebhookExpire = DateTime.Now;
        public static readonly string GAMECHANGE = "NotifyOnGameChange", HOST = "NotifyOnHost", ONLINE = "NotifyOnOnline", OFFLINE = "NotifyOnOffline", SHOWEMBED = "ShowEmbed", SHOWCHAT = "ShowChat", SHOWVOD = "ShowVod", THUMBNAIL = "LargeThumbnail", SENDPDF = "SendGraphPDFAfterOffline";

        public TwitchTracker() : base()
        {
        }

        public TwitchTracker(string streamerName) : base()
        {
            Name = streamerName;
            IsOnline = false;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                TwitchId = GetIdFromUsername(streamerName).Result;
                SetTimer();
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Streamer {TrackerUrl()} could not be found on Twitch!", e);
            }
        }

        public async override void PostChannelAdded(ulong channelId)
        {
            base.PostChannelAdded(channelId);

            var config = ChannelConfig[channelId];
            config[SHOWEMBED] = true;
            config[SHOWCHAT] = false;
            config[SHOWVOD] = true;
            config[THUMBNAIL] = false;
            config[GAMECHANGE] = true;
            config[HOST] = false;
            config[OFFLINE] = true;
            config[ONLINE] = true;
            config[SENDPDF] = false;

            await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
        }

        public async override void PostInitialisation(object info = null)
        {
            if (IsOnline) SetTimer(60000, StaticBase.ran.Next(5000, 60000));

            if (ViewerGraph != null)
                ViewerGraph.InitPlot();

            if((WebhookExpire - DateTime.Now).TotalMinutes < 10){
                await SubscribeWebhookAsync();
            }
        }

        public async Task<string> SubscribeWebhookAsync(bool subscribe = true)
        {
            try{
                var url = "https://api.twitch.tv/helix/webhooks/hub" +
                          $"?hub.topic=https://api.twitch.tv/helix/streams?user_id={TwitchId}" +
                          "&hub.lease_seconds=64800" +
                          $"&hub.callback={Program.Config["ServerAddress"]}:5000/api/webhook/twitch" +
                          $"&hub.mode={(subscribe ? "subscribe" : "unsubscribe")}";

                var test = await MopsBot.Module.Information.PostURLAsync(url, headers:
                    KeyValuePair.Create("Authorization", "Bearer " + Program.Config["TwitchToken"])
                );

                WebhookExpire = DateTime.Now.AddHours(18);
                await UpdateTracker();

                return test;
            } catch(Exception e){
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
                return "Failed";
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                if((WebhookExpire - DateTime.Now).TotalMinutes < 10){
                    await SubscribeWebhookAsync();
                }
                
                await CheckStreamerInfoAsync();
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        public async Task CheckStreamerInfoAsync()
        {
            try
            {
                StreamerStatus = await streamerInformation();
                Boolean isStreaming = StreamerStatus.stream.channel != null;

                if (IsOnline != isStreaming)
                {
                    if (IsOnline)
                    {
                        if (++TimeoutCount >= 3)
                        {
                            TimeoutCount = 0;
                            IsOnline = false;

                            var pdf = ViewerGraph.DrawPlot(true, $"{Name}-{DateTime.UtcNow.ToString("MM-dd-yy_hh-mm")}");
                            foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][SENDPDF]).ToList())
                                await (Program.Client.GetChannel(channel) as SocketTextChannel).SendFileAsync(pdf, "Graph PDF for personal use:");
                            File.Delete(pdf);

                            ViewerGraph?.Dispose();
                            ViewerGraph = null;
                            VodUrl = null;

                            foreach (var channelMessage in ToUpdate)
                                await Program.ReactionHandler.ClearHandler((IUserMessage)await ((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value));

                            ToUpdate = new Dictionary<ulong, ulong>();

                            if (OnOffline != null) await OnOffline.Invoke(this);
                            foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][OFFLINE]).ToList())
                                await OnMinorChangeTracked(channel, $"{Name} went Offline!");

                            SetTimer(600000, 600000);

                        }
                        else if (!IsHosting)
                        {
                            var host = (await hostInformation()).hosts.First();
                            if (host.IsHosting())
                            {
                                if (OnHosting != null) await OnHosting.Invoke(host.host_display_name, host.target_display_name, (int)ViewerGraph.PlotDataPoints.LastOrDefault().Value.Value);

                                foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][HOST]).ToList())
                                    await OnMinorChangeTracked(channel, $"{Name} is now hosting {host.target_display_name} for {(int)ViewerGraph.PlotDataPoints.LastOrDefault().Value.Value} viewers!");

                                IsHosting = true;
                            }
                        }
                    }
                    else
                    {
                        ViewerGraph = new DatePlot(Name, "Time since start", "Viewers");
                        IsOnline = true;
                        IsHosting = false;
                        CurGame = StreamerStatus.stream.game;
                        ViewerGraph.AddValue(CurGame, 0, DateTime.Parse(StreamerStatus.stream.created_at).AddHours(-2));

                        if (OnLive != null) await OnLive.Invoke(this);
                        foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][ONLINE]).ToList())
                            await OnMinorChangeTracked(channel, (string)ChannelConfig[channel]["Notification"]);

                        SetTimer(60000, 60000);
                    }
                    await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
                }
                else
                    TimeoutCount = 0;

                if (isStreaming)
                {
                    if (VodUrl == null)
                        VodUrl = await GetVodAsync();

                    if (CurGame.CompareTo(StreamerStatus.stream.game) != 0)
                    {
                        CurGame = StreamerStatus.stream.game;
                        //ViewerGraph.AddValue(CurGame, StreamerStatus.stream.viewers);

                        foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][GAMECHANGE]).ToList())
                            await OnMinorChangeTracked(channel, $"{Name} switched games to **{CurGame}**");
                    }

                    await ModifyAsync(x => x.ViewerGraph.AddValue(CurGame, StreamerStatus.stream.viewers));

                    foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][SHOWEMBED]).ToList())
                        await OnMajorChangeTracked(channel, createEmbed((bool)ChannelConfig[channel][THUMBNAIL], (bool)ChannelConfig[channel][SHOWCHAT], (bool)ChannelConfig[channel][SHOWVOD]));
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        public override async void Conversion(object obj = null)
        {
            bool save = false;
            foreach (var channel in ChannelConfig.Keys.ToList())
            {
                if (!ChannelConfig[channel].ContainsKey(SHOWCHAT))
                {
                    ChannelConfig[channel][SHOWCHAT] = true;
                    ChannelConfig[channel][SHOWVOD] = true;
                    save = true;
                }
            }
            if (save)
                await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
        }

        private async Task<TwitchResult> streamerInformation()
        {
            TwitchResult tmpResult = await FetchJSONDataAsync<TwitchResult>($"https://api.twitch.tv/kraken/streams/{TwitchId}?client_id={Program.Config["Twitch"]}", acceptHeader);

            if (tmpResult.stream == null) tmpResult.stream = new APIResults.Twitch.Stream();
            if (tmpResult.stream.game == "" || tmpResult.stream.game == null) tmpResult.stream.game = "Nothing";

            return tmpResult;
        }

        private async Task<HostObject> hostInformation()
        {
            return await FetchJSONDataAsync<HostObject>($"https://tmi.twitch.tv/hosts?include_logins=1&host={TwitchId}");
        }

        public static async Task<ulong> GetIdFromUsername(string name)
        {
            var tmpResult = await FetchJSONDataAsync<dynamic>($"https://api.twitch.tv/kraken/users?login={name}&client_id={Program.Config["Twitch"]}", acceptHeader);

            return tmpResult["users"][0]["_id"];
        }

        public async Task<string> GetVodAsync()
        {
            var tmpResult = await FetchJSONDataAsync<dynamic>($"https://api.twitch.tv/kraken/channels/{TwitchId}/videos?client_id={Program.Config["Twitch"]}", acceptHeader);

            try
            {
                return tmpResult["videos"][0]["url"];
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static async Task<RootChatObject> GetVodChat(ulong vodID, string nextCursor = null)
        {
            return await FetchJSONDataAsync<RootChatObject>($"https://api.twitch.tv/v5/videos/" + vodID + "/comments?cursor=" + nextCursor, KeyValuePair.Create("Client-ID", $"{Program.Config["Twitch"]}"), acceptHeader);
        }

        public static async Task<RootChatObject> GetVodChat(ulong vodID, uint secsIntoVod = 0, bool fetchNexts = true)
        {
            var result = await FetchJSONDataAsync<RootChatObject>($"https://api.twitch.tv/v5/videos/" + vodID + "/comments?content_offset_seconds=" + secsIntoVod, KeyValuePair.Create("Client-ID", $"{Program.Config["Twitch"]}"), acceptHeader);
            string next = result._next;
            while (fetchNexts && next != null)
            {
                var tmpResult = await GetVodChat(vodID, next);
                next = tmpResult._next;
                result.comments = result.comments.Concat(tmpResult.comments).ToList();
            }
            if (result.comments == null) result.comments = new List<Comment>();
            else result.comments.Reverse();
            return result;
        }

        public Embed createEmbed(bool largeThumbnail = false, bool showChat = false, bool showVod = false)
        {
            Channel streamer = StreamerStatus.stream.channel;
            ViewerGraph.SetMaximumLine();

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = streamer.status;
            e.Url = streamer.url;
            e.WithCurrentTimestamp();

            if (VodUrl != null)
            {
                if (showVod)
                {
                    List<KeyValuePair<string, double>> games = new List<KeyValuePair<string, double>>();
                    for (int i = 0; i < ViewerGraph.PlotDataPoints.Count; i++)
                    {
                        string current = ViewerGraph.PlotDataPoints[i].Key;
                        games.Add(new KeyValuePair<string, double>(current, ViewerGraph.PlotDataPoints[i].Value.Key));

                        while (i < ViewerGraph.PlotDataPoints.Count && ViewerGraph.PlotDataPoints[i].Key.Equals(current))
                        {
                            i++;
                        }
                        i--;
                    }

                    string vods = "";
                    for (int i = Math.Max(0, games.Count - 6); i < games.Count; i++)
                    {
                        TimeSpan duration = i != games.Count - 1 ? OxyPlot.Axes.DateTimeAxis.ToDateTime(games[i + 1].Value) - OxyPlot.Axes.DateTimeAxis.ToDateTime(games[i].Value)
                                                                 : DateTime.UtcNow - OxyPlot.Axes.DateTimeAxis.ToDateTime(games[i].Value);
                        TimeSpan timestamp = OxyPlot.Axes.DateTimeAxis.ToDateTime(games[i].Value) - OxyPlot.Axes.DateTimeAxis.ToDateTime(games[0].Value);
                        vods += $"\n[{games[i].Key}]({VodUrl}?t={(int)timestamp.TotalMinutes}m) ({duration.ToString("hh")}h {duration.ToString("mm")}m)";
                    }
                    e.AddField("VOD Segments", vods);
                }

                if (showChat)
                {
                    var streamDuration = DateTime.UtcNow - OxyPlot.Axes.DateTimeAxis.ToDateTime(ViewerGraph.PlotDataPoints[0].Value.Key);
                    var chat = GetVodChat(ulong.Parse(VodUrl.Split("/").Last()), (uint)streamDuration.TotalSeconds - 10).Result;

                    if (chat.comments.Count >= 5) comments = chat.comments.Take(5).ToList();
                    else comments = chat.comments.Concat(comments.Take(Math.Min(comments.Count, 5 - chat.comments.Count))).ToList();

                    string chatPreview = "```asciidoc\n";
                    for (int i = comments.Count - 1; i >= 0; i--)
                    {
                        if (comments[i].message.body.Length > 100)
                            chatPreview += comments[i].commenter.display_name + ":: " + string.Join("", comments[i].message.body.Take(100)) + " [...]\n";
                        else
                            chatPreview += comments[i].commenter.display_name + ":: " + comments[i].message.body + "\n";
                    }
                    if (chatPreview.Equals("```asciidoc\n")) chatPreview += "Could not fetch chat messages.\nFollowers/Subs only, or empty?";
                    chatPreview += "```";

                    e.AddField("Chat Preview", chatPreview);
                }
            }

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            author.Url = streamer.url;
            author.IconUrl = streamer.logo;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
            footer.Text = "Twitch";
            e.Footer = footer;

            e.ThumbnailUrl = largeThumbnail ? ViewerGraph.DrawPlot() : $"{StreamerStatus.stream.preview.medium}?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = largeThumbnail ? $"{StreamerStatus.stream.preview.large}?rand={StaticBase.ran.Next(0, 99999999)}" : ViewerGraph.DrawPlot();

            //e.AddField("Game", CurGame, true);
            //e.AddField("Viewers", StreamerStatus.stream.viewers, true);

            return e.Build();
        }

        public async Task ModifyAsync(Action<TwitchTracker> action)
        {
            action(this);
            await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
        }

        public override void Dispose()
        {
            base.Dispose(true);
            GC.SuppressFinalize(this);
            ViewerGraph?.Dispose();
            ViewerGraph = null;
            SubscribeWebhookAsync(false).Wait();
        }

        public override string TrackerUrl()
        {
            return "https://www.twitch.tv/" + Name;
        }

        public override async Task UpdateTracker()
        {
            await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
        }
    }
}
