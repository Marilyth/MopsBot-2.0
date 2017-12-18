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

namespace MopsBot.Module.Data.Session
{
    public class OverwatchTracker
    {
        private System.Threading.Timer checkForChange;
        public string name;
        public HashSet<ulong> ChannelIds;
        private OStatsResult information;


        public OverwatchTracker(string OWName)
        {
            ChannelIds = new HashSet<ulong>();
            name = OWName;

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6, 59) * 1000, 600000);
        }

        private void CheckForChange_Elapsed(object stateinfo)
        {
            OStatsResult newInformation = overwatchInformation();
            Dictionary<string, string> changedStats = new Dictionary<string, string>();

            if (information == null)
            {
                information = newInformation;
            }

            if (newInformation == null) return;

            if (newInformation.eu.stats.quickplay.overall_stats.level > information.eu.stats.quickplay.overall_stats.level)
            {
                changedStats.Add("Level", newInformation.eu.stats.quickplay.overall_stats.level.ToString() +
                                $" (+{newInformation.eu.stats.quickplay.overall_stats.level - information.eu.stats.quickplay.overall_stats.level})");
            }

            if (newInformation.eu.stats.quickplay.overall_stats.wins > information.eu.stats.quickplay.overall_stats.wins)
            {
                changedStats.Add("Games won", newInformation.eu.stats.quickplay.overall_stats.wins.ToString() +
                                $" (+{newInformation.eu.stats.quickplay.overall_stats.wins - information.eu.stats.quickplay.overall_stats.wins})");
            }

            if (changedStats.Count != 0)
            {
                sendNotification(newInformation, changedStats);
                information = newInformation;
            }
        }

        private OStatsResult overwatchInformation()
        {
            string query = Information.readURL($"https://owapi.net/api/v3/u/{name}/stats");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<OStatsResult>(query, _jsonWriter);
        }

        private async void sendNotification(OStatsResult overwatchInformation, Dictionary<string, string> changedStats)
        {
            OverallStats stats = overwatchInformation.eu.stats.quickplay.overall_stats;

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = "New Stats!";
            e.Url = $"https://playoverwatch.com/en-us/career/pc/eu/{name}";

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = name.Split("-")[0];
            author.Url = $"https://playoverwatch.com/en-us/career/pc/eu/{name}";
            author.IconUrl = stats.avatar;
            e.Author = author;

            e.ThumbnailUrl = stats.avatar;
            //e.ImageUrl = $"{overwatchInformation.stream.preview.medium}?rand={StaticBase.ran.Next(0,99999999)}";

            foreach (var kvPair in changedStats)
            {
                e.AddInlineField(kvPair.Key, kvPair.Value);
            }

            foreach (var channel in ChannelIds)
            {
                await ((SocketTextChannel)Program.client.GetChannel(channel)).SendMessageAsync("", false, e);
            }
        }
    }
}
