using System;
using Discord;
using Discord.Net;
using Newtonsoft.Json;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MopsBot.Data.Tracker
{
    public class OsuTracker : ITracker
    {
        public Dictionary<string, double> allPP;
        public double PPThreshold;

        public OsuTracker() : base(60000, (ExistingTrackers * 2000 + 500) % 60000)
        {
        }

        public OsuTracker(string name) : base(60000)
        {
            Name = name;
            allPP = new Dictionary<string, double>();
            allPP.Add("m=0", 0);
            allPP.Add("m=1", 0);
            allPP.Add("m=2", 0);
            allPP.Add("m=3", 0);
            PPThreshold = 0.1;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var checkExists = fetchUser().Result;
                var test = checkExists.username;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Person `{Name}` could not be found on Osu!");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                foreach (var pp in allPP.ToList())
                {
                    APIResults.OsuResult userInformation = await fetchUser(pp.Key);
                    if (userInformation == null) return;

                    if (pp.Value > 0 && pp.Value + PPThreshold <= double.Parse(userInformation.pp_raw, CultureInfo.InvariantCulture))
                    {
                        var recentScores = await fetchRecent(pp.Key);
                        APIResults.RecentScore scoreInformation = recentScores.First(x => !x.rank.Equals("F"));

                        APIResults.Beatmap beatmapInformation = await fetchBeatmap(scoreInformation.beatmap_id, pp.Key);

                        foreach (ulong channel in ChannelIds)
                        {
                            await OnMajorChangeTracked(channel, createEmbed(userInformation, beatmapInformation, await fetchScore(scoreInformation.beatmap_id, pp.Key),
                                                       Math.Round(double.Parse(userInformation.pp_raw, CultureInfo.InvariantCulture) - pp.Value, 2), pp.Key), ChannelMessages[channel]);
                        }
                    }
                    allPP[pp.Key] = double.Parse(userInformation.pp_raw ?? "0", CultureInfo.InvariantCulture);
                    StaticBase.trackers["osu"].SaveJson();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        public async Task<APIResults.OsuResult> fetchUser(string mode = "m=0")
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://osu.ppy.sh/api/get_user?u={Name}&{mode}&event_days=31&k={Program.Config["Osu"]}");

            return JsonConvert.DeserializeObject<APIResults.OsuResult>(query.Substring(1, query.Length - 2));
        }

        public async Task<List<APIResults.RecentScore>> fetchRecent(string mode = "m=0")
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://osu.ppy.sh/api/get_user_recent?u={Name}&limit=50&{mode}&k={Program.Config["Osu"]}");

            return JsonConvert.DeserializeObject<List<APIResults.RecentScore>>(query);
        }

        public async Task<APIResults.Score> fetchScore(string beatmapID, string mode = "m=0")
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://osu.ppy.sh/api/get_scores?b={beatmapID}&{mode}&u={Name}&limit=1&k={Program.Config["Osu"]}");

            return JsonConvert.DeserializeObject<List<APIResults.Score>>(query)[0];
        }

        public async Task<APIResults.Beatmap> fetchBeatmap(string beatmapID, string mode = "m=0")
        {
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://osu.ppy.sh/api/get_beatmaps?b={beatmapID}&{mode}&a=1&k={Program.Config["Osu"]}");

            return JsonConvert.DeserializeObject<APIResults.Beatmap>(query.Substring(1, query.Length - 2));
        }

        private Embed createEmbed(APIResults.OsuResult userInformation, APIResults.Beatmap beatmapInformation,
                                        APIResults.Score scoreInformation, double ppChange, string mode = "m=0")
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = $"{beatmapInformation.artist} - {beatmapInformation.title} [{beatmapInformation.version}]";
            e.Url = $"https://osu.ppy.sh/b/{beatmapInformation.beatmap_id}&m={beatmapInformation.mode}";
            e.Description = Math.Round(double.Parse(beatmapInformation.difficultyrating, CultureInfo.InvariantCulture), 2) + "*";
            e.Timestamp = DateTime.Parse(scoreInformation.date).AddHours(2);

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            author.Url = $"https://osu.ppy.sh/u/{userInformation.user_id}";
            author.IconUrl = $"https://a.ppy.sh/{userInformation.user_id}_0.png";
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://vignette.wikia.nocookie.net/cytus/images/5/51/Osu_icon.png";
            footer.Text = "Osu!";
            e.Footer = footer;

            switch (mode)
            {
                case "m=0":
                    e.ThumbnailUrl = "https://w.ppy.sh/1/19/Mode-osu.png";
                    break;
                case "m=1":
                    e.ThumbnailUrl = "http://w.ppy.sh/4/40/Mode-taiko.png";
                    break;
                case "m=2":
                    e.ThumbnailUrl = "https://w.ppy.sh/2/26/Mode-ctb.png";
                    break;
                case "m=3":
                    e.ThumbnailUrl = "http://w.ppy.sh/5/51/Mode-mania.png";
                    break;
            }
            e.ImageUrl = $"https://b.ppy.sh/thumb/{beatmapInformation.beatmapset_id}l.jpg";

            e.AddField("Score", scoreInformation.score + $" ({scoreInformation.maxcombo}x)", true);
            e.AddField("Acc", calcAcc(scoreInformation, int.Parse(beatmapInformation.mode)) + $"% {scoreInformation.rank}", true);
            e.AddField("PP for play", Math.Round(double.Parse(scoreInformation.pp, CultureInfo.InvariantCulture), 2) + $" (+{ppChange})", true);
            e.AddField("Rank", userInformation.pp_rank, true);

            return e.Build();
        }

        private double calcAcc(APIResults.Score scoreInformation, int mode)
        {
            double pointsOfHits = 0, numberOfHits = 0, accuracy = 0, fruitsCaught = 0, numberOfFruits = 0;
            int count50 = int.Parse(scoreInformation.count50);
            int count100 = int.Parse(scoreInformation.count100);
            int count300 = int.Parse(scoreInformation.count300);
            int countmiss = int.Parse(scoreInformation.countmiss);
            int countkatu = int.Parse(scoreInformation.countkatu);
            int countgeki = int.Parse(scoreInformation.countgeki);

            switch (mode)
            {
                case 3:
                    pointsOfHits = count50 * 50 + count100 * 100 + count300 * 300 + countkatu * 200 + countgeki * 300;
                    numberOfHits = count50 + count100 + count300 + countmiss + countkatu + countgeki;
                    accuracy = ((double)pointsOfHits / (numberOfHits * 300));
                    return Math.Round(accuracy * 100, 2);
                case 2:
                    fruitsCaught = count50 + count100 + count300;
                    numberOfFruits = count50 + count100 + count300 + countkatu + countmiss;
                    accuracy = (numberOfFruits / (double)fruitsCaught);
                    return Math.Round(accuracy * 100, 2);
                case 1:
                    pointsOfHits = (count100 * 0.5 + count300) * 300;
                    numberOfHits = countmiss + count100 + count300;
                    accuracy = ((double)pointsOfHits / (numberOfHits * 300));
                    return Math.Round(accuracy * 100, 2);
                case 0:
                    pointsOfHits = (count50 * 50) + (count100 * 100) + (count300 * 300);
                    numberOfHits = count50 + count100 + count300 + countmiss;
                    accuracy = ((double)pointsOfHits / (numberOfHits * 300));
                    return Math.Round(accuracy * 100, 2);
            }
            return 0;
        }
    }
}