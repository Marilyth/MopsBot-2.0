using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data.Tracker
{
    public class TwitchTracker : ITracker
    {
        private Plot viewerGraph;
        private APIResults.TwitchResult StreamerStatus;
        public Dictionary<ulong, ulong> ToUpdate;
        public Boolean IsOnline;
        public string CurGame;
        public Dictionary<ulong, string> ChannelMessages;

        public TwitchTracker() : base(60000)
        {
        }

        public override void PostInitialisation()
        {
            viewerGraph = new Plot(Name, "Time In Minutes", "Viewers", IsOnline);
        }

        public TwitchTracker(string streamerName) : base(60000, 0)
        {
            viewerGraph = new Plot(streamerName, "Time In Minutes", "Viewers", false);

            Console.Out.WriteLine($"{DateTime.Now} Started Twitchtracker for {streamerName}");
            ToUpdate = new Dictionary<ulong, ulong>();
            ChannelMessages = new Dictionary<ulong, string>();
            Name = streamerName;
            IsOnline = false;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                string query = MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/channels/{Name}?client_id={Program.twitchId}").Result;
                Channel checkExists = JsonConvert.DeserializeObject<Channel>(query);
                var test = checkExists.broadcaster_language;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Person `{Name}` could not be found on Twitch!");
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
                        IsOnline = false;
                        Console.Out.WriteLine($"{DateTime.Now} {Name} went Offline");
                        viewerGraph.RemovePlot();
                        viewerGraph = new Plot(Name, "Time In Minutes", "Viewers", false);
                        ToUpdate = new Dictionary<ulong, ulong>();

                        foreach (ulong channel in ChannelMessages.Keys)
                            await OnMinorChangeTracked(channel, $"{Name} went Offline!");
                    }
                    else
                    {
                        IsOnline = true;
                        CurGame = StreamerStatus.stream.game;
                        viewerGraph.SwitchTitle(CurGame);

                        foreach (ulong channel in ChannelMessages.Keys)
                            await OnMinorChangeTracked(channel, ChannelMessages[channel]);
                    }
                    StaticBase.trackers["twitch"].SaveJson();
                }

                if (IsOnline)
                {
                    viewerGraph.AddValue(StreamerStatus.stream.viewers);
                    if (CurGame.CompareTo(StreamerStatus.stream.game) != 0)
                    {
                        CurGame = StreamerStatus.stream.game;
                        viewerGraph.SwitchTitle(CurGame);
                        viewerGraph.AddValue(StreamerStatus.stream.viewers);

                        foreach (ulong channel in ChannelMessages.Keys)
                            await OnMinorChangeTracked(channel, $"{Name} switched games to **{CurGame}**");
                        StaticBase.trackers["twitch"].SaveJson();
                    }

                    foreach (ulong channel in ChannelIds)
                        await OnMajorChangeTracked(channel, createEmbed());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " " + e.Message);
            }
        }

        private async Task<TwitchResult> streamerInformation()
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/streams/{Name}?client_id={Program.twitchId}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            TwitchResult tmpResult = JsonConvert.DeserializeObject<TwitchResult>(query, _jsonWriter);
            if (tmpResult.stream == null) tmpResult.stream = new APIResults.Stream();
            if (tmpResult.stream.game == "" || tmpResult.stream.game == null) tmpResult.stream.game = "Nothing";

            return tmpResult;
        }

        private async static Task<TwitchResult> streamerInformation(string name)
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/streams/{name}?client_id={Program.twitchId}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            TwitchResult tmpResult = JsonConvert.DeserializeObject<TwitchResult>(query, _jsonWriter);
            if (tmpResult.stream == null) tmpResult.stream = new APIResults.Stream();
            if (tmpResult.stream.game == "" || tmpResult.stream.game == null) tmpResult.stream.game = "Nothing";

            return tmpResult;
        }

        public EmbedBuilder createEmbed()
        {
            Channel streamer = StreamerStatus.stream.channel;

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = streamer.status;
            e.Url = streamer.url;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            author.Url = streamer.url;
            author.IconUrl = streamer.logo;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
            footer.Text = "Twitch";
            e.Footer = footer;

            e.ThumbnailUrl = $"{StreamerStatus.stream.preview.medium}?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = $"{viewerGraph.DrawPlot()}";

            e.AddInlineField("Game", CurGame);
            e.AddInlineField("Viewers", StreamerStatus.stream.viewers);

            return e;
        }
    }
}
