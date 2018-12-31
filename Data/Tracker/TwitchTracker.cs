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
        public Plot ViewerGraph;
        private TwitchResult StreamerStatus;
        public Boolean IsOnline;
        public string CurGame, VodUrl;
        public bool isThumbnailLarge;
        public int TimeoutCount;
        public ulong TwitchId;

        public TwitchTracker() : base(60000, ExistingTrackers * 2000)
        {
        }

        public async override void PostInitialisation()
        {
            if (ViewerGraph != null)
                ViewerGraph.InitPlot();

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

        public TwitchTracker(string streamerName) : base(60000)
        {
            ToUpdate = new Dictionary<ulong, ulong>();
            ChannelMessages = new Dictionary<ulong, string>();
            Name = streamerName;
            IsOnline = false;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                ulong Id = GetIdFromUsername(streamerName).Result;
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"Streamer {TrackerUrl()} could not be found on Twitch!");
            }
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
                            Console.WriteLine("\n" + $"{DateTime.Now} {Name} went Offline");
                            VodUrl = null;
                            ViewerGraph.Dispose();
                            ViewerGraph = null;

                            foreach (var channelMessage in ToUpdate)
                                await Program.ReactionHandler.ClearHandler((IUserMessage)await ((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value));

                            ToUpdate = new Dictionary<ulong, ulong>();

                            foreach (ulong channel in ChannelMessages.Keys.ToList())
                                await OnMinorChangeTracked(channel, $"{Name} went Offline!");
                        }
                    }
                    else
                    {
                        ViewerGraph = new Plot(Name, "Time In Minutes", "Viewers");
                        IsOnline = true;
                        CurGame = StreamerStatus.stream.game;
                        ViewerGraph.AddValue(CurGame, 0, DateTime.Parse(StreamerStatus.stream.created_at));

                        foreach (ulong channel in ChannelMessages.Keys.ToList())
                            await OnMinorChangeTracked(channel, ChannelMessages[channel]);
                    }
                    await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
                }
                else
                    TimeoutCount = 0;

                if (isStreaming)
                {
                    if (VodUrl == null)
                        VodUrl = await GetVodAsync();
                    ViewerGraph.AddValue(CurGame, StreamerStatus.stream.viewers);
                    if (CurGame.CompareTo(StreamerStatus.stream.game) != 0)
                    {
                        CurGame = StreamerStatus.stream.game;
                        ViewerGraph.AddValue(CurGame, StreamerStatus.stream.viewers);

                        foreach (ulong channel in ChannelMessages.Keys.ToList())
                            await OnMinorChangeTracked(channel, $"{Name} switched games to **{CurGame}**");
                    }

                    await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);

                    foreach (ulong channel in ChannelMessages.Keys.ToList())
                        await OnMajorChangeTracked(channel, createEmbed());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + $"[Error] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<TwitchResult> streamerInformation()
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/streams/{TwitchId}?client_id={Program.Config["Twitch"]}", acceptHeader);

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            TwitchResult tmpResult = JsonConvert.DeserializeObject<TwitchResult>(query, _jsonWriter);

            if (tmpResult.stream == null) tmpResult.stream = new APIResults.Twitch.Stream();
            if (tmpResult.stream.game == "" || tmpResult.stream.game == null) tmpResult.stream.game = "Nothing";

            return tmpResult;
        }

        private async static Task<TwitchResult> streamerInformation(string name)
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/streams/{await GetIdFromUsername(name)}?client_id={Program.Config["Twitch"]}", acceptHeader);

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            TwitchResult tmpResult = JsonConvert.DeserializeObject<TwitchResult>(query, _jsonWriter);
            if (tmpResult.stream == null) tmpResult.stream = new APIResults.Twitch.Stream();
            if (tmpResult.stream.game == "" || tmpResult.stream.game == null) tmpResult.stream.game = "Nothing";

            return tmpResult;
        }

        public static async Task<ulong> GetIdFromUsername(string name)
        {
            var result = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/users?login={name}&client_id={Program.Config["Twitch"]}", acceptHeader);
            var tmpResult = JsonConvert.DeserializeObject<dynamic>(result);

            return tmpResult["users"][0]["_id"];
        }

        public async Task<string> GetVodAsync()
        {
            var result = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/channels/{TwitchId}/videos?client_id={Program.Config["Twitch"]}", acceptHeader);
            var tmpResult = JsonConvert.DeserializeObject<dynamic>(result);

            try
            {
                return tmpResult["videos"][0]["url"];
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public Embed createEmbed()
        {
            Channel streamer = StreamerStatus.stream.channel;

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = streamer.status;
            e.Url = streamer.url;
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
                    e.Description += $"\n[{games[i].Key}]({VodUrl}?t={timestamp.TotalMinutes}m) ({duration.Hours}h {duration.Minutes}m)";
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

            e.ThumbnailUrl = isThumbnailLarge ? ViewerGraph.DrawPlot() : $"{StreamerStatus.stream.preview.medium}?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = isThumbnailLarge ? $"{StreamerStatus.stream.preview.large}?rand={StaticBase.ran.Next(0, 99999999)}" : ViewerGraph.DrawPlot();

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
                isThumbnailLarge = !isThumbnailLarge;
                await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);

                foreach (ulong channel in ChannelMessages.Keys.ToList())
                    await OnMajorChangeTracked(channel, createEmbed());
            }
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            ViewerGraph.Dispose();
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
                Name = this.Name,
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId,
                IsThumbnailLarge = this.isThumbnailLarge
            };
        }

        public override void Update(params string[] args)
        {
            var channelId = ulong.Parse(args[2].Split(":")[1]);
            ChannelMessages[channelId] = args[1];
            isThumbnailLarge = bool.Parse(args[3]);
        }

        public new struct ContentScope
        {
            public string Name;
            public string Notification;
            public string Channel;
            public bool IsThumbnailLarge;
        }
    }
}
