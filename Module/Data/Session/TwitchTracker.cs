using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
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
        public event EventHandler StreamerWentOnline;
        public event EventHandler StreamerWentOffline;
        public event EventHandler StreamerGameChanged;
        public event EventHandler StreamerStatusChanged;
        public delegate Task EventHandler(TwitchTracker e);
        private System.Threading.Timer checkForChange;
        private PlotModel viewerChart;
        private Dictionary<string, OxyPlot.Series.LineSeries> series;
        private int columnCount;
        public Dictionary<ulong, Discord.IUserMessage> toUpdate;
        public Boolean isOnline;
        public string name, curGame;
        public Dictionary<ulong, string> ChannelIds;
        public APIResults.TwitchResult streamerStatus;

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
            try
            {
                streamerStatus = streamerInformation();
            }
            catch
            {
                return;
            }

            if (streamerStatus == null) return;
            Boolean isStreaming = streamerStatus.stream != null;

            if (isOnline != isStreaming)
            {
                if (isOnline)
                {
                    isOnline = false;
                    columnCount = 0;
                    Console.Out.WriteLine($"{DateTime.Now} {name} went Offline");
                    var file = new FileInfo($"data//plots//{name}.txt");
                    file.Delete();
                    initViewerChart();
                    OnStreamerWentOffline();
                }
                else
                {
                    isOnline = true;
                    toUpdate = new Dictionary<ulong, IUserMessage>();
                    curGame = (streamerStatus.stream == null) ? "Nothing" : streamerStatus.stream.game;
                    gameChange();
                    OnStreamerWentOnline();
                }
            }

            if (isOnline)
            {
                columnCount++;
                series[curGame].Points.Add(new DataPoint(columnCount, streamerStatus.stream.viewers));
                if (streamerStatus.stream != null && curGame.CompareTo(streamerStatus.stream.game) != 0 && !streamerStatus.stream.game.Equals(""))
                {
                    curGame = streamerStatus.stream.game;
                    gameChange();
                    series[curGame].Points.Add(new DataPoint(columnCount, streamerStatus.stream.viewers));
                    OnStreamerGameChanged();
                }

                OnStreamerStatusChanged();
                updateChart();
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

        public EmbedBuilder createEmbed()
        {
            Channel streamer = streamerStatus.stream.channel;

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

            e.ThumbnailUrl = $"{streamerStatus.stream.preview.medium}?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = $"http://5.45.104.29/StreamCharts/{name}plot.png?rand={StaticBase.ran.Next(0, 99999999)}";

            e.AddInlineField("Game", (streamer.game == "") ? "no Game" : streamer.game);
            e.AddInlineField("Viewers", streamerStatus.stream.viewers);

            return e;
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
            else
            {
                Boolean occured = false;
                for (int i = 0; i < viewerChart.Series.Count; i++)
                {

                    if (occured && !viewerChart.Series[i].Title.Equals(newGame))
                    {
                        var pointsToAdd = (viewerChart.Series[i] as OxyPlot.Series.LineSeries).Points;
                        series[newGame].Points.AddRange(pointsToAdd);

                        var sortedPoints = series[newGame].Points.OrderBy(x => x.X);
                        series[newGame].Points.Clear();
                        series[newGame].Points.AddRange(sortedPoints);
                    }

                    else if (viewerChart.Series[i].Title.Equals(newGame))
                    {
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

        private void readPlotPoints(){
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

        protected async virtual void OnStreamerGameChanged()
        {
            if (StreamerGameChanged != null)
                await StreamerGameChanged(this);
        }

        protected async virtual void OnStreamerWentOnline()
        {
            if (StreamerWentOnline != null)
                await StreamerWentOnline(this);
        }

        protected async virtual void OnStreamerWentOffline()
        {
            if (StreamerWentOffline != null)
                await StreamerWentOffline(this);
        }

        protected async virtual void OnStreamerStatusChanged()
        {
            if (StreamerStatusChanged != null)
                await StreamerStatusChanged(this);
        }
    }
}
