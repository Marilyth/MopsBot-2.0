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
using Discord.Addons.Interactive;

namespace MopsBot.Module
{
    public class VoterBeta : ModuleBase
    {
        [Group("YoutubeLive")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class YoutubeLive : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified Youtubers livestreams, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(2)]
            public async Task trackYoutube(string channelID, [Remainder]string notificationMessage = "New Stream")
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await Trackers[BaseTracker.TrackerType.YoutubeLive].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                        await ReplyAsync("Keeping track of " + channelID + "'s streams, from now on!");

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtubers streams, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(BaseTracker channelID)
            {
                if (await Trackers[BaseTracker.TrackerType.YoutubeLive].TryRemoveTrackerAsync(channelID.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + channelID.Name + "'s streams!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new stream goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker channelID, [Remainder]string notification = "")
            {
                channelID.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{channelID.Name}` to `{notification}`");
            }
        }

        [Group("Steam")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Steam : ModuleBase
        {
            [Command("Track", RunMode = RunMode.Async)]
            [Summary("Keeps track of the specified steam user, in the Channel you are calling this command in.\nWill notify on game changes and achievements.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
            [RequireUserVotepoints(2)]
            public async Task Track([Remainder]string SteamNameOrId)
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        SteamNameOrId = SteamNameOrId.ToLower();
                        await Trackers[BaseTracker.TrackerType.Steam].AddTrackerAsync(SteamNameOrId, Context.Channel.Id);
                        var worked = long.TryParse(SteamNameOrId, out long test);

                        await ReplyAsync("Keeping track of " + SteamNameOrId + $"'s Achievements and playing status from now on.");
                        if (!worked) await ReplyAsync($"Make sure this is you: https://steamcommunity.com/id/{SteamNameOrId}\nOtherwise use your steamid instead of steam name");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("**Error**: " + e.InnerException.Message);
                    }
                }
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Steam user, in the Channel you are calling this command in.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder]BaseTracker SteamNameOrId)
            {
                if (await Trackers[BaseTracker.TrackerType.Steam].TryRemoveTrackerAsync(SteamNameOrId.Name, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + SteamNameOrId.Name + "'s Steam data!");
            }

            [Command("GetTrackers")]
            [Summary("Returns the Steam users that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Steam users are currently being tracked:");
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, StaticBase.Trackers[BaseTracker.TrackerType.Steam].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new achievement was achieved.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(BaseTracker SteamNameOrId, [Remainder]string notification)
            {
                SteamNameOrId.ChannelMessages[Context.Channel.Id] = notification;
                await ReplyAsync($"Changed notification for `{SteamNameOrId.Name}` to `{notification}`");
            }
        }
    }
}
