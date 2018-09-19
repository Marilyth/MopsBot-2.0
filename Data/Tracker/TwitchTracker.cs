using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
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
    public class TwitchTracker : ITracker
    {
        public Plot ViewerGraph;
        private TwitchResult StreamerStatus;

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, ulong> ToUpdate;
        public Boolean IsOnline;
        public string CurGame;
        public bool isThumbnailLarge;
        public int TimeoutCount;

        public TwitchTracker() : base(60000, ExistingTrackers * 2000)
        {
        }

        public async override void PostInitialisation()
        {
            if(ViewerGraph != null)
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

        public async Task setReaction(IUserMessage message)
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
                string query = MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/channels/{Name}?client_id={Program.Config["Twitch"]}").Result;
                Channel checkExists = JsonConvert.DeserializeObject<Channel>(query);
                var test = checkExists.broadcaster_language;
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
                            ViewerGraph.Dispose();
                            ToUpdate = new Dictionary<ulong, ulong>();

                            foreach (var channelMessage in ToUpdate)
                                await Program.ReactionHandler.ClearHandler((IUserMessage)await ((ITextChannel)Program.Client.GetChannel(channelMessage.Key)).GetMessageAsync(channelMessage.Value));
                            
                            foreach (ulong channel in ChannelMessages.Keys)
                                await OnMinorChangeTracked(channel, $"{Name} went Offline!");
                        }
                    }
                    else
                    {
                        ViewerGraph = new Plot(Name, "Time In Minutes", "Viewers", IsOnline);
                        IsOnline = true;
                        CurGame = StreamerStatus.stream.game;

                        foreach (ulong channel in ChannelMessages.Keys)
                            await OnMinorChangeTracked(channel, ChannelMessages[channel]);
                    }
                    await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);
                }
                else
                    TimeoutCount = 0;

                if (isStreaming)
                {
                    ViewerGraph.AddValue(CurGame, StreamerStatus.stream.viewers);
                    if (CurGame.CompareTo(StreamerStatus.stream.game) != 0)
                    {
                        CurGame = StreamerStatus.stream.game;
                        ViewerGraph.AddValue(CurGame, StreamerStatus.stream.viewers);

                        foreach (ulong channel in ChannelMessages.Keys)
                            await OnMinorChangeTracked(channel, $"{Name} switched games to **{CurGame}**");
                    }

                    await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);

                    foreach (ulong channel in ChannelIds.ToList())
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
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/streams/{Name}?client_id={Program.Config["Twitch"]}");

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
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/streams/{name}?client_id={Program.Config["Twitch"]}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            TwitchResult tmpResult = JsonConvert.DeserializeObject<TwitchResult>(query, _jsonWriter);
            if (tmpResult.stream == null) tmpResult.stream = new APIResults.Twitch.Stream();
            if (tmpResult.stream.game == "" || tmpResult.stream.game == null) tmpResult.stream.game = "Nothing";

            return tmpResult;
        }

        public Embed createEmbed()
        {
            Channel streamer = StreamerStatus.stream.channel;

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = streamer.status;
            e.Url = streamer.url;
            e.Description = "**For people with manage channel permission**:\n🖌: Change chart colour\n🔄: Switch thumbnail and chart position";

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

            e.AddField("Game", CurGame, true);
            e.AddField("Viewers", StreamerStatus.stream.viewers, true);

            return e.Build();
        }

        private async Task recolour(ReactionHandlerContext context)
        {
            if (((IGuildUser)await context.Reaction.Channel.GetUserAsync(context.Reaction.UserId)).GetPermissions((IGuildChannel)context.Channel).ManageChannel)
            {
                ViewerGraph.Recolour();

                foreach (ulong channel in ChannelIds)
                    await OnMajorChangeTracked(channel, createEmbed());
            }
        }

        private async Task switchThumbnail(ReactionHandlerContext context)
        {
            if (((IGuildUser)await context.Reaction.Channel.GetUserAsync(context.Reaction.UserId)).GetPermissions((IGuildChannel)context.Channel).ManageChannel)
            {
                isThumbnailLarge = !isThumbnailLarge;
                await StaticBase.Trackers[TrackerType.Twitch].UpdateDBAsync(this);

                foreach (ulong channel in ChannelIds)
                    await OnMajorChangeTracked(channel, createEmbed());
            }
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            ViewerGraph.RemovePlot();
            ViewerGraph = null;
        }

        public override string TrackerUrl()
        {
            return "https://www.twitch.tv/" + Name;
        }
    }
}
