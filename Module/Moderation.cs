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
            public async Task createInvite(SocketRole role, bool isGerman = false){
                var highestRole = ((SocketGuildUser)await Context.Guild.GetCurrentUserAsync()).Roles.OrderByDescending(x => x.Position).First();
                
                if(role != null && role.Position < highestRole.Position)
                    if(isGerman)
                        await StaticBase.ReactRoleJoin.AddInviteGerman((ITextChannel)Context.Channel, role);
                    else
                        await StaticBase.ReactRoleJoin.AddInvite((ITextChannel)Context.Channel, role);
                else
                    await ReplyAsync($"**Error**: Role `{role.Name}` could either not be found, or was beyond Mops' permissions.");
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

            StaticBase.Poll = new Data.Updater.Poll(match[0].Value, match[1].Value.Split(","), participants.ToArray());

            foreach (IGuildUser part in participants)
            {
                string output = "";
                for (int i = 0; i < StaticBase.Poll.answers.Length; i++)
                {
                    output += $"\n``{i + 1}`` {StaticBase.Poll.answers[i]}";
                }
                try
                {
                    await part.GetOrCreateDMChannelAsync().Result.SendMessageAsync($"{Context.User.Username} has created a poll:\n\n📄: {StaticBase.Poll.question}\n{output}\n\nTo vote, simply PM me the **Number** of the answer you agree with.");
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
            StaticBase.Poll.isPrivate = isPrivate;
            await base.ReplyAsync(StaticBase.Poll.DrawPlot());

            foreach (IGuildUser part in StaticBase.Poll.participants)
            {
                await part.GetOrCreateDMChannelAsync().Result.SendMessageAsync($"📄:{StaticBase.Poll.question}\n\nHas ended without your participation, sorry!");
                StaticBase.Poll.participants.Remove(part);
            }

            StaticBase.Poll = null;
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

            if (GuildPrefix.ContainsKey(Context.Guild.Id))
            {
                oldPrefix = GuildPrefix[Context.Guild.Id];
                GuildPrefix[Context.Guild.Id] = prefix;
            }

            else
            {
                oldPrefix = "!";
                GuildPrefix.Add(Context.Guild.Id, prefix);
            }

            savePrefix();

            await ReplyAsync($"Changed prefix from `{oldPrefix}` to `{prefix}`");
        }

        [Command("CreateCommand")]
        [Summary("Allows you to create a simple response command.\n"+
                 "Name of user: {User.Username}"+
                 "Mention of user: {User.Mention}")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task CreateCommand(string command, [Remainder] string responseText){
            if(!StaticBase.CustomCommands.ContainsKey(Context.Guild.Id)){
                StaticBase.CustomCommands.Add(Context.Guild.Id, new Dictionary<string, string>());
            }

            if(!StaticBase.CustomCommands[Context.Guild.Id].ContainsKey(command)){
                StaticBase.CustomCommands[Context.Guild.Id].Add(command, responseText);
                await ReplyAsync($"Added new command **{command}**.");
            }

            else{
                StaticBase.CustomCommands[Context.Guild.Id][command] = responseText;
                await ReplyAsync($"Replaced command **{command}**.");
            }

            StaticBase.saveCommand();
        }

        [Command("RemoveCommand")]
        [Summary("Removes the specified custom command.")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task RemoveCommand(string command){
            if(StaticBase.CustomCommands[Context.Guild.Id].ContainsKey(command)){
                if(StaticBase.CustomCommands[Context.Guild.Id].Count == 1)
                    StaticBase.CustomCommands.Remove(Context.Guild.Id);
                else
                    StaticBase.CustomCommands[Context.Guild.Id].Remove(command);

                StaticBase.saveCommand();
                await ReplyAsync($"Removed command **{command}**.");
            } else {
                await ReplyAsync($"Command **{command}** not found.");
            }
        }

        /*[Command("UseCustomCommand", RunMode = RunMode.Async)]
        [Hide()]
        public async Task UseCustomCommand(string command){
            var script = CSharpScript.Create($"return $\"{StaticBase.CustomCommands[Context.Guild.Id][command]}\";", globalsType: typeof(CustomContext));
            var result = await script.RunAsync(new CustomContext {User = Context.User});
            await ReplyAsync(result.ReturnValue.ToString());
        }*/

        [Command("UseCustomCommand", RunMode = RunMode.Async)]
        [Hide()]
        public async Task UseCustomCommand(string command){
            var reply = StaticBase.CustomCommands[Context.Guild.Id][command];
            reply = reply.Replace("{User.Username}", $"{Context.User.Username}")
                         .Replace("{User.Mention}", $"{Context.User.Mention}");
            await ReplyAsync(reply);
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

        [Command("eval", RunMode = RunMode.Async)]
        [RequireBotManage()]
        [Hide]
        public async Task eval([Remainder]string expression)
        {
            try{
                var imports = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default.WithReferences(typeof(MopsBot.Program).Assembly, typeof(Discord.Attachment).Assembly).WithImports("MopsBot", "Discord");
                var script = CSharpScript.Create(expression, globalsType: typeof(MopsBot.Module.Moderation));
                var result = await script.WithOptions(imports).RunAsync(this);
                await ReplyAsync(result.ReturnValue.ToString());
            } catch(Exception e){
                await ReplyAsync("**Error:** " + e.Message);
            }
        }

        [Command("help")]
        [Hide]
        public async Task help()
        {
            var output = "For more information regarding a specific command, please use ?<command>";

            foreach (var module in Program.Handler.commands.Modules.Where(x=> !x.Preconditions.OfType<HideAttribute>().Any()))
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

            if(StaticBase.CustomCommands.ContainsKey(Context.Guild.Id)){
                output += "\n**Custom Commands**: ";
                foreach (var commands in StaticBase.CustomCommands.Where(x => x.Key == Context.Guild.Id)){
                    foreach (var command in commands.Value)
                        output += $"`{command.Key}` ";
                }
            }

            await ReplyAsync(output);
        }
    }

    public class CustomContext{
        public IUser User;
    }
}
