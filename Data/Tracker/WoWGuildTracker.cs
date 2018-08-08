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
    public class WoWGuildTracker : ITracker
    {
        public Region WoWRegion;
        public string Realm;
        public string GuildName;
        public bool trackLoot;
        public bool trackAchievements;
        private Guild oldStats;

        public WoWGuildTracker() : base(300000, (ExistingTrackers * 2000 + 500) % 300000)
        {
        }

        public WoWGuildTracker(string WoWInformation) : base(300000)
        {
            var information = WoWInformation.Split("|");
            Name = WoWInformation;
            GuildName = information[2];
            Realm = information[1];
            trackAchievements = true;
            trackLoot = true;

            if (!Enum.TryParse<Region>(information[0], true, out WoWRegion))
                throw new Exception($"No Realm called {information[0]} could be found.");

            //Check if person exists by forcing Exceptions if not.
            try
            {
                oldStats = WoWTracker.WoWClient.GetGuild(WoWRegion, Realm, GuildName, GuildOptions.GetEverything);
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"No Guild called `{GuildName}` could be found in {Realm}!");
            }
        }

        public override void PostInitialisation()
        {
            oldStats = WoWTracker.WoWClient.GetGuild(WoWRegion, Realm, GuildName, GuildOptions.GetEverything);
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                Guild newStats = WoWTracker.WoWClient.GetGuild(WoWRegion, Realm, GuildName, GuildOptions.GetEverything);

                var changes = getNewsFeed(newStats);
                if (changes.Count > 0)
                {
                    foreach (ulong channel in ChannelIds)
                    {
                        foreach (var embed in changes)
                            await OnMajorChangeTracked(channel, embed, ChannelMessages[channel]);
                    }
                }

                oldStats = newStats;
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + $"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private List<Embed> getNewsFeed(Guild WoWGuild)
        {
            List<Embed> newsEmbeds = new List<Embed>();

            //New Loot
            if (trackLoot)
            {
                var oldLootDict = oldStats.News.Where(x => x.Type.Equals("itemLoot") || x.Type.Equals("itemPurchase")).ToDictionary(x => x.Timestamp + x.ItemID + x.Character);
                var newLootDict = WoWGuild.News.Where(x => x.Type.Equals("itemLoot") || x.Type.Equals("itemPurchase")).ToDictionary(x => x.Timestamp + x.ItemID + x.Character);
                foreach (var item in newLootDict)
                {
                    if (!oldLootDict.ContainsKey(item.Key))
                    {
                        var equipment = WoWTracker.WoWClient.GetItem(item.Value.ItemID);
                        Character character = null;

                        try
                        {
                            character = WoWTracker.WoWClient.GetCharacter(WoWRegion, Realm, item.Value.Character);
                        }
                        catch{}

                        var e = new EmbedBuilder();
                        e.WithAuthor(item.Value.Character, character != null ? "http://render-eu.worldofwarcraft.com/character/" + character?.Thumbnail ?? "Whoops" + $"?rand={StaticBase.ran.Next(0, 99999999)}" : null).WithTimestamp(DateTimeOffset.FromUnixTimeMilliseconds(item.Value?.Timestamp ?? DateTime.UtcNow.Ticks))
                            .WithThumbnailUrl("https://render-eu.worldofwarcraft.com/icons/56/" + equipment.Icon + ".jpg").WithTitle("Item Acquired")
                            .WithFooter("World of Warcraft", "https://nerdygamergirls.files.wordpress.com/2015/03/featured-image-wow-logo-221x221.png?w=1920&h=768&crop=1")
                            .WithDescription($"Player {item.Value.Character} got the Item:\n[{equipment.Name}](http://www.wowhead.com/item={equipment.Id}) **{((rarity)equipment.Quality).ToString()}**\n");

                        newsEmbeds.Add(e.Build());
                    }
                }
            }

            if (trackAchievements)
            {
                var oldAchievementDict = oldStats.News.Where(x => x.Type.Equals("playerAchievement")).ToDictionary(x => x.Achievement.Id + x.Character);
                var newAchievementDict = WoWGuild.News.Where(x => x.Type.Equals("playerAchievement")).ToDictionary(x => x.Achievement.Id + x.Character);
                foreach (var item in newAchievementDict)
                {
                    if (!oldAchievementDict.ContainsKey(item.Key))
                    {
                        Character character = null;

                        try
                        {
                            character = WoWTracker.WoWClient.GetCharacter(WoWRegion, Realm, item.Value.Character);
                        }
                        catch{}

                        var e = new EmbedBuilder();
                        e.WithAuthor(item.Value.Character, character != null ? "http://render-eu.worldofwarcraft.com/character/" + character?.Thumbnail ?? "Whoops" + $"?rand={StaticBase.ran.Next(0, 99999999)}" : null).WithTimestamp(DateTimeOffset.FromUnixTimeMilliseconds(item.Value?.Timestamp ?? DateTime.UtcNow.Ticks))
                            .WithThumbnailUrl("https://render-eu.worldofwarcraft.com/icons/56/" + item.Value.Achievement.Icon + ".jpg").WithTitle("Achievement Acquired")
                            .WithFooter("World of Warcraft", "https://nerdygamergirls.files.wordpress.com/2015/03/featured-image-wow-logo-221x221.png?w=1920&h=768&crop=1")
                            .WithDescription($"Player {item.Value.Character} got the Achievement:\n[{item.Value.Achievement.Title}](http://www.wowhead.com/achievement={item.Value.Achievement.Id}) **{item.Value.Achievement.Points} Points**\n");

                        newsEmbeds.Add(e.Build());
                    }
                }
            }

            return newsEmbeds;
        }
    }
}
