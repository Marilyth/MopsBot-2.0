using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OxyPlot;
using OxyPlot.Axes;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;

namespace MopsBot.Data
{

    public class ColumnPlot
    {
        private PlotModel viewerChart;
        private OxyPlot.Series.ColumnSeries columnSeries;
        public string ID;
        public Dictionary<string, double> Categories;

        public ColumnPlot(string name, params string[] categories)
        {
            ID = name;
            Categories = new Dictionary<string, double>();

            int duplicates = 0;
            foreach (var category in categories)
            {
                if (!Categories.ContainsKey(category)) Categories.Add(category, 0);
                else Categories.Add(category + (++duplicates), 0);
            }

            initPlot();
        }

        private void initPlot()
        {
            viewerChart = new PlotModel();
            viewerChart.TextColor = OxyColor.FromRgb(175, 175, 175);
            viewerChart.PlotAreaBorderThickness = new OxyThickness(0);

            viewerChart.Axes.Add(new OxyPlot.Axes.CategoryAxis()
            {
                Key = ID,
                ItemsSource = Categories.Count > 10 ? Categories.Keys.Select(x => "") : Categories.Keys,
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

            columnSeries = new OxyPlot.Series.ColumnSeries()
            {
                ItemsSource = new List<OxyPlot.Series.ColumnItem>(Categories.Values.Select(x => new OxyPlot.Series.ColumnItem(x))),
                LabelPlacement = OxyPlot.Series.LabelPlacement.Outside,
                LabelMargin = 0.1,
                FontSize = 16,
                LabelFormatString = Categories.Count > 10 ? null : "{0:0.##}",
                FillColor = OxyColor.FromRgb(190, 192, 187)
            };
            viewerChart.Series.Add(columnSeries);
        }

        public static string DrawPlot(string name, Dictionary<string, double> dict)
        {
            var tmpBarPlot = new ColumnPlot(name, dict.Keys.ToArray());
            foreach (var key in dict.Keys)
            {
                tmpBarPlot.AddValue(key, dict[key]);
            }

            return tmpBarPlot.DrawPlot();
        }

        public static string DrawPlotSorted(string name, List<KeyValuePair<string, double>> values)
        {
            var tmpBarPlot = new ColumnPlot(name, values.Select(x => x.Key).ToArray());
            foreach (var value in values)
            {
                tmpBarPlot.AddValue(value.Key, value.Value);
            }

            return tmpBarPlot.DrawPlot();
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

            using (var prc = new System.Diagnostics.Process())
            {
                prc.StartInfo.FileName = "convert";
                prc.StartInfo.Arguments = $"-set density 300 \"mopsdata//{ID}barplot.pdf\" \"mopsdata//Images//{ID}barplot.png\"";

                prc.Start();

                prc.WaitForExit();
            }

            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Name.Contains($"{ID}barplot"));
            foreach (var f in files)
                f.Delete();

            return $"{Program.Config["ServerAddress"]}/Content?name={ID.Replace(" ", "%20")}barplot.png&rand={StaticBase.ran.Next(0, 999999999)}";
        }

        /// <summary>
        /// Adds a Value to the plot, to its' current Title
        /// </summary>
        /// <param name="value">The Value to add to the plot</param>
        public void AddValue(string title, double value)
        {
            Categories[title] += value;
        }

        /// <summary>
        /// Removes all files created by the plot class to function.
        /// </summary>
        public void RemovePlot()
        {
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

    public class BarPlot
    {
        private PlotModel viewerChart;
        private OxyPlot.Series.BarSeries BarSeries;
        public string ID;
        public Dictionary<string, double> Categories;

        public BarPlot(string name, params string[] categories)
        {
            ID = name;
            Categories = new Dictionary<string, double>();

            int duplicates = 0;
            foreach (var category in categories)
            {
                if (!Categories.ContainsKey(category)) Categories.Add(category, 0);
                else Categories.Add(category + (++duplicates), 0);
            }

            initPlot();
        }

        private void initPlot()
        {
            viewerChart = new PlotModel();
            viewerChart.TextColor = OxyColor.FromRgb(175, 175, 175);
            viewerChart.PlotAreaBorderThickness = new OxyThickness(0);

            viewerChart.Axes.Add(new OxyPlot.Axes.CategoryAxis()
            {
                Key = ID,
                Position = OxyPlot.Axes.AxisPosition.Left,
                ItemsSource = Categories.Count > 20 ? null : Categories.Keys,
                FontSize = 24,
                AxislineColor = OxyColor.FromRgb(125, 125, 155),
                TicklineColor = OxyColor.FromRgb(125, 125, 155)
            });

            viewerChart.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Minimum = 0,
                FontSize = 24,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155)
            });

            //viewerChart.LegendFontSize = 24;
            //viewerChart.LegendPosition = LegendPosition.BottomCenter;

            BarSeries = new OxyPlot.Series.BarSeries()
            {
                ItemsSource = new List<OxyPlot.Series.BarItem>(Categories.Values.Select(x => new OxyPlot.Series.BarItem(x))),
                //LabelPlacement = OxyPlot.Series.LabelPlacement.Outside, 
                FontSize = 24,
                //LabelFormatString = "{0}",
                FillColor = OxyColor.FromRgb(190, 192, 187)
            };
            viewerChart.Series.Add(BarSeries);
        }

        public static string DrawPlot(string name, Dictionary<string, double> dict)
        {
            var tmpBarPlot = new BarPlot(name, dict.Keys.ToArray());
            foreach (var key in dict.Keys)
            {
                tmpBarPlot.AddValue(key, dict[key]);
            }

            return tmpBarPlot.DrawPlot();
        }

        public static string DrawPlotSorted(string name, List<KeyValuePair<string, double>> values)
        {
            var tmpBarPlot = new BarPlot(name, values.Select(x => x.Key).ToArray());
            foreach (var value in values)
            {
                tmpBarPlot.AddValue(value.Key, value.Value);
            }

            return tmpBarPlot.DrawPlot();
        }

        /// <summary>
        /// Saves the plot as a .png and returns the URL.
        /// </summary>
        /// <returns>The URL</returns>
        public string DrawPlot()
        {
            BarSeries.ItemsSource = Categories.Values.Select(x => new OxyPlot.Series.BarItem(x));

            using (var stream = File.Create($"mopsdata//{ID}barplot.pdf"))
            {
                var pdfExporter = new PdfExporter { Width = 800, Height = 400 };
                pdfExporter.Export(viewerChart, stream);
            }

            using (var prc = new System.Diagnostics.Process())
            {
                prc.StartInfo.FileName = "convert";
                prc.StartInfo.Arguments = $"-set density 300 \"mopsdata//{ID}barplot.pdf\" \"mopsdata//Images//{ID}barplot.png\"";

                prc.Start();

                prc.WaitForExit();
            }

            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Name.Contains($"{ID}barplot"));
            foreach (var f in files)
                f.Delete();

            return $"{Program.Config["ServerAddress"]}:5000/Content?name={ID.Replace(" ", "%20")}barplot.png&rand={StaticBase.ran.Next(0, 999999999)}";
        }

        /// <summary>
        /// Adds a Value to the plot, to its' current Title
        /// </summary>
        /// <param name="value">The Value to add to the plot</param>
        public void AddValue(string title, double value)
        {
            Categories[title] += value;
        }

        /// <summary>
        /// Removes all files created by the plot class to function.
        /// </summary>
        public void RemovePlot()
        {
            viewerChart = null;
            BarSeries = null;
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
    [BsonIgnoreExtraElements]
    public class DatePlot
    {
        private PlotModel viewerChart;
        private List<OxyPlot.Series.AreaSeries> areaSeries;
        private static string COLLECTIONNAME = "TwitchTracker";
        //public List<KeyValuePair<string, double>> PlotPoints;
        public List<KeyValuePair<string, KeyValuePair<double, double>>> PlotDataPoints;
        private DateTime? StartTime;

        [BsonId]
        public string ID;
        public string xName, yName, format;
        public bool MultipleLines;
        public bool? RelativeTime;

        public DatePlot(string name, string xName = "x", string yName = "y", string format = "HH:mm", bool relativeTime = true, bool multipleLines = false)
        {
            ID = name;
            MultipleLines = multipleLines;
            this.xName = xName;
            this.yName = yName;
            this.format = format;
            this.RelativeTime = relativeTime;
            //PlotPoints = new List<KeyValuePair<string, double>>();
            PlotDataPoints = new List<KeyValuePair<string, KeyValuePair<double, double>>>();
            InitPlot(xName, yName, format, relative: relativeTime);
        }

        public void InitPlot(string xAxis = "Time", string yAxis = "Viewers", string format = "HH:mm", bool relative = true)
        {
            if (RelativeTime == null) RelativeTime = relative;
            if (xName == null) xName = xAxis;
            if (yName == null) yName = yAxis;
            if (this.format == null) this.format = format;

            if (PlotDataPoints == null) PlotDataPoints = new List<KeyValuePair<string, KeyValuePair<double, double>>>();

            viewerChart = new PlotModel();
            viewerChart.TextColor = OxyColor.FromRgb(175, 175, 175);
            viewerChart.PlotAreaBorderThickness = new OxyThickness(0);
            var valueAxisY = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Title = yName,
                FontSize = 26,
                TitleFontSize = 26,
                AxislineThickness = 3,
                MinorGridlineThickness = 5,
                MajorGridlineThickness = 5,
                MajorGridlineStyle = LineStyle.Solid,
                FontWeight = 700,
                TitleFontWeight = 700,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155)
            };
            if (relative) valueAxisY.Minimum = 0;

            var valueAxisX = new OxyPlot.Axes.DateTimeAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Title = xName,
                FontSize = 26,
                TitleFontSize = 26,
                AxislineThickness = 3,
                MinorGridlineThickness = 5,
                MajorGridlineThickness = 5,
                MajorGridlineStyle = LineStyle.Solid,
                FontWeight = 700,
                TitleFontWeight = 700,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155),
                StringFormat = this.format
            };

            viewerChart.Axes.Add(valueAxisY);
            viewerChart.Axes.Add(valueAxisX);
            viewerChart.LegendFontSize = 24;
            viewerChart.LegendPosition = LegendPosition.BottomCenter;
            viewerChart.LegendBorder = OxyColor.FromRgb(125, 125, 155);
            viewerChart.LegendBackground = OxyColor.FromArgb(200, 46, 49, 54);
            viewerChart.LegendTextColor = OxyColor.FromRgb(175, 175, 175);

            areaSeries = new List<OxyPlot.Series.AreaSeries>();
            /*foreach (var plotPoint in PlotPoints)
            {
                AddValue(plotPoint.Key, plotPoint.Value, false);
            }*/

            if (PlotDataPoints.Count > 0)
            {
                PlotDataPoints = PlotDataPoints.Skip(Math.Max(0, PlotDataPoints.Count - 2000)).ToList();
                StartTime = DateTimeAxis.ToDateTime(PlotDataPoints.First().Value.Key);
                foreach (var dataPoint in PlotDataPoints)
                {
                    if (!MultipleLines) AddValue(dataPoint.Key, dataPoint.Value.Value, DateTimeAxis.ToDateTime(dataPoint.Value.Key), false);
                    else AddValueSeperate(dataPoint.Key, dataPoint.Value.Value, DateTimeAxis.ToDateTime(dataPoint.Value.Key), false);
                }
            }
        }

        /// <summary>
        /// Saves the plot as a .png and returns the URL.
        /// </summary>
        /// <returns>The URL</returns>
        //Don't redraw if only 1 minute old
        private DateTime lastPlot;
        public string DrawPlot(bool returnPdf = false, string fileName = null, bool path = false)
        {
            if (fileName == null)
                fileName = $"{ID}plot";

            if ((DateTime.UtcNow - lastPlot).TotalMinutes > 1 || returnPdf || path)
            {
                lastPlot = DateTime.UtcNow;

                using (var stream = File.Create($"mopsdata//{fileName}.pdf"))
                {
                    var pdfExporter = new PdfExporter { Width = 800, Height = 400 };
                    pdfExporter.Export(viewerChart, stream);
                }

                if (!returnPdf)
                {
                    using (var prc = new System.Diagnostics.Process())
                    {
                        prc.StartInfo.FileName = "convert";
                        prc.StartInfo.Arguments = $"-set density 300 \"mopsdata//{fileName}.pdf\" \"mopsdata//Images//{fileName}.png\"";

                        prc.Start();

                        prc.WaitForExit();
                    }

                    var dir = new DirectoryInfo("mopsdata//");
                    var files = dir.GetFiles().Where(x => x.Name.Contains($"{fileName}"));
                    foreach (var f in files)
                        f.Delete();

                    if (!path)
                        return $"{Program.Config["ServerAddress"]}:5000/Content?name={fileName}.png&rand={StaticBase.ran.Next(0, 999999999)}";
                    else return $"mopsdata//Images//{fileName}.png";
                }
                else
                {
                    return $"mopsdata//{fileName}.pdf";
                }
            }
            return $"{Program.Config["ServerAddress"]}:5000/Content?name={fileName}.png&rand={StaticBase.ran.Next(0, 999999999)}";
        }

        public void AddValue(string name, double value, DateTime? xValue = null, bool savePlot = true, bool replace = false)
        {
            if (xValue == null) xValue = DateTime.UtcNow;
            if (StartTime == null) StartTime = xValue;
            var relativeXValue = RelativeTime.Value ? new DateTime(1970, 01, 01).Add((xValue - StartTime).Value) : xValue;

            if (areaSeries.LastOrDefault()?.Title?.Equals(name) ?? false)
            {
                if (replace)
                    areaSeries.Last().Points.RemoveAt(areaSeries.Last().Points.Count - 1);

                areaSeries.Last().Points.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), value));
            }

            else
            {
                var series = new OxyPlot.Series.AreaSeries();
                //series.InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline;

                var colour = StringToColour(name);
                series.Color = colour;
                series.Fill = OxyColor.FromAColor(100, colour);
                series.Color2 = OxyColors.Transparent;

                if (!areaSeries.Any(x => x.Title?.Equals(name) ?? false))
                    series.Title = name;

                series.StrokeThickness = 3;
                areaSeries.LastOrDefault()?.Points?.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), value));
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), value));
                viewerChart.Series.Add(series);
                areaSeries.Add(series);
            }

            if (savePlot)
            {
                if (replace)
                    PlotDataPoints.RemoveAt(PlotDataPoints.Count - 1);

                PlotDataPoints.Add(new KeyValuePair<string, KeyValuePair<double, double>>(name, new KeyValuePair<double, double>(DateTimeAxis.ToDouble(xValue), value)));
                AdjustAxisRange();
            }
        }

        public void AddValueSeperate(string name, double value, DateTime? xValue = null, bool savePlot = true)
        {
            if (xValue == null) xValue = DateTime.UtcNow;
            if (StartTime == null) StartTime = xValue;
            var relativeXValue = RelativeTime.Value ? new DateTime(1970, 01, 01).Add((xValue - StartTime).Value) : xValue;

            if (areaSeries.FirstOrDefault(x => x.Title.Equals(name)) != null)
                areaSeries.FirstOrDefault(x => x.Title.Equals(name)).Points.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), value));

            else
            {
                var series = new OxyPlot.Series.AreaSeries();
                //series.InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline;
                var colour = StringToColour(name);
                series.Color = colour;
                series.Fill = OxyColor.FromAColor(100, colour);
                series.Color2 = OxyColors.Transparent;

                if (!areaSeries.Any(x => x.Title?.Equals(name) ?? false))
                    series.Title = name;

                series.StrokeThickness = 3;
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), value));
                viewerChart.Series.Add(series);
                areaSeries.Add(series);
            }

            if (savePlot)
            {
                PlotDataPoints.Add(new KeyValuePair<string, KeyValuePair<double, double>>(name, new KeyValuePair<double, double>(DateTimeAxis.ToDouble(xValue), value)));
                AdjustAxisRange();
            }
        }

        public void AdjustAxisRange()
        {
            var axis = viewerChart.Axes.First(x => x.Position == OxyPlot.Axes.AxisPosition.Bottom);
            var yaxis = viewerChart.Axes.First(x => x.Position == OxyPlot.Axes.AxisPosition.Left);
            var max = areaSeries.Max(x => x.Points.Max(y => y.X));
            var min = areaSeries.Min(x => x.Points.Min(y => y.X));
            var ymin = areaSeries.Min(x => x.Points.Min(y => y.Y));
            foreach (var series in areaSeries)
            {
                series.ConstantY2 = ymin;
            }
            axis.AbsoluteMaximum = max;
            axis.AbsoluteMinimum = Math.Min(min, max - 1);
            yaxis.AbsoluteMinimum = ymin;
        }

        public DataPoint? SetMaximumLine()
        {
            try
            {
                OxyPlot.Series.LineSeries max = viewerChart.Series.FirstOrDefault(x => x.Title.Contains("Max Value")) as OxyPlot.Series.LineSeries;

                if (max == null)
                {
                    max = new OxyPlot.Series.LineSeries();
                    max.Color = OxyColor.FromRgb(200, 0, 0);
                    max.StrokeThickness = 1;
                    viewerChart.Series.Add(max);
                }
                else
                    max.Points.Clear();

                DataPoint maxPoint = new DataPoint(0, 0);
                foreach (var series in areaSeries)
                {
                    foreach (var point in series.Points)
                    {
                        if (point.Y >= maxPoint.Y) maxPoint = point;
                    }
                }

                var ymin = areaSeries.Min(x => x.Points.Min(y => y.Y));
                max.Points.Add(maxPoint);
                max.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(maxPoint.X).AddMilliseconds(-1)), ymin));
                max.Title = "Max Value: " + maxPoint.Y;

                return maxPoint;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Removes all files created by the plot class to function.
        /// </summary>
        public void RemovePlot()
        {
            viewerChart = null;
            areaSeries = null;
            // var file = new FileInfo($"mopsdata//plots//{ID}plot.json");
            // file.Delete();
            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Extension.ToLower().Equals($"{ID.ToLower()}plot.pdf"));
            foreach (var f in files)
                f.Delete();
        }

        /// Forces r, g or b to be bright enough for darkmode
        public static OxyColor StringToColour(string name)
        {
            int[] colour = { 0, 0, 0 };
            foreach (char c in name)
            {
                colour[0] = (((int)c * (colour[0]) + 1) % 105);
            }
            colour[0] += 150;
            for (int i = 1; i < 3; i++)
            {
                colour[i] = colour[i - 1];
                foreach (char c in name)
                {
                    colour[i] = (((int)c * (colour[i]) + 1) % 255);
                }
            }
            int firstPos = colour[0] % 3;
            int tempColour = colour[firstPos];
            colour[firstPos] = colour[0];
            colour[0] = tempColour;

            var oxycolour = OxyColor.FromRgb((byte)colour[0], (byte)colour[1], (byte)colour[2]);
            return oxycolour;
        }

        public void Recolour()
        {
            foreach (var series in areaSeries)
            {
                OxyColor newColour = OxyColor.FromRgb((byte)StaticBase.ran.Next(30, 220), (byte)StaticBase.ran.Next(30, 220), (byte)StaticBase.ran.Next(30, 220));
                series.Color = newColour;
                series.Fill = OxyColor.FromAColor(100, newColour);
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

        public void Dispose()
        {
            RemovePlot();
        }
    }
}