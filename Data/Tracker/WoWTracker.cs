using System;
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
using WowDotNetAPI;
using WowDotNetAPI.Models;

namespace MopsBot.Data.Tracker
{
    public class WoWTracker : ITracker
    {
        public static WowExplorer WoWClient;
        public Region WoWRegion;
        public string Realm;
        public string WoWName;
        public bool trackEquipment;
        public bool trackStats;
        public bool trackFeed;
        private Character oldStats;

        public WoWTracker() : base(300000, (ExistingTrackers * 2000 + 500) % 300000)
        {
        }

        public WoWTracker(string WoWInformation) : base(300000)
        {
            var information = WoWInformation.Split("|");
            Name = WoWInformation;
            WoWName = information[2];
            Realm = information[1];
            trackFeed = true;


            if (!Enum.TryParse<Region>(information[0], true, out WoWRegion))
                throw new Exception($"No Realm called {information[0]} could be found.");

            //Check if person exists by forcing Exceptions if not.
            try
            {
                oldStats = WoWClient.GetCharacter(WoWRegion, Realm, WoWName, CharacterOptions.GetEverything);
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"No Character called `{WoWName}` could be found in {Realm}!");
            }
        }

        public override void PostInitialisation()
        {
            oldStats = WoWClient.GetCharacter(WoWRegion, Realm, WoWName, CharacterOptions.GetEverything);
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                Character newStats = WoWClient.GetCharacter(WoWRegion, Realm, WoWName, CharacterOptions.GetEverything);

                var changes = getChangedStats(newStats);
                if (changes.Count > 0)
                {
                    foreach (ulong channel in ChannelIds.ToList())
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

        private Embed createEmbed(Character WoWChar, Dictionary<string, string> changedStats)
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
            Character WoWChar = WoWClient.GetCharacter(Enum.Parse<Region>(Region, true), Realm, Name, CharacterOptions.GetEverything);

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
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

        private static Dictionary<string, string> getStats(Character WoWChar)
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

        private Dictionary<string, string> getChangedStats(Character WoWChar)
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
                        var equipment = WoWClient.GetItem(item.Value.ItemId);
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
}