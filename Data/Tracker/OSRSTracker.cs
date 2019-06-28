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
using System.Xml;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class OSRSTracker : BaseTracker
    {
        private string channelThumbnailUrl, uploadPlaylistId;
        private List<long[]> stats;

        public OSRSTracker() : base()
        {
        }

        public OSRSTracker(Dictionary<string, string> args) : base(){
            base.SetBaseValues(args, true);

            //Check if Name ist valid
            try{
                var test = new OSRSTracker(Name);
                test.Dispose();
                stats = test.stats;
                SetTimer();
            } catch (Exception e){
                this.Dispose();
                throw e;
            }

            if(StaticBase.Trackers[TrackerType.OSRS].GetTrackers().ContainsKey(Name)){
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.OSRS].GetTrackers()[Name];
                curTracker.ChannelMessages[ulong.Parse(args["Channel"].Split(":")[1])] = args["Notification"];
                StaticBase.Trackers[TrackerType.OSRS].UpdateContent(new Dictionary<string, Dictionary<string, string>>{{"NewValue", args}, {"OldValue", args}}).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }

        public OSRSTracker(string name) : base()
        {
            Name = name;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var checkExists = fetchStats(Name).Result;
                stats = checkExists;
                SetTimer();
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"Player `{name}` could not be found on OSRS!\nPerhaps none of their skills are in the top 2 million?");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var newStats = await fetchStats(Name);
                if (stats == null)
                    stats = newStats;

                List<string> changedStats = new List<string>();

                for (int i = 0; i < 24; i++)
                {
                    if (newStats[i][1] != stats[i][1])
                    {
                        string statName = ((StatNames)i).ToString();
                        var id = (long)Enum.Parse(typeof(StatEmojiId), $"{statName.ToLower()}");
                        string statEmoji = $"<:{statName.ToLower()}:{id}>";
                        changedStats.Add($"{statEmoji} Level {newStats[i][1]} (+{newStats[i][1] - stats[i][1]})");
                    }
                }

                if (changedStats.Count > 0)
                    foreach (var channel in ChannelConfig.Keys.ToList())
                        await OnMajorChangeTracked(channel, CreateChangeEmbed(changedStats), (string)ChannelConfig[channel]["Notification"]);

                stats = newStats;
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        private static async Task<List<long[]>> fetchStats(string name)
        {
            string query = await MopsBot.Module.Information.GetURLAsync($"https://secure.runescape.com/m=hiscore_oldschool/index_lite.ws?player={name}");

            var allStats = query.Split("\n").ToList();
            List<long[]> statList = new List<long[]>();
            allStats.RemoveAt(allStats.Count - 1);

            foreach (var stat in allStats)
            {
                statList.Add(stat.Split(",").Select(x => long.Parse(x)).ToArray());
            }

            return statList;
        }

        private Embed CreateChangeEmbed(List<string> changedStats)
        {
            EmbedBuilder e = new EmbedBuilder();

            e.Color = new Color(136, 107, 62);
            e.Title = $"Level up!";
            e.Description = string.Join("\n", changedStats);
            e.WithCurrentTimestamp();

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://imgb.apk.tools/150/b/c/2/com.jagex.oldscape.android.png";
            footer.Text = "Old School RuneScape";
            e.Footer = footer;

            return e.Build();
        }

        public static async Task<Embed> GetStatEmbed(string name)
        {
            var stats = await fetchStats(name);

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(136, 107, 62);
            e.WithTitle(name + " Skills");
            StringBuilder statString = new StringBuilder();

            for (int i = 0; i < 24; i++)
            {
                var stat = stats[i];
                if (stat[1] > 1)
                {
                    string statName = ((StatNames)i).ToString();
                    var id = (long)Enum.Parse(typeof(StatEmojiId), $"{statName.ToLower()}");
                    string statEmoji = $"<:{statName.ToLower()}:{id}>";
                    statString.Append($"{statEmoji} Rank {stat[0]}, Level {stat[1]}\n");
                }
            }

            e.Description = statString.ToString();

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://imgb.apk.tools/150/b/c/2/com.jagex.oldscape.android.png";
            footer.Text = "Old School RuneScape";
            e.Footer = footer;


            return e.Build();
        }

        public static async Task<Embed> GetCompareEmbed(string name1, string name2)
        {
            var stats1 = await fetchStats(name1);
            var stats2 = await fetchStats(name2);

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(136, 107, 62);
            e.Title = $"{name1} vs {name2}";
            StringBuilder statString = new StringBuilder();

            for (int i = 0; i < 24; i++)
            {
                var stat1 = stats1[i];
                var stat2 = stats2[i];
                if (stat1[1] > 1 || stat2[1] > 1)
                {
                    string statName = ((StatNames)i).ToString();
                    var id = (long)Enum.Parse(typeof(StatEmojiId), $"{statName.ToLower()}");
                    string statEmoji = $"<:{statName.ToLower()}:{id}>";
                    statString.Append($"{statEmoji} {stat1[1]} {(stat1[1] > stat2[1] ? " > " : stat1[1] == stat2[1] ? " = " : " < ")} {stat2[1]}\n");
                }
            }

            e.Description = statString.ToString();

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://imgb.apk.tools/150/b/c/2/com.jagex.oldscape.android.png";
            footer.Text = "Old School RuneScape";
            e.Footer = footer;

            return e.Build();
        }

        public static async Task<Embed> GetItemEmbed(string name)
        {
            var stats = await MopsBot.Module.Information.GetURLAsync($"http://oldschoolrunescape.wikia.com/wiki/{name.ToLower().Replace(" ", "_")}?action=raw");
            var information = stats.Split(new string[] { "{{", "}}", "==" }, 1000, StringSplitOptions.RemoveEmptyEntries);

            
            string description = information.First(x => x.Contains("px]]"));
            description = string.Join("\n", description.Split("\n").Where(x => !x.StartsWith("[[File:")).Select(x => x.Replace("[[", "").Replace("]]", "")));
            
            List<string> replaced = new List<string>();
            foreach (string word in description.Split(new string[] { " ", ",", "." }, 1000, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(word, out int n) && !replaced.Contains(word))
                {
                    StatEmojiId id;
                    if (Enum.TryParse<StatEmojiId>(word, true, out id))
                    {
                        description = description.Replace(word, $"<:{word.ToLower()}:{(long)id}>");
                        replaced.Add(word);
                    }
                }
            }

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(136, 107, 62);
            e.Title = $"{name}";
            e.WithUrl($"http://oldschoolrunescape.wikia.com/wiki/{name.Replace(" ", "_")}");
            e.WithThumbnailUrl($"http://oldschoolrunescape.wikia.com/wiki/File:{name.Replace(" ", "_")}.png");
            e.WithDescription(string.Join("\n", description.Split("\n").Where(x => !x.StartsWith("[[File:")).Select(x => x.Replace("[[", "").Replace("]]", ""))));

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://imgb.apk.tools/150/b/c/2/com.jagex.oldscape.android.png";
            footer.Text = "Old School RuneScape";
            e.Footer = footer;

            return e.Build();
        }


        private enum StatNames
        {
            Total,
            Attack,
            Defence,
            Strength,
            Hitpoints,
            Ranged,
            Prayer,
            Magic,
            Cooking,
            Woodcutting,
            Fletching,
            Fishing,
            Firemaking,
            Crafting,
            Smithing,
            Mining,
            Herblore,
            Agility,
            Thieving,
            Slayer,
            Farming,
            Runecraft,
            Hunter,
            Construction,
        }

        private enum StatEmojiId : long
        {
            total = 494862969793019914,
            woodcutting = 494855851451088897,
            thieving = 494855851664736257,
            strength = 494855851526324235,
            smithing = 494855851799085056,
            slayer = 494855851501158401,
            fletching = 494855812918018073,
            fishing = 494855802394247189,
            firemaking = 494855791333998602,
            farming = 494855776125583360,
            defence = 494855761969807380,
            crafting = 494855747943923712,
            cooking = 494855735256154112,
            construction = 494855720878211084,
            attack = 494855228630368267,
            agility = 494855695775301632,
            prayer = 494855851870519306,
            mining = 494855851786371072,
            magic = 494855851887034368,
            runecraft = 494855851467866124,
            ranged = 494855851836964864,
            hunter = 494855851711004678,
            hitpoints = 494855851753078794,
            herblore = 494855851782176768,
        }
    }
}