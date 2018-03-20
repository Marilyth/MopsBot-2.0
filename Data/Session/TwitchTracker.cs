using System;
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
         bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        private System.Threading.Timer checkForChange;
        private PlotModel viewerChart;
        private Dictionary<string, List<OxyPlot.Series.LineSeries>> series;
        private int columnCount;
        public Dictionary<ulong, Discord.IUserMessage> toUpdate;
        public Boolean isOnline;
        public string name, curGame;
        public Dictionary<ulong, string> ChannelMessages;
        public APIResults.TwitchResult streamerStatus;

        public TwitchTracker(string streamerName)
        {
            initViewerChart();

            Console.Out.WriteLine($"{DateTime.Now} Started Twitchtracker for {streamerName}");
            toUpdate = new Dictionary<ulong, Discord.IUserMessage>();
            ChannelMessages = new Dictionary<ulong, string>();
            ChannelIds = new HashSet<ulong>();
            name = streamerName;
            isOnline = false;
            curGame = "Nothing";
            try{
                curGame = streamerInformation().stream.game;
            }catch(Exception e){}

            gameChange();

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6, 59) * 1000, 60000);
        }

        public TwitchTracker(string[] initArray)
        {
            initViewerChart();
            toUpdate = new Dictionary<ulong, Discord.IUserMessage>();
            ChannelMessages = new Dictionary<ulong, string>();
            ChannelIds = new HashSet<ulong>();

            name = initArray[0];
            isOnline = Boolean.Parse(initArray[1]);
            foreach(string channel in initArray[2].Split(new char[]{'{','}',';'})){
                if(channel != ""){
                    string[] channelText = channel.Split("=");
                    ChannelMessages.Add(ulong.Parse(channelText[0]), channelText[1]);
                    ChannelIds.Add(ulong.Parse(channelText[0]));
                }
            }

            curGame = "Nothing";
            try{
                curGame = streamerInformation().stream.game;
            }catch(Exception e){}

            if(isOnline){
                foreach(string message in initArray[3].Split(new char[]{'{','}',';'})){
                    if(message != ""){
                        string[] messageInformation = message.Split("=");
                        var channel = Program.client.GetChannel(ulong.Parse(messageInformation[0]));
                        var discordMessage = ((Discord.ITextChannel)channel).GetMessageAsync(ulong.Parse(messageInformation[1])).Result;
                        toUpdate.Add(ulong.Parse(messageInformation[0]), (Discord.IUserMessage)discordMessage);
                    }
                }
                readPlotPoints();
            }

            else gameChange();

            Console.Out.WriteLine($"{DateTime.Now} Started Twitchtracker for {name} per array");

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6, 59) * 1000, 60000);
        }

        protected override void CheckForChange_Elapsed(object stateinfo)
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
                    var file = new FileInfo($"mopsdata//plots//{name}.txt");
                    file.Delete();
                    initViewerChart();

                    foreach(ulong channel in ChannelMessages.Keys)
                        OnMinorChangeTracked(channel, $"{name} went Offline!");
                    StaticBase.streamTracks.writeList();
                }
                else
                {
                    isOnline = true;
                    toUpdate = new Dictionary<ulong, Discord.IUserMessage>();
                    curGame = (streamerStatus.stream == null) ? "Nothing" : streamerStatus.stream.game;
                    gameChange();
                    
                    foreach(ulong channel in ChannelMessages.Keys)
                        OnMinorChangeTracked(channel, ChannelMessages[channel]);
                }
            }

            if (isOnline)
            {
                columnCount++;
                series[curGame].Last().Points.Add(new DataPoint(columnCount, streamerStatus.stream.viewers));
                if (streamerStatus.stream != null && curGame.CompareTo(streamerStatus.stream.game) != 0 && !streamerStatus.stream.game.Equals(""))
                {
                    curGame = streamerStatus.stream.game;
                    gameChange();
                    series[curGame].Last().Points.Add(new DataPoint(columnCount, streamerStatus.stream.viewers));
                    
                    foreach(ulong channel in ChannelMessages.Keys)
                        OnMinorChangeTracked(channel, $"{name} switched games to **{curGame}**");
                }
                foreach(ulong channel in ChannelIds)
                    OnMajorChangeTracked(channel, createEmbed());
                updateChart();
            }
        }

        private TwitchResult streamerInformation()
        {
            string query = MopsBot.Module.Information.readURL($"https://api.twitch.tv/kraken/streams/{name}?client_id={Program.twitchId}");

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
            using (var stream = File.Create($"mopsdata//{name}plot.pdf"))
            {
                var pdfExporter = new PdfExporter { Width = 800, Height = 400 };
                pdfExporter.Export(viewerChart, stream);
            }

            var prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = "convert";
            prc.StartInfo.Arguments = $"-set density 300 \"mopsdata//{name}plot.pdf\" \"//var//www//html//StreamCharts//{name}plot.png\"";

            prc.Start();

            prc.WaitForExit();

            var dir = new DirectoryInfo("mopsdata//");
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

            series = new Dictionary<string, List<OxyPlot.Series.LineSeries>>();
        }

        private void gameChange()
        {
            gameChange(curGame);
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
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//plots//{name}.txt", FileMode.Create)))
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

            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//plots//{name}.txt", FileMode.OpenOrCreate)))
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

        public override string[] getInitArray(){
            string[] informationArray = new string[4];
            informationArray[0] = name;
            informationArray[1] = isOnline.ToString();
            informationArray[2] = "{" + string.Join(";", ChannelMessages.Select(x => x.Key + "=" + x.Value)) + "}";
            informationArray[3] = "{" + string.Join(";", toUpdate.Select(x => x.Key + "=" + x.Value.Id)) + "}";

            return informationArray;
        }

        public override void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);           
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return; 
      
            if (disposing) {
                handle.Dispose();
                checkForChange.Dispose();
            }
      
            disposed = true;
        }
    }
}
