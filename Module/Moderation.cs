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

        [Group("MeetUp")]
        public class treffen : ModuleBase
        {
            [Command("create")]
            [Summary("Creates a Meet-Up others can participate in.\n<Text> = 13.12.2017;Bowling;China")]
            public async Task create([Remainder] string Text)
            {
                meetups.addMeetUp(Text, (SocketGuildUser)Context.User);
                await ReplyAsync("Done ~");
            }

            [Command("blow")]
            [Summary("Deletes a Meet-Up, if you are the creator.")]
            public Task blow(int id)
            {
                meetups.blowMeetUp(id, (SocketGuildUser)Context.User);
                return Task.CompletedTask;
            }

            [Command("join")]
            [Summary("Join the specified meet-up")]
            public Task join(int id)
            {
                meetups.upcoming[id - 1].addParticipant(Context.User.Id);
                return Task.CompletedTask;
            }

            [Command("leave")]
            [Summary("Leave the specified meet-up")]
            public Task leave(int id)
            {
                meetups.upcoming[id - 1].removeParticipant(Context.User.Id);
                return Task.CompletedTask;
            }

            [Command("get")]
            [Summary("Provides you with a list of all upcoming Meet-Ups, including their ID")]
            public async Task get()
            {
                await ReplyAsync(meetups.meetupToString());
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
        
        [Command("trackStreamer")]
        [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage)
        {
            streamTracks.addTracker(streamerName, Context.Channel.Id, notificationMessage);

            await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
        }

        [Command("unTrackStreamer")]
        [Summary("Stops tracking the specified streamer.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task unTrackStreamer(string streamerName)
        {
            streamTracks.removeTracker(streamerName, Context.Channel.Id);

            await ReplyAsync("Stopped tracking " + streamerName + "'s streams!");
        }

        [Command("changeChartColour")]
        [Summary("Changes the colour of the chart of the specified Streamer, in case it is not distinguishable-")]
        public Task changeColour(string streamerName)
        {
            if (streamTracks.trackers.ContainsKey(streamerName))
            {
                streamTracks.trackers[streamerName].recolour();
            }

            return Task.CompletedTask;
        }

        [Command("trackTwitter")]
        [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task trackTwitter(string twitterUser)
        {
            twitterTracks.addTracker(twitterUser, Context.Channel.Id);

            await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, from now on!");
        }

        [Command("unTrackTwitter")]
        [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task unTrackTwitter(string twitterUser)
        {
            twitterTracks.removeTracker(twitterUser, Context.Channel.Id);

            await ReplyAsync("Stopped keeping track of " + twitterUser + "'s tweets!");
        }

        [Command("trackOverwatch")]
        [Summary("Keeps track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
        public async Task trackOW(string owUser)
        {
            OverwatchTracks.addTracker(owUser, Context.Channel.Id);

            await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");
        }

        [Command("unTrackOverwatch")]
        [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
        public async Task unTrackOW(string owUser)
        {
            OverwatchTracks.removeTracker(owUser, Context.Channel.Id);

            await ReplyAsync("Stopped keeping track of " + owUser + "'s stats!");
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

            if(guildPrefix.ContainsKey(Context.Guild.Id)){
                oldPrefix = guildPrefix[Context.Guild.Id];
                guildPrefix[Context.Guild.Id] = prefix;
            }

            else{
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
