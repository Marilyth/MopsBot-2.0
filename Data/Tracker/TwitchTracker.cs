using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
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
        private static KeyValuePair<string, string> authHeader = new KeyValuePair<string, string>("Authorization", "Bearer ");
        private static KeyValuePair<string, string> clientIdHeader = new KeyValuePair<string, string>("Client-Id", Program.Config["TwitchKey"]);
        public event HostingEventHandler OnHosting;
        public event StatusEventHandler OnLive;
        public event StatusEventHandler OnOffline;
        public delegate Task HostingEventHandler(string hostName, string targetName, int viewers);
        public delegate Task StatusEventHandler(BaseTracker sender);
        private List<Comment> comments = new List<Comment>();
        public List<Tuple<string, DateTime>> GameChanges = new List<Tuple<string, DateTime>>();
        public DatePlot ViewerGraph;
        private TwitchStreamResult StreamerStatus;
        public Boolean IsOnline;
        public string CurGame, VodUrl, Callback, CallbackId;
        public bool IsHosting;
        public int TimeoutCount;
        public ulong TwitchId;
        public static readonly string GAMECHANGE = "NotifyOnGameChange", HOST = "NotifyOnHost", ONLINE = "NotifyOnOnline", OFFLINE = "NotifyOnOffline", SHOWEMBED = "ShowEmbed", SHOWCHAT = "ShowChat", SHOWVOD = "ShowVod", THUMBNAIL = "LargeThumbnail", SHOWGRAPH = "ShowGraph", SENDPDF = "SendGraphPDFAfterOffline", TRACKRERUN = "TrackReRun";

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
            config[SHOWGRAPH] = false;
            config[THUMBNAIL] = false;
            config[GAMECHANGE] = true;
            config[HOST] = false;
            config[OFFLINE] = true;
            config[ONLINE] = true;
            config[SENDPDF] = false;
            config[TRACKRERUN] = false;
        }

        public async override void PostInitialisation(object info = null)
        {
            //if (IsOnline) SetTimer(120000, StaticBase.ran.Next(5000, 120000));

            if (ViewerGraph != null)
                ViewerGraph.InitPlot();

            int counter = 0;
            while(!Program.Config.ContainsKey("TwitchToken") && counter++ < 5){
                await Task.Delay(30000);
            }

            await SubscribeWebhookAsync(false);
            await SubscribeWebhookAsync();
        }

        public static KeyValuePair<string, string>[] GetHelixHeaders(){
            var acceptHeader = KeyValuePair.Create("Accept", "application/vnd.twitchtv.v5+json");
            var authHeader = KeyValuePair.Create("Authorization", "Bearer " + Program.Config["TwitchToken"]);
            var clientIdHeader = KeyValuePair.Create("Client-Id", Program.Config["TwitchKey"]);
            var contentTypeHeader = KeyValuePair.Create("Content-Type", "application/json");

            return new KeyValuePair<string, string>[]{acceptHeader, authHeader, clientIdHeader, contentTypeHeader};
        } 

        /// <summary>
        /// Unsubscribes the specified callback from the Twitch EventSub.
        /// </summary>
        /// <param name="callbackId"></param>
        /// <returns>Twitch's response, or "Failed" if unsuccessful.</returns>
        public static async Task<string> UnsubscribeWebhookAsync(string callbackId){
            try
            {
                if (Program.Config.ContainsKey("TwitchToken") && !Program.Config["TwitchToken"].Equals(""))
                {
                    var url = "https://api.twitch.tv/helix/eventsub/subscriptions";
                    var response = await MopsBot.Module.Information.GetURLAsync(url + $"?id={callbackId}", System.Net.Http.HttpMethod.Delete, GetHelixHeaders());

                    return response;
                }

                return "Failed";
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error unsubscribing Twitch webhook {callbackId}", e));
                return "Failed";
            }
        }

        /// <summary>
        /// Subscribes the specified twitchId to the Twitch EventSub endpoints.
        /// Callback is set to the localtunnel Url.
        /// </summary>
        /// <param name="twitchId"></param>
        /// <returns>Twitch's response, or "Failed" if unsuccessful.</returns>
        public static async Task<string> SubscribeWebhookAsync(ulong twitchId){
            try
            {
                if (Program.Config.ContainsKey("TwitchToken") && !Program.Config["TwitchToken"].Equals(""))
                {
                    var url = "https://api.twitch.tv/helix/eventsub/subscriptions";
                    Dictionary<string, object> body = new Dictionary<string, object>(){
                        {"type", "stream.online"},
                        {"version", "1"},
                        {"condition", new Dictionary<string, string>(){
                            {"broadcaster_user_id", twitchId.ToString()}
                        }},
                        {"transport", new Dictionary<string, string>(){
                            {"method", "webhook"},
                            {"callback", $"{Program.Config["ServerAddress"]}:{Program.Config["Port"]}/api/webhook/twitch"},
                            {"secret", Program.Config["TwitchSecret"]} //Not recommended
                        }},
                    };

                    var response = await MopsBot.Module.Information.PostURLAsync(url, JsonConvert.SerializeObject(body), GetHelixHeaders());

                    return response;
                }

                return "Failed";
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error subscribing Twitch Id {twitchId}", e));
                return "Failed";
            }
        }

        /// <summary>
        /// Subscribes or unsubscribes the current Twitch tracker, depending on the flag.
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns>Twitch's response, or "Failed" if unsuccessful.</returns>
        public async Task<string> SubscribeWebhookAsync(bool subscribe = true)
        {
            return subscribe ? await SubscribeWebhookAsync(TwitchId) : await UnsubscribeWebhookAsync(CallbackId);
        }

        public async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                await SubscribeWebhookAsync(false);
                await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"Tried subscribing {Name} to Twitch webhooks:\n" + await SubscribeWebhookAsync()));

                await CheckStreamerInfoAsync();
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        bool timerChanged = false;
        public async Task CheckStreamerInfoAsync()
        {
            try
            {
                StreamerStatus = await streamerInformation();
                bool isStreaming = StreamerStatus.data.FirstOrDefault().type != null && StreamerStatus.data.FirstOrDefault().type.Equals("live");
                bool isRerun = false; //StreamerStatus.Stream?.BroadcastPlatform?.Contains("other") ?? false;

                if (!timerChanged && !IsOnline)
                {
                    //SetTimer(3600000, StaticBase.ran.Next(5000, 3600000));
                    timerChanged = true;
                }

                if (IsOnline != isStreaming)
                {
                    if (IsOnline)
                    {
                        if (++TimeoutCount >= 3)
                        {
                            TimeoutCount = 0;
                            IsOnline = false;

                            try
                            {
                                var pdf = ViewerGraph.DrawPlot(true, $"{Name}-{DateTime.UtcNow.ToString("MM-dd-yy_hh-mm")}");
                                foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][SENDPDF]).ToList())
                                    await (Program.Client.GetChannel(channel) as SocketTextChannel)?.SendFileAsync(pdf, "Graph PDF for personal use:");
                                File.Delete(pdf);
                            }
                            catch (Exception e)
                            {
                                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error sending graph by {Name}", e));
                            }

                            ViewerGraph?.Dispose();
                            ViewerGraph = null;
                            VodUrl = null;

                            foreach (var channelMessage in ToUpdate)
                                await Program.ReactionHandler.ClearHandler((IUserMessage)await ((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value));

                            ToUpdate = new Dictionary<ulong, ulong>();
                            GameChanges = new List<Tuple<string, DateTime>>();

                            if (OnOffline != null) await OnOffline.Invoke(this);
                            foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][OFFLINE] && (isRerun ? (bool)ChannelConfig[x][TRACKRERUN] : true)).ToList())
                                await OnMinorChangeTracked(channel, $"{Name} went Offline!");

                            //SetTimer(3600000, 3600000);

                        }
                        else if (!IsHosting)
                        {
                            var host = (await hostInformation()).hosts.First();
                            if (host.IsHosting())
                            {
                                if (OnHosting != null) await OnHosting.Invoke(host.host_display_name, host.target_display_name, (int)ViewerGraph.PlotDataPoints.LastOrDefault().Value.Value);

                                foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][HOST] && (isRerun ? (bool)ChannelConfig[x][TRACKRERUN] : true)).ToList())
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
                        CurGame = StreamerStatus.data.FirstOrDefault().game_name;
                        ViewerGraph.AddValue(CurGame, 0, StreamerStatus.data.FirstOrDefault().started_at.ToUniversalTime());
                        GameChanges.Add(Tuple.Create(StreamerStatus.data.FirstOrDefault().game_name, StreamerStatus.data.FirstOrDefault().started_at.ToUniversalTime()));

                        if (OnLive != null) await OnLive.Invoke(this);
                        foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][ONLINE] && (isRerun ? (bool)ChannelConfig[x][TRACKRERUN] : true)).ToList())
                            await OnMinorChangeTracked(channel, (string)ChannelConfig[channel]["Notification"]);

                        //SetTimer(120000, 120000);
                    }
                    await UpdateTracker();
                }
                else
                    TimeoutCount = 0;

                if (isStreaming)
                {
                    if (VodUrl == null)
                        VodUrl = await GetVodAsync();

                    if (CurGame.ToLower().CompareTo(StreamerStatus.data.FirstOrDefault().game_name.ToLower()) != 0)
                    {
                        CurGame = StreamerStatus.data.FirstOrDefault().game_name;
                        GameChanges.Add(Tuple.Create(StreamerStatus.data.FirstOrDefault().game_name, DateTime.UtcNow));
                        await UpdateTracker();

                        foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][GAMECHANGE] && (isRerun ? (bool)ChannelConfig[x][TRACKRERUN] : true)).ToList())
                            await OnMinorChangeTracked(channel, $"{Name} switched games to **{CurGame}**");
                    }

                    if (ChannelConfig.Any(x => (bool)x.Value[SHOWGRAPH]))
                        await ModifyAsync(x => x.ViewerGraph.AddValue(CurGame, StreamerStatus.data.FirstOrDefault().viewer_count));

                    foreach (ulong channel in ChannelConfig.Keys.Where(x => (bool)ChannelConfig[x][SHOWEMBED] && (isRerun ? (bool)ChannelConfig[x][TRACKRERUN] : true)).ToList())
                        await OnMajorChangeTracked(channel, createEmbed((bool)ChannelConfig[channel][THUMBNAIL], (bool)ChannelConfig[channel][SHOWCHAT], (bool)ChannelConfig[channel][SHOWVOD], (bool)ChannelConfig[channel][SHOWGRAPH]));
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
                if (!ChannelConfig[channel].ContainsKey(SHOWGRAPH))
                {
                    ChannelConfig[channel][SHOWGRAPH] = false;
                    save = true;
                }
            }
            if (save)
                await UpdateTracker();
        }

        private async Task<TwitchStreamResult> streamerInformation()
        {
            var tmpResult = await FetchJSONDataAsync<TwitchStreamResult>($"https://api.twitch.tv/helix/streams?user_id={TwitchId}", GetHelixHeaders());

            if (tmpResult.data.Count == 0) tmpResult.data.Add(new APIResults.Twitch.TwitchStreamInfo());
            if (string.IsNullOrWhiteSpace(tmpResult.data.FirstOrDefault().game_name)) tmpResult.data.FirstOrDefault().game_name = "Nothing";

            return tmpResult;
        }

        private async Task<HostObject> hostInformation()
        {
            return await FetchJSONDataAsync<HostObject>($"https://tmi.twitch.tv/hosts?include_logins=1&host={TwitchId}");
        }

        public static async Task<ulong> GetIdFromUsername(string name)
        {
            var tmpResult = await FetchJSONDataAsync<dynamic>($"https://api.twitch.tv/helix/users?login={name}", GetHelixHeaders());

            return tmpResult["data"][0]["id"];
        }

        public async Task<string> GetVodAsync()
        {
            var tmpResult = await FetchJSONDataAsync<dynamic>($"https://api.twitch.tv/helix/videos?user_id={TwitchId}", GetHelixHeaders());

            try
            {
                return tmpResult["data"][0]["url"];
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static async Task<RootChatObject> GetVodChat(ulong vodID, string nextCursor = null)
        {
            return await FetchJSONDataAsync<RootChatObject>($"https://api.twitch.tv/v5/videos/" + vodID + "/comments?cursor=" + nextCursor, KeyValuePair.Create("Client-ID", $"{Program.Config["TwitchKey"]}"), acceptHeader);
        }

        public static async Task<RootChatObject> GetVodChat(ulong vodID, uint secsIntoVod = 0, bool fetchNexts = true)
        {
            var result = await FetchJSONDataAsync<RootChatObject>($"https://api.twitch.tv/v5/videos/" + vodID + "/comments?content_offset_seconds=" + secsIntoVod, KeyValuePair.Create("Client-ID", $"{Program.Config["TwitchKey"]}"), acceptHeader);
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

        public Embed createEmbed(bool largeThumbnail = false, bool showChat = false, bool showVod = false, bool showGraph = false)
        {
            if (showGraph)
                ViewerGraph.SetMaximumLine();

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = StreamerStatus.data.FirstOrDefault().title;
            e.Url = TrackerUrl();
            e.WithCurrentTimestamp();

            if (VodUrl != null)
            {
                if (showVod)
                {
                    string vods = "";
                    for (int i = Math.Max(0, GameChanges.Count - 6); i < GameChanges.Count; i++)
                    {
                        TimeSpan duration = i != GameChanges.Count - 1 ? GameChanges[i + 1].Item2 - GameChanges[i].Item2
                                                                 : DateTime.UtcNow - GameChanges[i].Item2;
                        TimeSpan timestamp = GameChanges[i].Item2 - GameChanges[0].Item2;
                        vods += $"\n[{GameChanges[i].Item1}]({VodUrl}?t={(int)timestamp.TotalMinutes}m) ({duration.ToString("hh")}h {duration.ToString("mm")}m)";
                    }
                    e.AddField("VOD Segments", String.IsNullOrEmpty(vods) ? "/" : vods);
                }

                // Does not work in helix
                if (showChat)
                {
                    /*var streamDuration = DateTime.UtcNow - OxyPlot.Axes.DateTimeAxis.ToDateTime(ViewerGraph.PlotDataPoints[0].Value.Key);
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

                    e.AddField("Chat Preview", chatPreview);*/
                }
            }

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            author.Url = TrackerUrl();

            author.IconUrl = GetBroadcasterLogoUrl(TwitchId.ToString()).Result;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
            footer.Text = "Twitch";
            e.Footer = footer;

            if (largeThumbnail)
            {
                e.ImageUrl = $"{StreamerStatus.data.FirstOrDefault().thumbnail_url.Replace("{height}", "180").Replace("{width}", "320")}?rand={StaticBase.ran.Next(0, 99999999)}";
                if (showGraph)
                    e.ThumbnailUrl = ViewerGraph.DrawPlot();
            }
            else
            {
                e.ThumbnailUrl = $"{StreamerStatus.data.FirstOrDefault().thumbnail_url.Replace("{height}", "180").Replace("{width}", "320")}?rand={StaticBase.ran.Next(0, 99999999)}";
                if (showGraph)
                    e.ImageUrl = ViewerGraph.DrawPlot();
            }
            if (!showGraph)
            {
                e.AddField("Viewers", StreamerStatus.data.FirstOrDefault().viewer_count, true);
                e.AddField("Game", StreamerStatus.data.FirstOrDefault().game_name, true);
            }

            return e.Build();
        }

        public async Task ModifyAsync(Action<TwitchTracker> action)
        {
            action(this);
            await UpdateTracker();
        }

        public static async Task ObtainTwitchToken()
        {
            while (!Program.Config.ContainsKey("TwitchToken") || Program.Config["TwitchToken"].Equals(""))
            {
                try
                {
                    var result = MopsBot.Module.Information.PostURLAsync($"https://id.twitch.tv/oauth2/token?client_id={Program.Config["TwitchKey"]}&client_secret={Program.Config["TwitchSecret"]}&grant_type=client_credentials").Result;
                    Program.Config["TwitchToken"] = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(result)["access_token"].ToString();
                    authHeader = new KeyValuePair<string, string>("Authorization", "Bearer " + Program.Config["TwitchToken"]);
                    Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Getting token succeeded."));
                    RemoveBadSubscriptionsAsync();
                }
                catch (Exception)
                {
                    Program.MopsLog(new LogMessage(LogSeverity.Critical, "", $"Getting token has failed. Trying again in 30 seconds."));
                }
                await Task.Delay(30000);
            }
        }

        /// <summary>
        /// Iterates through the list of all subscriptions and deletes them if they are untracked, broken or stale.
        /// </summary>
        /// <returns></returns>
        public static async Task RemoveBadSubscriptionsAsync(){
            try{
	        var subscriptions = await GetAllSubscriptions();
                var foundSubscriptions = new HashSet<ulong>();

                // https://dev.twitch.tv/docs/api/reference#get-eventsub-subscriptions
                foreach(var subscription in subscriptions){
                    ulong twitchId = subscription["condition"]["broadcaster_user_id"];
                    string subscriptionId = subscription["id"];
                    string callbackUrl = subscription["transport"]["callback"];
                    string status = subscription["status"];

                    TwitchTracker tracker = (TwitchTracker)StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackers().FirstOrDefault(x => (x.Value as TwitchTracker).TwitchId.Equals(twitchId)).Value;

                    if(!callbackUrl.Equals($"{Program.Config["ServerAddress"]}:{Program.Config["Port"]}/api/webhook/twitch") || !status.Equals("enabled") || tracker is null){
                        // Bad subscription, remove it.
                        await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"Bad Twitch subscription for {twitchId}, removing."));
                        var response = await UnsubscribeWebhookAsync(subscriptionId);

                        if(response.Equals("Failed")){
                            foundSubscriptions.Add(twitchId);
                        }

                        // Try not to spam the endpoint too much.
                        await Task.Delay(100);
                    } else {
                        foundSubscriptions.Add(twitchId);

                        // Update tracker data if it isn't accurate.
                        if(tracker is not null && (!string.Equals(tracker.Callback, callbackUrl) || !string.Equals(tracker.CallbackId, subscriptionId))){
                            tracker.Callback = callbackUrl;
                            tracker.CallbackId = subscriptionId;
                            await tracker.UpdateTracker();
                        }
                    }
                }

                // Remove subscription information for trackers not found or removed in the Twitch response.
                foreach(var missingTracker in StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackers().Where(x => !foundSubscriptions.Contains((x.Value as TwitchTracker).TwitchId))){
                    await Program.MopsLog($"Twitch tracker for {missingTracker.Key} needs a new subscription. Resetting callbacks.");
                    TwitchTracker tracker = (TwitchTracker)missingTracker.Value;
                    tracker.Callback = null;
                    tracker.CallbackId = null;
                    await tracker.UpdateTracker();
                    // Will resubscribe itself on next CheckForChange_Elapsed.
                }
            } catch (Exception ex){
                await Program.MopsLog(new LogMessage(LogSeverity.Critical, "", $"Getting subscriptions failed.", ex));
            }
        }

        public static async Task<List<dynamic>> GetAllSubscriptions(){
            var result = await FetchJSONDataAsync<dynamic>("https://api.twitch.tv/helix/eventsub/subscriptions", 
                KeyValuePair.Create("Authorization", "Bearer " + Program.Config["TwitchToken"]),
                KeyValuePair.Create("Client-Id", Program.Config["TwitchKey"]));
            
            List<dynamic> data = new List<dynamic>(result["data"]);
            while(result["pagination"]["cursor"] is not null){
                var currentCursor = result["pagination"]["cursor"];
                result = await FetchJSONDataAsync<dynamic>($"https://api.twitch.tv/helix/eventsub/subscriptions?after={currentCursor}", 
                    KeyValuePair.Create("Authorization", "Bearer " + Program.Config["TwitchToken"]),
                    KeyValuePair.Create("Client-Id", Program.Config["TwitchKey"]));
                data.AddRange(result["data"]);
            }

            return data;
        }

        private static Dictionary<string, string> broadcasterLogoUrls = new Dictionary<string, string>();
        public static async Task<string> GetBroadcasterLogoUrl(string broadcasterId){
            if(!broadcasterLogoUrls.ContainsKey(broadcasterId)) broadcasterLogoUrls[broadcasterId] = (await FetchJSONDataAsync<dynamic>($"https://api.twitch.tv/helix/users?id={broadcasterId}", GetHelixHeaders()))["data"][0]["profile_image_url"];
            return broadcasterLogoUrls[broadcasterId];
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
