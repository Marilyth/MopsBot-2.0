using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Tweetinvi;
using Tweetinvi.Models;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using WowDotNetAPI;
using WowDotNetAPI.Models;
using System.Text.RegularExpressions;

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
                    foreach (ulong channel in ChannelIds)
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
            e.Title = "WoW Stat Changes";
            e.Timestamp = DateTime.UtcNow;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://nerdygamergirls.files.wordpress.com/2015/03/featured-image-wow-logo-221x221.png?w=1920&h=768&crop=1";
            footer.Text = "World of Warcraft";
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = WoWChar.Name;
            e.Author = author;

            e.ThumbnailUrl = "http://render-eu.worldofwarcraft.com/character/" + WoWChar.Thumbnail;
            e.ImageUrl = "https://render-eu.worldofwarcraft.com/character/" + WoWChar.Thumbnail.Replace("avatar", "main");

            foreach (var kvp in changedStats)
                e.AddField(kvp.Key, kvp.Value.Substring(0, 1024 > kvp.Value.Length ? kvp.Value.Length : 1024), true);

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

            e.ThumbnailUrl = "http://render-eu.worldofwarcraft.com/character/" + WoWChar.Thumbnail;
            e.ImageUrl = "https://render-eu.worldofwarcraft.com/character/" + WoWChar.Thumbnail.Replace("avatar", "main");

            foreach (var kvp in getStats(WoWChar))
                e.AddField(kvp.Key, kvp.Value.Substring(0, 1024 > kvp.Value.Length ? kvp.Value.Length : 1024), true);

            return e.Build();
        }

        private static Dictionary<string, string> getStats(Character WoWChar)
        {
            Dictionary<string, string> stats = new Dictionary<string, string>();

            stats["Level"] = $"Level: {WoWChar.Level}\n";
            stats["Level"] += $"ILevel: {WoWChar.Items.AverageItemLevel}";

            stats["Stats"] = $"Stamina: {WoWChar.Stats.Stamina}\n";
            stats["Stats"] += $"Strength: {WoWChar.Stats.Strength}\n";
            stats["Stats"] += $"Intellect: {WoWChar.Stats.Intellect}\n";
            stats["Stats"] += $"Agility: {WoWChar.Stats.Agility}\n";

            stats["Equipment"] = $"[{WoWChar.Items.Back?.Name}](http://www.wowhead.com/item={WoWChar.Items.Back?.Id})\n";
            stats["Equipment"] += $"[{WoWChar.Items.Chest?.Name}](http://www.wowhead.com/item={WoWChar.Items.Chest?.Id})\n";
            stats["Equipment"] += $"[{WoWChar.Items.Feet?.Name}](http://www.wowhead.com/item={WoWChar.Items.Feet?.Id})\n";
            stats["Equipment"] += $"[{WoWChar.Items.Finger1?.Name}](http://www.wowhead.com/item={WoWChar.Items.Finger1?.Id})\n";
            stats["Equipment"] += $"[{WoWChar.Items.Finger2?.Name}](http://www.wowhead.com/item={WoWChar.Items.Finger2?.Id})\n";
            stats["Equipment"] += $"[{WoWChar.Items.Hands?.Name}](http://www.wowhead.com/item={WoWChar.Items.Hands?.Id})\n";
            stats["Equipment"] += $"[{WoWChar.Items.Head?.Name}](http://www.wowhead.com/item={WoWChar.Items.Head?.Id})\n";
            stats["Equipment"] += $"[{WoWChar.Items.Legs?.Name}](http://www.wowhead.com/item={WoWChar.Items.Legs?.Id}) Legs\n";
            stats["Equipment"] += $"[{WoWChar.Items.MainHand?.Name}](http://www.wowhead.com/item={WoWChar.Items.MainHand?.Id}) MainHand\n";
            //stats["Equipment"] += $"[{WoWChar.Items.Neck?.Name}](http://www.wowhead.com/item={WoWChar.Items.Neck?.Id}) Neck\n";
            stats["Equipment"] += $"[{WoWChar.Items.OffHand?.Name}](http://www.wowhead.com/item={WoWChar.Items.OffHand?.Id}) OffHand\n";
            //stats["Equipment"] += $"[{WoWChar.Items.Ranged?.Name}](http://www.wowhead.com/item={WoWChar.Items.Ranged?.Id}) Ranged\n";
            //stats["Equipment"] += $"[{WoWChar.Items.Shirt?.Name}](http://www.wowhead.com/item={WoWChar.Items.Shirt?.Id}) Shirt\n";
            stats["Equipment"] += $"[{WoWChar.Items.Shoulder?.Name}](http://www.wowhead.com/item={WoWChar.Items.Shoulder?.Id}) Shoulder\n";
            //stats["Equipment"] += $"[{WoWChar.Items.Tabard?.Name}](http://www.wowhead.com/item={WoWChar.Items.Tabard?.Id}) Tabard\n";
            stats["Equipment"] += $"[{WoWChar.Items.Trinket1?.Name}](http://www.wowhead.com/item={WoWChar.Items.Trinket1?.Id}) Trinket1\n";
            stats["Equipment"] += $"[{WoWChar.Items.Trinket2?.Name}](http://www.wowhead.com/item={WoWChar.Items.Trinket2?.Id}) Trinket2\n";
            stats["Equipment"] += $"[{WoWChar.Items.Waist?.Name}](http://www.wowhead.com/item={WoWChar.Items.Waist?.Id}) Waist\n";
            //stats["Equipment"] += $"[{WoWChar.Items.Wrist?.Name}](http://www.wowhead.com/item={WoWChar.Items.Wrist?.Id}) Wrist\n";

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

                if (!oldStats.Stats.Stamina.Equals(WoWChar.Stats.Stamina))
                {
                    changes["Stats"] += $"Stamina: {WoWChar.Stats.Stamina} ({(oldStats.Stats.Stamina < WoWChar.Stats.Stamina ? "+" : "")}{WoWChar.Stats.Stamina - oldStats.Stats.Stamina})\n";
                }
                if (!oldStats.Stats.Strength.Equals(WoWChar.Stats.Strength))
                {
                    changes["Stats"] += $"Strength: {WoWChar.Stats.Strength} ({(oldStats.Stats.Strength < WoWChar.Stats.Strength ? "+" : "")}{WoWChar.Stats.Strength - oldStats.Stats.Strength})\n";
                }
                if (!oldStats.Stats.Intellect.Equals(WoWChar.Stats.Intellect))
                {
                    changes["Stats"] += $"Intellect: {WoWChar.Stats.Intellect} ({(oldStats.Stats.Intellect < WoWChar.Stats.Intellect ? "+" : "")}{WoWChar.Stats.Intellect - oldStats.Stats.Intellect})\n";
                }
                if (!oldStats.Stats.Agility.Equals(WoWChar.Stats.Agility))
                {
                    changes["Stats"] += $"Agility: {WoWChar.Stats.Agility} ({(oldStats.Stats.Agility < WoWChar.Stats.Agility ? "+" : "")}{WoWChar.Stats.Agility - oldStats.Stats.Agility})\n";
                }

                if (string.IsNullOrEmpty(changes["Stats"]))
                    changes.Remove("Stats");
            }

            //Equipment changes
            if (trackEquipment)
            {
                changes["Equipment"] = "";

                if (!oldStats.Items.Back?.Id.Equals(WoWChar.Items.Back?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Back.Name}](http://www.wowhead.com/item={WoWChar.Items.Back.Id}) Back\n";
                }
                if (!oldStats.Items.Chest?.Id.Equals(WoWChar.Items.Chest?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Chest.Name}](http://www.wowhead.com/item={WoWChar.Items.Chest.Id}) Chest\n";
                }
                if (!oldStats.Items.Feet?.Id.Equals(WoWChar.Items.Feet?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Feet.Name}](http://www.wowhead.com/item={WoWChar.Items.Feet.Id}) Feet\n";
                }
                if (!oldStats.Items.Finger1?.Id.Equals(WoWChar.Items.Finger1?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Finger1.Name}](http://www.wowhead.com/item={WoWChar.Items.Finger1.Id}) Finger-1\n";
                }
                if (!oldStats.Items.Finger2?.Id.Equals(WoWChar.Items.Finger2?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Finger2.Name}](http://www.wowhead.com/item={WoWChar.Items.Finger2.Id}) Finger-2\n";
                }
                if (!oldStats.Items.Hands?.Id.Equals(WoWChar.Items.Hands?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Hands.Name}](http://www.wowhead.com/item={WoWChar.Items.Hands.Id}) Hands\n";
                }
                if (!oldStats.Items.Head?.Id.Equals(WoWChar.Items.Head?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Head.Name}](http://www.wowhead.com/item={WoWChar.Items.Head.Id}) Head\n";
                }
                if (!oldStats.Items.Legs?.Id.Equals(WoWChar.Items.Legs?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Legs.Name}](http://www.wowhead.com/item={WoWChar.Items.Legs.Id}) Legs\n";
                }
                if (!oldStats.Items.MainHand?.Id.Equals(WoWChar.Items.MainHand?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.MainHand.Name}](http://www.wowhead.com/item={WoWChar.Items.MainHand.Id}) Main Hand\n";
                }
                if (!oldStats.Items.OffHand?.Id.Equals(WoWChar.Items.OffHand?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.OffHand.Name}](http://www.wowhead.com/item={WoWChar.Items.OffHand.Id}) Off Hand\n";
                }
                if (!oldStats.Items.Neck?.Id.Equals(WoWChar.Items.Neck?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Neck.Name}](http://www.wowhead.com/item={WoWChar.Items.Neck.Id}) Neck\n";
                }
                if (!oldStats.Items.Ranged?.Id.Equals(WoWChar.Items.Ranged?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Ranged.Name}](http://www.wowhead.com/item={WoWChar.Items.Ranged.Id}) Ranged\n";
                }
                if (!oldStats.Items.Shirt?.Id.Equals(WoWChar.Items.Shirt?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Shirt.Name}](http://www.wowhead.com/item={WoWChar.Items.Shirt.Id}) Shirt\n";
                }
                if (!oldStats.Items.Shoulder?.Id.Equals(WoWChar.Items.Shoulder?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Shoulder.Name}](http://www.wowhead.com/item={WoWChar.Items.Shoulder.Id}) Shoulder\n";
                }
                if (!oldStats.Items.Tabard?.Id.Equals(WoWChar.Items.Tabard?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Tabard.Name}](http://www.wowhead.com/item={WoWChar.Items.Tabard.Id}) Tabard\n";
                }
                if (!oldStats.Items.Trinket1?.Id.Equals(WoWChar.Items.Trinket1?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Trinket1.Name}](http://www.wowhead.com/item={WoWChar.Items.Trinket1.Id}) Trinket-1\n";
                }
                if (!oldStats.Items.Trinket2?.Id.Equals(WoWChar.Items.Trinket2?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Trinket2.Name}](http://www.wowhead.com/item={WoWChar.Items.Trinket2.Id}) Trinket-2\n";
                }
                if (!oldStats.Items.Waist?.Id.Equals(WoWChar.Items.Waist?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Waist.Name}](http://www.wowhead.com/item={WoWChar.Items.Waist.Id}) Waist\n";
                }
                if (!oldStats.Items.Wrist?.Id.Equals(WoWChar.Items.Wrist?.Id) ?? false)
                {
                    changes["Equipment"] += $"[{WoWChar.Items.Wrist.Name}](http://www.wowhead.com/item={WoWChar.Items.Wrist.Id}) Wrist\n";
                }

                if (string.IsNullOrEmpty(changes["Equipment"]))
                    changes.Remove("Equipment");
            }

            //New Loot
            if (trackFeed)
            {
                changes["Loot"] = "";
                var oldLootDict = oldStats.Feed.Where(x => x.Type.Equals("LOOT")).ToDictionary(x => x.ItemId);
                var newLootDict = WoWChar.Feed.Where(x => x.Type.Equals("LOOT")).ToDictionary(x => x.ItemId);
                foreach (var item in newLootDict)
                {
                    if (!oldLootDict.ContainsKey(item.Key))
                    {
                        var equipment = WoWClient.GetItem(item.Value.ItemId);
                        changes["Loot"] += $"[{equipment.Name}](http://www.wowhead.com/item={item.Key})\n";
                    }
                }
                if (string.IsNullOrEmpty(changes["Loot"]))
                    changes.Remove("Loot");

                changes["Achievements"] = "";
                var oldAchievementDict = oldStats.Feed.Where(x => x.Type.Equals("ACHIEVEMENT") || x.Type.Equals("BOSSKILL") || x.Type.Equals("CRITERIA")).ToDictionary(x => x.Achievement.Id);
                var newAchievementDict = WoWChar.Feed.Where(x => x.Type.Equals("ACHIEVEMENT") || x.Type.Equals("BOSSKILL") || x.Type.Equals("CRITERIA")).ToDictionary(x => x.Achievement.Id);
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
