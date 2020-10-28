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
        public int Money, Experience, CharactersSent, Punched, Hugged, Kissed;
        public int WeaponId;
        public bool IsBanned;
        public List<int> Inventory;
        public DateTime LastTaCReminder = DateTime.MinValue;
        public DatePlot ActivityGraph;

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
            return (int)Math.Sqrt(CharactersSent / 200.0);
        }

        public double CalcCurLevelDouble()
        {
            return Math.Sqrt(CharactersSent / 200.0);
        }

        public bool IsTaCDue(){
            return (DateTime.UtcNow - LastTaCReminder).TotalDays >= 7;
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

        public void AddGraphValue(int value){
            double dateValue = OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Today);
            
            if(ActivityGraph == null){
                ActivityGraph = new DatePlot(Id + "ExperienceGraph", "Date", "Characters sent", "dd-MMM", false);
                ActivityGraph.AddValue("Value", 0, DateTime.Today.AddDays(-1));
                ActivityGraph.AddValue("Value", 0, DateTime.Today.AddMilliseconds(-1));
                ActivityGraph.AddValue("Value", value, DateTime.Today);
            }

            else {
                if(ActivityGraph.PlotDataPoints.Last().Value.Key < dateValue){
                    //Only show past year
                    ActivityGraph.PlotDataPoints = ActivityGraph.PlotDataPoints.SkipWhile(x => (DateTime.Today - OxyPlot.Axes.DateTimeAxis.ToDateTime(x.Value.Key)).Days >= 365).ToList();

                    //Finalize block of the last date captured
                    var endOfLastDay = OxyPlot.Axes.DateTimeAxis.ToDouble(OxyPlot.Axes.DateTimeAxis.ToDateTime(ActivityGraph.PlotDataPoints.Last().Value.Key).AddDays(1).AddMilliseconds(-2));
                    ActivityGraph.PlotDataPoints.Add(new KeyValuePair<string, KeyValuePair<double, double>>("Value", new KeyValuePair<double, double>(endOfLastDay, ActivityGraph.PlotDataPoints.Last().Value.Value)));
                    ActivityGraph.PlotDataPoints.Add(new KeyValuePair<string, KeyValuePair<double, double>>("Value", new KeyValuePair<double, double>(endOfLastDay, 0)));
                    
                    //Start new block for today
                    var startOfToday = OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Today.AddMilliseconds(-1));
                    ActivityGraph.PlotDataPoints.Add(new KeyValuePair<string, KeyValuePair<double, double>>("Value", new KeyValuePair<double, double>(startOfToday, 0)));
                    ActivityGraph.PlotDataPoints.Add(new KeyValuePair<string, KeyValuePair<double, double>>("Value", new KeyValuePair<double, double>(dateValue, value)));
                } else {
                    ActivityGraph.PlotDataPoints[ActivityGraph.PlotDataPoints.Count - 1] = new KeyValuePair<string, KeyValuePair<double, double>>("Value", new KeyValuePair<double, double>(dateValue, ActivityGraph.PlotDataPoints.Last().Value.Value + value));
                }
            }
        }

        public void InitPlot(){
            if(ActivityGraph == null){
                ActivityGraph = new DatePlot(Id + "ExperienceGraph", "Date", "Characters sent", "dd-MMM", false);
            } else {
                ActivityGraph.InitPlot("Date", "Characters sent", "dd-MMM", false);
            }
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

            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < end - (begin - 1); i++){
                if(end-begin < 10) sb.Append($"#{begin+i}: {(await StaticBase.GetUserAsync(users[i].Id))?.Mention ?? $"<@{users[i].Id}>"}\n");
                stats.Add(KeyValuePair.Create(""+(begin+i), stat(users[i])));
            }

            var embed = new EmbedBuilder();
            return embed.WithCurrentTimestamp().WithImageUrl(ColumnPlot.DrawPlotSorted(guildId + "Leaderboard", stats))
                        .WithDescription(sb.ToString()).Build();
        }

        private async Task ModifyAsync(Action<User> modification)
        {
            modification(this);
            await StaticBase.Database.GetCollection<User>("Users").ReplaceOneAsync(x => x.Id == Id, this);
        }

        private string DrawProgressBar()
        {
            int Level = CalcCurLevel();
            double expCurrentHold = CharactersSent - CalcExperience(Level);
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

        public async Task<Embed> StatEmbed()
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithAuthor((await StaticBase.GetUserAsync(Id)).Username, (await StaticBase.GetUserAsync(Id)).GetAvatarUrl());
            e.WithCurrentTimestamp().WithColor(Discord.Color.Blue);

            e.AddField("Level", $"{CalcCurLevel()} ({CharactersSent}/{CalcExperience(CalcCurLevel() + 1)}xp)\n{DrawProgressBar()}", true);
            e.AddField("Interactions", $"**Kissed** {Kissed} times\n**Hugged** {Hugged} times\n**Punched** {Punched} times", true);
            e.AddField("Votepoints", Money, false);

            if(ActivityGraph != null){
                InitPlot();
                if(ActivityGraph.PlotDataPoints.Last().Value.Key < OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Today)){
                    ActivityGraph.AddValue("Value", ActivityGraph.PlotDataPoints.Last().Value.Value, OxyPlot.Axes.DateTimeAxis.ToDateTime(ActivityGraph.PlotDataPoints.Last().Value.Key).AddDays(1).AddMilliseconds(-1), false, false);
                    ActivityGraph.AddValue("Value", 0, OxyPlot.Axes.DateTimeAxis.ToDateTime(ActivityGraph.PlotDataPoints.Last().Value.Key).AddDays(1), false, false);
                    ActivityGraph.AddValue("Value", 0, DateTime.Now, false, false);
                }
                else
                    ActivityGraph.AddValue("Value", ActivityGraph.PlotDataPoints.Last().Value.Value, DateTime.Now, false, false);

                e.WithImageUrl(ActivityGraph.DrawPlot());
            }

            return e.Build();
        }

        public async Task<Discord.Rest.RestUser> GetRestUserAsync() => await Program.Client.Rest.GetUserAsync(Id);
        public async Task<Discord.Rest.RestGuildUser> GetRestGuildUserAsync(ulong guildId) => await Program.Client.Rest.GetGuildUserAsync(guildId, Id);

        public enum Stats {Punch, Hug, Kiss, Experience, Level, Votepoints}
    }
}
