/*using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SharprWowApi;
using SharprWowApi.Models;
using SharprWowApi.Models.Character;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class WoWTracker : BaseTracker
    {
        public static WowClient WoWClient;
        public Region WoWRegion;
        public string Realm;
        public string WoWName;
        public bool trackEquipment;
        public bool trackStats;
        public bool trackFeed;
        private SharprWowApi.Models.Character.CharacterRoot oldStats;

        public WoWTracker() : base(300000, ExistingTrackers * 2000)
        {
        }

        public WoWTracker(Dictionary<string, string> args) : base(300000, 60000){
            base.SetBaseValues(args);
            Name = args["Region"] +"|"+ args["Realm"] +"|"+ args["_Name"];

            if(StaticBase.Trackers[TrackerType.HTML].GetTrackers().ContainsKey(Name)){
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.WoW].GetTrackers()[Name];
                curTracker.ChannelMessages[ulong.Parse(args["Channel"].Split(":")[1])] = args["Notification"];
                StaticBase.Trackers[TrackerType.WoW].UpdateContent(new Dictionary<string, Dictionary<string, string>>{{"NewValue", args}, {"OldValue", args}}).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }

        public WoWTracker(string WoWInformation) : base(300000)
        {
            var information = WoWInformation.Split("|");
            Name = WoWInformation;
            WoWName = information[2];
            Realm = information[1];
            trackFeed = true;


            //if (!Enum.TryParse<Region>(information[0], true, out WoWRegion))
            //    throw new Exception($"No Realm called {information[0]} could be found.");

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var test = WoWClient.GetAchievementAsync(2144).Result;
                oldStats = WoWClient.GetCharacterAsync(WoWName, CharacterOptions.AllOptions, Realm).Result;
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"No Character called `{WoWName}` could be found in {Realm}!");
            }
        }

        public override void PostInitialisation()
        {
            oldStats = WoWClient.GetCharacterAsync(WoWName, CharacterOptions.AllOptions).Result;
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var newStats = await WoWClient.GetCharacterAsync(WoWName, CharacterOptions.AllOptions);

                var changes = await getChangedStats(newStats);
                if (changes.Count > 0)
                {
                    foreach (ulong channel in ChannelMessages.Keys.ToList())
                    {
                        await OnMajorChangeTracked(channel, createEmbed(newStats, changes), ChannelMessages[channel]);
                    }
                }
                oldStats = newStats;
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + $"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private Embed createEmbed(CharacterRoot WoWChar, Dictionary<string, string> changedStats)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = "WoW Session Summary";
            e.Timestamp = DateTime.UtcNow;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://nerdygamergirls.files.wordpress.com/2015/03/featured-image-wow-logo-221x221.png?w=1920&h=768&crop=1";
            footer.Text = "World of Warcraft";
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = WoWChar.Name;
            e.Author = author;

            e.ThumbnailUrl = "http://render-eu.worldofwarcraft.com/character/" + WoWChar.Thumbnail + $"?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = "https://render-eu.worldofwarcraft.com/character/" + WoWChar.Thumbnail.Replace("avatar", "main") + $"?rand={StaticBase.ran.Next(0, 99999999)}";

            foreach (var kvp in changedStats)
            {
                Dictionary<string, string> subDict = new Dictionary<string, string>();
                var entries = kvp.Value.Split("\n");
                int characters = 0;

                foreach (string entry in entries)
                {
                    characters += entry.Length + 2;
                    if (subDict.ContainsKey(kvp.Key + (characters >= 1024 ? ((characters / 1024) + 1).ToString() : "")))
                        subDict[kvp.Key + (characters >= 1024 ? ((characters / 1024) + 1).ToString() : "")] += entry + "\n";
                    else
                        subDict[kvp.Key + (characters >= 1024 ? ((characters / 1024) + 1).ToString() : "")] = entry + "\n";
                }

                foreach (var entryKvp in subDict)
                {
                    e.AddField(entryKvp.Key, entryKvp.Value, true);
                }
            }

            return e.Build();
        }

        public static Embed createStatEmbed(string Region, string Realm, string Name)
        {
            CharacterRoot WoWChar = WoWClient.GetCharacterAsync(Name, CharacterOptions.AllOptions).Result;

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(255, 226, 84);
            e.Title = "WoW Stats";
            e.Timestamp = DateTime.UtcNow;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://nerdygamergirls.files.wordpress.com/2015/03/featured-image-wow-logo-221x221.png?w=1920&h=768&crop=1";
            footer.Text = "World of Warcraft";
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = WoWChar.Name;
            e.Author = author;

            e.ThumbnailUrl = "http://render-eu.worldofwarcraft.com/character/" + WoWChar.Thumbnail + $"?rand={StaticBase.ran.Next(0, 99999999)}";
            e.ImageUrl = "https://render-eu.worldofwarcraft.com/character/" + WoWChar.Thumbnail.Replace("avatar", "main") + $"?rand={StaticBase.ran.Next(0, 99999999)}";

            foreach (var kvp in getStats(WoWChar))
            {
                Dictionary<string, string> subDict = new Dictionary<string, string>();
                var entries = kvp.Value.Split("\n");
                int characters = 0;

                foreach (string entry in entries)
                {
                    characters += entry.Length + 2;
                    if (subDict.ContainsKey(kvp.Key + (characters >= 1024 ? ((characters / 1024) + 1).ToString() : "")))
                        subDict[kvp.Key + (characters >= 1024 ? ((characters / 1024) + 1).ToString() : "")] += entry + "\n";
                    else
                        subDict[kvp.Key + (characters >= 1024 ? ((characters / 1024) + 1).ToString() : "")] = entry + "\n";
                }

                foreach (var entryKvp in subDict)
                {
                    e.AddField(entryKvp.Key, entryKvp.Value, true);
                }
            }

            return e.Build();
        }

        private static Dictionary<string, string> getStats(CharacterRoot WoWChar)
        {
            Dictionary<string, string> stats = new Dictionary<string, string>();

            stats["Level"] = $"Level: {WoWChar.Level}\n";
            stats["Level"] += $"ILevel: {WoWChar.Items.AverageItemLevel}";
            stats["Stats"] = "";
            stats["Equipment"] = "";

            var statDict = WoWChar.Stats.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(prop => prop.Name, prop => Convert.ToDouble(prop.GetValue(WoWChar.Stats, null)));

            foreach (var kvp in statDict)
            {
                stats["Stats"] += $"{kvp.Key}: {kvp.Value}\n";
            }

            var itemDict = WoWChar.Items.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.GetValue(WoWChar.Items) is CharacterItem)
                    .ToDictionary(prop => prop.Name, prop => (CharacterItem)prop.GetValue(WoWChar.Items));

            foreach (var kvp in itemDict)
            {
                stats["Equipment"] += $"[{kvp.Value.Name}](http://www.wowhead.com/item={kvp.Value.Id}) **{((rarity)kvp.Value.Quality).ToString()}** {kvp.Key}\n";
            }

            return stats;
        }

        private async Task<Dictionary<string, string>> getChangedStats(CharacterRoot WoWChar)
        {
            Dictionary<string, string> changes = new Dictionary<string, string>();

            //Level changes
            changes["Level"] = "";

            if (oldStats.Level < WoWChar.Level)
                changes["Level"] += $"Level: {WoWChar.Level} (+{WoWChar.Level - oldStats.Level})\n";
            //if (oldStats.Items.AverageItemLevel < WoWChar.Items.AverageItemLevel)
            //    changes["Level"] += $"ILevel: {WoWChar.Items.AverageItemLevel} (+{WoWChar.Items.AverageItemLevel - oldStats.Items.AverageItemLevel})";

            if (string.IsNullOrEmpty(changes["Level"]))
                changes.Remove("Level");

            //Stat changes
            if (trackStats)
            {
                changes["Stats"] = "";

                var newDict = WoWChar.Stats.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(prop => prop.Name, prop => Convert.ToDouble(prop.GetValue(WoWChar.Stats, null)));

                var oldDict = oldStats.Stats.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(prop => prop.Name, prop => Convert.ToDouble(prop.GetValue(oldStats.Stats, null)));

                foreach (var kvp in newDict)
                {
                    if (kvp.Value != oldDict[kvp.Key])
                        changes["Stats"] += $"{kvp.Key}: {kvp.Value} ({(oldDict[kvp.Key] < kvp.Value ? "+" : "")}{kvp.Value - oldDict[kvp.Key]})\n";
                }

                if (string.IsNullOrEmpty(changes["Stats"]))
                    changes.Remove("Stats");
            }

            //Equipment changes
            if (trackEquipment)
            {
                changes["Equipment"] = "";

                var newDict = WoWChar.Items.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.GetValue(WoWChar.Items) is CharacterItem)
                    .ToDictionary(prop => prop.Name, prop => (CharacterItem)prop.GetValue(WoWChar.Items));


                var oldDict = oldStats.Items.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.GetValue(oldStats.Items) is CharacterItem)
                    .ToDictionary(prop => prop.Name, prop => (CharacterItem)prop.GetValue(oldStats.Items));


                foreach (var kvp in newDict)
                {
                    if (!oldDict.ContainsKey(kvp.Key) || kvp.Value.Id != oldDict[kvp.Key].Id)
                        changes["Equipment"] += $"[{kvp.Value.Name}](http://www.wowhead.com/item={kvp.Value.Id}) **{((rarity)kvp.Value.Quality).ToString()}** {kvp.Key}\n";
                }

                if (string.IsNullOrEmpty(changes["Equipment"]))
                    changes.Remove("Equipment");
            }

            //New Loot
            if (trackFeed)
            {
                changes["Loot"] = "";
                var oldLootDict = oldStats.Feed.Where(x => x.Type.Equals("LOOT")).ToDictionary(x => x.Timestamp + x.ItemId);
                var newLootDict = WoWChar.Feed.Where(x => x.Type.Equals("LOOT")).ToDictionary(x => x.Timestamp + x.ItemId);
                foreach (var item in newLootDict)
                {
                    if (!oldLootDict.ContainsKey(item.Key))
                    {
                        var equipment = await WoWClient.GetItemAsync(item.Value.ItemId.ToString());
                        changes["Loot"] += $"[{equipment.Name}](http://www.wowhead.com/item={equipment.Id}) **{((rarity)equipment.Quality).ToString()}**\n";
                    }
                }
                if (string.IsNullOrEmpty(changes["Loot"]))
                    changes.Remove("Loot");

                changes["Achievements"] = "";
                var oldAchievementDict = oldStats.Feed.Where(x => x.Type.Equals("ACHIEVEMENT") || x.Type.Equals("BOSSKILL") || x.Type.Equals("CRITERIA")).ToDictionary(x => x.Achievement.Id + x.Timestamp);
                var newAchievementDict = WoWChar.Feed.Where(x => x.Type.Equals("ACHIEVEMENT") || x.Type.Equals("BOSSKILL") || x.Type.Equals("CRITERIA")).ToDictionary(x => x.Achievement.Id + x.Timestamp);
                foreach (var item in newAchievementDict)
                {
                    if (!oldAchievementDict.ContainsKey(item.Key))
                    {
                        changes["Achievements"] += $"[{item.Value.Achievement.Title}](http://www.wowhead.com/achievement={item.Value.Achievement.Id})\n";
                    }
                }
                if (string.IsNullOrEmpty(changes["Achievements"]))
                    changes.Remove("Achievements");
            }

            return changes;
        }

        public override Dictionary<string, object> GetParameters(ulong guildId)
        {
            var parentParameters = base.GetParameters(guildId);
            (parentParameters["Parameters"] as Dictionary<string, object>)["TrackEquipment"] = new bool[]{true, false};
            (parentParameters["Parameters"] as Dictionary<string, object>)["TrackEquipment"] = new bool[]{true, false};
            (parentParameters["Parameters"] as Dictionary<string, object>)["TrackEquipment"] = new bool[]{true, false};
            (parentParameters["Parameters"] as Dictionary<string, object>)["Region"] = Enum.GetNames(typeof(Region));
            (parentParameters["Parameters"] as Dictionary<string, object>)["Realm"] = "";
            return parentParameters;
        }

        public override void Update(Dictionary<string, Dictionary<string, string>> args){
            base.Update(args);
            trackEquipment = bool.Parse(args["NewValue"]["TrackEquipment"]);
            trackStats = bool.Parse(args["NewValue"]["TrackStats"]);
            trackFeed = bool.Parse(args["NewValue"]["TrackFeed"]);
        }

        public override object GetAsScope(ulong channelId){
            return new ContentScope(){
                Id = this.Name,
                _Name = this.WoWName,
                _Region = this.WoWRegion.ToString(),
                _Realm = this.Realm,
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId,
                TrackEquipment = this.trackEquipment,
                TrackFeed = this.trackFeed,
                TrackStats = this.trackStats
            };
        }

        public new struct ContentScope
        {
            public string Id;
            public string _Name;
            public string _Region;
            public string _Realm;
            public string Notification;
            public string Channel;
            public bool TrackEquipment;
            public bool TrackStats;
            public bool TrackFeed;
        }

        public override string TrackerUrl()
        {
            return $"https://www.wowhead.com/list/{WoWRegion.ToString()}-{Realm}-{WoWName}";
        }
    }
}

namespace MopsBot.Data.Tracker
{
    public enum rarity
    {
        Poor = 0,
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        Epic = 4,
        Legendary = 5,
        Artifact = 6,
        Heirloom = 7
    }
}*/