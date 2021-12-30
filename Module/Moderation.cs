using Discord.WebSocket;
using Discord;
using Discord.Interactions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MopsBot.Module.Preconditions;
using System.Text.RegularExpressions;
using MopsBot.Data.Tracker;
using MongoDB.Driver;
using static MopsBot.StaticBase;
using Discord.Addons.Interactive;
using MopsBot.Data.Entities;

namespace MopsBot.Module
{
    public class Moderation : InteractionModuleBase<IInteractionContext>
    {
        public static Dictionary<ulong, ulong> CustomCaller = new Dictionary<ulong, ulong>();

        [Group("role", "Commands for role invitations")]
        [RequireBotPermission(ChannelPermission.ManageRoles)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Role : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("create_invite", "Creates a reaction-invite message for the specified Role.", runMode: RunMode.Async)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireUserPermission(ChannelPermission.ManageRoles)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.ApplyPerChannel)]
            public async Task createInvite(SocketRole role, string description = "DEFAULT")
            {
                throw new Exception("as");
                using (Context.Channel.EnterTypingState())
                {
                    var highestRole = (Context.User as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First();
                    if (description.Equals("DEFAULT")) description = "To join/leave the " + (role.IsMentionable ? role.Mention : $"**{role.Name}**") + " role, add/remove the ✅ Icon below this message!\n" + "If you can manage Roles, you may delete this invitation by pressing the 🗑 Icon";
                    if (role != null && role.Position < highestRole.Position)
                        await StaticBase.ReactRoleJoin.AddInvite((ITextChannel)Context.Channel, role, description);
                    else
                        await FollowupAsync($"**Error**: Role `{role.Name}` could either not be found, or was beyond Mops' permissions.");
                }
            }

            [SlashCommand("prune", "Remove all inactive role invites", runMode: RunMode.Async)]
            [RequireBotManage]
            [HideHelp]
            public async Task Prune(bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var pruned = await StaticBase.ReactRoleJoin.TryPruneAsync(testing);
                    var result = $"{"Channel",-20}{"Message"}\n{string.Join("\n", pruned.Select(x => $"{x.Key,-20}{x.Value,-20}"))}";
                    if (result.Length < 2048)
                        await RespondAsync($"```{result}```");
                    else
                        await RespondAsync($"Pruned {pruned.Count} objects");
                }
            }
        }

        [Group("poll", "Commands for poll creation")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
        public class Poll : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("create", "Creates a poll\nExample: !poll create \"What should I play\" \"Dark Souls\" \"Osu!\" \"WoW\"", runMode: RunMode.Async)]
            [RequireUserPermission(ChannelPermission.ManageMessages)]
            [Ratelimit(1, 60, Measure.Seconds, RatelimitFlags.ApplyPerChannel)]
            public async Task Create(string title, params string[] options)
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (options.Length <= 10)
                    {
                        Data.Interactive.Poll poll = new Data.Interactive.Poll(title, options);
                        await StaticBase.Poll.AddPoll((ITextChannel)Context.Channel, poll);
                    }
                    else
                        await RespondAsync("Can't have more than 10 options per poll.");
                }
            }

            [SlashCommand("get", "Returns a list of all open polls, and a link to their corresponding message.")]
            public async Task Get()
            {
                var infoEmbed = new EmbedBuilder().WithDescription(String.Join("\n", StaticBase.Poll.Polls[Context.Channel.Id].Select(x => $"[{x.Question}](https://discordapp.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{x.MessageID})")));
                await RespondAsync(embed: infoEmbed.Build());
            }

            [SlashCommand("prune", "Delete inactive polls", runMode: RunMode.Async)]
            [RequireBotManage]
            [HideHelp]
            public async Task Prune(bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var pruned = await StaticBase.Poll.TryPruneAsync(testing);
                    var result = $"{"Channel",-20}{"Message"}\n{string.Join("\n", pruned.Select(x => $"{x.Key,-20}{x.Value,-20}"))}";
                    if (result.Length < 2048)
                        await RespondAsync($"```{result}```");
                    else
                        await RespondAsync($"Pruned {pruned.Count} objects");
                }
            }
        }

        [Group("giveaway", "Commands for giveaway creation")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
        public class Giveaway : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("create", "Creates a giveaway", runMode: RunMode.Async)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.ApplyPerChannel)]
            public async Task Create(string game)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await ReactGiveaways.AddGiveaway(Context.Channel, game, Context.User);
                }
            }

            [SlashCommand("get", "Returns message links to all active giveaways", runMode: RunMode.Async)]
            public async Task Get()
            {
                using (Context.Channel.EnterTypingState())
                {
                    var allEmbeds = ReactGiveaways.Giveaways[Context.Channel.Id].Select(x => Tuple.Create(Context.Channel.GetMessageAsync(x.Key).Result.Embeds.First(), x.Key));
                    var infoEmbed = new EmbedBuilder().WithDescription(String.Join("\n", allEmbeds.Select(x => $"[{x.Item1.Title} by {x.Item1.Author.Value.Name}](https://discordapp.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{x.Item2})")));

                    await RespondAsync(embed: infoEmbed.Build());
                }
            }

            [SlashCommand("prune", "Remove all inactive giveaways", runMode: RunMode.Async)]
            [RequireBotManage]
            [HideHelp]
            public async Task Prune(bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var pruned = await StaticBase.ReactGiveaways.TryPruneAsync(testing);
                    var result = $"{"Channel",-20}{"Message"}\n{string.Join("\n", pruned.Select(x => $"{x.Key,-20}{x.Value,-20}"))}";
                    if (result.Length < 2048)
                        await RespondAsync($"```{result}```");
                    else
                        await RespondAsync($"Pruned {pruned.Count} objects");
                }
            }
        }

        [Group("janitor", "Commands for Janitor creation")]
        public class Janitor : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("set", "Adds a janitor service, which removes messages older than `messageDuration`")]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireUserPermission(ChannelPermission.ManageMessages)]
            public async Task SetJanitor(TimeSpan messageDuration)
            {
                if (messageDuration < TimeSpan.FromMinutes(1))
                {
                    await RespondAsync("Duration must be at least 1 minute long!");
                    return;
                }

                using (Context.Channel.EnterTypingState())
                {
                    if (!StaticBase.ChannelJanitors.ContainsKey(Context.Channel.Id))
                    {
                        var janitor = new ChannelJanitor(Context.Channel.Id, messageDuration);
                        await ChannelJanitor.InsertToDBAsync(janitor);
                        StaticBase.ChannelJanitors.Add(Context.Channel.Id, janitor);

                        await RespondAsync($"Added janitor with timespan: {messageDuration.ToString(@"d\d\ h\h\ m\m\ s\s")}\n**This will only check the most recent 100 messages starting from now!**");
                    }
                    else
                    {
                        var janitor = StaticBase.ChannelJanitors[Context.Channel.Id];
                        janitor.MessageDuration = messageDuration;
                        janitor.NextCheck = DateTime.UtcNow.AddMinutes(1);
                        await janitor.SetTimer();
                        await RespondAsync($"Replaced janitor timespan: {messageDuration.ToString(@"d\d\ h\h\ m\m\ s\s")}");
                    }
                }
            }
            [SlashCommand("remove", "Stops the janitor service in this channel")]
            [RequireUserPermission(ChannelPermission.ManageMessages)]
            public async Task RemoveJanitor()
            {
                using (Context.Channel.EnterTypingState())
                {
                    bool worked = StaticBase.ChannelJanitors.TryGetValue(Context.Channel.Id, out ChannelJanitor janitor);

                    if (worked)
                    {
                        await ChannelJanitor.RemoveFromDBAsync(janitor);
                        StaticBase.ChannelJanitors.Remove(Context.Channel.Id);
                        await RespondAsync("Removed janitor for this channel.");
                    }
                    else
                    {
                        await RespondAsync("Could not find a janitor service for this channel.");
                    }
                }
            }

            [SlashCommand("prune", "Removes all inactive janitors", runMode: RunMode.Async)]
            [RequireBotManage]
            [HideHelp]
            public async Task Prune(bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var toCheck = StaticBase.ChannelJanitors.Keys;
                    var toPrune = toCheck.Select(x => Tuple.Create(x, Program.Client.GetChannel(x) == null));
                    if (!testing)
                    {
                        foreach (var channel in toPrune.Where(x => x.Item2).Select(x => x.Item1).ToList())
                        {
                            bool worked = StaticBase.ChannelJanitors.TryGetValue(channel, out ChannelJanitor janitor);
                            if (worked)
                            {
                                await ChannelJanitor.RemoveFromDBAsync(janitor);
                                StaticBase.ChannelJanitors.Remove(channel);
                            }
                        }
                    }
                    await RespondAsync($"Pruned {toPrune.Where(x => x.Item2).Count()} objects");
                }
            }
        }

        [Group("welcome_message", "Commands for welcome message creation")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class WelcomeMessage : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("create", "Makes Mops greet people, in the channel you are calling this command in", runMode: RunMode.Async)]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task WelcomeCreate(string WelcomeMessage)
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (!StaticBase.WelcomeMessages.ContainsKey(Context.Guild.Id))
                    {
                        StaticBase.WelcomeMessages.Add(Context.Guild.Id, new Data.Entities.WelcomeMessage(Context.Guild.Id, Context.Channel.Id, WelcomeMessage));
                        await Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").InsertOneAsync(StaticBase.WelcomeMessages[Context.Guild.Id]);
                        await RespondAsync($"Created welcome message:\n{WelcomeMessage}");
                    }

                    else
                    {
                        var handler = StaticBase.WelcomeMessages[Context.Guild.Id];

                        if (handler.IsWebhook)
                        {
                            handler.IsWebhook = false;
                            await handler.RemoveWebhookAsync();
                            handler.WebhookId = 0;
                            handler.WebhookToken = null;
                            handler.AvatarUrl = null;
                            handler.Name = null;
                        }

                        handler.ChannelId = Context.Channel.Id;
                        handler.Notification = WelcomeMessage;
                        await Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").ReplaceOneAsync(x => x.GuildId == Context.Guild.Id, StaticBase.WelcomeMessages[Context.Guild.Id]);
                        await RespondAsync($"Replaced welcome message with:\n{WelcomeMessage}");
                    }
                }
            }

            [SlashCommand("create_webhook", "Makes Mops greet people, in the channel you are calling this command in.", runMode: RunMode.Async)]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ManageWebhooks)]
            public async Task WelcomeCreateWebhook(string WelcomeMessage, string Name = null, string AvatarUrl = null)
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (!StaticBase.WelcomeMessages.ContainsKey(Context.Guild.Id))
                    {
                        var webhook = await ((SocketTextChannel)Context.Channel).CreateWebhookAsync($"{Name ?? "Mops"} - Welcome Messages");
                        StaticBase.WelcomeMessages.Add(Context.Guild.Id, new Data.Entities.WelcomeMessage(Context.Guild.Id, Context.Channel.Id, WelcomeMessage, webhook.Id, webhook.Token, Name, AvatarUrl));
                        await Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").InsertOneAsync(StaticBase.WelcomeMessages[Context.Guild.Id]);
                        await RespondAsync($"Created welcome message:\n{WelcomeMessage}");
                    }

                    else
                    {
                        var handler = StaticBase.WelcomeMessages[Context.Guild.Id];

                        if (!handler.IsWebhook || handler.ChannelId != Context.Channel.Id)
                        {
                            await handler.RemoveWebhookAsync();

                            var webhook = await ((SocketTextChannel)Context.Channel).CreateWebhookAsync($"{Name ?? "Mops"} - Welcome Messages");
                            handler.WebhookId = webhook.Id;
                            handler.WebhookToken = webhook.Token;
                            handler.IsWebhook = true;
                            handler.ChannelId = Context.Channel.Id;
                        }

                        handler.Name = Name;
                        handler.AvatarUrl = AvatarUrl;
                        handler.Notification = WelcomeMessage;
                        await Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").ReplaceOneAsync(x => x.GuildId == Context.Guild.Id, StaticBase.WelcomeMessages[Context.Guild.Id]);
                        await RespondAsync($"Replaced welcome message with:\n{WelcomeMessage}");
                    }
                }
            }

            [SlashCommand("delete", "Stops Mops from sending welcome messages.", runMode: RunMode.Async)]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task WelcomeDelete()
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (StaticBase.WelcomeMessages.ContainsKey(Context.Guild.Id))
                    {
                        await StaticBase.WelcomeMessages[Context.Guild.Id].RemoveWebhookAsync();

                        StaticBase.WelcomeMessages.Remove(Context.Guild.Id);
                        await Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").DeleteOneAsync(x => x.GuildId == Context.Guild.Id);
                        await RespondAsync($"Removed welcome message!");
                    }
                }
            }

            [SlashCommand("prune", "Removes all inactive welcome messages", runMode: RunMode.Async)]
            [RequireBotManage]
            [HideHelp]
            public async Task Prune(bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var toCheck = StaticBase.WelcomeMessages.Keys;
                    var toPrune = toCheck.Select(x => Tuple.Create(x, Program.Client.GetGuild(x) == null));
                    if (!testing)
                    {
                        foreach (var guild in toPrune.Where(x => x.Item2).Select(x => x.Item1).ToList())
                        {
                            bool worked = StaticBase.WelcomeMessages.TryGetValue(guild, out MopsBot.Data.Entities.WelcomeMessage message);
                            if (worked)
                            {
                                await message.RemoveWebhookAsync();

                                StaticBase.WelcomeMessages.Remove(guild);
                                await Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").DeleteOneAsync(x => x.GuildId == guild);
                            }
                        }
                    }
                    await RespondAsync($"Pruned {toPrune.Where(x => x.Item2).Count()} objects");
                }
            }

        }

        /*
        [Group("Command", "Custom command creation")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Command : InteractionModuleBase<IInteractionContext>
        {
            [SlashCommand("Create", runMode: RunMode.Async)]
            [Summary("Allows you to create a simple response command.\n" +
                  "Name of user: {User.Username}\n" +
                  "Mention of user: {User.Mention}\n" +
                  "User parameters: {User.Parameters}\n" +
                  "Wrap another command (cannot be custom): {Command:CommandName Parameters}\n")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task CreateCommand(string command, [Remainder] string responseText)
            {
                if (responseText.Split("{Command:").Length <= 2)
                {
                    if (!StaticBase.CustomCommands.ContainsKey(Context.Guild.Id))
                    {
                        StaticBase.CustomCommands.Add(Context.Guild.Id, new Data.Entities.CustomCommands(Context.Guild.Id));
                    }

                    await StaticBase.CustomCommands[Context.Guild.Id].AddCommandAsync(command, responseText);

                    await RespondAsync($"Command **{command}** has been created.");
                }
                else
                    await RespondAsync("A command can only wrap a maximum of 1 other command!\nThis is for the safety of Mops.");
            }

            [SlashCommand("Remove", runMode: RunMode.Async)]
            [Summary("Removes the specified custom command.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task RemoveCommand(string command)
            {
                if (StaticBase.CustomCommands.ContainsKey(Context.Guild.Id))
                {
                    await StaticBase.CustomCommands[Context.Guild.Id].RemoveCommandAsync(command);
                    await RespondAsync($"Removed command **{command}**.");
                }
                else
                {
                    await RespondAsync($"Command **{command}** not found.");
                }
            }

            [SlashCommand("AddRestriction", runMode: RunMode.Async)]
            [Summary("Only users with the `role` will be able to use the command.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task AddRestriction(string command, [Remainder]SocketRole role)
            {
                if (StaticBase.CustomCommands.ContainsKey(Context.Guild.Id))
                {
                    await StaticBase.CustomCommands[Context.Guild.Id].AddRestriction(command, role);
                    await RespondAsync($"Command **{command}** is now only usable by roles:\n{string.Join("\n", StaticBase.CustomCommands[Context.Guild.Id].RoleRestrictions[SlashCommand].Select(x => Context.Guild.GetRole(x).Name))}");
                }
                else
                {
                    await RespondAsync($"Command **{command}** not found.");
                }
            }

            [SlashCommand("RemoveRestriction", runMode: RunMode.Async)]
            [Summary("Removes the restriction of `role` for the command.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task RemoveRestriction(string command, [Remainder]SocketRole role)
            {
                if (StaticBase.CustomCommands.ContainsKey(Context.Guild.Id))
                {
                    await StaticBase.CustomCommands[Context.Guild.Id].RemoveRestriction(command, role);
                    await RespondAsync($"Command **{command}** is not restricted to {role.Name} anymore.");
                }
                else
                {
                    await RespondAsync($"Command **{command}** not found.");
                }
            }
        }

        [SlashCommand("UseCustomCommand", runMode: RunMode.Async)]
        [Hide()]
        public async Task UseCustomCommand(string command){
            var script = CSharpScript.Create($"return $\"{StaticBase.CustomCommands[Context.Guild.Id][SlashCommand]}\";", globalsType: typeof(CustomContext));
            var result = await script.RunAsync(new CustomContext {User = Context.User});
            await RespondAsync(result.ReturnValue.ToString());
        }

        [SlashCommand("UseCustomCommand", runMode: RunMode.Async)]
        [HideHelp]
        public async Task UseCustomCommand(string command)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (command.Contains("{Command:"))
                {
                    await RespondAsync("Command arguments were probably an injection. Stopping execution.");
                    return;
                }

                var commandParams = command.Split(" ");
                var commandName = commandParams.First();
                var commandArgs = commandParams.Skip(1);

                var reply = StaticBase.CustomCommands[Context.Guild.Id].Commands[SlashCommandName];

                //Replace regular code
                reply = reply.Replace("{User.Username}", $"{Context.User.Username}")
                             .Replace("{User.Mention}", $"{Context.User.Mention}")
                             .Replace("{User.Parameters}", string.Join(" ", commandArgs));
                var paramRequests = reply.Split("{User.Parameters:");
                foreach (var param in paramRequests)
                {
                    var paramNumber = param.Split("}").First();
                    string toInsert = "";
                    if (paramNumber.Contains(":"))
                    {
                        var range = paramNumber.Split(":");
                        if (!int.TryParse(range.First(), out int from)) from = 0;
                        if (!int.TryParse(range.Last(), out int to)) to = commandArgs.Count();
                        toInsert = string.Join(" ", commandArgs.Skip(from).Take(to - from + 1));
                    }
                    reply = reply.Replace("{User.Parameters:" + paramNumber + "}", toInsert);
                }

                //Replace URL code
                reply = reply.Replace("%7BUser.Username%7D", $"{Context.User.Username.Replace(" ", "%20")}")
                             .Replace("%7BUser.Mention%7D", $"{Context.User.Mention.Replace(" ", "%20")}")
                             .Replace("%7BUser.Parameters%7D", string.Join(" ", commandArgs).Replace(" ", "%20"));
                paramRequests = reply.Split("%7BUser.Parameters:");
                foreach (var param in paramRequests)
                {
                    var paramNumber = param.Split("%7D").First();
                    string toInsert = "";
                    if (paramNumber.Contains(":"))
                    {
                        var range = paramNumber.Split(":");
                        if (!int.TryParse(range.First(), out int from)) from = 0;
                        if (!int.TryParse(range.Last(), out int to)) to = commandArgs.Count();
                        toInsert = string.Join(" ", commandArgs.Skip(from).Take(to - from + 1));
                    }
                    reply = reply.Replace("%7BUser.Parameters:" + paramNumber + "%7D", toInsert.Replace(" ", "%20"));
                }

                if (reply.Contains("{Command:"))
                {
                    CustomCaller[Context.Channel.Id] = Context.User.Id;
                    commandName = reply.Split("{Command:").Last().Split("}").First();
                    reply = reply.Replace("{Command:" + commandName + "}", "");
                    await (await RespondAsync("[ProcessBotMessage]" + commandName)).DeleteAsync();
                }

                if (!reply.Equals(string.Empty))
                    await RespondAsync(reply);
            }
        }

        [SlashCommand("kill")]
        // [Summary("Stops Mops to adapt to any new changes in code.")]
        [RequireBotManage()]
        [HideHelp]
        public Task kill()
        {
            Environment.Exit(0);
            return Task.CompletedTask;
        }

        [SlashCommand("openfiles", runMode: RunMode.Async)]
        [RequireBotManage()]
        [HideHelp]
        public async Task openfiles()
        {
            using (Context.Channel.EnterTypingState())
            {
                await RespondAsync(DateTime.Now + $" open files were {System.Diagnostics.Process.GetCurrentProcess().HandleCount}");
            }
        }*/

        [SlashCommand("eval", "Compile and run code on Mops", runMode: RunMode.Async)]
        [RequireBotManage]
        [HideHelp]
        public async Task eval(string expression)
        {
            using (Context.Channel.EnterTypingState())
            {
                var imports = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default.WithReferences(typeof(MopsBot.Program).Assembly, typeof(Discord.Attachment).Assembly).WithImports("MopsBot", "Discord");
                var preCompilationTime = DateTime.Now.Ticks / 10000;
                var script = CSharpScript.Create(expression, globalsType: typeof(MopsBot.Module.Moderation)).WithOptions(imports);
                script.Compile();
                var preExecutionTime = DateTime.Now.Ticks / 10000;
                var result = await script.RunAsync(this);
                var postExecutionTime = DateTime.Now.Ticks / 10000;

                var embed = new EmbedBuilder();
                embed.Author = new EmbedAuthorBuilder().WithName(Context.User.Username).WithIconUrl(Context.User.GetAvatarUrl());
                embed.WithDescription($"```csharp\n{expression}```").WithTitle("Evaluation of code");
                embed.AddField("Compilation time", $"{preExecutionTime - preCompilationTime}ms", true);
                embed.AddField("Execution time", $"{postExecutionTime - preExecutionTime}ms", true);
                embed.AddField("Return value", result.ReturnValue?.ToString() ?? "`null or void`");

                await RespondAsync("", embed: embed.Build());
            }
        }

        [SlashCommand("ban", "Bans someone from using Mops")]
        [HideHelp]
        [RequireBotManage]
        public async Task ban(ulong userId){
            await MopsBot.Data.Entities.User.ModifyUserAsync(userId, x => x.IsBanned = true);
            await RespondAsync($"Banned user with id {userId} indefinitely.");
        }

        [SlashCommand("help", "Displays help on specific modules")]
        [HideHelp]
        [Ratelimit(1, 2, Measure.Seconds, RatelimitFlags.ChannelwideLimit)]
        public async Task help(string helpModule = null)
        {
            try
            {
                if(helpModule != null && (!Program.Handler.commands.Modules.FirstOrDefault(x => x.Name.Equals(helpModule.Split(" ").Last(), StringComparison.InvariantCultureIgnoreCase))?.IsSubmodule ?? false)){
                    return;
                }
                EmbedBuilder e = new EmbedBuilder();
                e.WithDescription($"For more information regarding a **specific command** or **command group***,\nplease use **?{(helpModule == null ? "" : helpModule + " ")}<command>** or " +
                                  $"**{await StaticBase.GetGuildPrefixAsync(Context.Guild.Id)}help {(helpModule == null ? "" : helpModule + " ")}<command>**")
                 .WithColor(Discord.Color.Blue)
                 .WithAuthor(async x =>
                 {
                     //Don't require support server shard to be online for help. This failing actually kills Mops.
                     x.IconUrl = "https://cdn.discordapp.com/icons/435919579005321237/3e995c6b3df5776e262d8ce4a2c514c2.jpg";//Context.Client.GetGuild(435919579005321237).IconUrl;
                     x.Name = "Click to join the Support Server!";
                     x.Url = "https://discord.gg/wZFE2Zs";
                 });

                if (helpModule == null)
                {
                    foreach (var module in Program.Handler.commands.Modules.Where(x => !x.Preconditions.OfType<HideAttribute>().Any()))
                    {
                        if (!module.IsSubmodule)
                        {
                            string moduleInformation = "";
                            bool isTracking = module.Name.Contains("Tracking");
                            moduleInformation += string.Join(", ", module.Commands.Where(x => !x.Preconditions.Any(y => y is HideAttribute)).Select(x => $"[{x.Name}]({CommandHandler.GetCommandHelpImage(x.Name)})"));
                            moduleInformation += "\n";

                            moduleInformation += string.Join(", ", module.Submodules.Where(x => (!isTracking || Program.TrackerLimits[x.Name]["TrackersPerServer"] > 0) &&
                                                                                               !x.Preconditions.Any(y => y is HideAttribute)).Select(x => $"[{x.Name}\\*]({CommandHandler.GetCommandHelpImage(x.Name)})"));
                            var modulesections = moduleInformation.Length / 1024 + 1;
                            if (modulesections > 1)
                            {
                                var segments = moduleInformation.Split(", ");
                                var submoduleInformation = "";
                                foreach (var segment in segments)
                                {
                                    if (submoduleInformation.Length + segment.Length > 1000)
                                    {
                                        submoduleInformation = string.Concat(submoduleInformation.SkipLast(2));
                                        e.AddField($"**{module.Name}**", submoduleInformation);
                                        submoduleInformation = "";
                                    }
                                    submoduleInformation += segment + ", ";
                                }
                                e.AddField($"**{module.Name}**", submoduleInformation);
                            }
                            else
                            {
                                e.AddField($"**{module.Name}**", moduleInformation);
                            }
                        }
                    }

                    if (StaticBase.CustomCommands.ContainsKey(Context.Guild.Id))
                    {
                        e.AddField("**Custom Commands**", string.Join(", ", StaticBase.CustomCommands.Where(x => x.Key == Context.Guild.Id).First().Value.Commands.Select(x => $"`{x.Key}`")));
                    }
                }
                else
                {
                    // var module = Program.Handler.commands.Modules.First(x => x.Name.ToLower().Equals(helpModule.ToLower()));

                    // string moduleInformation = "";
                    // moduleInformation += string.Join(", ", module.Commands.Where(x => !x.Preconditions.OfType<HideAttribute>().Any()).Select(x => $"[{x.Name}]({CommandHandler.GetCommandHelpImage($"{module.Name} {x.Name}")})"));
                    // moduleInformation += "\n";

                    // moduleInformation += string.Join(", ", module.Submodules.Select(x => $"{x.Name}\\*"));

                    // e.AddField($"**{module.Name}**", moduleInformation);
                    var prefix = await GetGuildPrefixAsync(Context.Guild.Id);
                    e = Program.Handler.getHelpEmbed(helpModule.ToLower(), prefix, e);
                }

                await RespondAsync("", embed: e.Build());
            }
            catch
            {
                throw;
            }
        }
    }

    public class CustomContext
    {
        public IUser User;
    }
}
