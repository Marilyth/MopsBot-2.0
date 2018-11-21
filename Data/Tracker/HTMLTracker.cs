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
    public class HTMLTracker : ITracker
    {
        public string Regex;
        public string oldMatch;

        public HTMLTracker() : base(60000, ExistingTrackers * 2000)
        {
        }

        public HTMLTracker(string name) : base(60000)
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

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var match = await fetchData();

                if (!string.IsNullOrEmpty(match))
                {
                    if (oldMatch == null){
                        oldMatch = match;
                        await StaticBase.Trackers[TrackerType.HTML].UpdateDBAsync(this);
                    }

                    if (!match.Equals(oldMatch))
                        foreach (var channel in ChannelIds.ToList())
                            await OnMajorChangeTracked(channel, CreateChangeEmbed($"{oldMatch} -> {match}"), ChannelMessages[channel]);

                    oldMatch = match;
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

        private Embed CreateChangeEmbed(string changedData)
        {
            EmbedBuilder e = new EmbedBuilder();

            e.Color = new Color(136, 107, 62);
            e.Title = $"Data changed!";
            e.Description = changedData;
            e.WithCurrentTimestamp();

            return e.Build();
        }
    }
}