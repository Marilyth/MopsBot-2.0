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

        public HTMLTracker() : base(120000, ExistingTrackers * 5000)
        {
        }

        public HTMLTracker(Dictionary<string, string> args) : base(120000, 60000){
            if(!StaticBase.Trackers[TrackerType.HTML].GetTrackers().ContainsKey(args["Name"] + "|||" + args["Regex"])){
                base.SetBaseValues(args);
                Name = args["Name"] + "|||" + args["Regex"];
                Regex = args["Regex"];
            } else {
                this.Dispose();
                var curTracker = StaticBase.Trackers[TrackerType.HTML].GetTrackers()[args["Name"] + "|||" + args["Regex"]];
                var curGuild = ((ITextChannel)Program.Client.GetChannel(ulong.Parse(args["Channel"]))).GuildId;

                var OldValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(curTracker.GetAsScope(curGuild)));
                StaticBase.Trackers[TrackerType.HTML].UpdateContent(new Dictionary<string, Dictionary<string, string>>{{"NewValue", args}, {"OldValue", OldValues}});
                throw new ArgumentException($"Tracker for {args["Name"]} existed already, updated instead!");
            }
        }

        public HTMLTracker(string name) : base(120000)
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
            catch (Exception)
            {
                Dispose();
                throw new Exception($"Expression `{name}` yielded no result!");
            }
        }

        public async override void PostInitialisation()
        {
            if (DataGraph != null)
                DataGraph.InitPlot("Date", "Value", format: "dd-MMM", relative: false);
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var match = await fetchData();

                if (!string.IsNullOrEmpty(match))
                {
                    bool isNumeric;

                    if (oldMatch == null){
                        oldMatch = match;
                        await StaticBase.Trackers[TrackerType.HTML].UpdateDBAsync(this);
                    }

                    if((isNumeric = Double.TryParse(oldMatch, out double value)) && DataGraph == null){
                        DataGraph = new DatePlot("HTML" + Name.GetHashCode(), "Date", "Value", "dd-MMM", false);
                        DataGraph.AddValue("Value", value, relative: false);
                    }

                    if (!match.Equals(oldMatch)){
                        if(isNumeric){
                            DataGraph.AddValue("Value", value, relative: false);
                            var success = Double.TryParse(match, out value);
                            if(success) DataGraph.AddValue("Value", value, relative: false);
                        }

                        foreach (var channel in ChannelMessages.Keys.ToList())
                            await OnMajorChangeTracked(channel, CreateChangeEmbed($"{oldMatch} -> {match}", isNumeric), ChannelMessages[channel]);
                        
                        oldMatch = match;
                        await StaticBase.Trackers[TrackerType.HTML].UpdateDBAsync(this);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + $"[ERROR] by {Name} at {DateTime.Now} = \n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<string> fetchData()
        {
            var html = await Module.Information.ReadURLAsync(Name.Split("|||")[0]);
            var match = System.Text.RegularExpressions.Regex.Match(html, Regex, System.Text.RegularExpressions.RegexOptions.Singleline);
            return match.Groups.Last().Value;
        }

        public static async Task<string> FetchData(string expression)
        {
            var html = await Module.Information.ReadURLAsync(expression.Split("|||")[0]);
            var match = System.Text.RegularExpressions.Regex.Match(html, expression.Split("|||")[1], System.Text.RegularExpressions.RegexOptions.Singleline);
            return match.Groups.Last().Value;
        }

        public static async Task<System.Text.RegularExpressions.MatchCollection> FetchAllData(string expression)
        {
            var html = await Module.Information.ReadURLAsync(expression.Split("|||")[0]);
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

        public override Dictionary<string, object> GetParameters(ulong guildId){
            var parameters = base.GetParameters(guildId);
            (parameters["Parameters"] as Dictionary<string, object>)["Regex"] = "";
            return parameters;
        }

        public override void Update(Dictionary<string, Dictionary<string, string>> args){
            base.Update(args);
            Regex = args["NewValue"]["Regex"];
            Name = args["NewValue"]["Name"] + Regex;
        }

        public override object GetAsScope(ulong channelId){
            return new ContentScope(){
                Id = this.Name,
                Name = this.Name.Split("|||")[0],
                Regex = this.Regex,
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId
            };
        }

        public new struct ContentScope
        {
            public string Id;
            public string Name;
            public string Regex;
            public string Notification;
            public string Channel;
        }
    }
}