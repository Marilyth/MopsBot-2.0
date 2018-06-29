using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Microsoft.CodeAnalysis.CSharp.Scripting;
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
            [Command("CreateInvite", RunMode = RunMode.Async)]
            [Summary("Creates a reaction-invite message for the specified Role.\nPeople will be able to invite themselves into the role.")]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]   
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task createInvite(string roleName, bool isGerman = false){
                var highestRole = ((SocketGuildUser)await Context.Guild.GetCurrentUserAsync()).Roles.OrderByDescending(x => x.Position).First();
                var requestedRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower().Equals(roleName.ToLower()));
                
                if(requestedRole != null && requestedRole.Position < highestRole.Position)
                    if(isGerman)
                        await StaticBase.ReactRoleJoin.AddInviteGerman((ITextChannel)Context.Channel, roleName);
                    else
                        await StaticBase.ReactRoleJoin.AddInvite((ITextChannel)Context.Channel, roleName);
                else
                    await ReplyAsync($"**Error**: Role `{roleName}` could either not be found, or was beyond Mops' permissions.");
            }

            [Command("AddToUser")]
            [Summary("Adds the specified role, to the specified user, for the specified amount of time.")]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task joinRole(SocketGuildUser person, int durationInMinutes, [Remainder]string role)
            {var highestRole = ((SocketGuildUser)await Context.Guild.GetCurrentUserAsync()).Roles.OrderByDescending(x => x.Position).First();
                var requestedRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower().Equals(role.ToLower()));

                if(requestedRole == null || requestedRole.Position >= highestRole.Position){
                    await ReplyAsync($"**Error**: Role `{role}` could either not be found, or was beyond Mops' permissions.");
                    return;
                }
                await StaticBase.MuteHandler.AddMute(person, Context.Guild.Id, durationInMinutes, role);
                await ReplyAsync($"``{role}`` Role added to ``{person.Username}`` for **{durationInMinutes}** minutes.");
            }
        }

        [Command("poll"), Summary("Creates a poll\nExample: !poll (Am I sexy?) (Yes, No) @Panda @Demon @Snail")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Poll([Remainder] string Poll)
        {
            if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.Administrator)
                return;

            MatchCollection match = Regex.Matches(Poll, @"(?<=\().+?(?=\))");
            List<IGuildUser> participants = Context.Message.MentionedUserIds.Select(x => Context.Guild.GetUserAsync(x).Result).ToList();

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
            if(prefix.StartsWith("?")){
                await ReplyAsync($"`?` is required for Mops functionality. Cannot change prefix to `{prefix}`");
                return;
            }

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
        // [Summary("Stops Mops to adapt to any new changes in code.")]
        [RequireBotManage()]
        [Hide]
        public Task kill()
        {
            Environment.Exit(0);
            return Task.CompletedTask;
        }

        [Command("eval")]
        [RequireBotManage()]
        [Hide]
        public async Task eval([Remainder]string expression)
        {
            var script = CSharpScript.Create(expression, globalsType: typeof(MopsBot.Module.Moderation));
            var result = await script.RunAsync(this);
            await ReplyAsync(result.ReturnValue.ToString());
        }

        [Command("help")]
        [Hide]
        public async Task help()
        {
            var output = "For more information regarding a specific command, please use ?<command>";

            foreach (var module in Program.handler.commands.Modules.Where(x=> !x.Preconditions.OfType<HideAttribute>().Any()))
            {
                if (module.IsSubmodule && !module.Preconditions.OfType<HideAttribute>().Any())
                {
                    output += $"`{module.Name}*` ";
                }
                else
                {
                    output += $"\n**{module.Name}**: ";
                    foreach (var command in module.Commands)
                        if (!command.Preconditions.OfType<HideAttribute>().Any())
                    output += $"`{command.Name}` ";
                }
            }
            await ReplyAsync(output);
        }
    }
}
