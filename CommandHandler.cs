using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using MopsBot.Module.Preconditions;
using System.IO;
using MongoDB.Driver;
using static MopsBot.StaticBase;

namespace MopsBot
{
    public class CommandHandler
    {
        private static Exception mostRecentException = null;
        public CommandService commands { get; private set; }
        private DiscordShardedClient client;
        public IServiceProvider _provider { get; private set; }

        /// <summary>
        /// Add command/module Service and create Events
        /// </summary>
        /// <param name="provider">The Service Provider</param>
        /// <returns>A Task that can be awaited</returns>
        public async Task Install(IServiceProvider provider)
        {
            // Create Command Service, inject it into Dependency Map
            _provider = provider;
            client = _provider.GetService<DiscordShardedClient>();

            commands = new CommandService(new CommandServiceConfig()
            {
                DefaultRunMode = RunMode.Async,
            });
            //_map.Add(commands);

            commands.AddTypeReader<MopsBot.Data.Tracker.BaseTracker>(new Module.TypeReader.TrackerTypeReader(), true);
            commands.AddTypeReader<MopsBot.Data.Entities.User>(new Module.TypeReader.MopsUserReader(), true);
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);

            await loadCustomCommands();
            client.MessageReceived += HandleCommand;
            commands.CommandExecuted += CommandExecuted;
            commands.Log += (LogMessage log) => { mostRecentException = log.Exception; return Task.CompletedTask; };
            client.UserJoined += Client_UserJoined;
        }

        /// <summary>
        /// Greets a user when he joins a Guild
        /// </summary>
        /// <param name="User">The User who joined</param>
        private async Task Client_UserJoined(SocketGuildUser User)
        {
            if (StaticBase.WelcomeMessages.ContainsKey(User.Guild.Id))
            {
                await WelcomeMessages[User.Guild.Id].SendWelcomeMessageAsync(User);
            }
        }


        public static Dictionary<ulong, int> MessagesPerGuild = new Dictionary<ulong, int>();
        /// <summary>
        /// Checks if message is a command, and executes it
        /// </summary>
        /// <param name="parameterMessage">The message to check</param>
        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            Task.Run(async () =>
            {
                var message = parameterMessage as SocketUserMessage;
                if(!(message.Channel is IDMChannel)){
                    var guildId = ((SocketGuildChannel)message.Channel).Guild.Id;
                    MessagesPerGuild[guildId] = MessagesPerGuild.ContainsKey(guildId) ? MessagesPerGuild[guildId] + 1 : 1;
                }

                //Add experience the size of the message length
                /*if(message.Channel is SocketGuildChannel channel && (!message.Author.IsBot || message.Author.Id == Program.Client.CurrentUser.Id)){
                    if((DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).Minutes >= 2 && channel.Guild.MemberCount <= 10000){
                        await MopsBot.Data.Entities.User.ModifyUserAsync(message.Author.Id, x => {
                            x.CharactersSent += message.Content.Length;
                            x.AddGraphValue(message.Content.Length);
                        });
                    }
                }*/

                if (message == null || (message.Author.IsBot && !message.Content.StartsWith("[ProcessBotMessage]"))) return;

                // Mark where the prefix ends and the command begins
                int argPos = 0;

                //Determines if the Guild has set a special prefix, if not, ! is used
                ulong id = 0;
                if (message.Channel is Discord.IDMChannel) id = message.Channel.Id;
                else id = ((SocketGuildChannel)message.Channel).Guild.Id;
                var prefix = await GetGuildPrefixAsync(id);

                // Determine if the message has a valid prefix, adjust argPos 
                if (!message.Content.StartsWith("[ProcessBotMessage]") && !(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasStringPrefix(prefix, ref argPos) || message.HasCharPrefix('?', ref argPos))) return;

                if (char.IsWhiteSpace(message.Content[argPos]))
                    argPos += 1;

                var context = new ShardedCommandContext(client, message);

                if (message.HasCharPrefix('?', ref argPos))
                {
                    await commands.Commands.First(x => x.Name.Equals("help")).ExecuteAsync(context, new List<object> { $"{context.Message.Content.Substring(argPos)}" }, new List<object> { }, _provider);
                    return;
                }

                if (message.Content.StartsWith("[ProcessBotMessage]"))
                    argPos = "[ProcessBotMessage]".Length;

                //Execute if command exists
                if (commands.Search(context, argPos).IsSuccess)
                {
                    /*if((await MopsBot.Data.Entities.User.GetUserAsync(message.Author.Id)).IsBanned){
                        await context.Channel.SendMessageAsync("You are banned from using Mops.");
                        return;
                    }*/

                    await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"{parameterMessage.Author} ({parameterMessage.Author.Id}) executed command: {parameterMessage.Content.Substring(argPos)}"));
                    var result = await commands.ExecuteAsync(context, argPos, _provider);
                }

                //Else execute custom commands
                else if (!message.Author.IsBot && CustomCommands.ContainsKey(context.Guild.Id) && CustomCommands[context.Guild.Id].Commands.ContainsKey(context.Message.Content.Substring(argPos).Split(" ").First()))
                {
                    if (CustomCommands[context.Guild.Id].CheckPermission(context.Message.Content.Substring(argPos).Split(" ").First(), (SocketGuildUser)context.User))
                    {
                        /*if((await MopsBot.Data.Entities.User.GetUserAsync(message.Author.Id)).IsBanned){
                            await context.Channel.SendMessageAsync("You are banned from using Mops.");
                            return;
                        }*/

                        await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"executed command: {parameterMessage.Content.Substring(argPos)}"));
                        await commands.Commands.First(x => x.Name.Equals("UseCustomCommand")).ExecuteAsync(context, new List<object> { $"{context.Message.Content.Substring(argPos)}" }, new List<object> { }, _provider);
                    }
                }
            });
        }

        public async Task CommandExecuted(Discord.Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            // If the command failed, notify the user
            if (!result.IsSuccess && !result.ErrorReason.Equals("") && !context.Guild.Id.Equals(264445053596991498) && 
                !context.Message.Content.Contains("help", StringComparison.InvariantCultureIgnoreCase) && !context.Message.Content.StartsWith("?"))
            {
                Task.Run(async () =>
                {
                    //Wait for exception to reach log
                    await Task.Delay(100);
                    var embed = await CreateErrorEmbedAsync(commandInfo.Value, context as SocketCommandContext, result);
                    if (mostRecentException != null && !string.IsNullOrEmpty(Program.Config["ExceptionLogChannel"]))
                    {
                        using (var writer = File.CreateText("mopsdata//Exception.txt"))
                            writer.WriteLine(mostRecentException.ToString());
                        await (Program.Client.GetChannel(ulong.Parse(Program.Config["ExceptionLogChannel"])) as ITextChannel).SendFileAsync("mopsdata//Exception.txt", "", embed: embed);
                        File.Delete("mopsdata//Exception.txt");
                        mostRecentException = null;
                    }
                    else
                    {
                        await (Program.Client.GetChannel(ulong.Parse(Program.Config["ExceptionLogChannel"])) as ITextChannel).SendMessageAsync("", embed: embed);
                    }
                });

                if(!context.Message.Content.Contains("help", StringComparison.InvariantCultureIgnoreCase) && !context.Message.Content.StartsWith("?")){
                    if (result.ErrorReason.Contains("The input text has too many parameters"))
                        await context.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}\nIf your parameter contains spaces, please wrap it around quotation marks like this: `\"A Parameter\"`.");
                    else if (!result.ErrorReason.Contains("Command not found"))
                        await context.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
                }
            }

            else if(result.IsSuccess && !string.IsNullOrEmpty(Program.Config["CommandLogChannel"])){
                var cmdEmbed = new EmbedBuilder().AddField("Guild", $"{context.Guild.Name} ({context.Guild.Id})", true).AddField("Channel", $"{context.Channel.Name} ({context.Channel.Id})").AddField("User", $"{context.User} ({context.User.Id})").AddField("Command", context.Message.Content);
                await (Program.Client.GetChannel(ulong.Parse(Program.Config["CommandLogChannel"])) as ITextChannel).SendMessageAsync(embed: cmdEmbed.Build());
            }

            if (result.IsSuccess && context.Message.Content.Contains("track", StringComparison.InvariantCultureIgnoreCase))
            {
                if ((await MopsBot.Data.Entities.User.GetUserAsync(context.User.Id)).IsTaCDue())
                {
                    await context.Channel.SendMessageAsync($"Weekly reminder:\nBy using Mops' tracking services, you agree to our terms and conditions as well as our privacy policy: http://37.221.195.236/Terms_And_Conditions\nhttp://37.221.195.236/Privacy_Policy");
                    await MopsBot.Data.Entities.User.ModifyUserAsync(context.User.Id, x => x.LastTaCReminder = DateTime.UtcNow);
                }
            }
        }

        /// <summary>
        /// Creates the embed that is sent whenever ?command is called
        /// </summary>
        /// <param name="command">The command to create the embed for</param>
        /// <param name="usage">The usage example to include in the embed</param>
        /// <param name="description">The desciption to include in the embed</param>
        private EmbedBuilder createHelpEmbed(string command, string usage, string description, EmbedBuilder e, string preconditions = null)
        {
            //EmbedBuilder e = new EmbedBuilder();
            e.Title = command;
            e.ImageUrl = GetCommandHelpImage(command);

            if (!String.IsNullOrEmpty(preconditions))
                e.AddField("Preconditions", preconditions);

            e.AddField("Example usage", usage);

            e.Description = description;

            return e;
        }

        public EmbedBuilder getHelpEmbed(string message, string prefix, EmbedBuilder e = null)
        {
            if (e is null)
            {
                e = new EmbedBuilder();
                e.Color = new Color(0x0099ff);
            }

            var output = "";
            string commandName = message;
            string[] moduleNames;
            string[] tempMessages = message.Split(" ");

            commandName = tempMessages.LastOrDefault(x => commands.Commands.Any(y => y.Name.ToLower().Equals(x.ToLower())));
            moduleNames = tempMessages.TakeWhile(x => !x.Equals(commandName ?? "")).ToArray();

            if (commandName != null)
            {
                var matches = commands.Commands.Where(x => x.Name.ToLower().Equals(commandName) && (moduleNames.Length > 0 ? x.Module.Name.ToLower().Equals(moduleNames.LastOrDefault()) : true));
                var perfectMatch = matches.FirstOrDefault(match => CommandToString(match).ToLower().Equals(((moduleNames.Count() > 0 ? string.Join(" ", moduleNames) + " " : "") + commandName).ToLower()));

                if (matches.Count() > 1 && perfectMatch == null)
                {
                    throw new Exception($"Multiple commands found, please specify between:\n```{String.Join("\n", matches.Select(x => CommandToString(x)))}```");
                }

                CommandInfo curCommand = perfectMatch;

                if (curCommand?.Summary.Equals("") ?? true)
                {
                    throw new Exception("Command not found");
                }

                output += $"`{prefix}{CommandToString(curCommand)}";

                foreach (Discord.Commands.ParameterInfo p in curCommand.Parameters)
                {
                    output += $" {(p.IsOptional ? $"[Optional: {p.Name}]" : $"<{p.Name}>")}";
                }
                output += "`";

                string preconditions = "";
                foreach (var prec in curCommand.Preconditions)
                {
                    if (prec.GetType() == typeof(RequireUserPermissionAttribute))
                    {
                        var permission = ((RequireUserPermissionAttribute)prec).ChannelPermission.HasValue ?
                                         ((RequireUserPermissionAttribute)prec).ChannelPermission.Value.ToString() :
                                         ((RequireUserPermissionAttribute)prec).GuildPermission.Value.ToString();
                        preconditions += $"Requires UserPermission: {permission}\n";
                    }
                    else if (prec.GetType() == typeof(RequireBotPermissionAttribute))
                    {
                        var permission = ((RequireBotPermissionAttribute)prec).ChannelPermission.HasValue ?
                                         ((RequireBotPermissionAttribute)prec).ChannelPermission.Value.ToString() :
                                         ((RequireBotPermissionAttribute)prec).GuildPermission.Value.ToString();
                        preconditions += $"Requires BotPermission: {permission}\n";
                    }
                    else
                    {
                        preconditions += prec + "\n";
                    }
                }

                e = createHelpEmbed($"{CommandToString(curCommand)}", output, curCommand.Summary, e, preconditions);
                // if(curCommand.Parameters.Any(x=> x.IsOptional)){
                //     output +="\n\n**Default Values**:";
                //     foreach(var p in curCommand.Parameters.Where(x=>x.IsOptional))
                //         output+=$"\n    {p.Name}: {p.DefaultValue}";
                // }

            }
            else
            {
                var module = Program.Handler.commands.Modules.FirstOrDefault(x => x.Name.ToLower().Equals(moduleNames.First().ToLower()));
                foreach (var mod in moduleNames.Skip(1))
                {
                    module = module.Submodules.FirstOrDefault(x => x.Name.Equals(mod, StringComparison.InvariantCultureIgnoreCase));
                }
                if (module == null) throw new Exception("Command not found");

                string moduleInformation = "";
                moduleInformation += string.Join(", ", module.Commands.Where(x => !x.Preconditions.OfType<HideAttribute>().Any()).Select(x => $"[{x.Name}]({CommandHandler.GetCommandHelpImage($"{string.Join(" ", moduleNames)} {x.Name}")})"));
                moduleInformation += "\n";

                moduleInformation += string.Join(", ", module.Submodules.Where(x => !x.Preconditions.Any(y => y is HideAttribute)).Select(x => $"[{x.Name}\\*]({CommandHandler.GetCommandHelpImage(x.Name)})"));

                e.AddField($"**{module.Name}**", moduleInformation);
            }
            return e;
        }

        public static string CommandToString(CommandInfo c)
        {
            string output = c.Name;
            ModuleInfo module = c.Module;
            while (module?.IsSubmodule ?? false)
            {
                output = module.Name + " " + output;
                module = module.Parent;
            }

            return output;
        }

        public static string GetCommandHelpImage(string command)
        {
            return $"http://37.221.195.236/mops_example_usage/{command.ToLower()}.PNG?rand={StaticBase.ran.Next(0, 99)}".Replace(" ", "%20");
        }

        /// <summary>
        /// Reads all custom commands and saves them as a Dictionary
        /// </summary>
        private async Task loadCustomCommands()
        {
            try
            {
                CustomCommands = (await StaticBase.Database.GetCollection<Data.Entities.CustomCommands>("CustomCommands").FindAsync(x => true)).ToList().ToDictionary(x => x.GuildId, x => x);
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Critical, "", $"loading failed", e));
            }
        }

        private async Task<Embed> CreateErrorEmbedAsync(CommandInfo command, SocketCommandContext context, IResult result)
        {
            var embed = new EmbedBuilder();
            if (context.IsPrivate) return embed.WithDescription("Private channel. Not taken serious.").Build();

            embed.WithAuthor(x => x.WithName("Command failed").WithIconUrl(context.User.GetAvatarUrl()).WithUrl(context.Message.GetJumpUrl()));
            embed.WithDescription($"**{context.Message.Content}**\nGuild: {context.Guild.Name} ({context.Guild.Id})\nChannel: {context.Channel.Name} ({context.Channel.Id})");
            embed.WithCurrentTimestamp();
            embed.WithColor(Discord.Color.Red);
            embed.AddField("Command", command.Name + " (" + string.Join(", ", command.Parameters.Select(x => $"{x.Type.ToString()} {x.Name}{(x.IsOptional ? " = " + x.DefaultValue : "")}")) + ")");
            var userPermissions = (context.User as SocketGuildUser).GetPermissions(context.Channel as IGuildChannel);
            var mopsPermissions = context.Guild.GetUser(Program.Client.CurrentUser.Id).GetPermissions(context.Channel as IGuildChannel);
            embed.AddField("User", context.User.Username + "#" + context.User.Discriminator + " (" + context.User.Id + ")\nPerms: " +
                                   userPermissions.ToString(), inline: true);
            embed.AddField("Bot", Program.Client.CurrentUser.Username + "#" + Program.Client.CurrentUser.Discriminator + "\nPerms: " +
                                  mopsPermissions.ToString(), inline: true);

            string preconditions = "";
            foreach (var prec in command.Preconditions)
            {
                if (prec.GetType() == typeof(RequireUserPermissionAttribute))
                {
                    var permission = ((RequireUserPermissionAttribute)prec).ChannelPermission.HasValue ?
                                     ((RequireUserPermissionAttribute)prec).ChannelPermission.Value.ToString() :
                                     ((RequireUserPermissionAttribute)prec).GuildPermission.Value.ToString();
                    preconditions += $"Requires UserPermission: {permission} ({((await prec.CheckPermissionsAsync(context, command, _provider)).IsSuccess ? "**passed**" : "**failed**")})\n";
                }
                else if (prec.GetType() == typeof(RequireBotPermissionAttribute))
                {
                    var permission = ((RequireBotPermissionAttribute)prec).ChannelPermission.HasValue ?
                                     ((RequireBotPermissionAttribute)prec).ChannelPermission.Value.ToString() :
                                     ((RequireBotPermissionAttribute)prec).GuildPermission.Value.ToString();
                    preconditions += $"Requires BotPermission: {permission} ({((await prec.CheckPermissionsAsync(context, command, _provider)).IsSuccess ? "**passed**" : "**failed**")})\n";
                }
                else if (prec.GetType() != typeof(RequireUserVotepoints) && prec.GetType() != typeof(RatelimitAttribute))
                {
                    preconditions += prec + $" ({((await prec.CheckPermissionsAsync(context, command, _provider)).IsSuccess ? "**passed**" : "**failed**")})\n";
                }
                else
                {
                    preconditions += prec + " (Cannot test)\n";
                }
            }
            embed.AddField("Preconditions", preconditions.Length > 0 ? preconditions : "None");

            embed.AddField("Error", result.ErrorReason.Length > 0 ? result.ErrorReason : "None");
            if (mostRecentException != null)
                embed.AddField("Exception", (mostRecentException.GetBaseException().Message?.Length ?? 0) > 0 ? mostRecentException.GetBaseException().Message : "None");

            return embed.Build();
        }
    }
}
