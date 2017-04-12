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
        public async Task Poll([Remainder] string pPoll)
        {
            if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.Administrator)
                return;

            string[] pollSegments = pPoll.Split(';');
            List<IGuildUser> participants = Context.Message.MentionedUserIds.Select(id => Context.Guild.GetUserAsync(id).Result).ToList();

            foreach (var a in Context.Message.MentionedRoleIds.Select(id => Context.Guild.GetRole(id)))
            {
                participants.AddRange(Context.Guild.GetUsersAsync().Result.Where(u => u.RoleIds.Contains(a.Id)));
            }

            if (Context.Message.Tags.Select(t => t.Type).Contains(TagType.EveryoneMention))
            {
                participants.AddRange(Context.Guild.GetUsersAsync().Result);
            }
            if (Context.Message.Tags.Select(t => t.Type).Contains(TagType.HereMention))
            {
                participants.AddRange(Context.Guild.GetUsersAsync().Result.Where(u => u.Status.Equals(UserStatus.Online)));
            }

            StaticBase.poll = new Data.Session.Poll(pollSegments[0], pollSegments[1].Split(':'), participants.ToArray());

            foreach (Discord.IGuildUser part in participants)
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
    }
}
