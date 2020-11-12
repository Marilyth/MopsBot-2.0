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
using MopsBot.Data.Tracker;
using MongoDB.Driver;
using static MopsBot.StaticBase;
using Discord.Addons.Interactive;
using MopsBot.Data.Entities;

namespace MopsBot.Module
{
    public class Moderation : ModuleBase<ShardedCommandContext>
    {
        public static Dictionary<ulong, ulong> CustomCaller = new Dictionary<ulong, ulong>();

        [Group("Role")]
        [RequireBotPermission(ChannelPermission.ManageRoles)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Role : ModuleBase<ShardedCommandContext>
        {
            [Command("CreateInvite", RunMode = RunMode.Async)]
            [Summary("Creates a reaction-invite message for the specified Role.\nPeople will be able to invite themselves into the role.")]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireUserPermission(ChannelPermission.ManageRoles)]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.ApplyPerChannel)]
            public async Task createInvite(SocketRole role, [Remainder]string description = "DEFAULT")
            {
                using (Context.Channel.EnterTypingState())
                {
                    var highestRole = (Context.User as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First();
                    if (description.Equals("DEFAULT")) description = "To join/leave the " + (role.IsMentionable ? role.Mention : $"**{role.Name}**") + " role, add/remove the ✅ Icon below this message!\n" + "If you can manage Roles, you may delete this invitation by pressing the 🗑 Icon";
                    if (role != null && role.Position < highestRole.Position)
                        await StaticBase.ReactRoleJoin.AddInvite((ITextChannel)Context.Channel, role, description);
                    else
                        await ReplyAsync($"**Error**: Role `{role.Name}` could either not be found, or was beyond Mops' permissions.");
                }
            }

            [Command("Prune", RunMode = RunMode.Async)]
            [RequireBotManage]
            [Hide]
            public async Task Prune(bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var pruned = await StaticBase.ReactRoleJoin.TryPruneAsync(testing);
                    var result = $"{"Channel",-20}{"Message"}\n{string.Join("\n", pruned.Select(x => $"{x.Key,-20}{x.Value,-20}"))}";
                    if (result.Length < 2048)
                        await ReplyAsync($"```{result}```");
                    else
                        await ReplyAsync($"Pruned {pruned.Count} objects");
                }
            }
        }

        [Group("Poll")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
        public class Poll : ModuleBase<ShardedCommandContext>
        {
            [Command("Create", RunMode = RunMode.Async), Summary("Creates a poll\nExample: !poll \"What should I play\" \"Dark Souls\" \"Osu!\" \"WoW\"")]
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
                        await ReplyAsync("Can't have more than 10 options per poll.");
                }
            }

            [Command("Get")]
            [Summary("Returns a list of all open polls, and a link to their corresponding message.")]
            public async Task Get()
            {
                var infoEmbed = new EmbedBuilder().WithDescription(String.Join("\n", StaticBase.Poll.Polls[Context.Channel.Id].Select(x => $"[{x.Question}](https://discordapp.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{x.MessageID})")));
                await ReplyAsync(embed: infoEmbed.Build());
            }

            [Command("Prune", RunMode = RunMode.Async)]
            [RequireBotManage]
            [Hide]
            public async Task Prune(bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var pruned = await StaticBase.Poll.TryPruneAsync(testing);
                    var result = $"{"Channel",-20}{"Message"}\n{string.Join("\n", pruned.Select(x => $"{x.Key,-20}{x.Value,-20}"))}";
                    if (result.Length < 2048)
                        await ReplyAsync($"```{result}```");
                    else
                        await ReplyAsync($"Pruned {pruned.Count} objects");
                }
            }
        }

        [Group("Giveaway")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
        public class Giveaway : ModuleBase<ShardedCommandContext>
        {
            [Command("Create", RunMode = RunMode.Async)]
            [Summary("Creates giveaway.")]
            [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.ApplyPerChannel)]
            public async Task Create([Remainder]string game)
            {
                using (Context.Channel.EnterTypingState())
                {
                    await ReactGiveaways.AddGiveaway(Context.Channel, game, Context.User);
                }
            }

            [Command("Get", RunMode = RunMode.Async)]
            [Summary("Returns message links to all active giveaways.")]
            public async Task Get()
            {
                using (Context.Channel.EnterTypingState())
                {
                    var allEmbeds = ReactGiveaways.Giveaways[Context.Channel.Id].Select(x => Tuple.Create(Context.Channel.GetMessageAsync(x.Key).Result.Embeds.First(), x.Key));
                    var infoEmbed = new EmbedBuilder().WithDescription(String.Join("\n", allEmbeds.Select(x => $"[{x.Item1.Title} by {x.Item1.Author.Value.Name}](https://discordapp.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{x.Item2})")));

                    await ReplyAsync(embed: infoEmbed.Build());
                }
            }

            [Command("Prune", RunMode = RunMode.Async)]
            [RequireBotManage]
            [Hide]
            public async Task Prune(bool testing = true)
            {
                using (Context.Channel.EnterTypingState())
                {
                    var pruned = await StaticBase.ReactGiveaways.TryPruneAsync(testing);
                    var result = $"{"Channel",-20}{"Message"}\n{string.Join("\n", pruned.Select(x => $"{x.Key,-20}{x.Value,-20}"))}";
                    if (result.Length < 2048)
                        await ReplyAsync($"```{result}```");
                    else
                        await ReplyAsync($"Pruned {pruned.Count} objects");
                }
            }
        }

        [Command("SetPrefix", RunMode = RunMode.Async)]
        [Summary("Changes the prefix of Mops in the current Guild")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task setPrefix([Remainder]string prefix)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (prefix.StartsWith("?"))
                {
                    await ReplyAsync($"`?` is required for Mops functionality. Cannot change prefix to `{prefix}`");
                    return;
                }

                string oldPrefix = await GetGuildPrefixAsync(Context.Guild.Id);

                await InsertOrUpdatePrefixAsync(Context.Guild.Id, prefix);

                await ReplyAsync($"Changed prefix from `{oldPrefix}` to `{prefix}`");
            }
        }

        [Group("Janitor")]
        public class Janitor : ModuleBase<ShardedCommandContext>
        {
            [Command("Set")]
            [Alias("AutoRemove")]
            [Summary("Adds a janitor service, which removes messages older than `messageDuration`\nOnly checks the past 100 messages")]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            [RequireUserPermission(ChannelPermission.ManageMessages)]
            public async Task SetJanitor([Remainder]TimeSpan messageDuration)
            {
                if (messageDuration < TimeSpan.FromMinutes(1))
                {
                    await ReplyAsync("Duration must be at least 1 minute long!");
                    return;
                }

                using (Context.Channel.EnterTypingState())
                {
                    if (!StaticBase.ChannelJanitors.ContainsKey(Context.Channel.Id))
                    {
                        var janitor = new ChannelJanitor(Context.Channel.Id, messageDuration);
                        await ChannelJanitor.InsertToDBAsync(janitor);
                        StaticBase.ChannelJanitors.Add(Context.Channel.Id, janitor);

                        await ReplyAsync($"Added janitor with timespan: {messageDuration.ToString(@"d\d\ h\h\ m\m\ s\s")}\n**This will only check the most recent 100 messages starting from now!**");
                    }
                    else
                    {
                        var janitor = StaticBase.ChannelJanitors[Context.Channel.Id];
                        janitor.MessageDuration = messageDuration;
                        janitor.NextCheck = DateTime.UtcNow.AddMinutes(1);
                        await janitor.SetTimer();
                        await ReplyAsync($"Replaced janitor timespan: {messageDuration.ToString(@"d\d\ h\h\ m\m\ s\s")}");
                    }
                }
            }
            [Command("Remove")]
            [Summary("Stops the janitor service in this channel")]
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
                        await ReplyAsync("Removed janitor for this channel.");
                    }
                    else
                    {
                        await ReplyAsync("Could not find a janitor service for this channel.");
                    }
                }
            }

            [Command("Prune", RunMode = RunMode.Async)]
            [RequireBotManage]
            [Hide]
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
                    await ReplyAsync($"Pruned {toPrune.Where(x => x.Item2).Count()} objects");
                }
            }
        }

        [Group("WelcomeMessage")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class WelcomeMessage : ModuleBase<ShardedCommandContext>
        {
            [Command("Create", RunMode = RunMode.Async)]
            [Summary("Makes Mops greet people, in the channel you are calling this command in.\n" +
                     "Name of user: **{User.Username}**\n" +
                     "Mention of user: **{User.Mention}**")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task WelcomeCreate([Remainder] string WelcomeMessage)
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (!StaticBase.WelcomeMessages.ContainsKey(Context.Guild.Id))
                    {
                        StaticBase.WelcomeMessages.Add(Context.Guild.Id, new Data.Entities.WelcomeMessage(Context.Guild.Id, Context.Channel.Id, WelcomeMessage));
                        await Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").InsertOneAsync(StaticBase.WelcomeMessages[Context.Guild.Id]);
                        await ReplyAsync($"Created welcome message:\n{WelcomeMessage}");
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
                        await ReplyAsync($"Replaced welcome message with:\n{WelcomeMessage}");
                    }
                }
            }

            [Command("CreateWebhook", RunMode = RunMode.Async)]
            [Summary("Makes Mops greet people, in the channel you are calling this command in.\n" +
                     "Additionally, avatar and name of the notification account can be set.\n" +
                     "Name of user: **{User.Username}**\n" +
                     "Mention of user: **{User.Mention}**")]
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
                        await ReplyAsync($"Created welcome message:\n{WelcomeMessage}");
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
                        await ReplyAsync($"Replaced welcome message with:\n{WelcomeMessage}");
                    }
                }
            }

            [Command("Delete", RunMode = RunMode.Async)]
            [Summary("Stops Mops from sending welcome messages.")]
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
                        await ReplyAsync($"Removed welcome message!");
                    }
                }
            }

            [Command("Prune", RunMode = RunMode.Async)]
            [RequireBotManage]
            [Hide]
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
                    await ReplyAsync($"Pruned {toPrune.Where(x => x.Item2).Count()} objects");
                }
            }

        }

        [Group("Command")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Command : ModuleBase<ShardedCommandContext>
        {
            [Command("Create", RunMode = RunMode.Async)]
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

                    await ReplyAsync($"Command **{command}** has been created.");
                }
                else
                    await ReplyAsync("A command can only wrap a maximum of 1 other command!\nThis is for the safety of Mops.");
            }

            [Command("Remove", RunMode = RunMode.Async)]
            [Summary("Removes the specified custom command.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task RemoveCommand(string command)
            {
                if (StaticBase.CustomCommands.ContainsKey(Context.Guild.Id))
                {
                    await StaticBase.CustomCommands[Context.Guild.Id].RemoveCommandAsync(command);
                    await ReplyAsync($"Removed command **{command}**.");
                }
                else
                {
                    await ReplyAsync($"Command **{command}** not found.");
                }
            }

            [Command("AddRestriction", RunMode = RunMode.Async)]
            [Summary("Only users with the `role` will be able to use the command.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task AddRestriction(string command, [Remainder]SocketRole role)
            {
                if (StaticBase.CustomCommands.ContainsKey(Context.Guild.Id))
                {
                    await StaticBase.CustomCommands[Context.Guild.Id].AddRestriction(command, role);
                    await ReplyAsync($"Command **{command}** is now only usable by roles:\n{string.Join("\n", StaticBase.CustomCommands[Context.Guild.Id].RoleRestrictions[command].Select(x => Context.Guild.GetRole(x).Name))}");
                }
                else
                {
                    await ReplyAsync($"Command **{command}** not found.");
                }
            }

            [Command("RemoveRestriction", RunMode = RunMode.Async)]
            [Summary("Removes the restriction of `role` for the command.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task RemoveRestriction(string command, [Remainder]SocketRole role)
            {
                if (StaticBase.CustomCommands.ContainsKey(Context.Guild.Id))
                {
                    await StaticBase.CustomCommands[Context.Guild.Id].RemoveRestriction(command, role);
                    await ReplyAsync($"Command **{command}** is not restricted to {role.Name} anymore.");
                }
                else
                {
                    await ReplyAsync($"Command **{command}** not found.");
                }
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
        public async Task UseCustomCommand(string command)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (command.Contains("{Command:"))
                {
                    await ReplyAsync("Command arguments were probably an injection. Stopping execution.");
                    return;
                }

                var commandParams = command.Split(" ");
                var commandName = commandParams.First();
                var commandArgs = commandParams.Skip(1);

                var reply = StaticBase.CustomCommands[Context.Guild.Id].Commands[commandName];

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
                    await (await ReplyAsync("[ProcessBotMessage]" + commandName)).DeleteAsync();
                }

                if (!reply.Equals(string.Empty))
                    await ReplyAsync(reply);
            }
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

        [Command("openfiles", RunMode = RunMode.Async)]
        [RequireBotManage()]
        [Hide]
        public async Task openfiles()
        {
            using (Context.Channel.EnterTypingState())
            {
                await ReplyAsync(DateTime.Now + $" open files were {System.Diagnostics.Process.GetCurrentProcess().HandleCount}");
            }
        }

        [Command("eval", RunMode = RunMode.Async)]
        [RequireBotManage]
        [Hide]
        public async Task eval([Remainder]string expression)
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

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [Command("ban")]
        [Hide]
        [RequireBotManage]
        public async Task ban(ulong userId){
            await MopsBot.Data.Entities.User.ModifyUserAsync(userId, x => x.IsBanned = true);
            await ReplyAsync($"Banned user with id {userId} indefinitely.");
        }

        [Command("help")]
        [Alias("commands")]
        [Hide]
        [Ratelimit(1, 2, Measure.Seconds, RatelimitFlags.ChannelwideLimit)]
        public async Task help([Remainder]string helpModule = null)
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

                await ReplyAsync("", embed: e.Build());
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
