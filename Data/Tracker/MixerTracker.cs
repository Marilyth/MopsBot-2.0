using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults.Mixer;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
//https://dev.mixer.com/reference/constellation/events/live
//https://dev.mixer.com/reference/webhooks
namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public class MixerTracker : BaseUpdatingTracker
    {
        public event HostingEventHandler OnHosting;
        public event StatusEventHandler OnLive;
        public event StatusEventHandler OnOffline;
        public delegate Task HostingEventHandler(string hostName, string targetName, int viewers);
        public delegate Task StatusEventHandler(BaseTracker sender);
        public DatePlot ViewerGraph;
        private MixerResult StreamerStatus;
        public Boolean IsOnline;
        public string CurGame;
        public int TimeoutCount;
        public ulong MixerId;
        //public DateTime WebhookExpire = DateTime.Now;
        public static readonly string GAMECHANGE = "NotifyOnGameChange", ONLINE = "NotifyOnOnline", OFFLINE = "NotifyOnOffline", SHOWEMBED = "ShowEmbed", SHOWTIMESTAMPS = "ShowTimestamps", THUMBNAIL = "LargeThumbnail";

        public MixerTracker() : base()
        {
        }

        public MixerTracker(string streamerName) : base()
        {
            Name = streamerName;
            IsOnline = false;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                MixerId = GetIdFromUsername(streamerName).Result;
                SetTimer();
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Streamer {TrackerUrl()} could not be found on Mixer!", e);
            }
        }

        public async override void PostChannelAdded(ulong channelId)
        {
            base.PostChannelAdded(channelId);

            var config = ChannelConfig[channelId];
            config[SHOWEMBED] = true;
            config[THUMBNAIL] = false;
            config[GAMECHANGE] = true;
            config[OFFLINE] = true;
            config[ONLINE] = true;
            config[SHOWTIMESTAMPS] = true;

            await UpdateTracker();
        }

        public async override void PostInitialisation(object info = null)
        {
            if (IsOnline) SetTimer(60000, StaticBase.ran.Next(5000, 60000));

            if (ViewerGraph != null)
                ViewerGraph.InitPlot();

            /*if((WebhookExpire - DateTime.Now).TotalMinutes < 10){
                await SubscribeWebhookAsync();
            }*/
        }

        public async Task<string> SubscribeWebhookAsync(bool subscribe = true)
        {
            try
            {
                var url = "https://mixer.com/api/v1/hooks";
                var data = "{ \"kind\": \"web\", \"events\":[\"channel:" + MixerId + ":broadcast\"], \"url\":\"https://dev.mixer.com/onHook\" }";

                var test = await MopsBot.Module.Information.PostURLAsync(url, data,
                    KeyValuePair.Create("Authorization", "Secret " + Program.Config["MixerSecret"]),
                    KeyValuePair.Create("Client-ID", Program.Config["MixerKey"])
                );

                //WebhookExpire = DateTime.Now.AddDays(90);
                await UpdateTracker();

                return test;
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
                return "Failed";
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                /*if((WebhookExpire - DateTime.Now).TotalMinutes < 10){
                    await SubscribeWebhookAsync();
                }*/

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
                bool isStreaming = StreamerStatus.online;

                if (IsOnline != isStreaming)
                {
                    if (IsOnline)
                    {
                        if (++TimeoutCount >= 3)
                        {
                            TimeoutCount = 0;
                            IsOnline = false;

                            ViewerGraph?.Dispose();
                            ViewerGraph = null;

                            foreach (var channelMessage in ToUpdate)
                                await Program.ReactionHandler.ClearHandler((IUserMessage)await ((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value));

                            ToUpdate = new Dictionary<ulong, ulong>();

                            if (OnOffline != null) await OnOffline.Invoke(this);
                            foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][OFFLINE]).ToList())
                                await OnMinorChangeTracked(channel, $"{Name} went Offline!");

                            SetTimer(600000, 600000);

                        }
                    }
                    else
                    {
                        ViewerGraph = new DatePlot(Name + "Mixer", "Time since start", "Viewers");
                        IsOnline = true;
                        CurGame = StreamerStatus.type?.name ?? "Nothing";
                        ViewerGraph.AddValue(CurGame, 0, (await GetBroadcastStartTime()).AddHours(-2));

                        if (OnLive != null) await OnLive.Invoke(this);
                        foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][ONLINE]).ToList())
                            await OnMinorChangeTracked(channel, (string)ChannelConfig[channel]["Notification"]);

                        SetTimer(60000, 60000);
                    }
                    await UpdateTracker();
                }
                else
                    TimeoutCount = 0;

                if (isStreaming)
                {
                    if (CurGame.CompareTo(StreamerStatus.type?.name ?? "Nothing") != 0)
                    {
                        CurGame = StreamerStatus.type?.name ?? "Noting";

                        foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][GAMECHANGE]).ToList())
                            await OnMinorChangeTracked(channel, $"{Name} switched games to **{CurGame}**");
                    }

                    await ModifyAsync(x => x.ViewerGraph.AddValue(CurGame, StreamerStatus.viewersCurrent));

                    foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][SHOWEMBED]).ToList())
                        await OnMajorChangeTracked(channel, createEmbed((bool)ChannelConfig[channel][THUMBNAIL], (bool)ChannelConfig[channel][SHOWTIMESTAMPS]));
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        private async Task<MixerResult> streamerInformation()
        {
            var tmpResult = await FetchJSONDataAsync<MixerResult>($"https://mixer.com/api/v1/channels/{Name}");

            return tmpResult;
        }

        public static async Task<ulong> GetIdFromUsername(string name)
        {
            var tmpResult = await FetchJSONDataAsync<dynamic>($"https://mixer.com/api/v1/channels/{name}?fields=id");

            return tmpResult["id"];
        }

        public async Task<DateTime> GetBroadcastStartTime()
        {
            var tmpResult = await FetchJSONDataAsync<dynamic>($"https://mixer.com/api/v1/channels/{MixerId}/broadcast");

            return DateTime.Parse(tmpResult["startedAt"].ToString());
        }

        public Embed createEmbed(bool largeThumbnail = false, bool showTimestamps = false)
        {
            ViewerGraph.SetMaximumLine();

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0, 163, 243);
            e.Title = StreamerStatus.name;
            e.Url = TrackerUrl();
            e.WithCurrentTimestamp();

            if (showTimestamps)
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
                    vods += $"\n{games[i].Key} ({duration.ToString("hh")}h {duration.ToString("mm")}m)";
                }
                e.AddField("VOD Segments", vods);
            }


            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            author.Url = TrackerUrl();
            author.IconUrl = StreamerStatus.user.avatarUrl;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://img.pngio.com/mixer-logo-png-images-png-cliparts-free-download-on-seekpng-mixer-logo-png-300_265.png";
            footer.Text = "Mixer";
            e.Footer = footer;

            e.ThumbnailUrl = largeThumbnail ? ViewerGraph.DrawPlot() : $"https://thumbs.mixer.com/channel/{MixerId}.small.jpg?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = largeThumbnail ? $"https://thumbs.mixer.com/channel/{MixerId}.small.jpg?rand={StaticBase.ran.Next(0, 99999999)}" : ViewerGraph.DrawPlot();

            return e.Build();
        }

        public async Task ModifyAsync(Action<MixerTracker> action)
        {
            action(this);
            await StaticBase.Trackers[TrackerType.Mixer].UpdateDBAsync(this);
        }

        public override void Dispose()
        {
            base.Dispose(true);
            GC.SuppressFinalize(this);
            ViewerGraph?.Dispose();
            ViewerGraph = null;
            //SubscribeWebhookAsync(false).Wait();
        }

        public override string TrackerUrl()
        {
            return "https://mixer.com/" + Name;
        }

        public override async Task UpdateTracker()
        {
            await StaticBase.Trackers[TrackerType.Mixer].UpdateDBAsync(this);
        }
    }
}