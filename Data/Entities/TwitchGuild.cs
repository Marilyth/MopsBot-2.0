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
using MopsBot.Data.Tracker;

namespace MopsBot.Data.Entities
{
    [BsonIgnoreExtraElements]
    public class TwitchGuild
    {
        [BsonId]
        public ulong DiscordId;
        //public List<TwitchUser> TwitchUsers;
        public ulong LiveRole;
        public ulong notifyChannel;
        public List<Tuple<int, ulong>> RankRoles;
        private List<TwitchUser> users;

        public TwitchGuild(){
            users = new List<TwitchUser>();
        }

        public TwitchGuild(ulong dId)
        {
            DiscordId = dId;
            users = new List<TwitchUser>();
            RankRoles = new List<Tuple<int, ulong>>();
        }

        public void AddUser(TwitchUser user){
            users.Add(user);
        }

        public void RemoveUser(TwitchUser user){
            users.Remove(user);
        }

        public List<TwitchUser> GetUsers() => users;
        public List<TwitchUser> GetUsers(ulong rankId){
            if(users.Count > 0){
                var rankUsers = users.Where(x => RankRoles.LastOrDefault(y => y.Item1 <= x.Points)?.Item2 == rankId).ToList();
                return rankUsers;
            } else
                return new List<TwitchUser>();
        }

        public bool ExistsUser(string twitchName, out TwitchUser user){
            user = users.FirstOrDefault(x => x.TwitchName.ToLower().Equals(twitchName.ToLower()));
            return user != null;
        }

        public async Task UpdateGuildAsync()
        {
            RankRoles = RankRoles.OrderBy(x => x.Item1).ToList();
            TwitchGuild user = (await StaticBase.Database.GetCollection<TwitchGuild>("TwitchGuilds").FindAsync(x => x.DiscordId == DiscordId)).FirstOrDefault();

            if (user == null)
            {
                await StaticBase.Database.GetCollection<TwitchGuild>("TwitchGuilds").InsertOneAsync(this);
            } else {
                await StaticBase.Database.GetCollection<TwitchGuild>("TwitchGuilds").ReplaceOneAsync(x => x.DiscordId == DiscordId, this);
            }
        }

        public Embed GetRankRoles(){
            var embed = new EmbedBuilder();
            embed.WithCurrentTimestamp().WithDescription(string.Join("\n", RankRoles.Select(x => $"{getRoleName(x.Item2)} starting at {x.Item1} points")));

            return embed.Build();
        }

        private string getRoleName(ulong id){
            return Program.Client.GetGuild(DiscordId).GetRole(id).Name;
        }

        public async Task<Embed> GetLeaderboardAsync(Func<TwitchUser, double> stat = null, int begin = 1, int end = 10)
        {
            if (stat == null)
                stat = x => x.Points;

            users = users.OrderByDescending(x => stat(x)).Skip(begin - 1).Take(end - (begin - 1)).ToList();

            List<KeyValuePair<string, double>> stats = new List<KeyValuePair<string, double>>();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < end - (begin - 1); i++)
            {
                if (end - begin < 10) sb.Append($"#{begin + i}: {(await StaticBase.GetUserAsync(users[i].DiscordId))?.Mention ?? $"<@{users[i].DiscordId}>"}\n");
                stats.Add(KeyValuePair.Create("" + (begin + i), stat(users[i])));
            }

            var embed = new EmbedBuilder();
            return embed.WithCurrentTimestamp().WithImageUrl(ColumnPlot.DrawPlotSorted(DiscordId + "Leaderboard", stats))
                        .WithDescription(sb.ToString()).Build();
        }
    }
}
