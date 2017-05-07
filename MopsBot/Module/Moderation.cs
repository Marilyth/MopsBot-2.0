using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module
{
    public class Moderation : ModuleBase
    {
        [Group("Role")]
        public class Role : ModuleBase
        {
            [Command("join")]
            [Summary("Joins the specified role")]
            public async Task joinRole(string role)
            {
                SocketRole pRole = (SocketRole)Context.Guild.Roles.First(x => x.Name.ToLower().Equals(role.ToLower()));

                await ((SocketGuildUser)Context.User).AddRoleAsync(pRole);

                await ReplyAsync($"You are now part of the {pRole.Name} role! Yay!");
            }

            [Command("leave")]
            [Summary("Leaves the specified role")]
            public async Task leaveRole(string role)
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
                    await part.CreateDMChannelAsync().Result.SendMessageAsync($"{Context.User.Username} has created a poll:\n\n📄: {StaticBase.poll.question}\n{output}\n\nTo vote, simply PM me the **Number** of the answer you agree with.");
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
                await part.CreateDMChannelAsync().Result.SendMessageAsync($"📄:{StaticBase.poll.question}\n\nHas ended without your participation, sorry!");
                StaticBase.poll.participants.Remove(part);
            }

            StaticBase.poll = null;
        }

        [Command("trackStreamer")]
        [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage)
        {
            if (!StaticBase.streamTracks.streamers.Exists(x => x.name.ToLower().Equals(streamerName.ToLower())))
            {
                StaticBase.streamTracks.streamers.Add(new Data.Session.TwitchTracker(streamerName, Context.Channel.Id, notificationMessage));
            }
            else
                StaticBase.streamTracks.streamers.Find(x => x.name.ToLower().Equals(streamerName.ToLower())).ChannelIds.Add(Context.Channel.Id, notificationMessage);

            StaticBase.streamTracks.writeList();

            await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
        }

        [Command("trackClips")]
        [Summary("Keeps track of clips from streams of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task trackClips(string streamerName)
        {
            StaticBase.ClipTracker.addTracker(streamerName, Context.Channel.Id);

            await ReplyAsync("Keeping track of clips of " + streamerName + "'s streams, from now on!");
        }

    }
}
