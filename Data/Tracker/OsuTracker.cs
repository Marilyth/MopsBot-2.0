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
        public string Username, CurMode;
        public double pp;

        public OsuTracker() : base(60000)
        {
        }

        public OsuTracker(string name) : base(60000, 0)
        {
            Username = name;
        }

        public OsuTracker(string[] initArray) : base(60000)
        {
            Username = initArray[0];

            foreach (string channel in initArray[1].Split(new char[] { '{', '}', ';' }))
            {
                if (channel != "")
                    ChannelIds.Add(ulong.Parse(channel));
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                APIResults.OsuResult userInformation = fetchUser();
                if(userInformation == null) return;
                if(pp == 0) {
                    pp = double.Parse(userInformation.pp_raw, CultureInfo.InvariantCulture);
                    return;
                }

                if (pp + 0.5 <= double.Parse(userInformation.pp_raw, CultureInfo.InvariantCulture))
                {
                    CurMode = userInformation.events[0].getMode();
                    APIResults.Score scoreInformation = fetchScore(userInformation.events[0].beatmap_id);

                    APIResults.Beatmap beatmapInformation = fetchBeatmap(userInformation.events[0].beatmap_id);

                    foreach(ulong channel in ChannelIds)
                        await OnMajorChangeTracked(channel, createEmbed(userInformation, beatmapInformation, scoreInformation, double.Parse(userInformation.pp_raw, CultureInfo.InvariantCulture) - pp));
                }
                pp = double.Parse(userInformation.pp_raw, CultureInfo.InvariantCulture);

            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] by {Username} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        public APIResults.OsuResult fetchUser()
        {
            string query = MopsBot.Module.Information.readURL($"https://osu.ppy.sh/api/get_user?u={Username}&k=8ad11f6daf7b439f96eee1c256d474cd9925d4d8");

            return JsonConvert.DeserializeObject<APIResults.OsuResult>(query.Substring(1, query.Length-2));
        }

        public APIResults.Score fetchScore(string beatmapID)
        {
            string query = MopsBot.Module.Information.readURL($"https://osu.ppy.sh/api/get_scores?b={beatmapID}&{CurMode}&u={Username}&limit=1&k=8ad11f6daf7b439f96eee1c256d474cd9925d4d8");

            return JsonConvert.DeserializeObject<APIResults.Score>(query.Substring(1, query.Length-2));;
        }

        public APIResults.Beatmap fetchBeatmap(string beatmapID)
        {
            string query = MopsBot.Module.Information.readURL($"https://osu.ppy.sh/api/get_beatmaps?b={beatmapID}&{CurMode}&a=1&k=8ad11f6daf7b439f96eee1c256d474cd9925d4d8");

            return JsonConvert.DeserializeObject<APIResults.Beatmap>(query.Substring(1, query.Length-2));;
        }

        private EmbedBuilder createEmbed(APIResults.OsuResult userInformation, APIResults.Beatmap beatmapInformation,
                                        APIResults.Score scoreInformation, double ppChange)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = beatmapInformation.artist + " - " + beatmapInformation.title;
            e.Url = $"https://osu.ppy.sh/b/{beatmapInformation.beatmap_id}&{beatmapInformation.mode}";
            e.Description = Math.Round(double.Parse(beatmapInformation.difficultyrating, CultureInfo.InvariantCulture), 2) + "*";

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Username;
            author.Url = $"https://osu.ppy.sh/u/{Username}";
            author.IconUrl = $"https://a.ppy.sh/{userInformation.user_id}_0.png";
            e.Author = author;

            e.ThumbnailUrl = $"https://a.ppy.sh/{userInformation.user_id}_0.png";
            e.ImageUrl = $"https://b.ppy.sh/thumb/{beatmapInformation.beatmapset_id}l.jpg";

            e.AddInlineField("Score", scoreInformation.score + $" ({scoreInformation.maxcombo}x)");
            e.AddInlineField("Acc", calcAcc(scoreInformation, int.Parse(beatmapInformation.mode)) + $"% {scoreInformation.rank}");
            e.AddInlineField("PP for play", Math.Round(double.Parse(scoreInformation.pp, CultureInfo.InvariantCulture), 2) + $" (+{ppChange})");
            e.AddInlineField("Rank", userInformation.pp_rank);

            return e;
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

        public override string[] GetInitArray()
        {
            string[] informationArray = new string[2];
            informationArray[0] = Username;
            informationArray[1] = "{" + string.Join(";", ChannelIds) + "}";

            return informationArray;
        }
    }
}