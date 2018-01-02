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
                StaticBase.meetups.addMeetUp(Text, (SocketGuildUser)Context.User);
                await ReplyAsync("Done ~");
            }

            [Command("blow")]
            [Summary("Deletes a Meet-Up, if you are the creator.")]
            public Task blow(int id)
            {
                StaticBase.meetups.blowMeetUp(id, (SocketGuildUser)Context.User);
                return Task.CompletedTask;
            }

            [Command("join")]
            [Summary("Join the specified meet-up")]
            public Task join(int id)
            {
                StaticBase.meetups.upcoming[id - 1].addParticipant(Context.User.Id);
                return Task.CompletedTask;
            }

            [Command("leave")]
            [Summary("Leave the specified meet-up")]
            public Task leave(int id)
            {
                StaticBase.meetups.upcoming[id - 1].removeParticipant(Context.User.Id);
                return Task.CompletedTask;
            }

            [Command("get")]
            [Summary("Provides you with a list of all upcoming Meet-Ups, including their ID")]
            public async Task get()
            {
                await ReplyAsync(StaticBase.meetups.meetupToString());
            }
        }

        [Command("poll"), Summary("Creates a poll\nExample: !poll Am I sexy?;Yes:No;@Panda @Demon @Snail")]
        public async Task Poll([Remainder] string Poll)
        {
            if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.Administrator)
                return;

            string[] pollSegments = Poll.Split(';');
            List<IGuildUser> participants = StaticBase.getMentionedUsers((CommandContext)Context);

            StaticBase.poll = new Data.Session.Poll(pollSegments[0], pollSegments[1].Split(':'), participants.ToArray());

            foreach (IGuildUser part in participants)
            {
                string output = "";
                for (int i = 0; i < StaticBase.poll.answers.Length; i++)
                {
                    output += $"\n``{i + 1}`` {StaticBase.poll.answers[i]}";
                }
                try
                {
                    await part.GetOrCreateDMChannelAsync().Result.SendMessageAsync($"{Context.User.Username} has created a poll:\n\n📄: {StaticBase.poll.question}\n{output}\n\nTo vote, simply PM me the **Number** of the answer you agree with.");
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

            await ReplyAsync(StaticBase.poll.pollToText());

            foreach (IGuildUser part in StaticBase.poll.participants)
            {
                await part.GetOrCreateDMChannelAsync().Result.SendMessageAsync($"📄:{StaticBase.poll.question}\n\nHas ended without your participation, sorry!");
                StaticBase.poll.participants.Remove(part);
            }

            StaticBase.poll = null;
        }

        [Command("trackStreamer")]
        [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage)
        {
            if (!StaticBase.streamTracks.streamers.ContainsKey(streamerName.ToLower()))
            {
                StaticBase.streamTracks.streamers.Add(streamerName, new Data.Session.TwitchTracker(streamerName, Context.Channel.Id, notificationMessage, false, "Nothing"));
            }
            else
                StaticBase.streamTracks.streamers[streamerName].ChannelIds.Add(Context.Channel.Id, notificationMessage);

            StaticBase.streamTracks.writeList();

            await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
        }

        [Command("changeChartColour")]
        [Summary("Changes the colour of the chart of the specified Streamer, in case it is not distinguishable-")]
        public Task changeColour(string streamerName)
        {
            if (StaticBase.streamTracks.streamers.ContainsKey(streamerName))
            {
                StaticBase.streamTracks.streamers[streamerName].recolour();
            }

            return Task.CompletedTask;
        }

        [Command("trackTwitter")]
        [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task trackStreamer(string twitterUser)
        {
            if (!StaticBase.twitterTracks.twitters.ContainsKey(twitterUser))
                StaticBase.twitterTracks.twitters.Add(twitterUser, new Data.Session.TwitterTracker(twitterUser, 0));

            StaticBase.twitterTracks.twitters[twitterUser].ChannelIds.Add(Context.Channel.Id);
            StaticBase.twitterTracks.writeList();

            await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, from now on!");
        }

        [Command("trackOverwatch")]
        [Summary("Keeps track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
        public async Task trackOW(string owUser)
        {
            if (!StaticBase.OverwatchTracks.owPlayers.ContainsKey(owUser))
                StaticBase.OverwatchTracks.owPlayers.Add(owUser, new Data.Session.OverwatchTracker(owUser));

            StaticBase.OverwatchTracks.owPlayers[owUser].ChannelIds.Add(Context.Channel.Id);
            StaticBase.OverwatchTracks.writeList();

            await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");
        }

        [Command("trackClips")]
        [Summary("Keeps track of clips from streams of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task trackClips(string streamerName)
        {
            StaticBase.ClipTracker.addTracker(streamerName, Context.Channel.Id);

            await ReplyAsync("Keeping track of clips of " + streamerName + "'s streams, from now on!");
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
