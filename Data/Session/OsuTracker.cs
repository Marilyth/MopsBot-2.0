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

namespace MopsBot.Data
{
    public class OsuTracker
    {
        public List<osuUser> osuUsers = new List<osuUser>();
        private System.Threading.Timer checkForChange;
        public OsuTracker()
        {
            StreamReader read = new StreamReader(new FileStream("mopsdata//osuid.txt", FileMode.Open));

            string stats = "";
            while ((stats = read.ReadLine()) != null)
            {
                string[] data = stats.Split(':');
                if (osuUsers.Exists(x => x.discordID == ulong.Parse(data[1])))
                    osuUsers.Find(x => x.discordID == ulong.Parse(data[1])).channels.Add(ulong.Parse(data[0]));
                else
                {
                    osuUsers.Add(new osuUser(ulong.Parse(data[1]), data[2], data[3], ulong.Parse(data[0])));
                }
            }

            read.Dispose();

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), 60000, 60000);
        }

        private void CheckForChange_Elapsed(object stateinfo)
        {
            foreach (osuUser OUser in osuUsers)
            {
                try
                {
                    if (!Program.client.GetChannel(OUser.channels[0]).GetUser(OUser.discordID).Status.Equals(UserStatus.Offline))
                    {
                        dynamic userInformation = osuUser.userStats(OUser.ident.ToString(), OUser.mainMode);
                        
                        if (OUser.pp + 0.5 <= double.Parse(userInformation["pp_raw"].ToString(), CultureInfo.InvariantCulture))
                        {
                            string query = MopsBot.Module.Information.readURL($"https://osu.ppy.sh/api/get_user_recent?u={OUser.ident}&{OUser.mainMode}&limit=10&k=8ad11f6daf7b439f96eee1c256d474cd9925d4d8");

                            dynamic recentScores = JsonConvert.DeserializeObject(query);

                            string test2 = recentScores[0]["rank"];
                            for(int i = 0; i < 10; i++){
                                if(!recentScores[i]["rank"].ToString().Contains("F")){
                                    recentScores = recentScores[i];
                                    break;
                                }
                            }

                            string beatmap_ID = recentScores["beatmap_id"];
                            query = MopsBot.Module.Information.readURL($@"https://osu.ppy.sh/api/get_scores?b={beatmap_ID}&{OUser.mainMode}&u={OUser.username}&limit=1&k=8ad11f6daf7b439f96eee1c256d474cd9925d4d8");

                            dynamic ppInformation = JsonConvert.DeserializeObject(query);
                            ppInformation = ppInformation[0];

                            query = MopsBot.Module.Information.readURL($@"https://osu.ppy.sh/api/get_beatmaps?b={beatmap_ID}&{OUser.mainMode}&a=1&k=8ad11f6daf7b439f96eee1c256d474cd9925d4d8");                       

                            dynamic beatmapInformation = JsonConvert.DeserializeObject(query);
                            beatmapInformation = beatmapInformation[0];

                            sendOsuNotification(new dynamic[]{userInformation, recentScores, ppInformation, beatmapInformation}, OUser.channels, OUser);

                            OUser.updateStats(userInformation);
                        }
                        else if (OUser.pp < double.Parse(userInformation["pp_raw"].ToString(), CultureInfo.InvariantCulture))
                            OUser.updateStats(userInformation);
                    }
                }
                catch { }
            }
        }

        private async void sendOsuNotification(dynamic[] osuInformation, List<ulong> ChannelIds, osuUser OUser)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = osuInformation[3]["artist"] + " - " + osuInformation[3]["title"];
            e.Url = $"https://osu.ppy.sh/b/{osuInformation[1]["beatmap_id"]}&{OUser.mainMode}";
            e.Description = Math.Round(double.Parse(osuInformation[3]["difficultyrating"].ToString(), CultureInfo.InvariantCulture), 2) + "*";

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = osuInformation[0]["username"];
            author.Url = $"https://osu.ppy.sh/u/{osuInformation[0]["user_id"]}";
            author.IconUrl = $"https://a.ppy.sh/{osuInformation[0]["user_id"]}_0.png";
            e.Author = author;

            e.ThumbnailUrl = $"https://a.ppy.sh/{osuInformation[0]["user_id"]}_0.png";
            e.ImageUrl = $"https://b.ppy.sh/thumb/{osuInformation[3]["beatmapset_id"]}l.jpg";

            e.AddInlineField("Score", osuInformation[1]["score"] + $" ({osuInformation[1]["maxcombo"]}x)");
            e.AddInlineField("Acc", calcAcc(osuInformation[2], OUser.mainMode) + $"% {osuInformation[2]["rank"]}");
            e.AddInlineField("PP for play", Math.Round(double.Parse(osuInformation[2]["pp"].ToString(), CultureInfo.InvariantCulture), 2)+ $" (+{Math.Round(double.Parse(osuInformation[0]["pp_raw"].ToString(), CultureInfo.InvariantCulture) - OUser.pp, 2)})");
            e.AddInlineField("Rank", osuInformation[0]["pp_rank"] + $" ({((OUser.ppRank - (ulong)osuInformation[0]["pp_rank"]) >= 0 ? "+" : "")}{OUser.ppRank - (ulong)osuInformation[0]["pp_rank"]})");

            foreach(var channel in ChannelIds)
            {
                await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channel)).SendMessageAsync("", false, e);
            }
        }

        public void writeInformation()
        {
            StreamWriter write = new StreamWriter(new FileStream("mopsdata//osuid.txt", FileMode.Create));

            foreach(osuUser user in osuUsers)
            {
                foreach (ulong ch in user.channels)
                    write.WriteLine($"{ch}:{user.discordID}:{user.ident}:{user.mainMode}");
            }

            write.Dispose();
        }

        private double calcAcc(dynamic dict3, string mainMode)
        {
            double pointsOfHits = 0, numberOfHits = 0, accuracy = 0, fruitsCaught = 0, numberOfFruits = 0;

            switch (mainMode)
            {
                case "m=3":
                    pointsOfHits =(int)dict3["count50"] * 50 +(int)dict3["count100"] * 100 +(int)dict3["count300"] * 300 + (int)dict3["countkatu"] * 200 + (int)dict3["countgeki"] * 300;
                    numberOfHits =(int)dict3["count50"] +(int)dict3["count100"] + (int)dict3["count300"] + (int)dict3["countmiss"] +(int)dict3["countkatu"] + (int)dict3["countgeki"];
                    accuracy = (pointsOfHits / (numberOfHits * 300));
                    return Math.Round(accuracy * 100, 2);
                case "m=2":
                    fruitsCaught =(int)dict3["count50"] + (int)dict3["count100"] + (int)dict3["count300"];
                    numberOfFruits = (int)dict3["count50"] +  (int)dict3["count100"] + (int)dict3["count300"] + (int)dict3["countkatu"] + (int)dict3["countmiss"];
                    accuracy = (numberOfFruits / fruitsCaught);
                    return Math.Round(accuracy * 100, 2);
                case "m=1":
                    pointsOfHits = (dict3["countmiss"] * 0 + (int)dict3["count100"] * 0.5 +(int)dict3["count300"]) * 300;
                    numberOfHits = (int)dict3["countmiss"] +  (int)dict3["count100"] +  (int)dict3["count300"];
                    accuracy = (pointsOfHits / (numberOfHits * 300));
                    return Math.Round(accuracy * 100, 2);
                case "m=0":
                    pointsOfHits = (dict3["count50"] * 50) + (dict3["count100"] * 100) + (dict3["count300"] * 300);
                    numberOfHits = (int)dict3["count50"] + (int)dict3["count100"] + (int)dict3["count300"] + (int)dict3["countmiss"];
                    accuracy = (pointsOfHits / (numberOfHits * 300));
                    return Math.Round(accuracy*100, 2);
            }
            return 0;
}
    }

    public class osuUser
    {
        public string username, ident, mainMode;
        public double accuracy, pp;
        public ulong score, playcount, ppRank;
        public ulong discordID;
        public List<ulong> channels;

        public osuUser(ulong disID, string osuID, string mode, ulong channel)
        {
            channels = new List<ulong>();
            ident = osuID;
            discordID = disID;
            mainMode = mode;
            channels.Add(channel);
            updateStats();
        }

        public osuUser(ulong disID, string osuID, ulong channel)
        {
            channels = new List<ulong>();
            ident = osuID;
            discordID = disID;
            mainMode = "m=0";
            channels.Add(channel);
            updateStats();
        }

        public static dynamic userStats(string id, string mode)
        {
            string query = MopsBot.Module.Information.readURL($"https://osu.ppy.sh/api/get_user?u={id}&{mode}&k=8ad11f6daf7b439f96eee1c256d474cd9925d4d8");
            query = query.Remove(0, 1);
            query = query.Remove(query.Length - 1, 1);

            return JsonConvert.DeserializeObject(query);
        }

        public static dynamic userStats(string id)
        {
            string query = MopsBot.Module.Information.readURL($"https://osu.ppy.sh/api/get_user?u={id}&k=8ad11f6daf7b439f96eee1c256d474cd9925d4d8");
            query = query.Remove(0, 1);
            query = query.Remove(query.Length - 1, 1);

            return JsonConvert.DeserializeObject(query);
        }

        public void updateStats()
        {
            dynamic tempDict = userStats(ident, mainMode);

            try
            {
                username = tempDict["username"];
                playcount = tempDict["playcount"];
                score = tempDict["total_score"];
                ppRank = tempDict["pp_rank"];
                pp = double.Parse(tempDict["pp_raw"].ToString(), CultureInfo.InvariantCulture);
                accuracy = double.Parse(tempDict["accuracy"].ToString(), CultureInfo.InvariantCulture);
            } catch(Exception e) { Console.WriteLine(e.Message); }
        }

        public void updateStats(dynamic tempDict)
        {
            pp = double.Parse(tempDict["pp_raw"].ToString(), CultureInfo.InvariantCulture);
            accuracy = double.Parse(tempDict["accuracy"].ToString(), CultureInfo.InvariantCulture);
            username = tempDict["username"];
            score = tempDict["total_score"];
            playcount = tempDict["playcount"];
            ppRank = tempDict["pp_rank"];
        }

        public string modeToString()
        {
            switch (mainMode)
            {
                case "m=0":
                    return "Standard";
                case "m=1":
                    return "Taiko";
                case "m=2":
                    return "CtB";
                case "m=3":
                    return "Mania";
                default:
                    return "Nothing";
            }
        }
    }
}