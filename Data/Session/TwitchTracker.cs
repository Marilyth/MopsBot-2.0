﻿using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Session.APIResults;
using OxyPlot;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data.Session
{
    public class TwitchTracker : ITracker
    {
        private PlotModel viewerChart;
        private Dictionary<string, List<OxyPlot.Series.LineSeries>> series;
        private int columnCount;
        public Dictionary<ulong, Discord.IUserMessage> ToUpdate;
        public Boolean IsOnline;
        public string Name, CurGame;
        public Dictionary<ulong, string> ChannelMessages;
        public APIResults.TwitchResult StreamerStatus;

        public TwitchTracker(string streamerName) : base(60000)
        {
            initViewerChart();

            Console.Out.WriteLine($"{DateTime.Now} Started Twitchtracker for {streamerName}");
            ToUpdate = new Dictionary<ulong, Discord.IUserMessage>();
            ChannelMessages = new Dictionary<ulong, string>();
            Name = streamerName;
            IsOnline = false;
        }

        public TwitchTracker(string[] initArray) : base(60000)
        {
            initViewerChart();
            ToUpdate = new Dictionary<ulong, Discord.IUserMessage>();
            ChannelMessages = new Dictionary<ulong, string>();

            Name = initArray[0];
            IsOnline = Boolean.Parse(initArray[1]);
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
                        var discordMessage = ((Discord.ITextChannel)channel).GetMessageAsync(ulong.Parse(messageInformation[1])).Result;
                        ToUpdate.Add(ulong.Parse(messageInformation[0]), (Discord.IUserMessage)discordMessage);
                    }
                }
                CurGame = streamerInformation().stream.game;
                readPlotPoints();
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
                    columnCount = 0;
                    Console.Out.WriteLine($"{DateTime.Now} {Name} went Offline");
                    var file = new FileInfo($"mopsdata//plots//{Name}.txt");
                    file.Delete();
                    initViewerChart();

                    foreach (ulong channel in ChannelMessages.Keys)
                        await OnMinorChangeTracked(channel, $"{Name} went Offline!");
                    StaticBase.streamTracks.writeList();
                }
                else
                {
                    IsOnline = true;
                    ToUpdate = new Dictionary<ulong, Discord.IUserMessage>();
                    CurGame = StreamerStatus.stream.game;
                    gameChange();

                    foreach (ulong channel in ChannelMessages.Keys)
                        await OnMinorChangeTracked(channel, ChannelMessages[channel]);
                }
            }

            if (IsOnline)
            {
                columnCount++;

                if (!series.ContainsKey(CurGame))
                    gameChange(CurGame);
                series[CurGame].Last().Points.Add(new DataPoint(columnCount, StreamerStatus.stream.viewers));

                if (CurGame.CompareTo(StreamerStatus.stream.game) != 0)
                {
                    CurGame = StreamerStatus.stream.game;
                    gameChange();

                    series[CurGame].Last().Points.Add(new DataPoint(columnCount, StreamerStatus.stream.viewers));

                    foreach (ulong channel in ChannelMessages.Keys)
                        await OnMinorChangeTracked(channel, $"{Name} switched games to **{CurGame}**");
                }
                updateChart();
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
            e.ImageUrl = $"http://5.45.104.29/StreamCharts/{Name}plot.png?rand={StaticBase.ran.Next(0, 99999999)}";

            e.AddInlineField("Game", CurGame);
            e.AddInlineField("Viewers", StreamerStatus.stream.viewers);

            return e;
        }


        private void updateChart()
        {
            using (var stream = File.Create($"mopsdata//{Name}plot.pdf"))
            {
                var pdfExporter = new PdfExporter { Width = 800, Height = 400 };
                pdfExporter.Export(viewerChart, stream);
            }

            var prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = "convert";
            prc.StartInfo.Arguments = $"-set density 300 \"mopsdata//{Name}plot.pdf\" \"//var//www//html//StreamCharts//{Name}plot.png\"";

            prc.Start();

            prc.WaitForExit();

            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Extension.ToLower().Equals($"{Name}.pdf"));
            foreach (var f in files)
                f.Delete();

            writePlotPoints();
        }

        private void initViewerChart()
        {
            viewerChart = new PlotModel();
            viewerChart.TextColor = OxyColor.FromRgb(175, 175, 175);
            viewerChart.PlotAreaBorderThickness = new OxyThickness(0);
            var valueAxisY = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Title = "Viewers",
                Minimum = 0,
                FontSize = 24,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155)
            };

            var valueAxisX = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Title = "Time in Minutes",
                FontSize = 24,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155)
            };

            viewerChart.Axes.Add(valueAxisY);
            viewerChart.Axes.Add(valueAxisX);
            viewerChart.LegendFontSize = 24;
            viewerChart.LegendPosition = LegendPosition.BottomCenter;

            series = new Dictionary<string, List<OxyPlot.Series.LineSeries>>();
        }

        private void gameChange()
        {
            gameChange(CurGame);
        }

        private void gameChange(string newGame)
        {
            if (!series.ContainsKey(newGame))
            {
                series.Add(newGame, new List<OxyPlot.Series.LineSeries>());
                series[newGame].Add(new OxyPlot.Series.LineSeries());
                series[newGame].Last().Color = OxyColor.FromRgb((byte)StaticBase.ran.Next(30, 220), (byte)StaticBase.ran.Next(30, 220), (byte)StaticBase.ran.Next(30, 220));
                series[newGame].Last().Title = newGame;
                series[newGame].Last().StrokeThickness = 3;
                viewerChart.Series.Add(series[newGame].Last());
            }
            else
            {
                series[newGame].Add(new OxyPlot.Series.LineSeries());
                series[newGame].Last().Color = series[newGame].First().Color;
                series[newGame].Last().StrokeThickness = 3;
                viewerChart.Series.Add(series[newGame].Last());
            }
        }

        public void recolour()
        {
            initViewerChart();
            series = new Dictionary<string, List<OxyPlot.Series.LineSeries>>();
            readPlotPoints();
        }

        private void writePlotPoints()
        {
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//plots//{Name}.txt", FileMode.Create)))
            {
                write.WriteLine(columnCount);
                foreach (var game in series.Keys)
                {
                    foreach (var lineseries in series[game])
                    {
                        write.WriteLine($"GAMECHANGE={game}");
                        foreach (var point in lineseries.Points)
                        {
                            write.WriteLine($"{point.X}:{point.Y}");
                        }
                    }
                }
            }
        }

        private void readPlotPoints()
        {

            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//plots//{Name}.txt", FileMode.OpenOrCreate)))
            {
                columnCount = int.Parse(read.ReadLine());
                string currentGame = "";
                string s = "";
                while ((s = read.ReadLine()) != null)
                {
                    if (s.StartsWith("GAMECHANGE"))
                    {
                        currentGame = s.Split("=")[1];
                        gameChange(currentGame);
                    }
                    else
                        series[currentGame].Last().Points.Add(new DataPoint(double.Parse(s.Split(":")[0]), double.Parse(s.Split(":")[1])));
                }
            }
        }

        public override string[] GetInitArray()
        {
            string[] informationArray = new string[4];
            informationArray[0] = Name;
            informationArray[1] = IsOnline.ToString();
            informationArray[2] = "{" + string.Join(";", ChannelMessages.Select(x => x.Key + "=" + x.Value)) + "}";
            informationArray[3] = "{" + string.Join(";", ToUpdate.Select(x => x.Key + "=" + x.Value.Id)) + "}";

            return informationArray;
        }
    }
}
