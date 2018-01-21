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
            try{
                OStatsResult newInformation = overwatchInformation();

                if (information == null)
                {
                    information = newInformation;
                }

                if (newInformation == null) return;

                var changedStats = getChangedStats(information, newInformation);

                if (changedStats.Count != 0)
                {
                    sendNotification(newInformation, changedStats, getSessionMostPlayed(information, newInformation));
                    information = newInformation;
                }
            }catch{
                
            }
        }

        private OStatsResult overwatchInformation()
        {
            string query = Information.readURL($"https://owapi.net/api/v3/u/{name}/blob");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<OStatsResult>(query, _jsonWriter);
        }

        private async void sendNotification(OStatsResult overwatchInformation, Dictionary<string, string> changedStats, Tuple<string, string> mostPlayed)
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

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "http://i.imgur.com/YZ4w2ey.png";
            footer.Text = "Overwatch";
            e.Timestamp = DateTime.Now;
            e.Footer = footer;

            e.ThumbnailUrl = stats.avatar;

            foreach (var kvPair in changedStats)
            {
                e.AddInlineField(kvPair.Key, kvPair.Value);
            }

            e.AddField("Sessions most played Hero", $"{mostPlayed.Item1}: {mostPlayed.Item2}");
            if(mostPlayed.Item1.Equals("Ana") || mostPlayed.Item1.Equals("Moira") || mostPlayed.Item1.Equals("Orisa") || mostPlayed.Item1.Equals("Doomfist") || mostPlayed.Item1.Equals("Sombra"))
                e.ImageUrl = $"https://blzgdapipro-a.akamaihd.net/hero/{mostPlayed.Item1.ToLower()}/full-portrait.png";
            else
                e.ImageUrl = $"https://blzgdapipro-a.akamaihd.net/media/thumbnail/{mostPlayed.Item1.ToLower()}-gameplay.jpg";

            foreach (var channel in ChannelIds)
            {
                await ((SocketTextChannel)Program.client.GetChannel(channel)).SendMessageAsync("", false, e);
            }
        }

        private Dictionary<string, string> getChangedStats(OStatsResult oldStats, OStatsResult newStats){
                Dictionary<string, string> changedStats = new Dictionary<string, string>();
                
                OverallStats compNew = newStats.getNotNull().stats.competitive.overall_stats;
                OverallStats compOld = oldStats.getNotNull().stats.competitive.overall_stats;
                OverallStats quickNew = newStats.getNotNull().stats.quickplay.overall_stats;
                OverallStats quickOld = oldStats.getNotNull().stats.quickplay.overall_stats;
            
                if (quickNew.level * (quickNew.prestige+1) > quickOld.level * (quickOld.prestige+1))
                {
                    changedStats.Add("Level", quickNew.level.ToString() +
                                    $" (+{(quickNew.level + (quickNew.prestige*100)) - (quickOld.level + (quickOld.prestige*100))})");
                }

                if (quickNew.wins > quickOld.wins)
                {
                    changedStats.Add("Games won", quickNew.wins.ToString() +
                                    $" (+{quickNew.wins - quickOld.wins})");
                }

                if (compNew.comprank != compOld.comprank)
                {
                    int difference = compNew.comprank - compOld.comprank;
                    changedStats.Add("Comp Rank", compNew.comprank.ToString() +
                                    $" ({(difference > 0 ? "+":"-") + difference})");
                }

                if (compNew.wins > compOld.wins)
                {
                    changedStats.Add("Comp Games won", compNew.wins.ToString() +
                                    $" (+{compNew.wins - compOld.wins})");
                }

                return changedStats;
        }

        private Tuple<string, string> getSessionMostPlayed(OStatsResult oldStats, OStatsResult newStats){
                var New = newStats.getNotNull().heroes.playtime.merge();
                var Old = oldStats.getNotNull().heroes.playtime.merge();
                var difference = new Dictionary<string, double>();
            
                foreach(string key in Old.Keys)
                    difference.Add(key, New[key] - Old[key]);

                string max = "McCree";

                foreach(string key in difference.Keys)
                    if(difference[key] - difference[max] > 0) 
                        max = key;

                return Tuple.Create(max, $"{Math.Round(New[max], 2)}hrs (+{Math.Round(difference[max], 2)})");
        }
    }
}
