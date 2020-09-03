using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MopsBot.Data.Tracker.APIResults.Youtube;
using MongoDB.Bson.Serialization.Attributes;
using System.Xml;

namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public class HTMLTracker : BaseTracker
    {
        public string Regex;
        public string oldMatch;
        public DatePlot DataGraph;
        public static readonly string TRACKEMPTYSTRINGS = "TrackEmptyStrings";

        public HTMLTracker() : base()
        {
        }

        public HTMLTracker(string name) : base()
        {
            Name = name;
            Regex = name.Split("|||")[1];

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var value = fetchData().Result;
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException();
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Expression `{name}` yielded no result!", e);
            }
        }

        public async override void PostInitialisation(object info = null)
        {
            if (DataGraph != null)
                DataGraph.InitPlot("Date", "Value", format: "dd-MMM", relative: false);
        }

        public async override void PostChannelAdded(ulong channelId)
        {
            base.PostChannelAdded(channelId);

            var config = ChannelConfig[channelId];
            config[TRACKEMPTYSTRINGS] = false;
        }

        public override async void Conversion(object obj = null)
        {
            bool save = false;
            foreach (var channel in ChannelConfig.Keys.ToList())
            {
                if (!ChannelConfig[channel].ContainsKey(TRACKEMPTYSTRINGS))
                {
                    ChannelConfig[channel][TRACKEMPTYSTRINGS] = false;
                    save = true;
                }
            }
            if (save)
                await UpdateTracker();
        }

        public async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var match = await fetchData();

                if (!string.IsNullOrEmpty(match) || ChannelConfig.Any(x => (bool)x.Value[TRACKEMPTYSTRINGS]))
                {
                    bool isNumeric;

                    if (oldMatch == null){
                        oldMatch = match;
                        await UpdateTracker();
                    }

                    if((isNumeric = Double.TryParse(oldMatch, out double value)) && DataGraph == null){
                        DataGraph = new DatePlot("HTML" + Name.GetHashCode(), "Date", "Value", "dd-MMM", false);
                        DataGraph.AddValue("Value", value);
                    }

                    if (!match.Equals(oldMatch)){
                        if(isNumeric){
                            DataGraph.AddValue("Value", value);
                            var success = Double.TryParse(match, out value);
                            if(success) DataGraph.AddValue("Value", value);
                        }

                        foreach (var channel in ChannelConfig.Keys.ToList())
                            await OnMajorChangeTracked(channel, CreateChangeEmbed($"{oldMatch} -> {match}", isNumeric), (string)ChannelConfig[channel]["Notification"]);
                        
                        oldMatch = match;
                        await UpdateTracker();
                    }
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        private async Task<string> fetchData()
        {
            var html = await Module.Information.GetURLAsync(Name.Split("|||")[0]);
            var match = System.Text.RegularExpressions.Regex.Match(html, Regex, System.Text.RegularExpressions.RegexOptions.Singleline);
            return match.Groups.Values.Last().Value;
        }

        public static async Task<string> FetchData(string expression)
        {
            var html = await Module.Information.GetURLAsync(expression.Split("|||")[0]);
            var match = System.Text.RegularExpressions.Regex.Match(html, expression.Split("|||")[1], System.Text.RegularExpressions.RegexOptions.Singleline);
            return match.Groups.Values.Last().Value;
        }

        public static async Task<System.Text.RegularExpressions.MatchCollection> FetchAllData(string expression)
        {
            var html = await Module.Information.GetURLAsync(expression.Split("|||")[0]);
            var match = System.Text.RegularExpressions.Regex.Matches(html, expression.Split("|||")[1], System.Text.RegularExpressions.RegexOptions.Singleline);
            return match;
        }

        private Embed CreateChangeEmbed(string changedData, bool showGraph = false)
        {
            EmbedBuilder e = new EmbedBuilder();

            e.Color = new Color(136, 107, 62);
            e.Title = $"Data changed!";
            e.Description = changedData;
            e.WithCurrentTimestamp();
            if(showGraph) e.ImageUrl = DataGraph.DrawPlot();

            return e.Build();
        }

        public override async Task UpdateTracker(){
            await StaticBase.Trackers[TrackerType.HTML].UpdateDBAsync(this);
        }
    }
}