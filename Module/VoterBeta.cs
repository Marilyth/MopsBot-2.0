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
        public class YoutubeLive : InteractiveBase
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
                channelID.ChannelConfig[Context.Channel.Id]["Notification"] = notification;
                await StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].UpdateDBAsync(channelID);
                await ReplyAsync($"Changed notification for `{channelID.Name}` to `{notification}`");
            }

            [Command("ShowConfig")]
            [Hide]
            [Summary("Shows all the settings for this tracker, and their values")]
            public async Task ShowConfig(BaseTracker tracker){
                await ReplyAsync($"```yaml\n{string.Join("\n", tracker.ChannelConfig[Context.Channel.Id].Select(x => x.Key + ": " + x.Value))}```");
            }

            [Command("ChangeConfig", RunMode=RunMode.Async)]
            [Summary("Edit the Configuration for the tracker")]
            public async Task ChangeConfig(BaseTracker ChannelID){
                await ReplyAsync($"Current Config:\n{string.Join("\n", ChannelID.ChannelConfig[Context.Channel.Id].Select(x => x.Key + ": " + x.Value))}\n\nPlease reply with one or more changed lines.");
                var reply = await NextMessageAsync(new EnsureSourceUserCriterion(), TimeSpan.FromMinutes(5));
                var settings = ChannelID.ChannelConfig[Context.Channel.Id];
                if(reply != null){
                    foreach(var line in reply.Content.Split("\n")){
                        var kv = line.Split(":",2);
                        if(kv.Length != 2){
                            await ReplyAsync($"Skipping `{line}` due to no value.");
                            continue;
                        }

                        var option = kv[0];
                        if(!settings.Keys.Contains(option)){
                            await ReplyAsync($"Skipping `{line}` due to unkown option.");
                            continue;
                        }

                        var value = kv[1].Trim();  
                        var worked = Tracking.TryCastUserConfig(settings[option], value, out var result);

                        if(!worked){
                            await ReplyAsync($"Skipping `{line}` due to false value type, must be `{settings[option].GetType().ToString()}`");
                        }else{
                            settings[option] = result;
                        }
                    }
                    await StaticBase.Trackers[BaseTracker.TrackerType.YoutubeLive].UpdateDBAsync(ChannelID);
                    await ReplyAsync($"New Config:\n```yaml\n{string.Join("\n", ChannelID.ChannelConfig[Context.Channel.Id].Select(x => x.Key + ": " + x.Value))}```");
                }else{
                    await ReplyAsync($"No timely reply received.");
                }
            }
        }
    }
}
