using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Module.Data.Session.APIResults;
using OxyPlot;
using System.IO;

namespace MopsBot.Module.Data.Session
{
    public class TwitchTracker
    {
        private System.Threading.Timer checkForChange;
        private PlotModel viewerChart;
        private Dictionary<string, OxyPlot.Series.LineSeries> series;
        private int columnCount;
        public Dictionary<ulong, Discord.IUserMessage> toUpdate;
        public Boolean isOnline;
        public string name, curGame;
        public Dictionary<ulong, string> ChannelIds;


        public TwitchTracker(string streamerName, ulong pChannel, string notificationText, Boolean pIsOnline, string pGame)
        {
            initViewerChart();

            Console.Out.WriteLine($"{DateTime.Now} Started Twitchtracker for {streamerName} w/ channel {pChannel}");
            toUpdate = new Dictionary<ulong, IUserMessage>();
            ChannelIds = new Dictionary<ulong, string>();
            ChannelIds.Add(pChannel, notificationText);
            name = streamerName;
            isOnline = pIsOnline;
            curGame = pGame;

            if (isOnline) readPlotPoints();

            else gameChange();

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6, 59) * 1000, 60000);
        }

        private void CheckForChange_Elapsed(object stateinfo)
        {
            TwitchResult information;
            try
            {
                information = streamerInformation();
            }
            catch
            {
                return;
            }
            if (information == null) return;
            Boolean isStreaming = information.stream != null;

            if (isOnline != isStreaming)
            {
                if (isOnline)
                {
                    isOnline = false;
                    Console.Out.WriteLine($"{DateTime.Now} {name} went Offline");
                    var file = new FileInfo($"data//plots//{name}.txt");
                    file.Delete();
                    initViewerChart();
                }
                else
                {
                    isOnline = true;
                    Console.Out.WriteLine($"{DateTime.Now} {name} went Online");
                    toUpdate = new Dictionary<ulong, IUserMessage>();
                    curGame = (information.stream == null) ? "Nothing" : information.stream.game;
                    gameChange();
                }
                StaticBase.streamTracks.writeList();
            }

            if (isOnline)
            {
                columnCount++;
                series[curGame].Points.Add(new DataPoint(columnCount, information.stream.viewers));
                if (information.stream != null && curGame.CompareTo(information.stream.game) != 0 && !information.stream.game.Equals(""))
                {
                    curGame = information.stream.game;
                    gameChange();
                    series[curGame].Points.Add(new DataPoint(columnCount, information.stream.viewers));

                    foreach (var channel in ChannelIds)
                        ((SocketTextChannel)Program.client.GetChannel(channel.Key)).SendMessageAsync($"{name} spielt jetzt **{curGame}**!");

                    StaticBase.streamTracks.writeList();
                }

                updateChart();
                sendTwitchNotification(information);
            }
        }

        private TwitchResult streamerInformation()
        {
            string query = Information.readURL($"https://api.twitch.tv/kraken/streams/{name}?client_id={Program.twitchId}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<TwitchResult>(query, _jsonWriter);
        }

        private async void sendTwitchNotification(TwitchResult streamInformation)
        {
            Channel streamer = streamInformation.stream.channel;

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = streamer.status;
            e.Url = streamer.url;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = name;
            author.Url = streamer.url;
            author.IconUrl = streamer.logo;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
            footer.Text = "Twitch";
            e.Footer = footer;

            e.ThumbnailUrl = $"{streamInformation.stream.preview.medium}?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = $"http://5.45.104.29/StreamCharts/{name}plot.png?rand={StaticBase.ran.Next(0, 99999999)}";

            e.AddInlineField("Game", (streamer.game == "") ? "no Game" : streamer.game);
            e.AddInlineField("Viewers", streamInformation.stream.viewers);

            foreach (var channel in ChannelIds)
            {
                if (!toUpdate.ContainsKey(channel.Key))
                {
                    toUpdate.Add(channel.Key, ((SocketTextChannel)Program.client.GetChannel(channel.Key)).SendMessageAsync(channel.Value, false, e).Result);
                    Console.Out.WriteLine($"{DateTime.Now} {name} toUpdate added {channel.Key}");
                    StaticBase.streamTracks.writeList();
                }

                else
                    await toUpdate[channel.Key].ModifyAsync(x =>
                    {
                        x.Content = channel.Value;
                        x.Embed = (Embed)e;
                    });
                Console.Out.WriteLine($"{DateTime.Now} {name} edit Message in {channel.Key}");
            }
        }


        private void updateChart()
        {
            using (var stream = File.Create($"data//{name}plot.pdf"))
            {
                var pdfExporter = new PdfExporter { Width = 800, Height = 400 };
                pdfExporter.Export(viewerChart, stream);
            }

            var prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = "convert";
            prc.StartInfo.Arguments = $"-set density 300 \"data//{name}plot.pdf\" \"//var//www//html//StreamCharts//{name}plot.png\"";

            prc.Start();

            prc.WaitForExit();

            var dir = new DirectoryInfo("data//");
            var files = dir.GetFiles().Where(x => x.Extension.ToLower().Equals($"{name}.pdf"));
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

            series = new Dictionary<string, OxyPlot.Series.LineSeries>();
        }

        private void gameChange()
        {
            gameChange(curGame);
        }

        private void gameChange(string newGame)
        {
            if (!series.ContainsKey(newGame))
            {
                series.Add(newGame, new OxyPlot.Series.LineSeries());
                series[newGame].Color = OxyColor.FromRgb((byte)StaticBase.ran.Next(30, 220), (byte)StaticBase.ran.Next(30, 220), (byte)StaticBase.ran.Next(30, 220));
                series[newGame].Title = newGame;
                series[newGame].StrokeThickness = 3;
                viewerChart.Series.Add(series[newGame]);
            }
            else{
                Boolean occured = false;
                for(int i = 0; i < viewerChart.Series.Count; i++){

                    if(occured && !viewerChart.Series[i].Title.Equals(newGame)){
                        var pointsToAdd = (viewerChart.Series[i] as OxyPlot.Series.LineSeries).Points;
                        series[newGame].Points.AddRange(pointsToAdd);
                        
                        var sortedPoints = series[newGame].Points.OrderBy(x => x.X);
                        series[newGame].Points.Clear();
                        series[newGame].Points.AddRange(sortedPoints);
                    }

                    else if(viewerChart.Series[i].Title.Equals(newGame)){
                        occured = true;
                    }
                }
            }
        }

        public void recolour()
        {
            initViewerChart();
            series = new Dictionary<string, OxyPlot.Series.LineSeries>();
            readPlotPoints();
        }

        private void writePlotPoints()
        {
            StreamWriter write = new StreamWriter(new FileStream($"data//plots//{name}.txt", FileMode.Create));
            write.AutoFlush = true;
            write.WriteLine(columnCount);
            foreach (var game in series.Keys)
            {
                write.WriteLine($"GAMECHANGE={game}");
                foreach (var point in series[game].Points)
                {
                    write.WriteLine($"{point.X}:{point.Y}");
                }
            }
            write.Dispose();
        }

        private void readPlotPoints()
        {
            StreamReader read = new StreamReader(new FileStream($"data//plots//{name}.txt", FileMode.OpenOrCreate));

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
                    series[currentGame].Points.Add(new DataPoint(double.Parse(s.Split(":")[0]), double.Parse(s.Split(":")[1])));
            }

            read.Dispose();
        }
    }
}
