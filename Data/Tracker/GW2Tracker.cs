using System;
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
using MopsBot.Data.Entities;
using MopsBot.Data.Tracker.APIResults.GW2;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class GW2Tracker : BaseTracker
    {
        public GW2Information PastInformation;
        public DatePlot MoneyGraph, LevelGraph;
        public static readonly string TRACKACHIEVEMENTS = "TrackAchievements", TRACKEQUIPMENT = "TrackEquipment", TRACKLEVEL = "TrackLevel", TRACKTPBUYS = "TrackTPBuys", TRACKTPSELLS = "TrackTPSells", TRACKTPDELIVERY = "TrackTPDelivery", TRACKWEALTH = "TrackWealth";
        public string CharacterName, APIKey;
        public static readonly string Gold = "<:gw2_gold:620886817717354496>", Silver = "<:gw2_silver:620886782262771722>", Copper = "<:gw2_copper:620611671093673985>";

        public GW2Tracker() : base()
        {
        }

        public GW2Tracker(string key) : base()
        {
            CharacterName = key.Split("|||")[1];
            APIKey = key.Split("|||")[0];
            Name = key;

            try{
                var character = GetCharacterEndpoint(CharacterName, APIKey).Result;
                PastInformation = new GW2Information();

                SetTimer();
            } catch(Exception e){
                Dispose();
                throw new Exception($"https://api.guildwars2.com/v2/characters/{CharacterName}?access_token={APIKey} yielded no results.");
            }
        }

        public override async void Conversion(object obj = null)
        {
            
        }

        public async override void PostChannelAdded(ulong channelId)
        {
            base.PostChannelAdded(channelId);

            var config = ChannelConfig[channelId];
            config[TRACKACHIEVEMENTS] = false;
            config[TRACKEQUIPMENT] = false;
            config[TRACKLEVEL] = true;
            config[TRACKTPBUYS] = false;
            config[TRACKTPSELLS] = false;
            config[TRACKTPDELIVERY] = false;
            config[TRACKWEALTH] = false;

            await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
        }

        public async override void PostInitialisation(object info = null)
        {
            if (LevelGraph != null)
                LevelGraph.InitPlot("Date", "Level", format: "dd-MMM", relative: false);
            if (MoneyGraph != null)
                MoneyGraph.InitPlot("Date", "Value", format: "dd-MMM", relative: false);
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                if(ChannelConfig.Any(x => (bool)x.Value[TRACKLEVEL])){
                    if(PastInformation.character == null){
                        PastInformation.character = await GetCharacterEndpoint(CharacterName, APIKey);
                        LevelGraph = new DatePlot($"{CharacterName.Replace(" ", "")}Level", "Date", "Level", "dd-MMM", false, true);
                        LevelGraph.AddValueSeperate("Level", PastInformation.character.level, relative: false);
                        LevelGraph.AddValueSeperate("M-Level", PastInformation.character.masteryLevel, relative: false);
                        await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                    }

                    else{
                        Character pastInfo = PastInformation.character;
                        Character currentInfo = await GetCharacterEndpoint(CharacterName, APIKey);

                        if(currentInfo.level != pastInfo.level || currentInfo.masteryLevel != pastInfo.masteryLevel){
                            LevelGraph.AddValueSeperate("Level", pastInfo.level, relative: false);
                            LevelGraph.AddValueSeperate("M-Level", pastInfo.masteryLevel, relative: false);
                            LevelGraph.AddValueSeperate("Level", currentInfo.level, relative: false);
                            LevelGraph.AddValueSeperate("M-Level", currentInfo.masteryLevel, relative: false);
                            foreach(var channel in ChannelConfig.Where(x => (bool)x.Value[TRACKLEVEL])){
                                await OnMajorChangeTracked(channel.Key, createLevelEmbed(pastInfo, currentInfo));
                            }

                            PastInformation.character = currentInfo;
                            await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                        }
                    }
                }

                if(ChannelConfig.Any(x => (bool)x.Value[TRACKACHIEVEMENTS])){
                    //ToDo
                }

                if(ChannelConfig.Any(x => (bool)x.Value[TRACKWEALTH])){
                    if(PastInformation.wallet == null){
                        PastInformation.wallet = (await GetWealth(APIKey)).FirstOrDefault();
                        MoneyGraph = new DatePlot($"{CharacterName.Replace(" ", "")}Gold", "Date", "Gold", "dd-MMM", false);
                        MoneyGraph.AddValue("Gold", PastInformation.wallet.value, relative: false);
                        await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                    }

                    else{
                        Wallet pastInfo = PastInformation.wallet;
                        Wallet currentInfo = (await GetWealth(APIKey)).FirstOrDefault();

                        if(currentInfo.value != pastInfo.value){
                            MoneyGraph.AddValue("Gold", pastInfo.value, relative: false);
                            MoneyGraph.AddValue("Gold", currentInfo.value, relative: false);
                            foreach(var channel in ChannelConfig.Where(x => (bool)x.Value[TRACKWEALTH])){
                                await OnMajorChangeTracked(channel.Key, createWealthEmbed(pastInfo, currentInfo));
                            }

                            PastInformation.wallet = currentInfo;
                            await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                        }
                    }
                }

                if(ChannelConfig.Any(x => (bool)x.Value[TRACKTPBUYS])){
                    if(PastInformation.buy == null){
                        PastInformation.buy = (await GetTPBuys(APIKey)).FirstOrDefault();
                        await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                    }

                    else{
                        TPTransaction pastInfo = PastInformation.buy;
                        List<TPTransaction> currentInfo = (await GetTPBuys(APIKey)).TakeWhile(x => x.purchased > pastInfo.purchased).ToList();
                        currentInfo.Reverse();

                        foreach(var transaction in currentInfo){
                            foreach(var channel in ChannelConfig.Where(x => (bool)x.Value[TRACKTPBUYS])){
                                await OnMajorChangeTracked(channel.Key, await CreateTPBuyEmbed(transaction));
                            }
                        }

                        if(currentInfo.Count > 0){
                            PastInformation.buy = currentInfo.LastOrDefault();
                            await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                        }
                    }
                }

                if(ChannelConfig.Any(x => (bool)x.Value[TRACKTPSELLS])){
                    if(PastInformation.sell == null){
                        PastInformation.sell = (await GetTPSells(APIKey)).FirstOrDefault();
                        await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                    }

                    else{
                        TPTransaction pastInfo = PastInformation.sell;
                        List<TPTransaction> currentInfo = (await GetTPSells(APIKey)).TakeWhile(x => x.purchased > pastInfo.purchased).ToList();
                        currentInfo.Reverse();

                        foreach(var transaction in currentInfo){
                            foreach(var channel in ChannelConfig.Where(x => (bool)x.Value[TRACKTPSELLS])){
                                await OnMajorChangeTracked(channel.Key, await CreateTPSellEmbed(transaction));
                            }
                        }

                        if(currentInfo.Count > 0){
                            PastInformation.sell = currentInfo.LastOrDefault();
                            await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                        }
                    }
                }

                if(ChannelConfig.Any(x => (bool)x.Value[TRACKTPDELIVERY])){
                    if(PastInformation.delivery == null){
                        PastInformation.delivery = await GetTPInbox(APIKey);
                        await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                    }

                    else{
                        TPInbox pastInfo = PastInformation.delivery;
                        TPInbox currentInfo = await GetTPInbox(APIKey);
                        if(currentInfo.items.Count != pastInfo.items.Count || currentInfo.coins != pastInfo.coins){
                            foreach(var channel in ChannelConfig.Where(x => (bool)x.Value[TRACKTPDELIVERY])){
                                await OnMajorChangeTracked(channel.Key, CreateTPInboxEmbed(pastInfo, currentInfo));
                            }

                            PastInformation.delivery = currentInfo;
                            await StaticBase.Trackers[TrackerType.GW2].UpdateDBAsync(this);
                        }
                    }
                }

                if(ChannelConfig.Any(x => (bool)x.Value[TRACKEQUIPMENT])){
                    
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }
        
        private Embed createLevelEmbed(Character oldC, Character newC)
        {
            var mChanged = oldC.masteryLevel != newC.masteryLevel;
            var levelChanged = oldC.level != newC.level;

            var embed = new EmbedBuilder();
            embed.WithColor(222, 39, 0);
            embed.WithTitle($"{newC.name} leveled up!").WithCurrentTimestamp();
            embed.AddField("Name", newC.name, true);
            embed.AddField("Playtime", TimeSpan.FromSeconds(newC.age).ToString(@"d\d\ h\h\ m\m\ s\s"), true);
            embed.AddField("Gender", newC.gender, true);
            embed.AddField("Class", newC.profession, true);
            embed.AddField("Level", newC.level + (levelChanged ? $" (+{newC.level - oldC.level})" : ""), true);
            embed.AddField("Mastery-Level", newC.masteryLevel + (mChanged ? $" (+{newC.masteryLevel - oldC.masteryLevel})" : ""), true);
            embed.WithFooter(x => {
                                   x.Text = "GW2Tracker"; 
                                   x.IconUrl="https://1001019.v1.pressablecdn.com/wp-content/uploads/2012/08/GW2-Logo.jpg";
                            });

            embed.WithImageUrl(LevelGraph.DrawPlot());

            return embed.Build();
        }

        public static Embed CreateLevelEmbed(Character newC)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(222, 39, 0);
            embed.WithTitle($"{newC.name}").WithCurrentTimestamp();
            embed.AddField("Name", newC.name, true);
            embed.AddField("Playtime", TimeSpan.FromSeconds(newC.age).ToString(@"d\d\ h\h\ m\m\ s\s"), true);
            embed.AddField("Gender", newC.gender, true);
            embed.AddField("Class", newC.profession, true);
            embed.AddField("Level", newC.level, true);
            embed.AddField("Mastery-Level", newC.masteryLevel, true);
            embed.WithFooter(x => {
                                   x.Text = "GW2Tracker"; 
                                   x.IconUrl="https://1001019.v1.pressablecdn.com/wp-content/uploads/2012/08/GW2-Logo.jpg";
                            });

            return embed.Build();
        }

        private Embed CreateTPInboxEmbed(TPInbox inboxBefore, TPInbox inboxAfter)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(222, 39, 0);
            embed.WithTitle(CharacterName + "s Tradepost Delivery").WithCurrentTimestamp();
            var currency = ToIngameCurrency(inboxAfter.coins);
            bool loss = inboxBefore.coins > inboxAfter.coins;
            var difference = ToIngameCurrency(loss ? inboxBefore.coins - inboxAfter.coins : inboxAfter.coins - inboxBefore.coins);
            embed.AddField("Money", $"{currency.gold}{Gold} {currency.silver}{Silver} {currency.copper}{Copper}\n{(loss ? $"-" : "+")} {difference.gold}{Gold} {difference.silver}{Silver} {difference.copper}{Copper}", true);
            var items = String.Join("\n", inboxAfter.items.Take(Math.Min(10, inboxAfter.items.Count)).Select(x => "**" + GetItemInfo(x.id).Result.name + $"** ({x.count}x)"));
            embed.AddField($"{inboxAfter.items.Count} Items ({(inboxAfter.items.Count > inboxBefore.items.Count ? "+" : "")}{inboxAfter.items.Count - inboxBefore.items.Count})", items.Length > 0 ? items : "No items in delivery", false);
            embed.WithFooter(x => {
                                   x.Text = "GW2Tracker"; 
                                   x.IconUrl="https://1001019.v1.pressablecdn.com/wp-content/uploads/2012/08/GW2-Logo.jpg";
                            });

            return embed.Build();
        }

        private async Task<Embed> CreateTPBuyEmbed(TPTransaction buy)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(222, 39, 0);
            embed.WithTitle(CharacterName + $" bought an item ({buy.quantity}x)").WithTimestamp(buy.purchased);

            var currency = ToIngameCurrency(buy.price);
            embed.AddField("Cost", $"{currency.gold}{Gold} {currency.silver}{Silver} {currency.copper}{Copper}", true);
            embed.AddField("Tradepost duration until bought", (buy.purchased - buy.created).ToString(@"d\d\ h\h\ m\m\ s\s"), true);
            var item = await GetItemInfo(buy.item_id);
            embed.AddField($"Item ({item.type})", ItemToText(item), true);
            embed.WithThumbnailUrl(item.icon);
            embed.WithFooter(x => {
                                   x.Text = "GW2Tracker"; 
                                   x.IconUrl="https://1001019.v1.pressablecdn.com/wp-content/uploads/2012/08/GW2-Logo.jpg";
                            });

            return embed.Build();
        }

        private async Task<Embed> CreateTPSellEmbed(TPTransaction sell)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(222, 39, 0);
            embed.WithTitle(CharacterName + $" sold an item ({sell.quantity}x)").WithTimestamp(sell.purchased);

            var currency = ToIngameCurrency(sell.price);
            embed.AddField("Cost", $"{currency.gold}{Gold} {currency.silver}{Silver} {currency.copper}{Copper}", true);
            embed.AddField("Tradepost duration until sold", (sell.purchased - sell.created).ToString(@"d\d\ h\h\ m\m\ s\s"), true);
            var item = await GetItemInfo(sell.item_id);
            embed.AddField($"Item ({item.type})", ItemToText(item), false);
            embed.WithThumbnailUrl(item.icon);
            embed.WithFooter(x => {
                                   x.Text = "GW2Tracker"; 
                                   x.IconUrl="https://1001019.v1.pressablecdn.com/wp-content/uploads/2012/08/GW2-Logo.jpg";
                            });

            return embed.Build();
        }

        private Embed createWealthEmbed(Wallet wealthBefore, Wallet wealthAfter)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(222, 39, 0);
            embed.WithTitle(CharacterName + "s wealth changed").WithCurrentTimestamp();

            bool loss = wealthAfter.value < wealthBefore.value;
            var currency = ToIngameCurrency(wealthBefore.value);
            var currencyAfter = ToIngameCurrency(wealthAfter.value);
            var difference = ToIngameCurrency(!loss ? wealthAfter.value - wealthBefore.value : wealthBefore.value - wealthAfter.value);
            embed.AddField("Money", $"{currencyAfter.gold}{Gold} {currencyAfter.silver}{Silver} {currencyAfter.copper}{Copper}\n{(loss ? $"-" : "+")} {difference.gold}{Gold} {difference.silver}{Silver} {difference.copper}{Copper}", true);
            embed.WithFooter(x => {
                                   x.Text = "GW2Tracker"; 
                                   x.IconUrl="https://1001019.v1.pressablecdn.com/wp-content/uploads/2012/08/GW2-Logo.jpg";
                            });

            embed.WithImageUrl(MoneyGraph.DrawPlot());

            return embed.Build();
        }

        private async Task<Embed> CreateAchievementEmbed(List<Achievement> newAchievements)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(222, 39, 0);
            embed.WithTitle("New achievements by " + CharacterName).WithCurrentTimestamp();
            foreach(var achievement in newAchievements){
                var achievementInfo = await GetAchievementInfo(achievement.id);
                embed.AddField(achievementInfo.name, achievementInfo.requirement.Length > 0 ? achievementInfo.requirement : "No description");
            }
            embed.WithFooter(x => {
                                   x.Text = "GW2Tracker"; 
                                   x.IconUrl="https://1001019.v1.pressablecdn.com/wp-content/uploads/2012/08/GW2-Logo.jpg";
                            });

            return embed.Build();
        }

        public static async Task<Character> GetCharacterEndpoint(string CharacterName, string APIKey){
            var character = await FetchJSONDataAsync<Character>($"https://api.guildwars2.com/v2/characters/{CharacterName}?access_token={APIKey}");
            character.masteryLevel = (await GetMasteries(CharacterName, APIKey)).Sum(x => {
                    int level = 0;
                    for(int i = 1; i <= x.level + 1; i++){
                        level += i;
                    }
                    return level;
                });

            return character;
        }

        public static async Task<List<Masteries>> GetMasteries(string CharacterName, string APIKey){
            return await FetchJSONDataAsync<List<Masteries>>($"https://api.guildwars2.com/v2/account/masteries?access_token={APIKey}");
        }

        public static async Task<List<Achievement>> GetCompletedAchievements(string APIKey){
            return await FetchJSONDataAsync<List<Achievement>>($"https://api.guildwars2.com/v2/account/achievements?access_token={APIKey}");
        }

        public static async Task<AchievementInfo> GetAchievementInfo(int id){
            return (await FetchJSONDataAsync<List<AchievementInfo>>($"https://api.guildwars2.com/v2/achievements?ids={id}")).First();
        }

        public static async Task<List<TPTransaction>> GetTPSells(string APIKey){
            return await FetchJSONDataAsync<List<TPTransaction>>($"https://api.guildwars2.com/v2/commerce/transactions/history/sells?access_token={APIKey}");
        }

        public static async Task<List<TPTransaction>> GetTPBuys(string APIKey){
            return await FetchJSONDataAsync<List<TPTransaction>>($"https://api.guildwars2.com/v2/commerce/transactions/history/buys?access_token={APIKey}");
        }

        public static async Task<TPInbox> GetTPInbox(string APIKey){
            return await FetchJSONDataAsync<TPInbox>($"https://api.guildwars2.com/v2/commerce/delivery?access_token={APIKey}");
        }

        public static async Task<List<Wallet>> GetWealth(string APIKey){
            return await FetchJSONDataAsync<List<Wallet>>($"https://api.guildwars2.com/v2/account/wallet?access_token={APIKey}");
        }

        public static async Task<ItemInfo> GetItemInfo(int id){
            return await FetchJSONDataAsync<ItemInfo>($"https://api.guildwars2.com/v2/items/{id}");
        }

        public static (long gold, long silver, long copper) ToIngameCurrency(long value){
            var gold = value/10000;
            var silver = (value % 10000)/100;
            var copper = value % 100;

            return (gold, silver, copper);
        }

        public static string ItemToText(ItemInfo item){
            string itemText = $"```css\n.{item.rarity} [{item.name}]\n";
            int barrier = $".{item.rarity} [{item.name}]".Length;
            itemText += new String('-', Math.Min(46, barrier)) + "\n";
            if(item.details?.infix_upgrade?.attributes != null && item.details.infix_upgrade.attributes.Count > 0){
                itemText += String.Join("\n", item.details.infix_upgrade.attributes.Select(x => x.attribute + ": +" + x.modifier));
            }

            return itemText + "```";
        }
    }

    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class GW2Information{
        public Character character;
        public Wallet wallet;
        public TPTransaction buy, sell;
        public TPInbox delivery;

    }
}
