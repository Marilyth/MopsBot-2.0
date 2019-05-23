using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MopsBot.Module.Preconditions;
using System.Text.RegularExpressions;
using static MopsBot.StaticBase;
using MopsBot.Data.Tracker;
using MopsBot.Data;
using MopsBot.Data.Entities;
using Discord.Addons.Interactive;
using MopsBot.Module.Preconditions;

namespace MopsBot.Module
{
    public class Tracking : ModuleBase
    {
        [Group("Twitter")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitter : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackTwitter(string twitterUser, [Remainder]string tweetNotification = "~Tweet Tweet~")
            {
                twitterUser = twitterUser.ToLower().Replace("@", "");
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.Twitter].AddTrackerAsync(twitterUser, Context.Channel.Id, tweetNotification + "|" + tweetNotification);

                        await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, replies and retweets, from now on!\nTo disable replies and retweets, please use the `Twitter DisableNonMain` subcommand!");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter(BaseTracker twitterUser)
            {
                if (await Trackers[BaseTracker.TrackerType.Twitter].TryRemoveTrackerAsync(twitterUser.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + twitterUser.Name + "'s tweets!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following twitters are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Twitter].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new Main-Tweet is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker TwitterName, [Remainder]string notification = "")
            {
                var nonMainNotification = TwitterName.ChannelMessages[Context.Channel.Id].Split("|")[1];
                nonMainNotification = $"{notification}|{nonMainNotification}";
                TwitterName.ChannelMessages[Context.Channel.Id] = nonMainNotification;
                await ReplyAsync($"Set notification for main tweets, for `{TwitterName.Name}`, to {notification}!");
            }

            [Command("SetNonMainNotification")]
            [Summary("Sets the notification text that is used each time a new retweet or reply is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNonMainNotification(BaseTracker TwitterName, [Remainder]string notification = "")
            {
                var mainNotification = TwitterName.ChannelMessages[Context.Channel.Id].Split("|")[0];
                mainNotification += $"|{notification}";
                TwitterName.ChannelMessages[Context.Channel.Id] = mainNotification;
                await ReplyAsync($"Set notification for retweets and replies, for `{TwitterName.Name}`, to {notification}!");
            }

            [Command("DisableNonMain")]
            [Alias("DisableReplies", "DisableRetweets")]
            [Summary("Disables tracking for the retweets and replies of the specified Twitter account.")]
            public async Task DisableRetweets(BaseTracker TwitterName)
            {
                var notification = TwitterName.ChannelMessages[Context.Channel.Id].Split("|")[0];
                notification += "|NONE";
                TwitterName.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Disabled retweets and replies for `{TwitterName.Name}`!\nTo reenable retweets and replies, please provide a notification via the `Twitter SetNonMainNotification` subcommand!");
            }

            [Command("Prune")]
            [Hide]
            [RequireBotManage]
            public async Task PruneTrackers(int failThreshold, bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var allTrackers = StaticBase.Trackers[BaseTracker.TrackerType.Twitter].GetTrackers();
                    Dictionary<string, int> pruneCount = new Dictionary<string, int>();
                    int totalCount = 0;

                    foreach (var tracker in allTrackers.Where(x => (x.Value as TwitterTracker).FailCount >= failThreshold))
                    {
                        totalCount++;
                        pruneCount[tracker.Key] = (tracker.Value as TwitterTracker).FailCount;
                        if (!testing)
                        {
                            foreach (var channel in tracker.Value.ChannelMessages.Keys.ToList())
                                await StaticBase.Trackers[BaseTracker.TrackerType.Twitter].TryRemoveTrackerAsync(tracker.Key, channel);
                        }
                    }
                    var result = $"{"Twitter User",-20}{"Fail count"}\n{string.Join("\n", pruneCount.Select(x => $"{x.Key,-20}{x.Value,-3}"))}";
                    if (result.Length < 2040)
                        await ReplyAsync($"```{result}```");
                    else
                        await ReplyAsync($"```Pruned {totalCount} trackers```");
                }
            }
        }

        [Group("Osu")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Osu : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Osu player, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackOsu([Remainder]string OsuUser)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        OsuUser = OsuUser.ToLower();
                        await Trackers[BaseTracker.TrackerType.Osu].AddTrackerAsync(OsuUser, Context.Channel.Id);

                        await ReplyAsync("Keeping track of " + OsuUser + "'s plays above `0.1pp` gain, from now on!\nYou can change the lower pp boundary by using the `Osu SetPPBounds` subcommand!");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Osu player, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder]BaseTracker OsuUser)
            {
                if (await Trackers[BaseTracker.TrackerType.Osu].TryRemoveTrackerAsync(OsuUser.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + OsuUser.Name + "'s plays!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the Osu players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Osu players are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Osu].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetPPBounds")]
            [Summary("Sets the lower bounds of pp gain that must be reached, to show a notification.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetPPBounds(BaseTracker osuUser, double threshold)
            {
                var tracker = osuUser as OsuTracker;
                if (threshold > 0.1)
                {
                    tracker.PPThreshold = threshold;
                    await StaticBase.Trackers[BaseTracker.TrackerType.Osu].UpdateDBAsync(tracker);
                    await ReplyAsync($"Changed threshold for `{osuUser}` to `{threshold}`");
                }
                else
                    await ReplyAsync("Threshold must be above 0.1!");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a player gained pp.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker osuUser, [Remainder]string notification)
            {
                osuUser.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{osuUser.Name}` to `{notification}`");
            }
        }

        [Group("Youtube")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Youtube : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Youtuber, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackYoutube(string channelID, [Remainder]string notificationMessage = "New Video")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.Youtube].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                        await ReplyAsync("Keeping track of " + channelID + "'s videos, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtuber, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(BaseTracker channelID)
            {
                if (await Trackers[BaseTracker.TrackerType.Youtube].TryRemoveTrackerAsync(channelID.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + channelID.Name + "'s videos!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Youtube].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new video appears.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker channelID, [Remainder]string notification = "")
            {
                channelID.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{channelID.Name}` to `{notification}`");
            }
        }

        [Group("Twitch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitch : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage = "Stream went live!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        streamerName = streamerName.ToLower();
                        await Trackers[BaseTracker.TrackerType.Twitch].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                        await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(BaseTracker streamerName)
            {
                if (await Trackers[BaseTracker.TrackerType.Twitch].TryRemoveTrackerAsync(streamerName.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName.Name + "'s streams!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a streamer goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker streamer, [Remainder]string notification)
            {
                streamer.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{streamer.Name}` to `{notification}`");
            }

            [Command("SetAxis")]
            [Hide]
            [RequireBotManage]
            public async Task SetAxis(BaseTracker streamer, int size){
                (streamer as TwitchTracker).ViewerGraph.SetAxisThickness(size);
            }

            [Command("SetFont")]
            [Hide]
            [RequireBotManage]
            public async Task SetFont(BaseTracker streamer, int size){
                (streamer as TwitchTracker).ViewerGraph.SetFontSize(size);
            }

            [Command("SetMajor")]
            [Hide]
            [RequireBotManage]
            public async Task SetMajor(BaseTracker streamer, int size){
                (streamer as TwitchTracker).ViewerGraph.SetAxisMajorThickness(size);
            }

            [Command("SetMinor")]
            [Hide]
            [RequireBotManage]
            public async Task SetMinor(BaseTracker streamer, int size){
                (streamer as TwitchTracker).ViewerGraph.SetAxisMinorThickness(size);
            }

            [Group("Notifications")]
            public class Notifications : ModuleBase
            {

                [Command("SwitchShowEmbed")]
                [Summary("Switches the bool on whether to show the tracker embed or not.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task SwitchEmbed(BaseTracker streamer)
                {
                    var tracker = streamer as TwitchTracker;

                    await tracker.ModifyAsync(x => x.Specifications[Context.Channel.Id].ShowEmbed = !tracker.Specifications[Context.Channel.Id].ShowEmbed);
                    await ReplyAsync($"Changed `ShowEmbed` for `{streamer.Name}` to `{tracker.Specifications[Context.Channel.Id].ShowEmbed}`");
                }

                [Command("SwitchNotifyGame")]
                [Summary("Switches the bool on whether to notify on game changes.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task SwitchGame(BaseTracker streamer)
                {
                    var tracker = streamer as TwitchTracker;

                    await tracker.ModifyAsync(x => x.Specifications[Context.Channel.Id].NotifyOnGameChange = !tracker.Specifications[Context.Channel.Id].NotifyOnGameChange);
                    await ReplyAsync($"Changed `NotifyOnGameChange` for `{streamer.Name}` to `{tracker.Specifications[Context.Channel.Id].NotifyOnGameChange}`");
                }

                [Command("SwitchNotifyOffline")]
                [Summary("Switches the bool on whether to notify on when the streamer goes offline.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task SwitchOffline(BaseTracker streamer)
                {
                    var tracker = streamer as TwitchTracker;

                    await tracker.ModifyAsync(x => x.Specifications[Context.Channel.Id].NotifyOnOffline = !tracker.Specifications[Context.Channel.Id].NotifyOnOffline);
                    await ReplyAsync($"Changed `NotifyOnOffline` for `{streamer.Name}` to `{tracker.Specifications[Context.Channel.Id].NotifyOnOffline}`");
                }

                [Command("SwitchNotifyOnline")]
                [Alias("SwitchNotifyLive")]
                [Summary("Switches the bool on whether to notify on when the streamer goes live.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task SwitchOnline(BaseTracker streamer)
                {
                    var tracker = streamer as TwitchTracker;

                    await tracker.ModifyAsync(x => x.Specifications[Context.Channel.Id].NotifyOnOnline = !tracker.Specifications[Context.Channel.Id].NotifyOnOnline);
                    await ReplyAsync($"Changed `NotifyOnOnline` for `{streamer.Name}` to `{tracker.Specifications[Context.Channel.Id].NotifyOnOnline}`");
                }

                [Command("SwitchNotifyHost")]
                [Summary("Switches the bool on whether to notify hosts or not.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task SwitchHosts(BaseTracker streamer)
                {
                    var tracker = streamer as TwitchTracker;

                    await tracker.ModifyAsync(x => x.Specifications[Context.Channel.Id].NotifyOnHost = !tracker.Specifications[Context.Channel.Id].NotifyOnHost);
                    await ReplyAsync($"Changed `NotifyOnHost` for `{streamer.Name}` to `{tracker.Specifications[Context.Channel.Id].NotifyOnHost}`");
                }
            }

            [Command("GroupTrackers")]
            [Summary("Adds all trackers of the guild to a unified embed, in the channel you are calling this command in.")]
            public async Task GroupTrackers([Remainder]SocketRole rank = null)
            {
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].AddTrackerAsync(Context.Guild.Id.ToString(), Context.Channel.Id);
                if (rank != null)
                {
                    var tracker = StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].GetTracker(Context.Channel.Id, Context.Guild.Id.ToString()) as TwitchGroupTracker;
                    tracker.RankChannels[Context.Channel.Id] = rank.Id;
                    await StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].UpdateDBAsync(tracker);
                }
                await ReplyAsync("Added group tracking for this channel");
            }

            [Command("UnGroupTrackers")]
            [Summary("Removes grouping for trackers")]
            public async Task UnGroupTrackers()
            {
                (StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].GetTracker(Context.Channel.Id, Context.Guild.Id.ToString()) as TwitchGroupTracker).RankChannels.Remove(Context.Channel.Id);
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].TryRemoveTrackerAsync(Context.Guild.Id.ToString(), Context.Channel.Id);
                await ReplyAsync("Removed group tracking in this channel");
            }

            [Group("Guild")]
            public class Guild : ModuleBase
            {

                [Command("SetHostNotificationChannel")]
                [Summary("Sets the channel which will receive host notification.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task AddHostChannel([Remainder]SocketTextChannel channel)
                {
                    if (!StaticBase.TwitchGuilds.ContainsKey(Context.Guild.Id))
                    {
                        StaticBase.TwitchGuilds.Add(Context.Guild.Id, new TwitchGuild(Context.Guild.Id));

                    }

                    StaticBase.TwitchGuilds[Context.Guild.Id].notifyChannel = channel.Id;
                    await StaticBase.TwitchGuilds[Context.Guild.Id].UpdateGuildAsync();

                    await ReplyAsync($"Set {channel.Mention} as notify channel.");
                }

                [Command("AddRankRole")]
                [Summary("Adds a role to your rank system.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task AddRank(int pointsNeeded, [Remainder]SocketRole role)
                {
                    var rankRoles = StaticBase.TwitchGuilds[Context.Guild.Id].RankRoles;

                    if (rankRoles.Exists(x => x.Item2 == role.Id))
                        rankRoles.RemoveAll(x => x.Item2 == role.Id);

                    StaticBase.TwitchGuilds[Context.Guild.Id].RankRoles.Add(Tuple.Create(pointsNeeded, role.Id));
                    await StaticBase.TwitchGuilds[Context.Guild.Id].UpdateGuildAsync();

                    await ReplyAsync($"Added {role.Name} as rank role for people above {pointsNeeded} points.", embed: StaticBase.TwitchGuilds[Context.Guild.Id].GetRankRoles());
                }

                [Command("RemoveRankRole")]
                [Summary("Removes a role of your rank system.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task RemoveRank([Remainder]SocketRole role)
                {
                    var rankRoles = StaticBase.TwitchGuilds[Context.Guild.Id].RankRoles;

                    if (rankRoles.Exists(x => x.Item2 == role.Id))
                        rankRoles.RemoveAll(x => x.Item2 == role.Id);

                    await StaticBase.TwitchGuilds[Context.Guild.Id].UpdateGuildAsync();

                    await ReplyAsync($"Removed {role.Name} as rank role.", embed: StaticBase.TwitchGuilds[Context.Guild.Id].GetRankRoles());
                }

                [Command("AddLiveRole")]
                [Summary("Assigns a role assigned when somebody goes live.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task AddLiveRole([Remainder]SocketRole role)
                {
                    StaticBase.TwitchGuilds[Context.Guild.Id].LiveRole = role.Id;
                    await StaticBase.TwitchGuilds[Context.Guild.Id].UpdateGuildAsync();

                    await ReplyAsync($"Added {role.Name} as live role.");
                }

                [Command("Register")]
                [Summary("Sets you or the `owner` as the owner of the Twitch Channel.\nIf hosting, Mops will notify the host in the `notifyChannel`.")]
                public async Task RegisterHost(string streamer, IUser owner = null)
                {
                    streamer = streamer.ToLower();
                    if (!StaticBase.TwitchGuilds.ContainsKey(Context.Guild.Id))
                    {
                        await ReplyAsync("A person of authority must first use the `Twitch Guild SetHostNotificationChannel` command.");
                        return;
                    }

                    if (owner == null) owner = Context.User;

                    bool exists = StaticBase.TwitchUsers.ContainsKey(owner.Id + Context.Guild.Id);
                    var tUser = exists ? StaticBase.TwitchUsers[owner.Id + Context.Guild.Id] : new MopsBot.Data.Entities.TwitchUser(owner.Id, streamer, Context.Guild.Id);

                    if (!tUser.TwitchName.Equals(streamer))
                    {
                        await ReplyAsync("**Error:** You are already registered as " + tUser.TwitchName);
                        return;
                    }

                    if (!exists)
                    {
                        StaticBase.TwitchUsers.Add(owner.Id + Context.Guild.Id, tUser);
                        await tUser.UpdateUserAsync();
                    }

                    await ReplyAsync($"Successfully linked {owner.Mention} to {streamer}.\nHost notifications will be sent to <#{StaticBase.TwitchGuilds[Context.Guild.Id]?.notifyChannel}>");
                }

                [Command("Unregister")]
                [Summary("Removes you or `owner` as the owner of the channel and disables host notifications.")]
                public async Task UnregisterHost(string streamer, IUser owner = null)
                {
                    streamer = streamer.ToLower();
                    if (!StaticBase.TwitchGuilds.ContainsKey(Context.Guild.Id))
                    {
                        await ReplyAsync("A person of authority must first use the `Twitch Guild SetHostNotification` command.");
                        return;
                    }
                    if (owner == null) owner = Context.User;

                    if (StaticBase.TwitchUsers.ContainsKey(owner.Id + Context.Guild.Id))
                    {
                        var tUser = StaticBase.TwitchUsers[owner.Id + Context.Guild.Id];
                        await tUser.DeleteAsync();
                        await ReplyAsync($"Successfully unlinked {owner.Mention} and {streamer}.\nHost notifications will no longer be sent.");
                    }
                }

                [Command("GetStats")]
                [Summary("Shows your current Twitch stats.")]
                public async Task GetStats()
                {
                    if (!StaticBase.TwitchGuilds.ContainsKey(Context.Guild.Id))
                    {
                        await ReplyAsync("A person of authority must first use the `Twitch Guild SetHostNotification` command.");
                        return;
                    }

                    await ReplyAsync(embed: await StaticBase.TwitchUsers[Context.User.Id + Context.Guild.Id].StatEmbed(Context.Guild.Id));
                }

                [Command("GetLeaderboard")]
                [Summary("Shows current point leaderboard")]
                public async Task GetLeaderboard(uint begin = 1, uint end = 10, bool isGlobal = false)
                {
                    using (Context.Channel.EnterTypingState())
                    {
                        try
                        {
                            if (begin >= end) throw new Exception("Begin was bigger than, or equal to end.");
                            if (begin == 0 || end == 0) throw new Exception("Begin or end was 0.");
                            if (end - begin >= 5000) throw new Exception("Range must be smaller than 5000! (performance)");

                            long userCount = StaticBase.TwitchGuilds[Context.Guild.Id].GetUsers().Count;

                            if (end > userCount)
                                end = (uint)userCount;

                            await ReplyAsync("", embed: await StaticBase.TwitchGuilds[Context.Guild.Id].GetLeaderboardAsync(begin: (int)begin, end: (int)end));

                        }
                        catch (Exception e)
                        {
                            await ReplyAsync("[Error]: " + e.Message);
                        }
                    }
                }
            }

        }

        [Group("TwitchClips")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class TwitchClips : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified streamer's top clips every 30 minutes, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackClips(string streamerName, [Remainder]string notificationMessage = "New trending clip found!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        streamerName = streamerName.ToLower();
                        await Trackers[BaseTracker.TrackerType.TwitchClip].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                        await ReplyAsync("Keeping track of " + streamerName + "'s top clips above **2** views every 30 minutes, from now on!\nUse the `SetViewThreshold` subcommand to change the threshold.");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer's clips.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(BaseTracker streamerName)
            {
                if (await Trackers[BaseTracker.TrackerType.TwitchClip].TryRemoveTrackerAsync(streamerName.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName.Name + "'s streams!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.TwitchClip].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new clip is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker streamer, [Remainder]string notification = "")
            {
                streamer.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{streamer.Name}` to `{notification}`");
            }

            [Command("SetViewThreshold")]
            [Summary("Sets the minimum views a top clip must have to be shown.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetViewThreshold(BaseTracker streamer, uint threshold)
            {
                var tracker = (TwitchClipTracker)streamer;

                tracker.ViewThreshold = threshold;
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchClip].UpdateDBAsync(tracker);
                await ReplyAsync($"Will only notify about clips equal or above **{threshold}** views for `{streamer.Name}` now.");
            }
        }


        [Group("Reddit")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Reddit : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Subreddit, in the Channel you are calling this command in."
            + "\n queries MUST look something like this: `title:mei+title:hanzo`")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackSubreddit(string subreddit, string query = null)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.Reddit].AddTrackerAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                        await ReplyAsync("Keeping track of " + subreddit + $"'s posts, from now on, using {query}!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified Subreddit.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackSubreddit(string subreddit, string query = null)
            {
                if (await Trackers[BaseTracker.TrackerType.Reddit].TryRemoveTrackerAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + subreddit + "'s posts!");
                else{
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n" +
                                     $"Currently tracked Subreddits are:");
                    await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id));
                }
            }

            [Command("GetTrackers")]
            [Summary("Returns the subreddits that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following subreddits are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string subreddit, string notification = "", string query = null)
            {
                if (await StaticBase.Trackers[BaseTracker.TrackerType.Reddit].TrySetNotificationAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id, notification))
                {
                    await ReplyAsync($"Changed notification for `{subreddit}` to `{notification}`");
                }
                else{
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n" +
                                     $"Currently tracked subreddits are:");
                    await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id));
                }
            }
        }

        [Group("Overwatch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Overwatch : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackOW(string owUser)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        owUser = owUser.Replace("#", "-");
                        await Trackers[BaseTracker.TrackerType.Overwatch].AddTrackerAsync(owUser, Context.Channel.Id);

                        await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOW(BaseTracker owUser)
            {
                if (await Trackers[BaseTracker.TrackerType.Overwatch].TryRemoveTrackerAsync(owUser.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + owUser.Name + "'s stats!");
            }

            [Command("GetStats")]
            [Summary("Returns an embed representing the stats of the specified Overwatch player")]
            public async Task GetStats(string owUser)
            {
                await ReplyAsync("Stats fetched:", false, await Data.Tracker.OverwatchTracker.GetStatEmbedAsync(owUser.Replace("#", "-")));
            }

            [Command("GetTrackers")]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Overwatch].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a players' stats changed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker owUser, [Remainder]string notification = "")
            {
                owUser.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{owUser.Name}` to `{notification}`");
            }
        }

        [Group("JSON")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class JSON : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the Json, using the specified locations.\n" +
                     "graph:<location> adds the numeric value to a time/value graph\n" +
                     "always:<location> adds the value to the embed, regardless of whether it changed or not.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task trackJson(string source, params string[] query)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.JSON].AddTrackerAsync(String.Join("|||", new string[] { source, String.Join(",", query) }), Context.Channel.Id);
                        await ReplyAsync($"Keeping track of `{source}`'s attributes from now on!");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking jsons.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackNews([Remainder]BaseTracker JsonSource)
            {
                if (await Trackers[BaseTracker.TrackerType.JSON].TryRemoveTrackerAsync(JsonSource.Name, Context.Channel.Id))
                    await ReplyAsync($"Stopped keeping track of {JsonSource.Name}");
            }

            [Command("GetTrackers")]
            [Summary("Returns the jsons that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following jsons are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.JSON].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a change in the json was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker jsonSource, [Remainder]string notification = "")
            {
                jsonSource.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{jsonSource.Name}` to `{notification}`");
            }
        }

        [Group("OSRS")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class OSRS : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the stats of the OSRS player.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task Track(string name, [Remainder]string notification = "")
            {
                name = name.ToLower();
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.OSRS].AddTrackerAsync(name, Context.Channel.Id);
                        await ReplyAsync($"Keeping track of `{name}` stats after each playsession, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the player with the specified name.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrack([Remainder]BaseTracker name)
            {
                if (await Trackers[BaseTracker.TrackerType.OSRS].TryRemoveTrackerAsync(name.Name, Context.Channel.Id))
                    await ReplyAsync($"Stopped keeping track of {name.Name}!");
            }

            [Command("GetStats")]
            [Summary("Gets all top 2kk stats of the specified player.")]
            public async Task GetStats([Remainder]string name)
            {
                await ReplyAsync("", embed: await OSRSTracker.GetStatEmbed(name));
            }

            [Command("Compare")]
            [Summary("Compares the stats of 2 players.")]
            public async Task Compare(string name1, string name2)
            {
                await ReplyAsync("", embed: await OSRSTracker.GetCompareEmbed(name1, name2));
            }

            [Command("GetItem")]
            [Summary("Gets information on an Item")]
            public async Task GetItem([Remainder]string name)
            {
                await ReplyAsync("", embed: await OSRSTracker.GetItemEmbed(name));
            }

            [Command("GetTrackers")]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.OSRS].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a level up takes place.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker name, [Remainder]string notification = "")
            {
                name.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{name.Name}` to `{notification}`");
            }
        }

        [Group("HTML")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Ratelimit(1, 60, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
        public class HTML : InteractiveBase
        {
            [Command("TrackRegex", RunMode = RunMode.Async)]
            [Summary("Tracks regex on a webpage. Use () around the text you want to track to signify a match.")]
            public async Task TrackRegex(string website, string scrapeRegex)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.HTML].AddTrackerAsync(website + "|||" + scrapeRegex, Context.Channel.Id);
                        await ReplyAsync($"Keeping track of `{website}` data using ```html\n{scrapeRegex}```, from now on!\n\nInitial value was: **{await HTMLTracker.FetchData(website + "|||" + scrapeRegex)}**");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Tracks plain text on a webpage, and notifies whenever that text changes.\nThis command will guide you through the process.")]
            public async Task TrackText(string website, string textToTrack, int leftContextLength = 4, int rightContextLength = 1)
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (leftContextLength > 0 && rightContextLength > 0)
                    {
                        string escapedTextToTrack = textToTrack.Replace("?", @"\?").Replace("*", @"\*").Replace(".", @"\.").Replace("+", @"\+").Replace(")", @"\)").Replace("(", @"\(").Replace("[", @"\[").Replace("]", @"\]");

                        MatchCollection matches = await HTMLTracker.FetchAllData(website + "|||" + $"(<[^<>]*?>[^<>]*?){{{leftContextLength}}}({escapedTextToTrack})[^<>]*?(<[^<>]*?>[^<>]*?){{{rightContextLength}}}");
                        await Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, matches.Select(x => $"**{textToTrack}** in context\n\n```html\n{x.Value}```"));

                        await ReplyAsync("Which page is the one you want to track?\nIf none are specific enough, consider extending the context, or writing your own regex using the `TrackRegex` subcommand.");
                        int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;

                        //Escape regex symbols
                        string unescapedMatchString = matches[page].Value.Replace(escapedTextToTrack, textToTrack);

                        //Find out position of text, and replace it with wild characters
                        var match = Regex.Match(unescapedMatchString, $@">[^<>]*?({escapedTextToTrack})[^<>]*?<", RegexOptions.Singleline);
                        int position = match.Groups.First(x => x.Value.Equals(textToTrack)).Index;
                        string scrapeRegex = unescapedMatchString.Remove(position, textToTrack.Length).Insert(position, $@"\(\[^<>\]\*\?\)");

                        //Make any additional occurences of text in context wild characters
                        scrapeRegex = scrapeRegex.Replace(escapedTextToTrack, @"\[^<>\]\*\?");
                        scrapeRegex = scrapeRegex.Replace("?", @"\?").Replace("*", @"\*").Replace(".", @"\.").Replace("+", @"\+").Replace(")", @"\)").Replace("(", @"\(").Replace("[", @"\[").Replace("]", @"\]");
                        scrapeRegex = scrapeRegex.Replace("\\\\?", @"?").Replace("\\\\*", @"*").Replace("\\\\.", @".").Replace("\\\\+", @"+").Replace("\\\\)", @")").Replace("\\\\(", @"(").Replace("\\\\[", @"[").Replace("\\\\]", @"]");

                        await ReplyAsync($"Is there anything, for the sake of context, that you want to have removed (e.g. tracking highest level, but don't want it to be bound to a certain name)?\n\n```html\n{scrapeRegex}```\n\nIf so, please enter the exact texts you want to be generic instead of fixed in a **comma seperated list**.");
                        string result = (await NextMessageAsync(timeout: new TimeSpan(0, 1, 0)))?.Content;

                        if (result != null)
                        {
                            foreach (string value in result?.Split(","))
                            {
                                if (value.ToLower().Equals("no") || value.ToLower().Equals("n") || value.ToLower().Equals("nope"))
                                    break;
                                string toRemove = value.Trim();
                                scrapeRegex = scrapeRegex.Replace(toRemove, "[^<>]*?");
                            }
                        }

                        await TrackRegex(website, scrapeRegex);
                    }
                }
            }

            [Command("TestRegex", RunMode = RunMode.Async)]
            [Summary("Tests the regex and returns it's value. Handy if you want to check your regex before tracking with it!")]
            public async Task Test(string website, string scrapeRegex)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await ReplyAsync($"Regex returned value: {await HTMLTracker.FetchData(website + "|||" + scrapeRegex)}");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack", RunMode = RunMode.Async)]
            [Summary("Creates a paginator of all trackers, out of which you have to choose one.")]
            public async Task UnTrack()
            {
                using (Context.Channel.EnterTypingState())
                {
                    var trackers = Trackers[BaseTracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList();
                    await Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, trackers.Select(x => $"```html\n{x.Name}```"));
                    await ReplyAsync("Which tracker do you want to delete?\nPlease enter the page number");

                    int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;
                    if (await Trackers[BaseTracker.TrackerType.HTML].TryRemoveTrackerAsync(trackers[page].Name, Context.Channel.Id))
                        await ReplyAsync($"Stopped keeping track of result {page + 1} of paginator!");
                }
            }

            [Command("UnTrackAll")]
            [Summary("Untracks all trackers in the current channel.")]
            public async Task UnTrackAll()
            {
                foreach (var tracker in Trackers[BaseTracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList())
                {
                    if (await Trackers[BaseTracker.TrackerType.HTML].TryRemoveTrackerAsync(tracker.Name, Context.Channel.Id))
                        await ReplyAsync($"Stopped keeping track of {tracker.Name.Split("|||")[0]}!");
                }
            }

            [Command("SetNotification", RunMode = RunMode.Async)]
            [Summary("Sets the notification for when the text of a regex match changes.\nRequires only the notification, paginator will guide you.")]
            public async Task SetNotification([Remainder]string notification = "")
            {
                using (Context.Channel.EnterTypingState())
                {
                    var trackers = Trackers[BaseTracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList();
                    await Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, trackers.Select(x => $"```html\n{x.Name}```"));
                    await ReplyAsync("Which tracker do you want to set the notification for?\nPlease enter the page number");

                    int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;
                    if (await Trackers[BaseTracker.TrackerType.HTML].TrySetNotificationAsync(trackers[page].Name, Context.Channel.Id, notification))
                        await ReplyAsync($"Set notification for result {page + 1} of paginator to `{notification}`!");
                }
            }
        }

        [Group("RSS")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class RSS : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified RSS feed url")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            public async Task TrackRSS(string url, string notification = "")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.RSS].AddTrackerAsync(url, Context.Channel.Id, notification);

                        await ReplyAsync("Keeping track of " + url + $"'s feed, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified RSS feed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrackFeed(BaseTracker url)
            {
                if (await Trackers[BaseTracker.TrackerType.RSS].TryRemoveTrackerAsync(url.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + url.Name + " 's feed!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the feeds that are tracked in the current channel.")]
            public async Task GetTrackers()
            {
                await ReplyAsync("Following feeds are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.RSS].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker url, string notification = "")
            {
                url.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{url.Name}` to `{notification}`");
            }
        }


        [Command("PruneTrackers", RunMode = RunMode.Async)]
        [RequireBotManage()]
        [Hide]
        public async Task PruneTrackers(bool testing = true)
        {
            using (Context.Channel.EnterTypingState())
            {
                Dictionary<string, int> pruneCount = new Dictionary<string, int>();

                foreach (var trackerHandler in StaticBase.Trackers)
                {
                    pruneCount[trackerHandler.Key.ToString()] = 0;
                    foreach (var tracker in trackerHandler.Value.GetTrackerSet())
                    {
                        foreach (var channel in tracker.ChannelMessages.Keys.ToList())
                        {
                            if (Program.Client.GetChannel(channel) == null)
                            {
                                if (!testing)
                                    await trackerHandler.Value.TryRemoveTrackerAsync(tracker.Name, channel);

                                pruneCount[trackerHandler.Key.ToString()]++;
                            }
                        }
                    }
                }

                await ReplyAsync($"```{"TrackerType",-20}{"PruneCount"}\n{string.Join("\n", pruneCount.Select(x => $"{x.Key,-20}{x.Value,-3}"))}```");
            }
        }

        [Command("MergeTrackers", RunMode = RunMode.Async)]
        [RequireBotManage]
        [Hide]
        public async Task MergeTrackers()
        {
            using (Context.Channel.EnterTypingState())
            {
                Dictionary<string, int> mergeCount = new Dictionary<string, int>();

                foreach (var trackerHandler in StaticBase.Trackers)
                {
                    var countBefore = trackerHandler.Value.GetTrackers().Count;
                    await trackerHandler.Value.MergeCapitalisation();
                    mergeCount[trackerHandler.Key.ToString()] = countBefore - trackerHandler.Value.GetTrackers().Count;
                }

                await ReplyAsync($"```{"TrackerType",-20}{"MergeCount"}\n{string.Join("\n", mergeCount.Select(x => $"{x.Key,-20}{x.Value,-3}"))}```");
            }
        }
    }
}