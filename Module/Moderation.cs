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
using static MopsBot.StaticBase;

namespace MopsBot.Module
{
    public class Moderation : ModuleBase
    {
        [Group("Role")]
        public class Role : ModuleBase
        {
            [Command("join")]
            [Summary("Joins the specified role")]
            public async Task joinRole([Remainder]string role)
            {
                SocketRole pRole = (SocketRole)Context.Guild.Roles.First(x => x.Name.ToLower().Equals(role.ToLower()));

                await ((SocketGuildUser)Context.User).AddRoleAsync(pRole);

                await ReplyAsync($"You are now part of the {pRole.Name} role! Yay!");
            }

            [Command("leave")]
            [Summary("Leaves the specified role")]
            public async Task leaveRole([Remainder]string role)
            {
                SocketRole pRole = (SocketRole)Context.Guild.Roles.First(x => x.Name.ToLower().Equals(role.ToLower()));

                await ((SocketGuildUser)Context.User).RemoveRoleAsync(pRole);

                await ReplyAsync($"You left the {pRole.Name} role.");
            }
        }

        [Command("poll"), Summary("Creates a poll\nExample: !poll Am I sexy?;Yes:No;@Panda @Demon @Snail")]
        public async Task Poll([Remainder] string Poll)
        {
            if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.Administrator)
                return;

            string[] pollSegments = Poll.Split(';');
            List<IGuildUser> participants = getMentionedUsers((CommandContext)Context);

            poll = new Data.Session.Poll(pollSegments[0], pollSegments[1].Split(':'), participants.ToArray());

            foreach (IGuildUser part in participants)
            {
                string output = "";
                for (int i = 0; i < poll.answers.Length; i++)
                {
                    output += $"\n``{i + 1}`` {poll.answers[i]}";
                }
                try
                {
                    await part.GetOrCreateDMChannelAsync().Result.SendMessageAsync($"{Context.User.Username} has created a poll:\n\n📄: {poll.question}\n{output}\n\nTo vote, simply PM me the **Number** of the answer you agree with.");
                }
                catch { }
            }

            await Context.Channel.SendMessageAsync("Poll started, Participants notified!");
        }

        [Command("pollEnd"), Summary("Ends the poll and returns the results.")]
        public async Task PollEnd()
        {
            if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.Administrator)
                return;

            await ReplyAsync(poll.pollToText());

            foreach (IGuildUser part in poll.participants)
            {
                await part.GetOrCreateDMChannelAsync().Result.SendMessageAsync($"📄:{poll.question}\n\nHas ended without your participation, sorry!");
                poll.participants.Remove(part);
            }

            poll = null;
        }
        [Group("Twitter")]
        public class Twitter : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannel)]
            public async Task trackTwitter(string twitterUser)
            {
                twitterTracks.addTracker(twitterUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannel)]
            public async Task unTrackTwitter(string twitterUser)
            {
                twitterTracks.removeTracker(twitterUser, Context.Channel.Id);

                await ReplyAsync("Stopped keeping track of " + twitterUser + "'s tweets!");
            }

            [Command("GetTracks")]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following twitters are currently being tracked:\n``" + StaticBase.twitterTracks.getTracker(Context.Channel.Id) + "``");
            }
        }

        [Group("Youtube")]
        public class Youtube : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannel)]
            public async Task trackTwitter(string channelID)
            {
                YoutubeTracks.addTracker(channelID, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + channelID + "'s videos, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannel)]
            public async Task unTrackYoutube(string channelID)
            {
                twitterTracks.removeTracker(channelID, Context.Channel.Id);

                await ReplyAsync("Stopped keeping track of " + channelID + "'s tweets!");
            }

            [Command("GetTracks")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:\n``" + StaticBase.YoutubeTracks.getTracker(Context.Channel.Id) + "``");
            }
        }
        [Group("Twitch")]
        public class Twitch : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannel)]
            public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage="Stream went live!")
            {
                streamTracks.addTracker(streamerName, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannel)]
            public async Task unTrackStreamer(string streamerName)
            {
                streamTracks.removeTracker(streamerName, Context.Channel.Id);

                await ReplyAsync("Stopped tracking " + streamerName + "'s streams!");
            }

            [Command("GetTracks")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following streamers are currently being tracked:\n``" + StaticBase.streamTracks.getTracker(Context.Channel.Id) + "``");
            }
        }
        [Group("Overwatch")]
        public class Overwatch : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannel)]
            public async Task trackOW(string owUser)
            {
                OverwatchTracks.addTracker(owUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannel)]
            public async Task unTrackOW(string owUser)
            {
                OverwatchTracks.removeTracker(owUser, Context.Channel.Id);

                await ReplyAsync("Stopped keeping track of " + owUser + "'s stats!");
            }

            [Command("GetStats")]
            [Summary("Returns an embed representating the stats of the specified Overwatch player")]
            public async Task GetStats(string owUser)
            {
                await ReplyAsync("Stats fetched:", false, (Embed)Data.Session.OverwatchTracker.overwatchInformation(owUser));
            }

            [Command("GetTracks")]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTracks()
            {
                await ReplyAsync("Following players are currently being tracked:\n``" + StaticBase.OverwatchTracks.getTracker(Context.Channel.Id) + "``");
            }
        }

        [Command("trackClips")]
        [Summary("Keeps track of clips from streams of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task trackClips(string streamerName)
        {
            ClipTracker.addTracker(streamerName, Context.Channel.Id);

            await ReplyAsync("Keeping track of clips of " + streamerName + "'s streams, from now on!");
        }


        [Command("setPrefix")]
        [Summary("Changes the prefix of Mops in the current Guild")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task setPrefix([Remainder]string prefix)
        {
            string oldPrefix;

            if (guildPrefix.ContainsKey(Context.Guild.Id))
            {
                oldPrefix = guildPrefix[Context.Guild.Id];
                guildPrefix[Context.Guild.Id] = prefix;
            }

            else
            {
                oldPrefix = "!";
                guildPrefix.Add(Context.Guild.Id, prefix);
            }

            savePrefix();

            await ReplyAsync($"Changed prefix from `{oldPrefix}` to `{prefix}`");
        }

        [Command("kill")]
        [Summary("Kills Mops to adapt to any new changes in code.")]
        [RequireBotManage()]
        [Hide()]
        public Task kill()
        {
            Process.GetCurrentProcess().Kill();
            return Task.CompletedTask;
        }

    }
}
