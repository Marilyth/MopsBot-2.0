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
                Dictionary<string, string> changedStats = new Dictionary<string, string>();

                if (information == null)
                {
                    information = newInformation;
                }

                if (newInformation == null) return;
            
                OverallStats compNew = newInformation.eu.stats.competitive.overall_stats;
                OverallStats compOld = information.eu.stats.competitive.overall_stats;
                OverallStats quickNew = newInformation.eu.stats.quickplay.overall_stats;
                OverallStats quickOld = information.eu.stats.quickplay.overall_stats;
            
                if (quickNew.level > quickOld.level)
                {
                    changedStats.Add("Level", quickNew.level.ToString() +
                                    $" (+{quickNew.level - quickOld.level})");
                }

                if (quickNew.wins > quickOld.wins)
                {
                    changedStats.Add("Games won", quickNew.wins.ToString() +
                                    $" (+{quickNew.wins - quickOld.wins})");
                }

                if (compNew.comprank != compOld.comprank)
                {
                    changedStats.Add("Comp Rank", compNew.comprank.ToString() +
                                    $" (+{compNew.comprank - compOld.comprank})");
                }

                if (changedStats.Count != 0)
                {
                    sendNotification(newInformation, changedStats);
                    information = newInformation;
                }
            }catch{
                
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

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "http://i.imgur.com/YZ4w2ey.png";
            footer.Text = "Overwatch";
            e.Footer = footer;

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
