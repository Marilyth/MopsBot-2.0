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
using TwitchLib;
using TwitchLib.Api;

namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public class TwitchTracker : BaseUpdatingTracker
    {
        private static KeyValuePair<string, string> acceptHeader = new KeyValuePair<string, string>("Accept", "application/vnd.twitchtv.v5+json");
        public event HostingEventHandler OnHosting;
        public delegate Task HostingEventHandler(string hostName, string targetName, int viewers);
        public DatePlot ViewerGraph;
        private TwitchResult StreamerStatus;
        public Boolean IsOnline;
        public string CurGame, VodUrl;
        public bool isThumbnailLarge, IsHosting;
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, NotifyConfig> Specifications;
        public int TimeoutCount;
        public ulong TwitchId;

        public TwitchTracker() : base()
        {
        }

        public TwitchTracker(Dictionary<string, string> args) : base(){
            isThumbnailLarge = bool.Parse(args["IsThumbnailLarge"]);
            base.SetBaseValues(args, true);

            //Check if Name ist valid
            try{
                new TwitchTracker(Name).Dispose();
                SetTimer();
            } catch (Exception e){
                this.Dispose();
                throw e;
            }

            if(StaticBase.Trackers[TrackerType.Twitch].GetTrackers().ContainsKey(Name)){
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.Twitch].GetTrackers()[Name];
                curTracker.ChannelMessages[ulong.Parse(args["Channel"].Split(":")[1])] = args["Notification"];
                StaticBase.Trackers[TrackerType.Twitch].UpdateContent(new Dictionary<string, Dictionary<string, string>>{{"NewValue", args}, {"OldValue", args}}).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }

        public TwitchTracker(string streamerName) : base()
        {
            Name = streamerName;
            IsOnline = false;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                ulong Id = GetIdFromUsername(streamerName).Result;
                Specifications = new Dictionary<ulong, NotifyConfig>();
                SetTimer();
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"Streamer {TrackerUrl()} could not be found on Twitch!");
            }
        }

        public async override void PostInitialisation(object info = null)
        {
            if(IsOnline) SetTimer(60000, StaticBase.ran.Next(5000, 60000));

            if (ViewerGraph != null)
                ViewerGraph.InitPlot();

            if(Specifications == null){
                Specifications = new Dictionary<ulong, NotifyConfig>();
                foreach(var channel in ChannelMessages){
                    Specifications.Add(channel.Key, new NotifyConfig(){
                        ShowEmbed = true,
                        LargeThumbnail = isThumbnailLarge,
                        NotifyOnGameChange = true,
                        NotifyOnHost = false,
                        NotifyOnOffline = true,
                        NotifyOnOnline = true
                    });
                }

                await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
            }

            foreach (var channelMessage in ToUpdate)
            {
                try
                {
                    await setReaction((IUserMessage)((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value).Result);
                }
                catch
                {
                    // if(Program.Client.GetChannel(channelMessage.Key)==null){
                    //     StaticBase.Trackers["twitch"].TryRemoveTracker(Name, channelMessage.Key);
                    //     Console.WriteLine("\n" + $"remove tracker for {Name} in channel: {channelMessage.Key}");  
                    // }
                    //
                    // the Tracker Should be removed on the first Event Call
                }
            }
        }

        public async override Task setReaction(IUserMessage message)
        {
            //await message.RemoveAllReactionsAsync();
            await Program.ReactionHandler.AddHandler(message, new Emoji("🖌"), recolour);
            await Program.ReactionHandler.AddHandler(message, new Emoji("🔄"), switchThumbnail);
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                if (TwitchId == 0)
                {
                    TwitchId = await GetIdFromUsername(Name);
                    await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
                }

                StreamerStatus = await streamerInformation();

                Boolean isStreaming = StreamerStatus.stream.channel != null;

                if (IsOnline != isStreaming)
                {
                    if (IsOnline)
                    {
                        if (++TimeoutCount >= 10)
                        {
                            TimeoutCount = 0;
                            IsOnline = false;
                            VodUrl = null;
                            ViewerGraph?.Dispose();
                            ViewerGraph = null;

                            foreach (var channelMessage in ToUpdate)
                                await Program.ReactionHandler.ClearHandler((IUserMessage)await ((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value));

                            ToUpdate = new Dictionary<ulong, ulong>();

                            foreach (ulong channel in ChannelMessages.Keys.Where(x => Specifications[x].NotifyOnOffline).ToList())
                                await OnMinorChangeTracked(channel, $"{Name} went Offline!");

                            SetTimer(600000, 600000);

                        } else if(!IsHosting) {
                            var host = (await hostInformation()).hosts.First();
                            if(host.IsHosting()){
                                await OnHosting.Invoke(host.host_display_name, host.target_display_name, (int)ViewerGraph.PlotDataPoints.LastOrDefault().Value.Value);

                                foreach (ulong channel in ChannelMessages.Keys.Where(x => Specifications[x].NotifyOnHost).ToList())
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

                        foreach (ulong channel in ChannelMessages.Keys.Where(x => Specifications[x].NotifyOnOnline).ToList())
                            await OnMinorChangeTracked(channel, ChannelMessages[channel]);

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

                        foreach (ulong channel in ChannelMessages.Keys.Where(x => Specifications[x].NotifyOnGameChange).ToList())
                            await OnMinorChangeTracked(channel, $"{Name} switched games to **{CurGame}**");
                    }

                    await ModifyAsync(x => x.ViewerGraph.AddValue(CurGame, StreamerStatus.stream.viewers));

                    foreach (ulong channel in ChannelMessages.Keys.Where(x => Specifications[x].ShowEmbed).ToList())
                        await OnMajorChangeTracked(channel, createEmbed(Specifications[channel].LargeThumbnail));
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        private async Task<TwitchResult> streamerInformation()
        {
            TwitchResult tmpResult = await FetchJSONDataAsync<TwitchResult>($"https://api.twitch.tv/kraken/streams/{TwitchId}?client_id={Program.Config["Twitch"]}", acceptHeader);

            if (tmpResult.stream == null) tmpResult.stream = new APIResults.Twitch.Stream();
            if (tmpResult.stream.game == "" || tmpResult.stream.game == null) tmpResult.stream.game = "Nothing";

            return tmpResult;
        }

        private async Task<HostObject> hostInformation(){
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

        public Embed createEmbed(bool largeThumbnail = false)
        {
            Channel streamer = StreamerStatus.stream.channel;
            ViewerGraph.SetMaximumLine();

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = streamer.status;
            e.Url = streamer.url;
            e.WithCurrentTimestamp();
            e.Description = "**For people with manage channel permission**:\n🖌: Change chart colour\n🔄: Switch thumbnail and chart position\n";

            if (VodUrl != null)
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

                e.Description += "\n**VOD Segments**";
                for (int i = Math.Max(0, games.Count - 10); i < games.Count; i++)
                {
                    TimeSpan duration = i != games.Count - 1 ? OxyPlot.Axes.DateTimeAxis.ToDateTime(games[i + 1].Value) - OxyPlot.Axes.DateTimeAxis.ToDateTime(games[i].Value)
                                                             : DateTime.UtcNow - OxyPlot.Axes.DateTimeAxis.ToDateTime(games[i].Value);
                    TimeSpan timestamp = OxyPlot.Axes.DateTimeAxis.ToDateTime(games[i].Value) - OxyPlot.Axes.DateTimeAxis.ToDateTime(games[0].Value);
                    e.Description += $"\n[{games[i].Key}]({VodUrl}?t={(int)timestamp.TotalMinutes}m) ({duration.ToString("hh")}h {duration.ToString("mm")}m)";
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
                await ModifyAsync(x => x.isThumbnailLarge = !x.isThumbnailLarge);

                foreach (ulong channel in ChannelMessages.Keys.ToList())
                    await OnMajorChangeTracked(channel, createEmbed());
            }
        }

        public async Task ModifyAsync(Action<TwitchTracker> action){
            action(this);
            await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
        }

        public new void Dispose()
        {
            base.Dispose(true);
            GC.SuppressFinalize(this);
            ViewerGraph?.Dispose();
            ViewerGraph = null;
        }

        public override string TrackerUrl()
        {
            return "https://www.twitch.tv/" + Name;
        }

        public override Dictionary<string, object> GetParameters(ulong guildId)
        {
            var parentParameters = base.GetParameters(guildId);
            (parentParameters["Parameters"] as Dictionary<string, object>)["IsThumbnailLarge"] = new bool[] { true, false };
            return parentParameters;
        }

        public override object GetAsScope(ulong channelId)
        {
            return new ContentScope()
            {
                Id = this.Name,
                _Name = this.Name,
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId,
                IsThumbnailLarge = this.isThumbnailLarge
            };
        }

        public override void Update(Dictionary<string, Dictionary<string, string>> args)
        {
            base.Update(args);
            isThumbnailLarge = bool.Parse(args["NewValue"]["IsThumbnailLarge"]);
        }

        public new struct ContentScope
        {
            public string Id;
            public string _Name;
            public string Notification;
            public string Channel;
            public bool IsThumbnailLarge;
        }

        public class NotifyConfig{
            public bool ShowEmbed, 
            NotifyOnGameChange, 
            NotifyOnOffline, 
            NotifyOnOnline, 
            NotifyOnHost, 
            LargeThumbnail;
        }
    }
}
