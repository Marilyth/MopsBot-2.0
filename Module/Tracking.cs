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
using static MopsBot.Data.Tracker.BaseTracker;
using MopsBot.Module.Preconditions;

namespace MopsBot.Module
{
    public class Tracking : ModuleBase<ShardedCommandContext>
    {
        public static readonly Dictionary<string, (string, string)> Names = new Dictionary<string, (string, string)>{
            //{"Twitter", ("Twitter username, without the @ in the beginning!", "https://cdn.discordapp.com/attachments/158166244493623296/681834842240974867/unknown.png")},
            {"Osu", ("name of the osu! player", "https://cdn.discordapp.com/attachments/158166244493623296/681835932047114240/unknown.png")},
            {"Youtube", ("YouTube channel-id", "https://cdn.discordapp.com/attachments/158166244493623296/681836381768777744/unknown.png")},
            {"YoutubeLive", ("YouTube channel-id", "https://cdn.discordapp.com/attachments/158166244493623296/681836381768777744/unknown.png")},
            {"Twitch", ("name of the Twitch streamer", "https://cdn.discordapp.com/attachments/158166244493623296/681836767158337542/unknown.png")},
            {"TwitchClip", ("name of the Twitch streamer", "https://cdn.discordapp.com/attachments/158166244493623296/681836767158337542/unknown.png")},
            //{"Reddit", ("name of the subreddit (without the r/)", "https://cdn.discordapp.com/attachments/158166244493623296/681837609898868753/unknown.png")},
            {"Overwatch", ("name and battletag of the Overwatch player (make sure to replace the `#` with `-`)", "https://cdn.discordapp.com/attachments/158166244493623296/681855715815587865/unknown.png")},
            //{"JSON", ("URL to the JSON source", "https://cdn.discordapp.com/attachments/158166244493623296/681838828092522507/unknown.png")},
            {"OSRS",("name of the OSRS player","https://cdn.discordapp.com/attachments/158166244493623296/681856581159223326/unknown.png")},
            {"RSS", ("URL to the RSS-feed", "https://cdn.discordapp.com/attachments/158166244493623296/681856139331371028/unknown.png")},
            {"Steam", ("steam-id64 of the user (you can find it here https://steamidfinder.com/ )", "https://cdn.discordapp.com/attachments/158166244493623296/681856888874598465/unknown.png")},
            {"TikTok", ("TikTok username", "https://cdn.discordapp.com/attachments/158166244493623296/870925608367063040/unknown.png")},
        };
        /*
        [Command("Setup", RunMode = RunMode.Async)]
        [HideHelp]
        [Summary("Helps the user set up the tracker they want.")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
        public async Task Setup()
        {
            await ReplyAsync($"Hello there, {Context.User.Username}!\nLet me guide you through your tracking setup:\n");
            await ReplyAsync($"First of all: What kind of tracker do you want to create?\nCopy & Paste one of these:```\n{string.Join("\n", Names.Keys)}```");
            string tracker = (await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content;
            TrackerType type = Enum.Parse<TrackerType>(tracker, true);

            await ReplyAsync($"To create a {type.ToString()}-Tracker you need the {Names[type.ToString()].Item1}.\nYou can find it like this: {Names[type.ToString()].Item2}");
            await ReplyAsync($"Please copy & paste it here after you found it!");
            string name = (await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content;
            if (!CapSensitive.Any(x => x == type))
                name = name.ToLower();

            var conditions = await Program.Handler.commands.Search($"{type.ToString()} Track").Commands.FirstOrDefault().CheckPreconditionsAsync(Context);
            if (!conditions.IsSuccess)
            {
                await ReplyAsync($"I am sorry, we failed due to:\n{conditions.ErrorReason}");
                return;
            }

            while (true)
            {
                try
                {
                    await StaticBase.Trackers[type].AddTrackerAsync(name, Context.Channel.Id);
                    break;
                }
                catch (Exception e)
                {
                    await ReplyAsync("**Error**: " + e.InnerException.Message);
                    await ReplyAsync($"Hmm, that didn't seem to work.\nRemember, you need to copy & paste the {Names[type.ToString()].Item1}, just like in the image above.\nTry it again:");
                    name = (await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content;
                    if (!CapSensitive.Any(x => x == type))
                        name = name.ToLower();
                }
            }

            var baseTracker = StaticBase.Trackers[type].GetTracker(Context.Channel.Id, name);

            await ReplyAsync($"Now, whenever the tracker finds something new to send, I will send a notification message alongside it.\nWhat do you want the notification message to be?");
            string notification = (await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content;
            baseTracker.ChannelConfig[Context.Channel.Id]["Notification"] = notification;

            await ReplyAsync($"And finally, where do you want the notifications to appear in?\nPlease mention a #channel:");
            ulong channel = ulong.Parse(string.Join("", (await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content.Skip(2).SkipLast(1)));
            if (channel != Context.Channel.Id)
            {
                var currentConfig = baseTracker.ChannelConfig[Context.Channel.Id];
                baseTracker.ChannelConfig[channel] = currentConfig;
                await StaticBase.Trackers[type].TryRemoveTrackerAsync(baseTracker.Name, Context.Channel.Id);
            }

            await ReplyAsync($"And we are all done!");
        } */

        /*
        [Group("Twitter")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitter : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Twitter)]
            public async Task trackTwitter(string twitterUser, [Remainder] string tweetNotification = "~Tweet Tweet~")
            {
                twitterUser = twitterUser.ToLower().Replace("@", "");
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[TrackerType.Twitter].AddTrackerAsync(twitterUser, Context.Channel.Id, tweetNotification);

                    await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, replies and retweets, from now on!\nTo disable replies and retweets, please use the `Twitter DisableNonMain` subcommand!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter(BaseTracker twitterUser)
            {
                if (await Trackers[BaseTracker.TrackerType.Twitter].TryRemoveTrackerAsync(twitterUser.Name, twitterUser.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + twitterUser.Name + "'s tweets!");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following twitters are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Twitter].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new Main-Tweet is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker TwitterName, [Remainder] string notification = "")
            {
                TwitterName.GetLastCalledConfig(Context.Guild.Id)["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Twitter].UpdateDBAsync(TwitterName);
                await ReplyAsync($"Set notification for main tweets, for `{TwitterName.Name}`, to {notification}!");
            }

            [Command("SetNonMainNotification")]
            [Summary("Sets the notification text that is used each time a new retweet or reply is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNonMainNotification(BaseTracker TwitterName, [Remainder] string notification = "")
            {
                var config = TwitterName.GetLastCalledConfig(Context.Guild.Id);
                config[TwitterTracker.REPLYNOTIFICATION] = notification;
                config[TwitterTracker.RETWEETNOTIFICATION] = notification;
                config[TwitterTracker.SHOWREPLIES] = true;
                config[TwitterTracker.SHOWRETWEETS] = true;
                await StaticBase.Trackers[TrackerType.Twitter].UpdateDBAsync(TwitterName);
                await ReplyAsync($"Set notification for retweets and replies, for `{TwitterName.Name}`, to {notification}!");
            }

            [Command("DisableNonMain")]
            [Alias("DisableReplies", "DisableRetweets")]
            [Summary("Disables tracking for the retweets and replies of the specified Twitter account.")]
            public async Task DisableRetweets(BaseTracker TwitterName)
            {
                TwitterName.GetLastCalledConfig(Context.Guild.Id)[TwitterTracker.SHOWREPLIES] = false;
                TwitterName.GetLastCalledConfig(Context.Guild.Id)[TwitterTracker.SHOWRETWEETS] = false;
                await StaticBase.Trackers[TrackerType.Twitter].UpdateDBAsync(TwitterName);
                await ReplyAsync($"Disabled retweets and replies for `{TwitterName.Name}`!\nTo reenable retweets and replies, please provide a notification via the `Twitter SetNonMainNotification` subcommand!");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.GetLastCalledConfig(Context.Guild.Id).Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker TwitterName, [Remainder]string config)
            {
                await ModifyConfig(this, TwitterName, TrackerType.Twitter, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Twitter, Context);
            }

            [Command("Prune")]
            [HideHelp]
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
                            foreach (var channel in tracker.Value.ChannelConfig.Keys.ToList())
                                await StaticBase.Trackers[BaseTracker.TrackerType.Twitter].TryRemoveTrackerAsync(tracker.Key, channel);
                        }
                    }
                    var result = $"{"Twitter User",-20}{"Fail count"}\n{string.Join("\n", pruneCount.Select(x => $"{x.Key,-20}{x.Value,-3}"))}";
                    if (result.Length < 2040)
                        await ReplyAsync($"```yaml\n{result}```");
                    else
                        await ReplyAsync($"```Pruned {totalCount} trackers```");
                }
            }
        }
        */

        [Group("Osu")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Osu : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Osu player, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Osu)]
            public async Task trackOsu([Remainder] string OsuUser)
            {
                using (Context.Channel.EnterTypingState())
                {
                    OsuUser = OsuUser.ToLower();
                    await Trackers[BaseTracker.TrackerType.Osu].AddTrackerAsync(OsuUser, Context.Channel.Id);

                    await ReplyAsync("Keeping track of " + OsuUser + "'s plays above `0.1pp` gain, from now on!\nYou can change the lower pp boundary by using the `Osu SetPPBounds` subcommand!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Osu player, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder] BaseTracker OsuUser)
            {
                if (await Trackers[BaseTracker.TrackerType.Osu].TryRemoveTrackerAsync(OsuUser.Name, OsuUser.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + OsuUser.Name + "'s plays!");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the Osu players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Osu players are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Osu].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetPPBounds")]
            [Summary("Sets the lower bounds of pp gain that must be reached, to show a notification.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetPPBounds(BaseTracker osuUser, double threshold)
            {
                var tracker = osuUser as OsuTracker;
                if (threshold > 0.1)
                {
                    tracker.ChannelConfig[osuUser.LastCalledChannelPerGuild[Context.Guild.Id]][OsuTracker.PPTHRESHOLD] = threshold;
                    await StaticBase.Trackers[BaseTracker.TrackerType.Osu].UpdateDBAsync(tracker);
                    await ReplyAsync($"Changed threshold for `{osuUser}` to `{threshold}`");
                }
                else
                    await ReplyAsync("Threshold must be above 0.1!");
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a player gained pp.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker osuUser, [Remainder] string notification = "")
            {
                osuUser.ChannelConfig[osuUser.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Osu].UpdateDBAsync(osuUser);
                await ReplyAsync($"Changed notification for `{osuUser.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker osuUser, [Remainder]string config)
            {
                await ModifyConfig(this, osuUser, TrackerType.Osu, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Osu, Context);
            }
        }

        [Group("Youtube")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Youtube : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Youtuber, in the Channel you are calling this command in.\nYoutubes Terms of Service and Googles privacy policy apply:\nhttps://www.youtube.com/t/terms \nhttp://www.google.com/policies/privacy")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Youtube)]
            public async Task trackYoutube(string channelID, [Remainder] string notificationMessage = "New Video")
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.Youtube].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                    await ReplyAsync("Keeping track of " + channelID + "'s videos, from now on!\nThis tracker **only tracks videos, no livestreams!**\nFor livestreams, start a `YoutubeLive` tracker, not a `Youtube` tracker!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtuber, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(BaseTracker channelID)
            {
                if (await Trackers[BaseTracker.TrackerType.Youtube].TryRemoveTrackerAsync(channelID.Name, channelID.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + channelID.Name + "'s videos!");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Youtube].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new video appears.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker channelID, [Remainder] string notification = "")
            {
                channelID.ChannelConfig[channelID.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Youtube].UpdateDBAsync(channelID);
                await ReplyAsync($"Changed notification for `{channelID.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker channelID, [Remainder]string config)
            {
                await ModifyConfig(this, channelID, TrackerType.Youtube, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Youtube, Context);
            }
        }

        [Group("Twitch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitch : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Twitch)]
            public async Task trackStreamer(string streamerName, [Remainder] string notificationMessage = "Stream went live!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    streamerName = streamerName.ToLower();
                    await Trackers[BaseTracker.TrackerType.Twitch].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                    await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(BaseTracker streamerName)
            {
                if (await Trackers[BaseTracker.TrackerType.Twitch].TryRemoveTrackerAsync(streamerName.Name, streamerName.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + streamerName.Name + "'s streams!");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a streamer goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker streamer, [Remainder] string notification = "")
            {
                streamer.ChannelConfig[streamer.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Twitch].UpdateDBAsync(streamer);
                await ReplyAsync($"Changed notification for `{streamer.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker streamerName, [Remainder]string config)
            {
                await ModifyConfig(this, streamerName, TrackerType.Twitch, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Twitch, Context);
            }

            [Command("GroupTrackers")]
            [Summary("Adds all trackers of the guild to a unified embed, in the channel you are calling this command in.")]
            public async Task GroupTrackers([Remainder] SocketRole rank = null)
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
            public class Guild : ModuleBase<ShardedCommandContext>
            {

                [Command("SetHostNotificationChannel")]
                [Summary("Sets the channel which will receive host notification.")]
                [RequireUserPermission(GuildPermission.ManageRoles)]
                public async Task AddHostChannel([Remainder] SocketTextChannel channel)
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
                public async Task AddRank(int pointsNeeded, [Remainder] SocketRole role)
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
                public async Task RemoveRank([Remainder] SocketRole role)
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
                public async Task AddLiveRole([Remainder] SocketRole role)
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
                        if (begin >= end) throw new Exception("Begin was bigger than, or equal to end.");
                        if (begin == 0 || end == 0) throw new Exception("Begin or end was 0.");
                        if (end - begin >= 5000) throw new Exception("Range must be smaller than 5000! (performance)");

                        long userCount = StaticBase.TwitchGuilds[Context.Guild.Id].GetUsers().Count;

                        if (end > userCount)
                            end = (uint)userCount;

                        await ReplyAsync("", embed: await StaticBase.TwitchGuilds[Context.Guild.Id].GetLeaderboardAsync(begin: (int)begin, end: (int)end));
                    }
                }
            }

        }

        [Group("TwitchClip")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class TwitchClip : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified streamer's top clips every 30 minutes, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.TwitchClip)]
            public async Task trackClips(string streamerName, [Remainder] string notificationMessage = "New trending clip found!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    streamerName = streamerName.ToLower();
                    await Trackers[BaseTracker.TrackerType.TwitchClip].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                    await ReplyAsync("Keeping track of " + streamerName + "'s top clips above **2** views every 30 minutes, from now on!\nUse the `SetViewThreshold` subcommand to change the threshold.");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer's clips.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(BaseTracker streamerName)
            {
                if (await Trackers[BaseTracker.TrackerType.TwitchClip].TryRemoveTrackerAsync(streamerName.Name, streamerName.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + streamerName.Name + "'s streams!");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.TwitchClip].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new clip is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker streamer, [Remainder] string notification = "")
            {
                streamer.ChannelConfig[streamer.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchClip].UpdateDBAsync(streamer);
                await ReplyAsync($"Changed notification for `{streamer.Name}` to `{notification}`");
            }

            [Command("SetViewThreshold")]
            [Summary("Sets the minimum views a top clip must have to be shown.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetViewThreshold(BaseTracker streamer, uint threshold)
            {
                var tracker = (TwitchClipTracker)streamer;

                tracker.ChannelConfig[streamer.LastCalledChannelPerGuild[Context.Guild.Id]][TwitchClipTracker.VIEWTHRESHOLD] = threshold;
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchClip].UpdateDBAsync(tracker);
                await ReplyAsync($"Will only notify about clips equal or above **{threshold}** views for `{streamer.Name}` now.");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker streamerName, [Remainder]string config)
            {
                await ModifyConfig(this, streamerName, TrackerType.TwitchClip, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.TwitchClip, Context);
            }
        }

        [Group("Reddit")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Reddit : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Subreddit, in the Channel you are calling this command in."
            + "\nYou can check how queries look like here https://www.reddit.com/wiki/search")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Reddit)]
            public async Task trackSubreddit(string subreddit, [Remainder] string query = null)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.Reddit].AddTrackerAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                    await ReplyAsync("Keeping track of " + subreddit + $"'s posts, from now on, using {query}!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified Subreddit.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackSubreddit([Remainder] BaseTracker subreddit)
            {
                if (await Trackers[BaseTracker.TrackerType.Reddit].TryRemoveTrackerAsync(subreddit.Name, subreddit.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + subreddit.Name + "'s posts!");
                else
                {
                    await ReplyAsync($"Could not find tracker for `{subreddit.Name}`\n" +
                                     $"Currently tracked Subreddits are:");
                    await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id, true));
                }
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the subreddits that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following subreddits are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker subreddit, string notification = "")
            {
                if (await StaticBase.Trackers[BaseTracker.TrackerType.Reddit].TrySetNotificationAsync(subreddit.Name, subreddit.LastCalledChannelPerGuild[Context.Guild.Id], notification))
                {
                    await ReplyAsync($"Changed notification for `{subreddit.Name}` to `{notification}`");
                }
                else
                {
                    await ReplyAsync($"Could not find tracker for `{subreddit.Name}`\n" +
                                     $"Currently tracked subreddits are:");
                    await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id, true));
                }
            }

            [Command("Check", RunMode = RunMode.Async)]
            [Summary("Returns the newest `limit` entries using the `subreddit` and `query` provided.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Check(int limit, string subreddit, [Remainder] string query = null)
            {
                var result = await RedditTracker.checkReddit(subreddit, query, limit);
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, result);
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Remainder] BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            /*[Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Remainder] BaseTracker subreddit)
            {
                await ModifyConfig(this, subreddit, TrackerType.Reddit);
            }*/

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Reddit, Context);
            }
        }

        [Group("Overwatch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Overwatch : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Overwatch)]
            public async Task trackOW(string owUser)
            {
                using (Context.Channel.EnterTypingState())
                {
                    owUser = owUser.Replace("#", "-");
                    await Trackers[BaseTracker.TrackerType.Overwatch].AddTrackerAsync(owUser, Context.Channel.Id);

                    await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOW(BaseTracker owUser)
            {
                if (await Trackers[BaseTracker.TrackerType.Overwatch].TryRemoveTrackerAsync(owUser.Name, owUser.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + owUser.Name + "'s stats!");
            }

            [Command("GetStats")]
            [Summary("Returns an embed representing the stats of the specified Overwatch player")]
            public async Task GetStats(string owUser)
            {
                await ReplyAsync("Stats fetched:", false, await Data.Tracker.OverwatchTracker.GetStatEmbedAsync(owUser.Replace("#", "-")));
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Overwatch].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a players' stats changed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker owUser, [Remainder] string notification = "")
            {
                owUser.ChannelConfig[owUser.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Overwatch].UpdateDBAsync(owUser);
                await ReplyAsync($"Changed notification for `{owUser.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker owUser, [Remainder]string config)
            {
                await ModifyConfig(this, owUser, TrackerType.Overwatch, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Overwatch, Context);
            }
        }

        [Group("JSON")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class JSON : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the Json, using the specified locations.\n" +
                     "graph:<location> adds the numeric value to a time/value graph\n" +
                     "always:<location> adds the value to the embed, regardless of whether it changed or not.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.JSON)]
            public async Task trackJson(string source, [Remainder] string paths)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.JSON].AddTrackerAsync(String.Join("|||", new string[] { source, paths }), Context.Channel.Id);
                    await ReplyAsync($"Keeping track of `{source}`'s attributes from now on!");
                }
            }

            [Command("TrackExtended", RunMode = RunMode.Async)]
            [HideHelp]
            [Summary("Keeps track of the Json, as a POST request with body, using the specified locations.\n" +
                     "graph:<location> adds the numeric value to a time/value graph\n" +
                     "always:<location> adds the value to the embed, regardless of whether it changed or not.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.JSON)]
            public async Task trackExtended(string source, string body, [Remainder] string paths)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.JSON].AddTrackerAsync(String.Join("|||", new string[] { source + "||" + body, paths }), Context.Channel.Id);
                    await ReplyAsync($"Keeping track of `{source}`'s attributes from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking jsons.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackNews([Remainder] BaseTracker JsonSource)
            {
                if (await Trackers[BaseTracker.TrackerType.JSON].TryRemoveTrackerAsync(JsonSource.Name, JsonSource.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync($"Stopped keeping track of {JsonSource.Name}");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the jsons that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following jsons are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.JSON].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a change in the json was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker jsonSource, [Remainder] string notification = "")
            {
                jsonSource.ChannelConfig[jsonSource.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.JSON].UpdateDBAsync(jsonSource);
                await ReplyAsync($"Changed notification for `{jsonSource.Name}` to `{notification}`");
            }

            [Command("Check", RunMode = RunMode.Async)]
            [Summary("Checks the json for the specified paths, and returns the values")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Check(string Url, [Remainder] string paths)
            {
                var result = await JSONTracker.GetResults(Url, paths.Split("\n"));
                var embed = new EmbedBuilder().WithCurrentTimestamp().WithColor(255, 227, 21).WithFooter(x =>
                {
                    x.Text = "JsonTracker";
                    x.IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c9/JSON_vector_logo.svg/160px-JSON_vector_logo.svg.png";
                });

                foreach (var cur in result)
                {
                    var resultName = cur.Key.Contains("as:") ? cur.Key.Split(":").Last() : cur.Key.Split("->").Last();
                    if (!cur.Key.Contains("image:"))
                        embed.AddField(resultName, cur.Value);
                    else
                        embed.ThumbnailUrl = cur.Value;
                }
                await ReplyAsync(embed: embed.Build());
            }

            [Command("CheckExtended", RunMode = RunMode.Async)]
            [HideHelp]
            [Summary("Checks the json for the specified paths, and returns the values")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task CheckExtended(string Url, string body, [Remainder] string paths)
            {
                var result = await JSONTracker.GetResults(Url, paths.Split("\n"), body);
                var embed = new EmbedBuilder().WithCurrentTimestamp().WithColor(255, 227, 21).WithFooter(x =>
                {
                    x.Text = "JsonTracker";
                    x.IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c9/JSON_vector_logo.svg/160px-JSON_vector_logo.svg.png";
                });

                foreach (var cur in result)
                {
                    var resultName = cur.Key.Contains("as:") ? cur.Key.Split(":").Last() : cur.Key.Split("->").Last();
                    if (!cur.Key.Contains("image:"))
                        embed.AddField(resultName, cur.Value);
                    else
                        embed.ThumbnailUrl = cur.Value;
                }
                await ReplyAsync(embed: embed.Build());
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Remainder]BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            /*[Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Remainder] BaseTracker jsonSource)
            {
                await ModifyConfig(this, jsonSource, TrackerType.JSON);
            }*/

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.JSON, Context);
            }
        }

        [Group("OSRS")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class OSRS : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the stats of the OSRS player.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.OSRS)]
            public async Task Track(string name, [Remainder] string notification = "")
            {
                name = name.ToLower();
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.OSRS].AddTrackerAsync(name, Context.Channel.Id);
                    await ReplyAsync($"Keeping track of `{name}` stats after each playsession, from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the player with the specified name.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrack([Remainder] BaseTracker name)
            {
                if (await Trackers[BaseTracker.TrackerType.OSRS].TryRemoveTrackerAsync(name.Name, name.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync($"Stopped keeping track of {name.Name}!");
            }

            [Command("GetStats")]
            [Summary("Gets all top 2kk stats of the specified player.")]
            public async Task GetStats([Remainder] string name)
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
            public async Task GetItem([Remainder] string name)
            {
                await ReplyAsync("", embed: await OSRSTracker.GetItemEmbed(name));
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.OSRS].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a level up takes place.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker name, [Remainder] string notification = "")
            {
                name.ChannelConfig[name.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.OSRS].UpdateDBAsync(name);
                await ReplyAsync($"Changed notification for `{name.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker name, [Remainder]string config)
            {
                await ModifyConfig(this, name, TrackerType.OSRS, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.OSRS, Context);
            }
        }

        [Group("HTML")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [Ratelimit(1, 60, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
        public class HTML : ModuleBase<ShardedCommandContext>
        {
            [Command("TrackRegex", RunMode = RunMode.Async)]
            [Summary("Tracks regex on a webpage. Use () around the text you want to track to signify a match.")]
            [TrackerLimit(TrackerType.HTML)]
            public async Task TrackRegex(string website, [Remainder] string scrapeRegex)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.HTML].AddTrackerAsync(website + "|||" + scrapeRegex, Context.Channel.Id);
                    await ReplyAsync($"Keeping track of `{website}` data using ```html\n{scrapeRegex}```, from now on!\n\nInitial value was: **{await HTMLTracker.FetchData(website + "|||" + scrapeRegex)}**");
                }
            }

            /*
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Tracks plain text on a webpage, and notifies whenever that text changes.\nThis command will guide you through the process.")]
            [TrackerLimit(TrackerType.HTML)]
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
                        int position = match.Groups.Values.First(x => x.Value.Equals(textToTrack)).Index;
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
            }*/

            [Command("TestRegex", RunMode = RunMode.Async)]
            [Summary("Tests the regex and returns it's value. Handy if you want to check your regex before tracking with it!")]
            public async Task Test(string website, [Remainder] string scrapeRegex)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await ReplyAsync($"Regex returned value: {await HTMLTracker.FetchData(website + "|||" + scrapeRegex)}");
                }
            }

            /*
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
            }*/

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

            /*
            [Command("SetNotification", RunMode = RunMode.Async)]
            [Summary("Sets the notification for when the text of a regex match changes.\nRequires only the notification, paginator will guide you.")]
            public async Task SetNotification([Remainder] string notification = "")
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

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig()
            {
                using (Context.Channel.EnterTypingState())
                {
                    var trackers = Trackers[BaseTracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList();
                    await Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, trackers.Select(x => $"```html\n{x.Name}```"));
                    await ReplyAsync("Which tracker do you want to change the config for?\nPlease enter the page number");

                    int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;
                    await ModifyConfig(this, trackers[page], TrackerType.HTML);
                }
            }*/
        }

        [Group("RSS")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class RSS : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified RSS feed url")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.RSS)]
            public async Task TrackRSS(string url, string notification = "")
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.RSS].AddTrackerAsync(url, Context.Channel.Id, notification);

                    await ReplyAsync("Keeping track of " + url + $"'s feed, from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified RSS feed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrackFeed(BaseTracker url)
            {
                if (await Trackers[BaseTracker.TrackerType.RSS].TryRemoveTrackerAsync(url.Name, url.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + url.Name + " 's feed!");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the feeds that are tracked in the current channel.")]
            public async Task GetTrackers()
            {
                await ReplyAsync("Following feeds are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.RSS].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker url, [Remainder] string notification = "")
            {
                url.ChannelConfig[url.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.RSS].UpdateDBAsync(url);
                await ReplyAsync($"Changed notification for `{url.Name}` to `{notification}`");
            }

            [Command("Check", RunMode = RunMode.Async)]
            [Summary("Returns the newest entry in the rss feed")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Check(string rssFeed)
            {
                var result = await RSSTracker.GetFeed(rssFeed);
                await ReplyAsync(embed: result);
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker url, [Remainder]string config)
            {
                await ModifyConfig(this, url, TrackerType.RSS, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.RSS, Context);
            }
        }

        [Group("Steam")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Steam : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified steam user, in the Channel you are calling this command in.\nWill notify on game changes and achievements.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Steam)]
            public async Task Track([Remainder] string SteamNameOrId)
            {
                using (Context.Channel.EnterTypingState())
                {
                    SteamNameOrId = SteamNameOrId.ToLower();
                    await Trackers[BaseTracker.TrackerType.Steam].AddTrackerAsync(SteamNameOrId, Context.Channel.Id);
                    var worked = long.TryParse(SteamNameOrId, out long test);

                    await ReplyAsync("Keeping track of " + SteamNameOrId + $"'s Achievements and playing status from now on.");
                    if (!worked) await ReplyAsync($"Make sure this is you: https://steamcommunity.com/id/{SteamNameOrId}\nOtherwise use your steamid instead of steam name");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Steam user, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder] BaseTracker SteamNameOrId)
            {
                if (await Trackers[BaseTracker.TrackerType.Steam].TryRemoveTrackerAsync(SteamNameOrId.Name, SteamNameOrId.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + SteamNameOrId.Name + "'s Steam data!");
            }

            [Command("GetTrackers", RunMode = RunMode.Async)]
            [Summary("Returns the Steam users that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Steam users are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Steam].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new achievement was achieved.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker SteamNameOrId, [Remainder] string notification = "")
            {
                SteamNameOrId.ChannelConfig[SteamNameOrId.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Steam].UpdateDBAsync(SteamNameOrId);
                await ReplyAsync($"Changed notification for `{SteamNameOrId.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker SteamNameOrId, [Remainder]string config)
            {
                await ModifyConfig(this, SteamNameOrId, TrackerType.Steam, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Steam, Context);
            }
        }

        [Group("YoutubeLive")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class YoutubeLive : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Youtubers livestreams, in the Channel you are calling this command in.\nYoutubes Terms of Service and Googles privacy policy apply:\nhttps://www.youtube.com/t/terms \nhttp://www.google.com/policies/privacy")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(0)]
            [TrackerLimit(TrackerType.YoutubeLive)]
            public async Task trackYoutube(string channelID, [Remainder]string notificationMessage = "New Stream")
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.YoutubeLive].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                    await ReplyAsync("Keeping track of " + channelID + "'s streams, from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtubers streams, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(BaseTracker channelID)
            {
                if (await Trackers[BaseTracker.TrackerType.YoutubeLive].TryRemoveTrackerAsync(channelID.Name, channelID.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + channelID.Name + "'s streams!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new stream goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker channelID, [Remainder]string notification = "")
            {
                channelID.ChannelConfig[channelID.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].UpdateDBAsync(channelID);
                await ReplyAsync($"Changed notification for `{channelID.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker ChannelID, [Remainder]string config)
            {
                await Tracking.ModifyConfig(this, ChannelID, BaseTracker.TrackerType.YoutubeLive, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel){
                await Tracking.ChangeChannelAsync(Name, FromChannel, TrackerType.YoutubeLive, Context);
            }
        }

        [Group("TikTok")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class TikTok : ModuleBase<ShardedCommandContext>
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified TikTok channels videos, in the channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.TikTok)]
            public async Task trackYoutube(string username, [Remainder]string notificationMessage = "New Video")
            {
                using (Context.Channel.EnterTypingState())
                {
                    username = username.ToLower().Replace("@", "");
                    await Trackers[BaseTracker.TrackerType.TikTok].AddTrackerAsync(username, Context.Channel.Id, notificationMessage);

                    await ReplyAsync("Keeping track of " + username + "'s videos, from now on!");
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TikTok channels videos, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(BaseTracker username)
            {
                if (await Trackers[BaseTracker.TrackerType.TikTok].TryRemoveTrackerAsync(username.Name, username.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await ReplyAsync("Stopped keeping track of " + username.Name + "'s videos!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the TikTok channels that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following TikTok channels are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.TikTok].GetTrackersEmbed(Context.Channel.Id, true));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new video gets detected.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker username, [Remainder]string notification = "")
            {
                username.ChannelConfig[username.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.TikTok].UpdateDBAsync(username);
                await ReplyAsync($"Changed notification for `{username.Name}` to `{notification}`");
            }

            [Command("Check")]
            [Summary("Gets the first few videos from the TikTok channel.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Check(string username, int amount=5)
            {
                username = username.ToLower().Replace("@", "");
                var videos = (await TikTokTracker.GetClips(username)).Take(amount).Select(x => x.Last() + "\n" + x.First()).ToList();
                await ReplyAsync(string.Join("\n", videos));
            }

            [Command("ShowConfig")]
            [HideHelp]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker username)
            {
                await ReplyAsync($"```yaml\n{string.Join("\n", username.ChannelConfig[username.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig(BaseTracker ChannelID, [Remainder]string config)
            {
                await Tracking.ModifyConfig(this, ChannelID, BaseTracker.TrackerType.TikTok, config);
            }

            [Command("ChangeChannel", RunMode = RunMode.Async)]
            [Summary("Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel){
                await Tracking.ChangeChannelAsync(Name, FromChannel, TrackerType.TikTok, Context);
            }
        }


        [Command("PurgeTrackers", RunMode = RunMode.Async)]
        [RequireBotManage()]
        [HideHelp]
        public async Task PurgeTrackers(bool testing = true)
        {
            using (Context.Channel.EnterTypingState())
            {
                Dictionary<string, int> pruneCount = new Dictionary<string, int>();

                foreach (var trackerHandler in StaticBase.Trackers)
                {
                    pruneCount[trackerHandler.Key.ToString()] = 0;
                    foreach (var tracker in trackerHandler.Value.GetTrackerSet())
                    {
                        //Tracker empty due to race conditions
                        if(tracker.ChannelConfig.Count == 0){
                            pruneCount[trackerHandler.Key.ToString()]++;
                            if(!testing){
                                await trackerHandler.Value.AddTrackerAsync(tracker.Name, 158166244493623296);
                                await trackerHandler.Value.TryRemoveTrackerAsync(tracker.Name, 158166244493623296);
                            }
                            continue;
                        }

                        foreach (var channel in tracker.ChannelConfig.Keys.ToList())
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

                await ReplyAsync($"```yaml\n{"TrackerType",-20}{"PruneCount"}\n{string.Join("\n", pruneCount.Select(x => $"{x.Key,-20}{x.Value,-3}"))}```");
            }
        }

        [Command("PruneTrackers", RunMode = RunMode.Async)]
        [RequireBotManage()]
        [HideHelp]
        public async Task PruneTrackers(int days = 45, bool testing = true)
        {
            using (Context.Channel.EnterTypingState())
            {
                Dictionary<string, int> pruneCount = new Dictionary<string, int>();

                foreach (var trackerHandler in StaticBase.Trackers)
                {
                    pruneCount[trackerHandler.Key.ToString()] = 0;
                    foreach (var tracker in trackerHandler.Value.GetTrackerSet())
                    {
                        double inactiveDays = (DateTime.Now - tracker.LastActivity).TotalDays;
                        if (inactiveDays >= days)
                        {
                            if (!testing)
                            {
                                var embed = new EmbedBuilder();
                                embed.WithCurrentTimestamp().WithFooter($"{trackerHandler.Key.ToString()}Tracker");
                                embed.WithTitle($"Your tracker has been removed due to inactivity above {days} days!");
                                embed.WithColor(Discord.Color.Blue);
                                embed.WithDescription("Feel free to add this tracker again once the activity resumes.");
                                embed.AddField("Name", string.Join("", tracker.Name.Take(1000)));
                                embed.AddField("Inactivity", inactiveDays > 100000 ? "Ever since records began." : $"{(int)inactiveDays} days.");
                                var builtEmbed = embed.Build();

                                foreach (var channel in tracker.ChannelConfig.Keys.ToList())
                                {
                                    if (Program.Client.GetChannel(channel) != null)
                                    {
                                        try{
                                            await (Program.Client.GetChannel(channel) as ITextChannel).SendMessageAsync(embed: embed.Build());
                                        } catch(Exception e){
                                            await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"Could not send prune message to {channel}", e));
                                        }
                                    }
                                    await trackerHandler.Value.TryRemoveTrackerAsync(tracker.Name, channel);
                                }
                            }

                            pruneCount[trackerHandler.Key.ToString()]++;
                        }
                    }
                }

                await ReplyAsync($"```yaml\n{"TrackerType",-20}{"PruneCount"}\n{string.Join("\n", pruneCount.Select(x => $"{x.Key,-20}{x.Value,-3}"))}```");
            }
        }

        [Command("GetAllTrackers", RunMode = RunMode.Async)]
        [Summary("Returns an embed containing all trackers of all channels of this server.")]
        [HideHelp]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [Ratelimit(1, 1, Measure.Minutes, RatelimitFlags.GuildwideLimit)]
        public async Task GetAllTrackers()
        {
            using (Context.Channel.EnterTypingState())
            {
                await ReplyAsync("Following trackers currently exist on this server:");

                IEnumerable<Embed> pages = new List<Embed>();
                foreach (var tracker in StaticBase.Trackers)
                {
                    var curPages = tracker.Value.GetTrackersEmbed(Context.Channel.Id, true);
                    if (!(curPages.Count() == 1 && (curPages.First().Description?.Equals("") ?? true)))
                    {
                        pages = pages.Concat(curPages);
                    }
                }

                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, pages);
            }
        }

        public static async Task ModifyConfig(ModuleBase<ShardedCommandContext> context, BaseTracker tracker, TrackerType trackerType, string reply)
        {
            await context.Context.Channel.SendMessageAsync($"Current Config:\n```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[context.Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```\nPlease reply with one or more changed lines.");
            //var reply = await context.NextMessageAsync(true, true, TimeSpan.FromMinutes(5));
            var settings = tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[context.Context.Guild.Id]].ToDictionary(x => x.Key, x => x.Value);

            if (reply != null)
            {
                foreach (var line in reply.Split("\n"))
                {
                    var kv = line.Split(":", 2);
                    if (kv.Length != 2)
                    {
                        await context.Context.Channel.SendMessageAsync($"Skipping `{line}` due to no value.");
                        continue;
                    }

                    var option = kv[0];
                    if (!settings.Keys.Contains(option))
                    {
                        await context.Context.Channel.SendMessageAsync($"Skipping `{line}` due to unkown option.");
                        continue;
                    }

                    var value = kv[1].Trim();
                    var worked = TryCastUserConfig(settings[option], value, out var result);

                    if (!worked)
                    {
                        await context.Context.Channel.SendMessageAsync($"Skipping `{line}` due to false value type, must be `{settings[option].GetType().ToString()}`");
                    }
                    else
                    {
                        settings[option] = result;
                    }
                }

                if (!tracker.IsConfigValid(settings, out string reason))
                {
                    await context.Context.Channel.SendMessageAsync($"Updating failed due to:\n{reason}");
                }
                else
                {
                    tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[context.Context.Guild.Id]] = settings;
                    await StaticBase.Trackers[trackerType].UpdateDBAsync(tracker);
                    await context.Context.Channel.SendMessageAsync($"New Config:\n```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[context.Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```");
                }
            }
            else
            {
                await context.Context.Channel.SendMessageAsync($"No timely reply received.");
            }
        }

        public static bool TryCastUserConfig(object srcOption, string value, out object result)
        {
            var worked = true;
            result = null;
            switch (srcOption)
            {
                case bool b:
                    worked = bool.TryParse(value, out bool boolResult);
                    result = boolResult;
                    break;
                case string s:
                    result = value;
                    break;
                case double d:
                    worked = double.TryParse(value, out double doubleResult);
                    result = doubleResult;
                    break;
                case int i:
                    worked = int.TryParse(value, out int intResult);
                    result = intResult;
                    break;
                case uint ui:
                    worked = uint.TryParse(value, out uint uintResult);
                    result = uintResult;
                    break;
                default:
                    worked = false;
                    break;
            }
            return worked;
        }

        public static async Task ChangeChannelAsync(string Name, SocketGuildChannel FromChannel, TrackerType currentType, SocketCommandContext Context)
        {
            var tracker = StaticBase.Trackers[currentType].GetTracker(FromChannel.Id, BaseTracker.CapSensitive.Any(x => x == currentType) ? Name : Name.ToLower());

            if (tracker == null)
            {
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[currentType].GetTrackersEmbed(Context.Channel.Id, true));
                throw new Exception($"Could not find a {currentType.ToString()} Tracker called {Name} in {FromChannel.Name}");
            }
            else if (FromChannel.Id.Equals(Context.Channel.Id))
            {
                throw new Exception($"The tracker is in your current channel already.");
            }

            var currentConfig = tracker.ChannelConfig[FromChannel.Id];
            tracker.ChannelConfig[Context.Channel.Id] = currentConfig;
            await StaticBase.Trackers[currentType].TryRemoveTrackerAsync(tracker.Name, FromChannel.Id);
            await Context.Channel.SendMessageAsync($"Successfully changed the channel of {tracker.Name} from {((ITextChannel)FromChannel).Mention} to {((ITextChannel)Context.Channel).Mention}");
        }
    }
}