using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OxyPlot;
using OxyPlot.Axes;
using System.Threading.Tasks;

namespace MopsBot.Data
{

    public class BarPlot
    {
        private PlotModel viewerChart;
        private OxyPlot.Series.ColumnSeries columnSeries;
        public string ID;
        public Dictionary<string, double> Categories;

        public BarPlot(string name, bool keepTrack = false, params string[] categories)
        {
            ID = name;
            Categories = new Dictionary<string, double>();
            foreach(var category in categories)
                Categories.Add(category, 0);

            initPlot();
            if (keepTrack)
            {
                readPlotPoints();
            }
        }

        private void initPlot()
        {
            viewerChart = new PlotModel();
            viewerChart.TextColor = OxyColor.FromRgb(175, 175, 175);
            viewerChart.PlotAreaBorderThickness = new OxyThickness(0);

            viewerChart.Axes.Add(new OxyPlot.Axes.CategoryAxis(){
                Key = ID,
                ItemsSource = Categories.Keys,
                FontSize = 24,
                AxislineColor = OxyColor.FromRgb(125, 125, 155),
                TicklineColor = OxyColor.FromRgb(125, 125, 155)
            });

            viewerChart.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Minimum = 0,
                FontSize = 24,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155)
            });

            viewerChart.LegendFontSize = 24;
            viewerChart.LegendPosition = LegendPosition.BottomCenter;

            columnSeries = new OxyPlot.Series.ColumnSeries(){
                ItemsSource = new List<OxyPlot.Series.ColumnItem>(Categories.Values.Select(x => new OxyPlot.Series.ColumnItem(x))), 
                //LabelPlacement = OxyPlot.Series.LabelPlacement.Outside, 
                FontSize = 24,
                //LabelFormatString = "{0}",
                FillColor = OxyColor.FromRgb(190, 192, 187)
            };
            viewerChart.Series.Add(columnSeries);
        }

        private void writePlotPoints()
        {
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//plots//{ID}barplot.json", FileMode.Create)))
            {
                write.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Categories));
            }
        }

        /// <summary>
        /// Saves the plot as a .png and returns the URL.
        /// </summary>
        /// <returns>The URL</returns>
        public string DrawPlot()
        {
            columnSeries.ItemsSource = Categories.Values.Select(x => new OxyPlot.Series.ColumnItem(x));

            using (var stream = File.Create($"mopsdata//{ID}barplot.pdf"))
            {
                var pdfExporter = new PdfExporter { Width = 800, Height = 400 };
                pdfExporter.Export(viewerChart, stream);
            }

            var prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = "convert";
            prc.StartInfo.Arguments = $"-set density 300 \"mopsdata//{ID}barplot.pdf\" \"//var//www//html//StreamCharts//{ID}barplot.png\"";

            prc.Start();

            prc.WaitForExit();

            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Name.Contains($"{ID}barplot"));
            foreach (var f in files)
                f.Delete();

            return $"http://5.45.104.29/StreamCharts/{ID.Replace(" ", "%20")}barplot.png?rand={StaticBase.ran.Next(0,999999999)}";
        }

        private void readPlotPoints()
        {
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//plots//{ID}barplot.json", FileMode.OpenOrCreate)))
            {
                Categories = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, double>>(read.ReadToEnd());
            }
        }

        /// <summary>
        /// Adds a Value to the plot, to its' current Title
        /// </summary>
        /// <param name="value">The Value to add to the plot</param>
        public void AddValue(string title, double value)
        {
            Categories[title] += value;
            writePlotPoints();
        }

        /// <summary>
        /// Removes all files created by the plot class to function.
        /// </summary>
        public void RemovePlot(){
            viewerChart = null;
            columnSeries = null;
            var file = new FileInfo($"mopsdata//plots//{ID}barplot.json");
            file.Delete();
            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Extension.ToLower().Equals($"{ID}plot.pdf"));
            foreach (var f in files)
                f.Delete();
        }
    }

    /// <summary>
    /// A Class that handles drawing plots.
    /// </summary>
    public class Plot
    {
        private PlotModel viewerChart;
        private Dictionary<string, List<OxyPlot.Series.LineSeries>> lineSeries;
        private Dictionary<string, OxyPlot.Series.BarSeries> barSeries;
        public int columnCount;
        public double lastValue;
        public string ID, xAxis, yAxis, CurTitle;

        public Plot(string name, string xName = "x", string yName = "y", bool keepTrack = false)
        {
            ID = name;
            xAxis = xName;
            yAxis = yName;
            initPlot();
            if (keepTrack)
            {
                readPlotPoints();
            }
        }

        public Plot(string name, bool keepTrack = false)
        {
            ID = name;
            initPlot();
            if (keepTrack)
            {
                readPlotPoints();
            }
        }

        private void initPlot()
        {
            viewerChart = new PlotModel();
            viewerChart.TextColor = OxyColor.FromRgb(175, 175, 175);
            viewerChart.PlotAreaBorderThickness = new OxyThickness(0);
            var valueAxisY = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Title = yAxis,
                Minimum = 0,
                FontSize = 24,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155)
            };

            var valueAxisX = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Title = xAxis,
                FontSize = 24,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155)
            };

            viewerChart.Axes.Add(valueAxisY);
            viewerChart.Axes.Add(valueAxisX);
            viewerChart.LegendFontSize = 24;
            viewerChart.LegendPosition = LegendPosition.BottomCenter;

            lineSeries = new Dictionary<string, List<OxyPlot.Series.LineSeries>>();
        }

        private void writePlotPoints()
        {
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//plots//{ID}plot.txt", FileMode.Create)))
            {
                write.WriteLine(columnCount);
                foreach (var title in lineSeries.Keys)
                {
                    foreach (var lineseries in lineSeries[title])
                    {
                        write.WriteLine($"TITLE={title}");
                        foreach (var point in lineseries.Points)
                        {
                            write.WriteLine($"{point.X}:{point.Y}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves the plot as a .png and returns the URL.
        /// </summary>
        /// <returns>The URL</returns>
        public string DrawPlot()
        {
            using (var stream = File.Create($"mopsdata//{ID}plot.pdf"))
            {
                var pdfExporter = new PdfExporter { Width = 800, Height = 400 };
                pdfExporter.Export(viewerChart, stream);
            }

            var prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = "convert";
            prc.StartInfo.Arguments = $"-set density 300 \"mopsdata//{ID}plot.pdf\" \"//var//www//html//StreamCharts//{ID}plot.png\"";

            prc.Start();

            prc.WaitForExit();

            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Name.Contains($"{ID}plot"));
            foreach (var f in files)
                f.Delete();

            return $"http://5.45.104.29/StreamCharts/{ID}plot.png?rand={StaticBase.ran.Next(0,999999999)}";
        }

        private void readPlotPoints()
        {
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//plots//{ID}plot.txt", FileMode.OpenOrCreate)))
            {
                columnCount = int.Parse(read.ReadLine() ?? "0");
                string currentGame = "";
                string s = "";
                while ((s = read.ReadLine()) != null)
                {
                    if (s.StartsWith("TITLE"))
                    {
                        currentGame = s.Split("=")[1];
                        SwitchTitle(currentGame);
                    }
                    else{
                        lineSeries[currentGame].Last().Points.Add(new DataPoint(double.Parse(s.Split(":")[0]), double.Parse(s.Split(":")[1])));
                        lastValue = double.Parse(s.Split(":")[1]);
                    }
                }
                CurTitle = currentGame;
            }
        }

        /// <summary>
        /// Adds a Value to the plot, to its' current Title
        /// </summary>
        /// <param name="value">The Value to add to the plot</param>
        public void AddValue(double value)
        {
            columnCount++;
            lineSeries[CurTitle].Last().Points.Add(new DataPoint(columnCount, value));
            lastValue = value;
            writePlotPoints();
        }

        /// <summary>
        /// Switches the current title plot points are added to automatically.
        /// Adds it to the legend and changes colour.
        /// </summary>
        /// <param name="newTitle">The Title to switch to</param>
        public void SwitchTitle(string newTitle, bool backToZero = false)
        {
            if (!lineSeries.ContainsKey(newTitle))
            {
                lineSeries.Add(newTitle, new List<OxyPlot.Series.LineSeries>());
                lineSeries[newTitle].Add(new OxyPlot.Series.LineSeries());

                long colour = 1;
                foreach(char c in newTitle){
                    colour = (((int)c * colour) % 12829635) + 1973790;
                }
                
                var oxycolour = OxyColor.FromUInt32((uint)colour + 4278190080);
                lineSeries[newTitle].Last().Color = oxycolour;
                lineSeries[newTitle].Last().Title = newTitle;
            }
            else
            {
                lineSeries[newTitle].Add(new OxyPlot.Series.LineSeries());
                lineSeries[newTitle].Last().Color = lineSeries[newTitle].First().Color;
            }

            lineSeries[newTitle].Last().StrokeThickness = 3;
            viewerChart.Series.Add(lineSeries[newTitle].Last());
            CurTitle = newTitle;
            columnCount--;
        }

        /// <summary>
        /// Removes all files created by the plot class to function.
        /// </summary>
        public void RemovePlot(){
            viewerChart = null;
            lineSeries = null;
            var file = new FileInfo($"mopsdata//plots//{ID}plot.txt");
            file.Delete();
            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Extension.ToLower().Equals($"{ID}plot.pdf"));
            foreach (var f in files)
                f.Delete();
        }

        public void Recolour(){
            foreach(var game in lineSeries){
                OxyColor newColour = OxyColor.FromRgb((byte)StaticBase.ran.Next(30, 220), (byte)StaticBase.ran.Next(30, 220), (byte)StaticBase.ran.Next(30, 220));
                foreach(var gameReoccurence in game.Value){
                    gameReoccurence.Color = newColour;
                }
            }
        }

        public static string CreateBarDiagram<T>(int count, Func<T, int> stat, Func<T, string> id, List<T> toSort)
        {
            var sortedList = (from entry in toSort orderby stat(entry) descending select entry).Take(count).ToArray();

            int maximum = 0;
            string[] lines = new string[count];

            maximum = stat(sortedList[0]);

            for (int i = 0; i < count; i++)
            {
                T cur = sortedList[i];
                lines[i] = (i + 1).ToString().Length < 2 ? $"#{i + 1} |" : $"#{i + 1}|";
                double relPercent = stat(cur) / ((double)maximum / 10);
                for (int j = 0; j < relPercent; j++)
                {
                    lines[i] += "■";
                }
                lines[i] += $"  ({stat(cur)} / {id(sortedList[i])})";
            }


            string output = "```" + string.Join("\n", lines) + "```";

            return output;
        }
    }
}