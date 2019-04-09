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
        private TwitchTracker tracker;

        public TwitchUser(ulong dId, string tId)
        {
            DiscordId = dId;
            TwitchName = tId;
            Hosts = new List<Tuple<DateTime, string, int>>();
            Guilds = new HashSet<ulong>();
            PostInitialisation();
        }

        public async Task PostInitialisation()
        {
            tracker = StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackers().FirstOrDefault(x => x.Key.ToLower().Equals(TwitchName.ToLower())).Value as TwitchTracker;

            if(tracker == null){
                await CreateSilentTrackerAsync(TwitchName, StaticBase.TwitchGuilds[Guilds.First()].notifyChannel);
                tracker = StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTracker(StaticBase.TwitchGuilds[Guilds.First()].notifyChannel, TwitchName) as TwitchTracker;
            } else if(tracker.IsOnline) {
                await WentLive();
            }

            tracker.OnHosting += Hosting;
            tracker.OnLive += WentLive;
            tracker.OnOffline += WentOffline;
        }

        public static async Task CreateSilentTrackerAsync(string name, ulong channelId){
            await StaticBase.Trackers[BaseTracker.TrackerType.Twitch].AddTrackerAsync(name.ToLower(), channelId);
            var tracker = StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTracker(channelId, name.ToLower()) as TwitchTracker;
            await tracker.ModifyAsync(x => x.Specifications[channelId] = new TwitchTracker.NotifyConfig(){
                LargeThumbnail = false,
                NotifyOnGameChange = false,
                NotifyOnHost = false,
                NotifyOnOffline = false,
                NotifyOnOnline = false,
                ShowEmbed = false
            });
        }

        private async Task SetRole()
        {
            foreach (var guild in Guilds)
            {
                var curGuild = Program.Client.GetGuild(guild);
                var rankRoleId = StaticBase.TwitchGuilds[guild].RankRoles.LastOrDefault(x => x.Item1 <= Points)?.Item2 ?? 0;
                if (rankRoleId == 0) return;

                var curRole = curGuild.GetRole(rankRoleId);
                if (curGuild.GetUser(DiscordId).Roles.Any(x => x.Id == curRole.Id)) return;

                var rolesToRemove = curGuild.Roles.Where(x => StaticBase.TwitchGuilds[guild].RankRoles.Any(y => y.Item2 == x.Id) && x.Id != rankRoleId);

                await curGuild.GetUser(DiscordId).AddRoleAsync(curRole);
                await curGuild.GetUser(DiscordId).RemoveRolesAsync(rolesToRemove);

                await curGuild.GetTextChannel(StaticBase.TwitchGuilds[guild].notifyChannel).SendMessageAsync($"{Program.Client.GetUser(DiscordId).Mention} is now rank {curRole.Name}");
            }
        }

        public async Task UpdateUserAsync()
        {
            TwitchUser user = (await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").FindAsync(x => x.DiscordId == DiscordId)).FirstOrDefault();

            if (user == null)
            {
                await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").InsertOneAsync(this);
            }
            else
            {
                await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").ReplaceOneAsync(x => x.DiscordId == DiscordId, this);
            }
        }

        public static async Task<Embed> GetLeaderboardAsync(ulong? guildId = null, Func<TwitchUser, double> stat = null, int begin = 1, int end = 10)
        {
            var users = StaticBase.TwitchUsers.Values.Where(x => guildId.HasValue ? x.Guilds.Contains(guildId.Value) : true).ToList();

            if (stat == null)
                stat = x => x.Points;

            users = users.OrderByDescending(x => stat(x)).Skip(begin - 1).Take(end - (begin - 1)).ToList();

            List<KeyValuePair<string, double>> stats = new List<KeyValuePair<string, double>>();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < end - (begin - 1); i++)
            {
                if (end - begin < 10) sb.Append($"#{begin + i}: {Program.Client.GetUser(users[i].DiscordId)?.Mention ?? $"<@{users[i].DiscordId}>"}\n");
                stats.Add(KeyValuePair.Create("" + (begin + i), stat(users[i])));
            }

            var embed = new EmbedBuilder();
            return embed.WithCurrentTimestamp().WithImageUrl(ColumnPlot.DrawPlotSorted(guildId + "Leaderboard", stats))
                        .WithDescription(sb.ToString()).Build();
        }

        public async Task WentLive(BaseTracker sender = null)
        {
            LiveCount++;
            await ModifyAsync(x => x.Points += 20);

            //Set live role on each server
            foreach (var guild in Guilds)
            {
                try
                {
                    var curGuild = Program.Client.GetGuild(guild) as IGuild;
                    var tGuild = StaticBase.TwitchGuilds[guild];
                    await (await curGuild.GetUserAsync(DiscordId)).AddRoleAsync(curGuild.GetRole(tGuild.LiveRole));
                }
                catch { }
            }
        }

        public async Task WentOffline(BaseTracker sender)
        {
            if(!(sender as TwitchTracker).IsHosting){
                await ModifyAsync(x => x.Points -= 40);
            }

            //Remove live role on each server
            foreach (var guild in Guilds)
            {
                try
                {
                    var curGuild = Program.Client.GetGuild(guild) as IGuild;
                    var tGuild = StaticBase.TwitchGuilds[guild];
                    await (await curGuild.GetUserAsync(DiscordId)).RemoveRoleAsync(curGuild.GetRole(tGuild.LiveRole));
                }
                catch { }
            }
        }

        public async Task ModifyAsync(Action<TwitchUser> modification)
        {
            modification(this);
            if (Points < 0) Points = 0;
            await UpdateUserAsync();
            await SetRole();
        }

        public async Task DeleteAsync()
        {
            Guilds = null;
            Hosts = null;
            //(StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackers().FirstOrDefault(x => x.Key.ToLower().Equals(TwitchName.ToLower())).Value as TwitchTracker).DiscordId = 0;
            await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").DeleteOneAsync(x => x.DiscordId == DiscordId);
        }

        public async Task Hosting(string hosterName, string targetName, int viewers)
        {
            await ModifyAsync(x => x.Hosts.Add(new Tuple<DateTime, string, int>(DateTime.UtcNow, targetName, viewers)));
            var hosterDiscordName = Program.Client.GetUser(DiscordId).Username;
            
            foreach (var guild in Guilds)
            {
                var curGuild = StaticBase.TwitchGuilds[guild];
                curGuild.ExistsUser(targetName, out TwitchUser user);
                var targetDiscordId = user?.DiscordId;
                
                //ToDo: Only do once
                int reward = -40;

                if (targetDiscordId != null)
                    reward = 20;

                await ModifyAsync(x => x.Points += reward);

                var embed = new EmbedBuilder().WithCurrentTimestamp().WithFooter(x =>
                        {
                            x.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
                            x.Text = "Twitch Hosting";
                        });
                embed.WithDescription($"[{hosterDiscordName}](https://www.twitch.tv/{hosterName}) is now hosting "+
                                      $"[{(targetDiscordId != null ? Program.Client.GetUser(targetDiscordId.Value).Username : targetName)}](https://www.twitch.tv/{targetName})");
                embed.AddField("Viewers", viewers, true);
                embed.AddField("Points", reward, true);

                var builtEmbed = embed.Build();
                await (Program.Client.GetChannel(curGuild.notifyChannel) as Discord.IMessageChannel).SendMessageAsync(embed: builtEmbed);
            }
        }

        public Embed StatEmbed(ulong guildId)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithAuthor(Program.Client.GetUser(DiscordId).Username, Program.Client.GetUser(DiscordId).GetAvatarUrl());
            e.WithCurrentTimestamp().WithColor(Discord.Color.Blue);

            var rankRoleId = StaticBase.TwitchGuilds[guildId].RankRoles.LastOrDefault(x => x.Item1 <= Points)?.Item2 ?? 0;

            e.AddField("Rank", $"{(rankRoleId != 0 ? Program.Client.GetGuild(guildId).GetRole(rankRoleId).Name : "Nothing")}", false);
            e.AddField("Information", $"Hosted {Hosts.Count} times\nStreamed {LiveCount} times", true);
            e.AddField("Points", Points, true);

            var hosts = string.Join("\n", Hosts.TakeLast(Math.Min(Hosts.Count, 10)).Select(x => $"`{x.Item1}`: hosted [{x.Item2}](https://www.twitch.tv/{x.Item2}) for {x.Item3} viewers."));
            e.AddField("Recent hosts", string.IsNullOrEmpty(hosts) ? "None" : hosts);

            return e.Build();
        }
    }
}
