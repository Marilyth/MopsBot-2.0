using Discord.WebSocket;
using Discord;
using Discord.Interactions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MopsBot.Module.Preconditions;
using System.Text.RegularExpressions;
using MopsBot.Data.Tracker;
using static MopsBot.StaticBase;
using MopsBot.Data.Entities;
using static MopsBot.Data.Tracker.BaseTracker;

namespace MopsBot.Module
{
    public class Slash : InteractionModuleBase<IInteractionContext>
    {
        #region Twitch
        [Group("twitch", "Commands for Twitch tracking")]
        [RequireBotPermission(ChannelPermission.ManageRoles)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitch : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified Streamer.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Twitch)]
            public async Task trackStreamer(string streamerName, string notificationMessage = "Stream went live!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    streamerName = streamerName.ToLower();
                    await Trackers[BaseTracker.TrackerType.Twitch].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                    await FollowupAsync("Keeping track of " + streamerName + "'s streams, from now on!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops tracking the specified streamer.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker streamerName)
            {
                if (await Trackers[BaseTracker.TrackerType.Twitch].TryRemoveTrackerAsync(streamerName.Name, streamerName.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + streamerName.Name + "'s streams!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following streamers are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a streamer goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker streamer, string notification = "")
            {
                streamer.ChannelConfig[streamer.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Twitch].UpdateDBAsync(streamer);
                await FollowupAsync($"Changed notification for `{streamer.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see what options you have.")]
            [Modal]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker streamerName)
            {
                string currentConfig = string.Join("\n", streamerName.ChannelConfig[streamerName.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value));
                var reply = await CommandHandler.SendAndAwaitModalAsync(Context, MopsBot.Module.Modals.ModalBuilders.GetConfigModal(currentConfig));

                await ModifyConfig(this, streamerName, TrackerType.TwitchClip, reply["new_config"]);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Twitch, Context);
                await FollowupAsync($"Successfully changed the channel of {Name} from {((ITextChannel)FromChannel).Mention} to {((ITextChannel)Context.Channel).Mention}", ephemeral: true);
            }

            [SlashCommand("grouptrackers", "Adds all trackers of the guild to a unified embed, in the channel you are calling this command in.")]
            public async Task GroupTrackers(SocketRole rank = null)
            {
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].AddTrackerAsync(Context.Guild.Id.ToString(), Context.Channel.Id);
                if (rank != null)
                {
                    var tracker = StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].GetTracker(Context.Channel.Id, Context.Guild.Id.ToString()) as TwitchGroupTracker;
                    tracker.RankChannels[Context.Channel.Id] = rank.Id;
                    await StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].UpdateDBAsync(tracker);
                }
                await FollowupAsync("Added group tracking for this channel", ephemeral: true);
            }

            [SlashCommand("ungrouptrackers", "Removes grouping for trackers")]
            public async Task UnGroupTrackers()
            {
                (StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].GetTracker(Context.Channel.Id, Context.Guild.Id.ToString()) as TwitchGroupTracker).RankChannels.Remove(Context.Channel.Id);
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchGroup].TryRemoveTrackerAsync(Context.Guild.Id.ToString(), Context.Channel.Id);
                await FollowupAsync("Removed group tracking in this channel", ephemeral: true);
            }
        }
        #endregion Twitch

        #region TwitchClip
        [Group("twitchclip", "Commands for twitch clip tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class TwitchClip : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified streamer's top clips, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.TwitchClip)]
            public async Task trackClips(string streamerName, string notificationMessage = "New trending clip found!")
            {
                using (Context.Channel.EnterTypingState())
                {
                    streamerName = streamerName.ToLower();
                    await Trackers[BaseTracker.TrackerType.TwitchClip].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                    await FollowupAsync("Keeping track of " + streamerName + "'s top clips above **2** views every 30 minutes, from now on!\nUse the `SetViewThreshold` subcommand to change the threshold.", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops tracking the specified streamer's clips.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker streamerName)
            {
                if (await Trackers[BaseTracker.TrackerType.TwitchClip].TryRemoveTrackerAsync(streamerName.Name, streamerName.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + streamerName.Name + "'s streams!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.TwitchClip].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following streamers are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a new clip is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker streamer, string notification = "")
            {
                streamer.ChannelConfig[streamer.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchClip].UpdateDBAsync(streamer);
                await FollowupAsync($"Changed notification for `{streamer.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("setviewthreshold", "Sets the minimum views a top clip must have to be shown.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetViewThreshold([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker streamer, uint threshold)
            {
                var tracker = (TwitchClipTracker)streamer;

                tracker.ChannelConfig[streamer.LastCalledChannelPerGuild[Context.Guild.Id]][TwitchClipTracker.VIEWTHRESHOLD] = threshold;
                await StaticBase.Trackers[BaseTracker.TrackerType.TwitchClip].UpdateDBAsync(tracker);
                await FollowupAsync($"Will only notify about clips equal or above **{threshold}** views for `{streamer.Name}` now.", ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see what options you have.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker streamerName, string config)
            {
                await ModifyConfig(this, streamerName, TrackerType.TwitchClip, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.TwitchClip, Context);
            }
        }
        #endregion TwitchClip

        #region Twitter
        [Group("twitter", "Commands for twitter tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitter : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified TwitterUser, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Twitter)]
            public async Task trackTwitter(string twitterUser, string tweetNotification = "~Tweet Tweet~")
            {
                twitterUser = twitterUser.ToLower().Replace("@", "");
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[TrackerType.Twitter].AddTrackerAsync(twitterUser, Context.Channel.Id, tweetNotification);

                    await FollowupAsync("Keeping track of " + twitterUser + "'s tweets, replies and retweets, from now on!\nTo disable replies and retweets, please use the `Twitter DisableNonMain` subcommand!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops keeping track of the specified TwitterUser, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker twitterUser)
            {
                if (await Trackers[BaseTracker.TrackerType.Twitter].TryRemoveTrackerAsync(twitterUser.Name, twitterUser.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + twitterUser.Name + "'s tweets!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the twitters that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.Twitter].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following twitter accounts are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a new Main-Tweet is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker TwitterName, string notification = "")
            {
                TwitterName.GetLastCalledConfig(Context.Guild.Id)["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Twitter].UpdateDBAsync(TwitterName);
                await FollowupAsync($"Set notification for main tweets, for `{TwitterName.Name}`, to {notification}!", ephemeral: true);
            }

            [SlashCommand("setnonmainnotification", "Sets the notification text that is used each time a new retweet or reply is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNonMainNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker TwitterName, string notification = "")
            {
                var config = TwitterName.GetLastCalledConfig(Context.Guild.Id);
                config[TwitterTracker.REPLYNOTIFICATION] = notification;
                config[TwitterTracker.RETWEETNOTIFICATION] = notification;
                config[TwitterTracker.SHOWREPLIES] = true;
                config[TwitterTracker.SHOWRETWEETS] = true;
                await StaticBase.Trackers[TrackerType.Twitter].UpdateDBAsync(TwitterName);
                await FollowupAsync($"Set notification for retweets and replies, for `{TwitterName.Name}`, to {notification}!", ephemeral: true);
            }

            [SlashCommand("disablenonmain", "Disables tracking for the retweets and replies of the specified Twitter account.")]
            public async Task DisableRetweets([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker TwitterName)
            {
                TwitterName.GetLastCalledConfig(Context.Guild.Id)[TwitterTracker.SHOWREPLIES] = false;
                TwitterName.GetLastCalledConfig(Context.Guild.Id)[TwitterTracker.SHOWRETWEETS] = false;
                await StaticBase.Trackers[TrackerType.Twitter].UpdateDBAsync(TwitterName);
                await FollowupAsync($"Disabled retweets and replies for `{TwitterName.Name}`!\nTo reenable retweets and replies, please provide a notification via the `Twitter SetNonMainNotification` subcommand!", ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.GetLastCalledConfig(Context.Guild.Id).Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see what options you have.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker TwitterName, string config)
            {
                await ModifyConfig(this, TwitterName, TrackerType.Twitter, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Twitter, Context);
            }

            [SlashCommand("prune", "Prune trackers")]
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
                        await FollowupAsync($"```yaml\n{result}```", ephemeral: true);
                    else
                        await FollowupAsync($"```Pruned {totalCount} trackers```", ephemeral: true);
                }
            }
        }
        #endregion Twitter

        #region Osu
        [Group("osu", "Commands for osu tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Osu : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified Osu player, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Osu)]
            public async Task trackOsu(string OsuUser)
            {
                using (Context.Channel.EnterTypingState())
                {
                    OsuUser = OsuUser.ToLower();
                    await Trackers[BaseTracker.TrackerType.Osu].AddTrackerAsync(OsuUser, Context.Channel.Id);

                    await FollowupAsync("Keeping track of " + OsuUser + "'s plays above `0.1pp` gain, from now on!\nYou can change the lower pp boundary by using the `Osu SetPPBounds` subcommand!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops keeping track of the specified Osu player, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu( BaseTracker OsuUser)
            {
                if (await Trackers[BaseTracker.TrackerType.Osu].TryRemoveTrackerAsync(OsuUser.Name, OsuUser.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + OsuUser.Name + "'s plays!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the Osu players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.Osu].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following players are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setppbounds", "Sets the lower bounds of pp gain that must be reached, to show a notification.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetPPBounds([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker osuUser, double threshold)
            {
                var tracker = osuUser as OsuTracker;
                if (threshold > 0.1)
                {
                    tracker.ChannelConfig[osuUser.LastCalledChannelPerGuild[Context.Guild.Id]][OsuTracker.PPTHRESHOLD] = threshold;
                    await StaticBase.Trackers[BaseTracker.TrackerType.Osu].UpdateDBAsync(tracker);
                    await FollowupAsync($"Changed threshold for `{osuUser}` to `{threshold}`", ephemeral: true);
                }
                else
                    await FollowupAsync("Threshold must be above 0.1!", ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a player gained pp.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker osuUser, string notification = "")
            {
                osuUser.ChannelConfig[osuUser.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Osu].UpdateDBAsync(osuUser);
                await FollowupAsync($"Changed notification for `{osuUser.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see what options you have.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker osuUser, string config)
            {
                await ModifyConfig(this, osuUser, TrackerType.Osu, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Osu, Context);
            }
        }
        #endregion Osu

        #region YouTube
        [Group("youtube", "Commands for YouTube tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Youtube : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified Youtuber, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Youtube)]
            public async Task trackYoutube(string channelID,  string notificationMessage = "New Video")
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.Youtube].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                    await FollowupAsync("Keeping track of " + channelID + "'s videos, from now on!\nThis tracker **only tracks videos, no livestreams!**\nFor livestreams, start a `YoutubeLive` tracker, not a `Youtube` tracker!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops keeping track of the specified Youtuber, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker channelID)
            {
                if (await Trackers[BaseTracker.TrackerType.Youtube].TryRemoveTrackerAsync(channelID.Name, channelID.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + channelID.Name + "'s videos!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.Youtube].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following youtubers are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a new video appears.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker channelID,  string notification = "")
            {
                channelID.ChannelConfig[channelID.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Youtube].UpdateDBAsync(channelID);
                await FollowupAsync($"Changed notification for `{channelID.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see what options you have.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker channelID, string config)
            {
                await ModifyConfig(this, channelID, TrackerType.Youtube, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Youtube, Context);
            }
        }
        #endregion YouTube

        #region Reddit
        [Group("reddit", "Commands for reddit tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Reddit : InteractionModuleBase<IInteractionContext>
        {
            //https://www.reddit.com/wiki/search
            [SlashCommand("track", "Keeps track of the specified Subreddit, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Reddit)]
            public async Task trackSubreddit(string subreddit,  string query = null)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.Reddit].AddTrackerAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                    await FollowupAsync("Keeping track of " + subreddit + $"'s posts, from now on, using {query}!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops tracking the specified Subreddit.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackSubreddit( BaseTracker subreddit)
            {
                if (await Trackers[BaseTracker.TrackerType.Reddit].TryRemoveTrackerAsync(subreddit.Name, subreddit.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + subreddit.Name + "'s posts!", ephemeral: true);
                else
                {
                    var embeds = StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id, true);
                    await FollowupAsync($"Could not find tracker for `{subreddit.Name}`\n" +
                                     $"Currently tracked Subreddits are:", embeds: embeds.ToArray());
                }
            }

            [SlashCommand("gettrackers", "Returns the subreddits that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following subreddits are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker subreddit, string notification = "")
            {
                if (await StaticBase.Trackers[BaseTracker.TrackerType.Reddit].TrySetNotificationAsync(subreddit.Name, subreddit.LastCalledChannelPerGuild[Context.Guild.Id], notification))
                {
                    await FollowupAsync($"Changed notification for `{subreddit.Name}` to `{notification}`", ephemeral: true);
                }
                else
                {
                    var embeds = StaticBase.Trackers[BaseTracker.TrackerType.Reddit].GetTrackersEmbed(Context.Channel.Id, true);
                    await FollowupAsync($"Could not find tracker for `{subreddit.Name}`\n" +
                                     $"Currently tracked subreddits are:", embeds: embeds.ToArray());
                }
            }

            [SlashCommand("check", "Returns the newest `limit` entries using the `subreddit` and `query` provided.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Check(int limit, string subreddit,  string query = null)
            {
                var result = await RedditTracker.checkReddit(subreddit, query, limit);
                await FollowupAsync(embeds: result.ToArray());
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig( BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Reddit, Context);
            }
        }
        #endregion Reddit

        #region JSON
        [Group("json", "Commands for JSON tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class JSON : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the Json, using the specified locations.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.JSON)]
            public async Task trackJson(string source, string paths)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.JSON].AddTrackerAsync(String.Join("|||", new string[] { source, paths }), Context.Channel.Id);
                    await FollowupAsync($"Keeping track of `{source}`'s attributes from now on!", ephemeral: true);
                }
            }

            [SlashCommand("trackextended", "Keeps track of the Json, as a POST request with body, using the specified locations.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.JSON)]
            public async Task trackExtended(string source, string body, string paths)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.JSON].AddTrackerAsync(String.Join("|||", new string[] { source + "||" + body, paths }), Context.Channel.Id);
                    await FollowupAsync($"Keeping track of `{source}`'s attributes from now on!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops tracking jsons.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackNews([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker JsonSource)
            {
                if (await Trackers[BaseTracker.TrackerType.JSON].TryRemoveTrackerAsync(JsonSource.Name, JsonSource.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync($"Stopped keeping track of {JsonSource.Name}", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the jsons that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.JSON].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following jsons are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a change in the json was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker jsonSource, string notification = "")
            {
                jsonSource.ChannelConfig[jsonSource.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.JSON].UpdateDBAsync(jsonSource);
                await FollowupAsync($"Changed notification for `{jsonSource.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("check", "Checks the json for the specified paths, and returns the values")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Check(string Url, string paths)
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
                await FollowupAsync(embed: embed.Build(), ephemeral: true);
            }

            [SlashCommand("checkextended", "Checks the json for the specified paths, and returns the values")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task CheckExtended(string Url, string body, string paths)
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
                await FollowupAsync(embed: embed.Build(), ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.JSON, Context);
            }
        }
        #endregion JSON

        #region OSRS
        [Group("osrs", "Commands for tracking old school runescape")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class OSRS : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the stats of the OSRS player.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.OSRS)]
            public async Task Track(string name, string notification = "")
            {
                name = name.ToLower();
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.OSRS].AddTrackerAsync(name, Context.Channel.Id);
                    await FollowupAsync($"Keeping track of `{name}` stats after each playsession, from now on!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops tracking the player with the specified name.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrack([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker name)
            {
                if (await Trackers[BaseTracker.TrackerType.OSRS].TryRemoveTrackerAsync(name.Name, name.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync($"Stopped keeping track of {name.Name}!", ephemeral: true);
            }

            [SlashCommand("getstats", "Gets all top 2kk stats of the specified player.")]
            public async Task GetStats(string name)
            {
                await FollowupAsync("", embed: await OSRSTracker.GetStatEmbed(name), ephemeral: true);
            }

            [SlashCommand("compare", "Compares the stats of 2 players.")]
            public async Task Compare(string name1, string name2)
            {
                await FollowupAsync("", embed: await OSRSTracker.GetCompareEmbed(name1, name2), ephemeral: true);
            }

            [SlashCommand("getitem", "Gets information on an Item")]
            public async Task GetItem(string name)
            {
                await FollowupAsync("", embed: await OSRSTracker.GetItemEmbed(name), ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.OSRS].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following players are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a level up takes place.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker name, string notification = "")
            {
                name.ChannelConfig[name.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.OSRS].UpdateDBAsync(name);
                await FollowupAsync($"Changed notification for `{name.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see your options.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker name, string config)
            {
                await ModifyConfig(this, name, TrackerType.OSRS, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.OSRS, Context);
            }
        }
        #endregion OSRS

        #region HTML
        [Group("html", "Commands for html tracking via regex")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [Ratelimit(1, 60, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
        public class HTML : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("trackregex", "Tracks regex on a webpage. Use () around the text you want to track to signify a match.")]
            [TrackerLimit(TrackerType.HTML)]
            public async Task TrackRegex(string website, string scrapeRegex)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.HTML].AddTrackerAsync(website + "|||" + scrapeRegex, Context.Channel.Id);
                    await FollowupAsync($"Keeping track of `{website}` data using ```html\n{scrapeRegex}```, from now on!\n\nInitial value was: **{await HTMLTracker.FetchData(website + "|||" + scrapeRegex)}**", ephemeral: true);
                }
            }

            /*
            [SlashCommand("Track", RunMode = RunMode.Async)]
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

                        await FollowupAsync("Which page is the one you want to track?\nIf none are specific enough, consider extending the context, or writing your own regex using the `TrackRegex` subcommand.", ephemeral: true);
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

                        await FollowupAsync($"Is there anything, for the sake of context, that you want to have removed (e.g. tracking highest level, but don't want it to be bound to a certain name)?\n\n```html\n{scrapeRegex}```\n\nIf so, please enter the exact texts you want to be generic instead of fixed in a **comma seperated list**.", ephemeral: true);
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

            [SlashCommand("testregex", "Tests the regex and returns it's value. Check your regex before tracking with it!")]
            public async Task Test(string website, string scrapeRegex)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await FollowupAsync($"Regex returned value: {await HTMLTracker.FetchData(website + "|||" + scrapeRegex)}", ephemeral: true);
                }
            }

            /*
            [SlashCommand("UnTrack", RunMode = RunMode.Async)]
            [Summary("Creates a paginator of all trackers, out of which you have to choose one.")]
            public async Task UnTrack()
            {
                using (Context.Channel.EnterTypingState())
                {
                    var trackers = Trackers[BaseTracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList();
                    await Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, trackers.Select(x => $"```html\n{x.Name}```"));
                    await FollowupAsync("Which tracker do you want to delete?\nPlease enter the page number", ephemeral: true);

                    int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;
                    if (await Trackers[BaseTracker.TrackerType.HTML].TryRemoveTrackerAsync(trackers[page].Name, Context.Channel.Id))
                        await FollowupAsync($"Stopped keeping track of result {page + 1} of paginator!", ephemeral: true);
                }
            }*/

            [SlashCommand("untrackall", "Untracks all trackers in the current channel.")]
            public async Task UnTrackAll()
            {
                foreach (var tracker in Trackers[BaseTracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList())
                {
                    if (await Trackers[BaseTracker.TrackerType.HTML].TryRemoveTrackerAsync(tracker.Name, Context.Channel.Id))
                        await FollowupAsync($"Stopped keeping track of {tracker.Name.Split("|||")[0]}!", ephemeral: true);
                }
            }

            /*
            [SlashCommand("SetNotification", RunMode = RunMode.Async)]
            [Summary("Sets the notification for when the text of a regex match changes.\nRequires only the notification, paginator will guide you.")]
            public async Task SetNotification([Remainder] string notification = "")
            {
                using (Context.Channel.EnterTypingState())
                {
                    var trackers = Trackers[BaseTracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList();
                    await Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, trackers.Select(x => $"```html\n{x.Name}```"));
                    await FollowupAsync("Which tracker do you want to set the notification for?\nPlease enter the page number", ephemeral: true);

                    int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;
                    if (await Trackers[BaseTracker.TrackerType.HTML].TrySetNotificationAsync(trackers[page].Name, Context.Channel.Id, notification))
                        await FollowupAsync($"Set notification for result {page + 1} of paginator to `{notification}`!", ephemeral: true);
                }
            }

            [SlashCommand("ChangeConfig", RunMode = RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig()
            {
                using (Context.Channel.EnterTypingState())
                {
                    var trackers = Trackers[BaseTracker.TrackerType.HTML].GetTrackers(Context.Channel.Id).ToList();
                    await Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, trackers.Select(x => $"```html\n{x.Name}```"));
                    await FollowupAsync("Which tracker do you want to change the config for?\nPlease enter the page number", ephemeral: true);

                    int page = int.Parse((await NextMessageAsync(timeout: new TimeSpan(0, 5, 0))).Content) - 1;
                    await ModifyConfig(this, trackers[page], TrackerType.HTML);
                }
            }*/
        }
        #endregion HTML

        #region RSS
        [Group("rss", "Commands for rss tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class RSS : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified RSS feed url")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.RSS)]
            public async Task TrackRSS(string url, string notification = "")
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.RSS].AddTrackerAsync(url, Context.Channel.Id, notification);

                    await FollowupAsync("Keeping track of " + url + $"'s feed, from now on!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops tracking the specified RSS feed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrackFeed([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker url)
            {
                if (await Trackers[BaseTracker.TrackerType.RSS].TryRemoveTrackerAsync(url.Name, url.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + url.Name + " 's feed!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the feeds that are tracked in the current channel.")]
            public async Task GetTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.RSS].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following feeds are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker url, string notification = "")
            {
                url.ChannelConfig[url.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.RSS].UpdateDBAsync(url);
                await FollowupAsync($"Changed notification for `{url.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("check", "Returns the newest entry in the rss feed")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Check(string rssFeed)
            {
                var result = await RSSTracker.GetFeed(rssFeed);
                await FollowupAsync(embed: result, ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see your options.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker url, string config)
            {
                await ModifyConfig(this, url, TrackerType.RSS, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.RSS, Context);
            }
        }
        #endregion RSS

        #region Steam
        [Group("steam", "Commands for Steam tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Steam : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified steam user, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.Steam)]
            public async Task Track(string SteamNameOrId)
            {
                using (Context.Channel.EnterTypingState())
                {
                    SteamNameOrId = SteamNameOrId.ToLower();
                    await Trackers[BaseTracker.TrackerType.Steam].AddTrackerAsync(SteamNameOrId, Context.Channel.Id);
                    var worked = long.TryParse(SteamNameOrId, out long test);

                    await FollowupAsync("Keeping track of " + SteamNameOrId + $"'s Achievements and playing status from now on.", ephemeral: true);
                    if (!worked) await FollowupAsync($"Make sure this is you: https://steamcommunity.com/id/{SteamNameOrId}\nOtherwise use your steamid instead of steam name", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops keeping track of the specified Steam user, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker SteamNameOrId)
            {
                if (await Trackers[BaseTracker.TrackerType.Steam].TryRemoveTrackerAsync(SteamNameOrId.Name, SteamNameOrId.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + SteamNameOrId.Name + "'s Steam data!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the Steam users that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.Steam].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following players are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a new achievement was achieved.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker SteamNameOrId, string notification = "")
            {
                SteamNameOrId.ChannelConfig[SteamNameOrId.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.Steam].UpdateDBAsync(SteamNameOrId);
                await FollowupAsync($"Changed notification for `{SteamNameOrId.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker SteamNameOrId, string config)
            {
                await ModifyConfig(this, SteamNameOrId, TrackerType.Steam, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel)
            {
                await ChangeChannelAsync(Name, FromChannel, TrackerType.Steam, Context);
            }
        }
        #endregion Steam

        #region YoutubeLive
        [Group("youtubelive", "Commands for youtube livestream tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class YoutubeLive : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified Youtubers livestreams, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(0)]
            [TrackerLimit(TrackerType.YoutubeLive)]
            public async Task trackYoutube(string channelID, string notificationMessage = "New Stream")
            {
                using (Context.Channel.EnterTypingState())
                {
                    await Trackers[BaseTracker.TrackerType.YoutubeLive].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                    await FollowupAsync("Keeping track of " + channelID + "'s streams, from now on!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops keeping track of the specified Youtubers, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker channelID)
            {
                if (await Trackers[BaseTracker.TrackerType.YoutubeLive].TryRemoveTrackerAsync(channelID.Name, channelID.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + channelID.Name + "'s streams!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following youtubers are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a new stream goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker channelID, string notification = "")
            {
                channelID.ChannelConfig[channelID.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].UpdateDBAsync(channelID);
                await FollowupAsync($"Changed notification for `{channelID.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker tracker)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see your options.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker ChannelID, string config)
            {
                await ModifyConfig(this, ChannelID, BaseTracker.TrackerType.YoutubeLive, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel){
                await ChangeChannelAsync(Name, FromChannel, TrackerType.YoutubeLive, Context);
            }
        }
        #endregion YoutubeLive

        #region TikTok
        [Group("tiktok", "Commands for TikTok tracking")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class TikTok : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("track", "Keeps track of the specified TikTok channels videos, in the channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [TrackerLimit(TrackerType.TikTok)]
            public async Task trackYoutube(string username, string notificationMessage = "New Video")
            {
                using (Context.Channel.EnterTypingState())
                {
                    username = username.ToLower().Replace("@", "");
                    await Trackers[BaseTracker.TrackerType.TikTok].AddTrackerAsync(username, Context.Channel.Id, notificationMessage);

                    await FollowupAsync("Keeping track of " + username + "'s videos, from now on!", ephemeral: true);
                }
            }

            [SlashCommand("untrack", "Stops keeping track of the specified TikTok channels videos.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker username)
            {
                if (await Trackers[BaseTracker.TrackerType.TikTok].TryRemoveTrackerAsync(username.Name, username.LastCalledChannelPerGuild[Context.Guild.Id]))
                    await FollowupAsync("Stopped keeping track of " + username.Name + "'s videos!", ephemeral: true);
            }

            [SlashCommand("gettrackers", "Returns the TikTok channels that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                var embeds = StaticBase.Trackers[BaseTracker.TrackerType.TikTok].GetTrackersEmbed(Context.Channel.Id, true);
                await FollowupAsync("Following tiktokers are currently being tracked:", embeds.ToArray(), ephemeral: true);
            }

            [SlashCommand("setnotification", "Sets the notification text that is used each time a new video gets detected.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker username, string notification = "")
            {
                username.ChannelConfig[username.LastCalledChannelPerGuild[Context.Guild.Id]]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.TikTok].UpdateDBAsync(username);
                await FollowupAsync($"Changed notification for `{username.Name}` to `{notification}`", ephemeral: true);
            }

            [SlashCommand("check", "Gets the first few videos from the TikTok channel.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Check(string username, int amount=5)
            {
                username = username.ToLower().Replace("@", "");
                var videos = (await TikTokTracker.GetClips(username)).Take(amount).Select(x => x.Last() + "\n" + x.First()).ToList();
                await FollowupAsync(string.Join("\n", videos), ephemeral: true);
            }

            [SlashCommand("showconfig", "Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker username)
            {
                await FollowupAsync($"```yaml\n{string.Join("\n", username.ChannelConfig[username.LastCalledChannelPerGuild[Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```", ephemeral: true);
            }

            [SlashCommand("changeconfig", "Edit the Configuration for the tracker. Use showconfig to see your options.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task ChangeConfig([Autocomplete(typeof(TrackerAutocompleteHandler))] BaseTracker ChannelID, string config)
            {
                await ModifyConfig(this, ChannelID, BaseTracker.TrackerType.TikTok, config);
            }

            [SlashCommand("changechannel", "Changes the channel of the specified tracker from #FromChannel to the current channel")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task ChangeChannel(string Name, SocketGuildChannel FromChannel){
                await ChangeChannelAsync(Name, FromChannel, TrackerType.TikTok, Context);
            }
        }
        #endregion TikTok

        #region Statics
        public static async Task ChangeChannelAsync(string Name, SocketGuildChannel FromChannel, TrackerType currentType, IInteractionContext Context)
        {
            var tracker = StaticBase.Trackers[currentType].GetTracker(FromChannel.Id, BaseTracker.CapSensitive.Any(x => x == currentType) ? Name : Name.ToLower());

            if (tracker == null)
            {
                throw new Exception($"Could not find a {currentType.ToString()} Tracker called {Name} in {FromChannel.Name}.\nPlease use `/{currentType.ToString().ToLower()} gettrackers` to see available trackers.");
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

        public static async Task ModifyConfig(InteractionModuleBase<IInteractionContext> context, BaseTracker tracker, TrackerType trackerType, string reply)
        {
            //await context.Context.Channel.SendMessageAsync($"Current Config:\n```yaml\n{string.Join("\n", tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[context.Context.Guild.Id]].Select(x => x.Key + ": " + x.Value))}```\nPlease reply with one or more changed lines.");
            //var reply = await context.NextMessageAsync(true, true, TimeSpan.FromMinutes(5));
            var settings = tracker.ChannelConfig[tracker.LastCalledChannelPerGuild[context.Context.Guild.Id]].ToDictionary(x => x.Key, x => x.Value);

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

        #endregion Statics
    }
}
