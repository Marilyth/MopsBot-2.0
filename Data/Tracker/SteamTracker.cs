using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults.Steam;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;

namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public class SteamTracker : BaseTracker
    {
        public long SteamId, LastCheck;
        public string CurrentGame;

        public SteamTracker() : base()
        {

        }

        public SteamTracker(string name)
        {
            try
            {
                Name = name;
                SteamId = GetUserSIDAsync(name).Result;
                if(IsProfilePrivate().Result) throw new Exception();
                LastCheck = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                SetTimer(600000);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not resolve {name}.\nMake sure your **Game Details** are public, then retry in about 10 minutes");
            }
        }

        protected async override void CheckForChange_Elapsed(object sender)
        {
            try
            {
                var summary = await GetUserSummaryAsync();
                if (summary.gameid != CurrentGame)
                {
                    await CheckForNewAchievements(null, CurrentGame, false);
                    CurrentGame = summary.gameid;
                    await StaticBase.Trackers[TrackerType.Steam].UpdateDBAsync(this);
                    if (CurrentGame != null)
                    {
                        foreach (var channel in ChannelMessages)
                        {
                            await OnMinorChangeTracked(channel.Key, summary.personaname + $" is now playing **{summary.gameextrainfo}**");
                        }
                    }
                }

                await CheckForNewAchievements(summary);
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        public async Task CheckForNewAchievements(PlayerSummary summary, string gameId = null, bool setLastCheck = true)
        {
            if (gameId == null && CurrentGame != null)
            {
                gameId = CurrentGame;
            }
            else if (gameId != null)
            {
                summary = await GetUserSummaryAsync();
                var name = (await FetchJSONDataAsync<GameStats>($"http://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?appid={gameId}&key={Program.Config["Steam"]}")).game.gameName;
                summary.gameextrainfo = name;
                summary.gameid = gameId;
            }

            if (gameId != null)
            {
                var achievements = await GetCompleteAchievements(gameId);
                var newAchievements = achievements.TakeWhile(x => x.unlocktime > LastCheck);
                foreach (var achievement in newAchievements)
                {
                    foreach (var channel in ChannelMessages)
                    {
                        await OnMajorChangeTracked(channel.Key, CreateAchievementEmbed(achievement, summary), channel.Value);
                    }
                }
                if (setLastCheck) LastCheck = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await StaticBase.Trackers[TrackerType.Steam].UpdateDBAsync(this);
            }
        }

        public Embed CreateAchievementEmbed(Achievement achievement, PlayerSummary summary)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(20, 48, 93);
            e.Title = $"New achievement in {summary.gameextrainfo}";
            e.Url = $"https://store.steampowered.com/app/{summary.gameid}";
            e.Timestamp = DateTimeOffset.FromUnixTimeSeconds(achievement.unlocktime);

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            author.Url = $"{summary.profileurl}";
            author.IconUrl = $"{summary.avatar}";
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/83/Steam_icon_logo.svg/2000px-Steam_icon_logo.svg.png";
            footer.Text = "Steam";
            e.Footer = footer;

            e.AddField($"{achievement.displayName}", achievement.description, true);
            e.AddField("Rarity", Math.Round(achievement.percent, 2) + "%", true);
            e.WithThumbnailUrl(achievement.icon);
            e.WithImageUrl($"https://steamcdn-a.akamaihd.net/steam/apps/{summary.gameid}/header.jpg");

            return e.Build();
        }

        public async Task<List<Achievement>> GetCompleteAchievements(string gameId)
        {
            var gAchievements = await GetGameAchievements(gameId);
            var percAchievements = await GetGameAchievementPercentage(gameId);
            var userAchievements = await GetUserAchievementsAsync(gameId);
            var completeAchievements = new List<Achievement>();

            foreach (var curAchievement in userAchievements)
            {
                var name = curAchievement.apiname;
                var percent = percAchievements.FirstOrDefault(x => x.name.Equals(name))?.percent ?? 0;
                var gAchievement = gAchievements.FirstOrDefault(x => x.name.Equals(name));

                completeAchievements.Add(new Achievement()
                {
                    achieved = curAchievement.achieved,
                    apiname = curAchievement.apiname,
                    unlocktime = curAchievement.unlocktime,
                    percent = percent,
                    description = gAchievement.description,
                    defaultvalue = gAchievement.defaultvalue,
                    displayName = gAchievement.displayName,
                    icon = gAchievement.icon,
                    icongray = gAchievement.icongray,
                    name = gAchievement.name,
                    hidden = gAchievement.hidden
                });
            }

            return completeAchievements.OrderByDescending(x => x.unlocktime).ToList();
        }

        public async Task<List<GameAchievement>> GetGameAchievements(string gameId)
        {
            return (await FetchJSONDataAsync<GameStats>($"http://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?appid={gameId}&key={Program.Config["Steam"]}")).game.availableGameStats.achievements;
        }

        public async Task<bool> IsProfilePrivate(){
            var response = await MopsBot.Module.Information.GetURLAsync($"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/?steamid={SteamId}&appid=736260&key={Program.Config["Steam"]}");
            return response.Contains("Profile is not public");
        }

        public async Task<List<PercentageAchievement>> GetGameAchievementPercentage(string gameId)
        {
            return (await FetchJSONDataAsync<AchievementPercentage>($"http://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v2/?gameid={gameId}")).achievementpercentages.achievements;
        }

        public async Task<List<StatsAchievement>> GetUserAchievementsAsync(string gameId)
        {
            return (await FetchJSONDataAsync<PlayerStats>($"http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/?key={Program.Config["Steam"]}&steamid={SteamId}&appid={gameId}")).playerstats.achievements.OrderByDescending(x => x.unlocktime).ToList();
        }

        public async Task<PlayerSummary> GetUserSummaryAsync()
        {
            return (await FetchJSONDataAsync<UserSummary>($"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={Program.Config["Steam"]}&language=en-us&format=json&steamids={SteamId}")).response.players.FirstOrDefault();
        }

        public async Task<List<RecentlyPlayedGame>> GetUserRecentGamesAsync()
        {
            return (await FetchJSONDataAsync<RecentlyPlayed>($"https://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v1/?steamid={SteamId}&count=10&key={Program.Config["Steam"]}")).response.games;
        }

        public async static Task<long> GetUserSIDAsync(string username)
        {
            return long.Parse((await FetchJSONDataAsync<Vanity>($"https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/?vanityurl={username}&key={Program.Config["Steam"]}")).response.steamid);
        }
    }
}
