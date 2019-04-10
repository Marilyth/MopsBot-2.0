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
        public ulong GuildPlusDiscordId;
        public ulong DiscordId;
        public string TwitchName;
        public int LiveCount, Points;
        public List<Tuple<DateTime, string, int>> Hosts;
        public ulong GuildId;
        private TwitchTracker tracker;

        public TwitchUser(ulong dId, string tId, ulong guildId)
        {
            GuildPlusDiscordId = dId + guildId;
            DiscordId = dId;
            TwitchName = tId;
            Hosts = new List<Tuple<DateTime, string, int>>();
            GuildId = guildId;
            PostInitialisation();
        }

        public async Task PostInitialisation()
        {
            StaticBase.TwitchGuilds[GuildId].AddUser(this);
            await CreateSilentTrackerAsync(TwitchName, StaticBase.TwitchGuilds[GuildId].notifyChannel);
            tracker = StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTracker(StaticBase.TwitchGuilds[GuildId].notifyChannel, TwitchName) as TwitchTracker;

            if (tracker.IsOnline)
            {
                await WentLive();
            }

            tracker.OnHosting += Hosting;
            tracker.OnLive += WentLive;
            tracker.OnOffline += WentOffline;
        }

        public static async Task CreateSilentTrackerAsync(string name, ulong channelId)
        {
            await StaticBase.Trackers[BaseTracker.TrackerType.Twitch].AddTrackerAsync(name.ToLower(), channelId);
            var tracker = StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTracker(channelId, name.ToLower()) as TwitchTracker;
            await tracker.ModifyAsync(x => x.Specifications[channelId] = new TwitchTracker.NotifyConfig()
            {
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
            var curGuild = Program.Client.GetGuild(GuildId);
            var rankRoleId = StaticBase.TwitchGuilds[GuildId].RankRoles.LastOrDefault(x => x.Item1 <= Points)?.Item2 ?? 0;
            if (rankRoleId == 0) return;

            var curRole = curGuild.GetRole(rankRoleId);
            if (curGuild.GetUser(DiscordId).Roles.Any(x => x.Id == curRole.Id)) return;

            var rolesToRemove = curGuild.Roles.Where(x => StaticBase.TwitchGuilds[GuildId].RankRoles.Any(y => y.Item2 == x.Id) && x.Id != rankRoleId);

            await curGuild.GetUser(DiscordId).AddRoleAsync(curRole);
            await curGuild.GetUser(DiscordId).RemoveRolesAsync(rolesToRemove);

            await curGuild.GetTextChannel(StaticBase.TwitchGuilds[GuildId].notifyChannel).SendMessageAsync($"{Program.Client.GetUser(DiscordId).Mention} is now rank {curRole.Name}");

        }

        public async Task UpdateUserAsync()
        {
            TwitchUser user = (await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").FindAsync(x => x.GuildPlusDiscordId == GuildPlusDiscordId)).FirstOrDefault();

            if (user == null)
            {
                await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").InsertOneAsync(this);
            }
            else
            {
                await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").ReplaceOneAsync(x => x.GuildPlusDiscordId == GuildPlusDiscordId, this);
            }
        }

        public async Task WentLive(BaseTracker sender = null)
        {
            if (sender != null)
            {
                LiveCount++;
                await ModifyAsync(x => x.Points += 20);
            }

            try
            {
                var curGuild = Program.Client.GetGuild(GuildId) as IGuild;
                var tGuild = StaticBase.TwitchGuilds[GuildId];
                await (await curGuild.GetUserAsync(DiscordId)).AddRoleAsync(curGuild.GetRole(tGuild.LiveRole));
            }
            catch { }
        }

        public async Task WentOffline(BaseTracker sender)
        {
            if (!(sender as TwitchTracker).IsHosting)
            {
                await ModifyAsync(x => x.Points -= 40);
            }

            try
            {
                var curGuild = Program.Client.GetGuild(GuildId) as IGuild;
                var tGuild = StaticBase.TwitchGuilds[GuildId];
                await (await curGuild.GetUserAsync(DiscordId)).RemoveRoleAsync(curGuild.GetRole(tGuild.LiveRole));
            }
            catch { }

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
            Hosts = null;

            try
            {
                var silentChannel = tracker.Specifications.FirstOrDefault(x =>
                                                  x.Value.NotifyOnGameChange == false &&
                                                  x.Value.NotifyOnHost == false &&
                                                  x.Value.NotifyOnOffline == false &&
                                                  x.Value.NotifyOnOnline == false &&
                                                  x.Value.ShowEmbed == false).Key;

                await StaticBase.Trackers[BaseTracker.TrackerType.Twitch].TryRemoveTrackerAsync(TwitchName.ToLower(), silentChannel);
            }
            catch { }

            StaticBase.TwitchGuilds[GuildId].RemoveUser(this);
            StaticBase.TwitchUsers.Remove(GuildPlusDiscordId);
            await StaticBase.Database.GetCollection<TwitchUser>("TwitchUsers").DeleteOneAsync(x => x.DiscordId == DiscordId);
        }

        public async Task Hosting(string hosterName, string targetName, int viewers)
        {
            await ModifyAsync(x => x.Hosts.Add(new Tuple<DateTime, string, int>(DateTime.UtcNow, targetName, viewers)));
            var hosterDiscordName = Program.Client.GetUser(DiscordId).Username;

            var curGuild = StaticBase.TwitchGuilds[GuildId];
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
            embed.WithDescription($"[{hosterDiscordName}](https://www.twitch.tv/{hosterName}) is now hosting " +
                                  $"[{(targetDiscordId != null ? Program.Client.GetUser(targetDiscordId.Value).Username : targetName)}](https://www.twitch.tv/{targetName})");
            embed.AddField("Viewers", viewers, true);
            embed.AddField("Points", reward, true);

            var builtEmbed = embed.Build();
            await (Program.Client.GetChannel(curGuild.notifyChannel) as Discord.IMessageChannel).SendMessageAsync(embed: builtEmbed);
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
