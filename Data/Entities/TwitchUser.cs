using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using Discord;
using DiscordBotsList.Api.Objects;
using MopsBot.Data.Tracker;

namespace MopsBot.Data.Entities
{
    [BsonIgnoreExtraElements]
    public class TwitchUser
    {
        [BsonId]
        public ulong DiscordId;
        public string TwitchName;
        public int LiveCount, Points;
        public List<Tuple<DateTime, string, int>> Hosts;
        public HashSet<ulong> Guilds;

        public TwitchUser(ulong dId, string tId)
        {
            DiscordId = dId;
            TwitchName = tId;
            Hosts = new List<Tuple<DateTime, string, int>>();
            Guilds = new HashSet<ulong>();
        }

        public int CalcExperience(int level)
        {
            return 200 * level * level;
        }

        public int CalcCurRank()
        {
            return (int)Math.Sqrt(Points / 200.0);
        }

        public double CalcCurRankDouble()
        {
            return Math.Sqrt(Points / 200.0);
        }

        private int calculatePoints(TwitchUser other, int viewers){
            return viewers/(other.CalcCurRank() + 1);
        }

        public async Task UpdateUserAsync()
        {
            TwitchUser user = (await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").FindAsync(x => x.DiscordId == DiscordId)).FirstOrDefault();

            if (user == null)
            {
                await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").InsertOneAsync(this);
            } else {
                await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").ReplaceOneAsync(x => x.DiscordId == DiscordId, this);
            }
        }

        public static async Task<Embed> GetLeaderboardAsync(ulong? guildId = null, Func<TwitchUser, double> stat = null, int begin = 1, int end = 10){
            var usersInGuild = guildId != null ? Program.Client.GetGuild(guildId.Value).Users.Select(x => x.Id).ToHashSet() : null;

            var users = StaticBase.TwitchUsers.Values.Where(x => usersInGuild?.Contains(x.DiscordId) ?? true).ToList();

            if(stat == null)
                stat = x => x.CalcCurRankDouble();

            users = users.OrderByDescending(x => stat(x)).Skip(begin - 1).Take(end - (begin - 1)).ToList();

            List<KeyValuePair<string, double>> stats = new List<KeyValuePair<string, double>>();

            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < end - (begin - 1); i++){
                if(end-begin < 10) sb.Append($"#{begin+i}: {Program.Client.GetUser(users[i].DiscordId)?.Mention ?? $"<@{users[i].DiscordId}>"}\n");
                stats.Add(KeyValuePair.Create(""+(begin+i), stat(users[i])));
            }

            var embed = new EmbedBuilder();
            return embed.WithCurrentTimestamp().WithImageUrl(ColumnPlot.DrawPlotSorted(guildId + "Leaderboard", stats))
                        .WithDescription(sb.ToString()).Build();
        }

        public async Task WentLive(){
            LiveCount++;
            await UpdateUserAsync();

            //Set live role on each server
            foreach(var guild in Guilds){
                var curGuild = Program.Client.GetGuild(guild) as IGuild;
                var tGuild = StaticBase.TwitchGuilds[guild];
                await (await curGuild.GetUserAsync(DiscordId)).AddRoleAsync(curGuild.GetRole(tGuild.LiveRole));
            }
        }

        public async Task WentOffline(){
            //Remove live role on each server
            foreach(var guild in Guilds){
                var curGuild = Program.Client.GetGuild(guild) as IGuild;
                var tGuild = StaticBase.TwitchGuilds[guild];
                await (await curGuild.GetUserAsync(DiscordId)).RemoveRoleAsync(curGuild.GetRole(tGuild.LiveRole));
            }
        }

        public async Task ModifyAsync(Action<TwitchUser> modification)
        {
            modification(this);
            await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").ReplaceOneAsync(x => x.DiscordId == DiscordId, this);
        }

        public async Task DeleteAsync(){
            Guilds = null;
            Hosts = null;
            //(StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackers().FirstOrDefault(x => x.Key.ToLower().Equals(TwitchName.ToLower())).Value as TwitchTracker).DiscordId = 0;
            await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").DeleteOneAsync(x => x.DiscordId == DiscordId);
        }

        public async Task Hosting(string hosterName, string targetName, int viewers){
            await WentOffline();
            await ModifyAsync(x => x.Hosts.Add(new Tuple<DateTime, string, int>(DateTime.UtcNow, targetName, viewers)));

            var embed = new EmbedBuilder().WithCurrentTimestamp().WithFooter(x => {x.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
                                                                                                           x.Text = "Twitch Hosting";});
            var hosterDiscordName = Program.Client.GetUser(DiscordId).Username;
            string targetDiscordName = null;
            var reward = 0;
            var targetTracker = StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackers().FirstOrDefault(x => x.Key.ToLower().Equals(targetName.ToLower())).Value as TwitchTracker;

            if(targetTracker != null && targetTracker.DiscordId != 0){
                targetDiscordName = Program.Client.GetUser(targetTracker.DiscordId).Username;
                reward = calculatePoints(StaticBase.TwitchUsers[targetTracker.DiscordId], viewers);
            }

            embed.WithDescription($"[{hosterDiscordName}](https://www.twitch.tv/{hosterName}) is now hosting [{targetDiscordName ?? targetName}](https://www.twitch.tv/{targetName})");
            embed.AddField("Viewers", viewers, true);
            embed.AddField("Points", reward, true);
            
            var builtEmbed = embed.Build();
            foreach(var guild in Guilds){
                var curGuild = StaticBase.TwitchGuilds[guild];
                await (Program.Client.GetChannel(curGuild.notifyChannel) as Discord.IMessageChannel).SendMessageAsync(embed:builtEmbed);
            }
        }

        public Embed StatEmbed()
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithAuthor(Program.Client.GetUser(DiscordId).Username, Program.Client.GetUser(DiscordId).GetAvatarUrl());
            e.WithCurrentTimestamp().WithColor(Discord.Color.Blue);

            e.AddField("Rank", $"{CalcCurRank()}", false);
            e.AddField("Information", $"Hosted {Hosts.Count} times\nStreamed {LiveCount} times", true);
            e.AddField("Host-Points", Points, true);
            e.AddField("Recent hosts", string.Join("\n", Hosts.TakeLast(Math.Min(Hosts.Count, 10)).Select(x => $"`{x.Item1}`: hosted [{x.Item2}](https://www.twitch.tv/{x.Item2}) for {x.Item3} viewers.")));

            return e.Build();
        }
    }
}
