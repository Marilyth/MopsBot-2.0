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
        public string Name, CurGame;
        public Dictionary<ulong, string> ChannelMessages;

        public TwitchTracker() : base(60000)
        {
        }

        public override void PostInitialisation()
        {
            viewerGraph = new Plot(Name, "Time In Minutes","Viewers", IsOnline);
        }

        public TwitchTracker(string streamerName) : base(60000, 0)
        {
            viewerGraph = new Plot(streamerName, "Time In Minutes","Viewers", false);

            Console.Out.WriteLine($"{DateTime.Now} Started Twitchtracker for {streamerName}");
            ToUpdate = new Dictionary<ulong, ulong>();
            ChannelMessages = new Dictionary<ulong, string>();
            Name = streamerName;
            IsOnline = false;
        }

        public TwitchTracker(string[] initArray) : base(60000)
        {
            ToUpdate = new Dictionary<ulong, ulong>();
            ChannelMessages = new Dictionary<ulong, string>();

            Name = initArray[0];
            IsOnline = Boolean.Parse(initArray[1]);
            viewerGraph = new Plot(Name, "Time In Minutes","Viewers", IsOnline);

            foreach (string channel in initArray[2].Split(new char[] { '{', '}', ';' }))
            {
                if (channel != "")
                {
                    string[] channelText = channel.Split("=");
                    ChannelMessages.Add(ulong.Parse(channelText[0]), channelText[1]);
                    ChannelIds.Add(ulong.Parse(channelText[0]));
                }
            }

            if (IsOnline)
            {
                foreach (string message in initArray[3].Split(new char[] { '{', '}', ';' }))
                {
                    if (message != "")
                    {
                        string[] messageInformation = message.Split("=");
                        var channel = Program.client.GetChannel(ulong.Parse(messageInformation[0]));
                        var discordMessage = ulong.Parse(messageInformation[1]);
                        ToUpdate.Add(ulong.Parse(messageInformation[0]), discordMessage);
                    }
                }
                CurGame = streamerInformation().stream.game;
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                StreamerStatus = streamerInformation();
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " " + e.Message);
            }

            Boolean isStreaming = StreamerStatus.stream.channel != null;

            if (IsOnline != isStreaming)
            {
                if (IsOnline)
                {
                    IsOnline = false;
                    Console.Out.WriteLine($"{DateTime.Now} {Name} went Offline");
                    viewerGraph.RemovePlot();
                    viewerGraph = new Plot(Name, "Time In Minutes", "Viewers", false);

                    foreach (ulong channel in ChannelMessages.Keys)
                        await OnMinorChangeTracked(channel, $"{Name} went Offline!");
                }
                else
                {
                    IsOnline = true;
                    ToUpdate = new Dictionary<ulong, ulong>();
                    CurGame = StreamerStatus.stream.game;
                    viewerGraph.SwitchTitle(CurGame);

                    foreach (ulong channel in ChannelMessages.Keys)
                        await OnMinorChangeTracked(channel, ChannelMessages[channel]);
                }
                StaticBase.streamTracks.SaveJson();
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
                    StaticBase.streamTracks.SaveJson();
                }
                    
                foreach (ulong channel in ChannelIds)
                    await OnMajorChangeTracked(channel, createEmbed());
            }
        }

        private TwitchResult streamerInformation()
        {
            string query = MopsBot.Module.Information.readURL($"https://api.twitch.tv/kraken/streams/{Name}?client_id={Program.twitchId}");

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

        public override string[] GetInitArray()
        {
            string[] informationArray = new string[4];
            informationArray[0] = Name;
            informationArray[1] = IsOnline.ToString();
            informationArray[2] = "{" + string.Join(";", ChannelMessages.Select(x => x.Key + "=" + x.Value)) + "}";
            informationArray[3] = "{" + string.Join(";", ToUpdate.Select(x => x.Key + "=" + x.Value)) + "}";

            return informationArray;
        }
    }
}
