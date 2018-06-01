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

namespace MopsBot.Module
{
    public class Moderation : ModuleBase
    {
        [Group("Role")]
        [RequireBotPermission(ChannelPermission.ManageRoles)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
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
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task leaveRole([Remainder]string role)
            {
                SocketRole pRole = (SocketRole)Context.Guild.Roles.First(x => x.Name.ToLower().Equals(role.ToLower()));

                await ((SocketGuildUser)Context.User).RemoveRoleAsync(pRole);

                await ReplyAsync($"You left the {pRole.Name} role.");
            }
        }

        [Command("poll"), Summary("Creates a poll\nExample: !poll (Am I sexy?) (Yes, No) @Panda @Demon @Snail")]
        public async Task Poll([Remainder] string Poll)
        {
            if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.Administrator)
                return;

            MatchCollection match = Regex.Matches(Poll, @"(?<=\().+?(?=\))");
            List<IGuildUser> participants = getMentionedUsers((CommandContext)Context);

            poll = new Data.Updater.Poll(match[0].Value, match[1].Value.Split(","), participants.ToArray());

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
        public async Task PollEnd(bool isPrivate = true)
        {
            if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.Administrator)
                return;
            poll.isPrivate = isPrivate;
            await ReplyAsync(poll.DrawPlot());

            foreach (IGuildUser part in poll.participants)
            {
                await part.GetOrCreateDMChannelAsync().Result.SendMessageAsync($"📄:{poll.question}\n\nHas ended without your participation, sorry!");
                poll.participants.Remove(part);
            }

            poll = null;
        }

        [Group("Giveaway")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]   
        [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
        public class Giveaway : ModuleBase
        {
            [Command("create")]
            [Summary("Creates giveaway.")]
            public async Task create([Remainder]string game)
            {
                await ReactGiveaways.AddGiveaway(Context.Channel, game, Context.User);
            }
        }

        [Command("setPrefix")]
        [Summary("Changes the prefix of Mops in the current Guild")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
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
        [Summary("Stops Mops to adapt to any new changes in code.")]
        [RequireBotManage()]
        [Hide]
        public Task kill()
        {
            Environment.Exit(0);
            return Task.CompletedTask;
        }
    }
}
