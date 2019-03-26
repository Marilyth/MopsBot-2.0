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

namespace MopsBot.Data.Entities
{
    [BsonIgnoreExtraElements]
    public class User
    {

        [BsonId]
        public ulong Id;
        public int Money, Experience, Punched, Hugged, Kissed;
        public int WeaponId;
        public List<int> Inventory;

        private User(ulong pId)
        {
            Id = pId;
        }

        public int CalcExperience(int level)
        {
            return 200 * level * level;
        }

        public int CalcCurLevel()
        {
            return (int)Math.Sqrt(Experience / 200.0);
        }

        public double CalcCurLevelDouble()
        {
            return Math.Sqrt(Experience / 200.0);
        }

        public static async Task<User> GetUserAsync(ulong id)
        {
            User user = (await StaticBase.Database.GetCollection<User>("Users").FindAsync(x => x.Id == id)).FirstOrDefault();

            if (user == null)
            {
                user = new User(id);
                await StaticBase.Database.GetCollection<User>("Users").InsertOneAsync(user);
            }

            return user;
        }

        public static async Task ModifyUserAsync(ulong id, Action<User> modification)
        {
            await (await GetUserAsync(id)).ModifyAsync(modification);
        }

        public static async Task<long> GetDBUserCount(ulong? guildId = null){
            var usersInGuild = guildId != null ? Program.Client.GetGuild(guildId.Value).Users.Select(x => x.Id).ToHashSet() : null;

            var users = guildId == null ? await StaticBase.Database.GetCollection<User>("Users").CountDocumentsAsync(x => true)
                                        : await StaticBase.Database.GetCollection<User>("Users").CountDocumentsAsync(x => usersInGuild.Contains(x.Id));

            return users;
        }

        public static async Task<Embed> GetLeaderboardAsync(ulong? guildId = null, Func<User, double> stat = null, int begin = 1, int end = 10){
            var usersInGuild = guildId != null ? Program.Client.GetGuild(guildId.Value).Users.Select(x => x.Id).ToHashSet() : null;

            var users = guildId != null ? await (await StaticBase.Database.GetCollection<User>("Users").FindAsync(x => usersInGuild.Contains(x.Id))).ToListAsync()
                                        : await (await StaticBase.Database.GetCollection<User>("Users").FindAsync(x => true)).ToListAsync();

            if(stat == null)
                stat = x => x.CalcCurLevelDouble();

            users = users.OrderByDescending(x => stat(x)).Skip(begin - 1).Take(end - (begin - 1)).ToList();

            List<KeyValuePair<string, double>> stats = new List<KeyValuePair<string, double>>();

            for(int i = 0; i < end - (begin - 1); i++){
                stats.Add(KeyValuePair.Create(Program.Client.GetUser(users[i].Id)?.Username ?? "Unknown"+i, stat(users[i])));
            }

            var embed = new EmbedBuilder();
            return embed.WithCurrentTimestamp().WithImageUrl(ColumnPlot.DrawPlotSorted(guildId + "Leaderboard", stats)).Build();
        }

        private async Task ModifyAsync(Action<User> modification)
        {
            modification(this);
            await StaticBase.Database.GetCollection<User>("Users").ReplaceOneAsync(x => x.Id == Id, this);
        }

        private string DrawProgressBar()
        {
            int Level = CalcCurLevel();
            double expCurrentHold = Experience - CalcExperience(Level);
            string output = "", TempOutput = "";
            double diffExperience = CalcExperience(Level + 1) - CalcExperience(Level);
            for (int i = 0; i < Math.Floor(expCurrentHold / (diffExperience / 10)); i++)
            {
                output += "■";
            }
            for (int i = 0; i < 10 - output.Length; i++)
            {
                TempOutput += "□";
            }
            return output + TempOutput;
        }

        public Embed StatEmbed()
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithAuthor(Program.Client.GetUser(Id).Username, Program.Client.GetUser(Id).GetAvatarUrl());
            e.WithCurrentTimestamp().WithColor(Discord.Color.Blue);

            e.AddField("Level", $"{CalcCurLevel()} ({Experience}/{CalcExperience(CalcCurLevel() + 1)}xp)\n{DrawProgressBar()}", true);
            e.AddField("Interactions", $"**Kissed** {Kissed} times\n**Hugged** {Hugged} times\n**Punched** {Punched} times", true);
            e.AddField("Votepoints", Money, false);

            return e.Build();
        }

        public enum Stats {Punch, Hug, Kiss, Experience, Level, Votepoints}
    }
}
