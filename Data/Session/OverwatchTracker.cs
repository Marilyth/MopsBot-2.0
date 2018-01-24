using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Session.APIResults;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data.Session
{
    /// <summary>
    /// A tracker which keeps track of an Overwatch players stats
    /// </summary>
    public class OverwatchTracker : IDisposable
    {
        bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        private System.Threading.Timer checkForChange;
        public string name;
        public HashSet<ulong> ChannelIds;
        private OStatsResult information;

        /// <summary>
        /// Initialises the tracker by setting attributes and setting up a Timer with a 10 minutes interval
        /// </summary>
        /// <param name="OWName"> The Name-Battletag combination of the player to track </param>
        public OverwatchTracker(string OWName)
        {
            ChannelIds = new HashSet<ulong>();
            name = OWName;

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), 0, 600000);
            Console.WriteLine(DateTime.Now + " OW Tracker started for " + name);
        }

        /// <summary>
        /// Event for the Timer, to check for changed stats
        /// </summary>
        /// <param name="stateinfo"></param>
        private void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                OStatsResult newInformation = overwatchInformation();

                if (information == null)
                {
                    information = newInformation;
                }

                if (newInformation == null) return;

                var changedStats = getChangedStats(information, newInformation);
                Console.WriteLine(DateTime.Now + " OW Tracker fetched Stats for " + name + "\nNew Stats?: " + (changedStats.Count != 0) + $" (Wins: {newInformation.eu.stats.quickplay.overall_stats.wins})");

                if (changedStats.Count != 0)
                {
                    sendNotification(newInformation, changedStats, getSessionMostPlayed(information.eu.heroes.playtime, newInformation.eu.heroes.playtime));
                    information = newInformation;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Queries the API to fetch a JSON containing all the stats for the player
        /// Then converts it into OStatsResult
        /// </summary>
        /// <returns>An OStatsResult representing the fetched JSON as an object</returns>
        private OStatsResult overwatchInformation()
        {
            string query = MopsBot.Module.Information.readURL($"https://owapi.net/api/v3/u/{name}/blob");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<OStatsResult>(query, _jsonWriter);
        }

        ///<summary>Builds an embed out of the changed stats, and sends it as a Discord message </summary>
        /// <param name="overwatchInformation">All fetched stats of the user </param>
        /// <param name="changedStats">All changed stats of the user, together with a string presenting them </param>
        /// <param name="mostPlayed">The most played Hero of the session, together with a string presenting them </param>
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

            e.AddField("Sessions most played Hero", $"{mostPlayed.Item2}");
            if (mostPlayed.Item1.Equals("Ana") || mostPlayed.Item1.Equals("Moira") || mostPlayed.Item1.Equals("Orisa") || mostPlayed.Item1.Equals("Doomfist") || mostPlayed.Item1.Equals("Sombra"))
                e.ImageUrl = $"https://blzgdapipro-a.akamaihd.net/hero/{mostPlayed.Item1.ToLower()}/full-portrait.png";
            else
                e.ImageUrl = $"https://blzgdapipro-a.akamaihd.net/media/thumbnail/{mostPlayed.Item1.ToLower()}-gameplay.jpg";

            foreach (var channel in ChannelIds)
            {
                await ((SocketTextChannel)Program.client.GetChannel(channel)).SendMessageAsync("", false, e);
            }
        }

        /// <summary>
        /// Checks if there were any changes in major stats
        /// Major stats are: Level, Quickplay Wins, Competitive Wins, Competitive Rank
        /// </summary>
        /// <param name="oldStats">OStatsResult representing the stats before the Timer elapsed</param>
        /// <param name="newStats">OStatsResult representing the stats after the Timer elapsed</param>
        /// <returns>A Dictionary with changed stats as Key, and a string presenting them as Value</returns>
        private Dictionary<string, string> getChangedStats(OStatsResult oldStats, OStatsResult newStats)
        {
            Dictionary<string, string> changedStats = new Dictionary<string, string>();

            OverallStats quickNew = newStats.eu.stats.quickplay.overall_stats;
            OverallStats quickOld = oldStats.eu.stats.quickplay.overall_stats;

            if (quickNew.level * (quickNew.prestige + 1) > quickOld.level * (quickOld.prestige + 1))
            {
                changedStats.Add("Level", quickNew.level.ToString() +
                                $" (+{(quickNew.level + (quickNew.prestige * 100)) - (quickOld.level + (quickOld.prestige * 100))})");
            }

            if (quickNew.wins > quickOld.wins)
            {
                changedStats.Add("Games won", quickNew.wins.ToString() +
                                $" (+{quickNew.wins - quickOld.wins})");
            }

            if (oldStats.eu.stats.competitive != null)
            {
                OverallStats compNew = newStats.eu.stats.competitive.overall_stats;
                OverallStats compOld = oldStats.eu.stats.competitive.overall_stats;

                if (compNew.comprank != compOld.comprank)
                {
                    int difference = compNew.comprank - compOld.comprank;
                    changedStats.Add("Comp Rank", compNew.comprank.ToString() +
                                    $" ({(difference > 0 ? "+" : "-") + difference})");
                }

                if (compNew.wins > compOld.wins)
                {
                    changedStats.Add("Comp Games won", compNew.wins.ToString() +
                                    $" (+{compNew.wins - compOld.wins})");
                }
            }

            return changedStats;
        }

        /// <summary>
        /// Determines the most played Hero of the last play session
        /// </summary>
        /// <param name="oldStats">Playtime of each hero before the Timer elapsed</param>
        /// <param name="newStats">Playtime of each hero after the Timer elapsed</param>
        /// <returns>A Tuple with the name of the most played Hero, and a string presenting the change in playtime</returns>
        private Tuple<string, string> getSessionMostPlayed(Playtime oldStats, Playtime newStats)
        {
            var New = newStats.merge();
            var Old = oldStats.merge();
            var difference = new Dictionary<string, double>();

            foreach (string key in Old.Keys)
                difference.Add(key, New[key] - Old[key]);
                
            var sortedList = (from entry in difference orderby entry.Value descending select entry).ToList();
            string leaderboard = "";
            for(int i = 0; i < 5; i++){
                if(sortedList[i].Value > 0)
                    leaderboard += $"{sortedList[i].Key}: {Math.Round(New[sortedList[i].Key], 2)}hrs (+{Math.Round(sortedList[i].Value, 2)})\n";
                else
                    break;
            }

            if(difference[sortedList[0].Key] > 0)
                return Tuple.Create(sortedList[0].Key, leaderboard);

            return Tuple.Create("CannotFetchArcade", "");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                checkForChange.Dispose();
            }

            disposed = true;
        }
    }
}
