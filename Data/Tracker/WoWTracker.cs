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

namespace MopsBot.Data.Tracker
{
    public class WoWTracker : ITracker
    {
        public static WowExplorer WoWClient;
        public Region WoWRegion;
        public string Realm;
        public string WoWName;
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

                if (newStats.Level > oldStats.Level)
                {
                    foreach (ulong channel in ChannelIds)
                    {
                        await OnMajorChangeTracked(channel, createEmbed(newStats, getChangedStats(newStats)), ChannelMessages[channel]);
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
                e.AddField(kvp.Key, kvp.Value, true);

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
                e.AddField(kvp.Key, kvp.Value, true);

            return e.Build();
        }

        private static Dictionary<string, string> getStats(Character WoWChar){
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

            return stats;
        }

        private Dictionary<string, string> getChangedStats(Character WoWChar)
        {
            Dictionary<string, string> changes = new Dictionary<string, string>();

            //Level changes
            changes["Level"] = "";
            
            if (oldStats.Level < WoWChar.Level)
                changes["Level"] += $"Level: {WoWChar.Level} (+{WoWChar.Level - oldStats.Level})\n";
            if (oldStats.Items.AverageItemLevel < WoWChar.Items.AverageItemLevel)
                changes["Level"] += $"ILevel: {WoWChar.Items.AverageItemLevel} (+{WoWChar.Items.AverageItemLevel - oldStats.Items.AverageItemLevel})";
            
            if (string.IsNullOrEmpty(changes["Level"]))
                changes.Remove("Level");

            //Stat changes
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

            //Equipment changes
            changes["Equipment"] = "";
            
            if (!oldStats.Items.Back.Id.Equals(WoWChar.Items.Back.Id))
            {
                changes["Equipment"] += $"[{WoWChar.Items.Back.Name}](http://www.wowhead.com/item={WoWChar.Items.Back.Id})\n";
            }
            if (!oldStats.Items.Chest.Id.Equals(WoWChar.Items.Chest.Id))
            {
                changes["Equipment"] += $"[{WoWChar.Items.Chest.Name}](http://www.wowhead.com/item={WoWChar.Items.Chest.Id})\n";
            }
            if (!oldStats.Items.Feet.Id.Equals(WoWChar.Items.Feet.Id))
            {
                changes["Equipment"] += $"[{WoWChar.Items.Feet.Name}](http://www.wowhead.com/item={WoWChar.Items.Feet.Id})\n";
            }
            if (!oldStats.Items.Finger1.Id.Equals(WoWChar.Items.Finger1.Id))
            {
                changes["Equipment"] += $"[{WoWChar.Items.Finger1.Name}](http://www.wowhead.com/item={WoWChar.Items.Finger1.Id})\n";
            }
            if (!oldStats.Items.Finger2.Id.Equals(WoWChar.Items.Finger2.Id))
            {
                changes["Equipment"] += $"[{WoWChar.Items.Finger2.Name}](http://www.wowhead.com/item={WoWChar.Items.Finger2.Id})\n";
            }
            if (!oldStats.Items.Hands.Id.Equals(WoWChar.Items.Hands.Id))
            {
                changes["Equipment"] += $"[{WoWChar.Items.Hands.Name}](http://www.wowhead.com/item={WoWChar.Items.Hands.Id})\n";
            }
            if (!oldStats.Items.Head.Id.Equals(WoWChar.Items.Head.Id))
            {
                changes["Equipment"] += $"[{WoWChar.Items.Head.Name}](http://www.wowhead.com/item={WoWChar.Items.Head.Id})\n";
            }
            
            if (string.IsNullOrEmpty(changes["Equipment"]))
                changes.Remove("Equipment");

            return changes;
        }
    }
}