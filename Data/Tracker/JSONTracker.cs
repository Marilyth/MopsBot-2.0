using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using NewsAPI;
using NewsAPI.Constants;
using NewsAPI.Models;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MopsBot.Data.Entities;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class JSONTracker : BaseTracker
    {
        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, string> PastInformation;
        public List<string> ToTrack;

        public JSONTracker() : base(300000, ExistingTrackers * 2000)
        {
        }

        public JSONTracker(Dictionary<string, string> args) : base(300000, 60000)
        {
            base.SetBaseValues(args, true);

            //Check if Name ist valid
            try
            {
                var test = new JSONTracker(Name);
                PastInformation = test.PastInformation;
                test.Dispose();
            }
            catch (Exception e)
            {
                this.Dispose();
                throw e;
            }

            if (StaticBase.Trackers[TrackerType.JSON].GetTrackers().ContainsKey(Name))
            {
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.JSON].GetTrackers()[Name];
                curTracker.ChannelMessages[ulong.Parse(args["Channel"].Split(":")[1])] = args["Notification"];
                StaticBase.Trackers[TrackerType.JSON].UpdateContent(new Dictionary<string, Dictionary<string, string>> { { "NewValue", args }, { "OldValue", args } }).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }

        public JSONTracker(string name) : base(300000)
        {
            name = name.Replace(" ", string.Empty);
            Name = name.Split("|||")[0];
            ToTrack = name.Split("|||")[1].Split(",").ToList();

            //Check if name yields proper results.
            try
            {
                PastInformation = getResults().Result;
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"{Name} could not be resolved, using the given paths.");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var newInformation = await getResults();

                if(PastInformation == null) PastInformation = newInformation;

                var embed = createEmbed(newInformation, PastInformation);
                if(embed.Fields.Length > 0){
                    foreach (var channel in ChannelMessages.Keys.ToList()){
                        await OnMajorChangeTracked(channel, embed, ChannelMessages[channel]);
                    }

                    PastInformation = newInformation;
                    await StaticBase.Trackers[TrackerType.JSON].UpdateDBAsync(this);
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        private async Task<Dictionary<string, string>> getResults()
        {
            var json = await FetchJSONDataAsync<dynamic>(Name);
            var result = new Dictionary<string, string>();

            foreach(string cur in ToTrack){
                string[] keywords = cur.Split("->");
                var tmpJson = json;

                foreach(string keyword in keywords){
                    int.TryParse(keyword, out int index);
                    if(index > 0) tmpJson = tmpJson[0];
                    else tmpJson = tmpJson[keyword];
                }

                result[cur] = tmpJson.ToString();
            }

            return result;
        }

        private Embed createEmbed(Dictionary<string, string> newInformation, Dictionary<string, string> oldInformation)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(255, 227, 21);
            embed.WithTitle("Change tracked").WithUrl(Name).WithCurrentTimestamp();
            embed.WithFooter(x => {
                                   x.Text = "JsonTracker"; 
                                   x.IconUrl="https://upload.wikimedia.org/wikipedia/commons/thumb/c/c9/JSON_vector_logo.svg/160px-JSON_vector_logo.svg.png";
                            });

            foreach(var kvp in newInformation){
                string oldS = oldInformation[kvp.Key];
                string newS = kvp.Value;

                if(!newS.Equals(oldS)){
                    embed.AddField(kvp.Key, kvp.Value.GetType().IsArray ? newS : $"{oldS} -> {newS}");
                }
            }

            return embed.Build();
        }

        public override string TrackerUrl()
        {
            return Name;
        }
    }
}
