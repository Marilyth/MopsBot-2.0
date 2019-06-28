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
        public CommandService commands { get; private set; }
        private DiscordSocketClient client;
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
            client = _provider.GetService<DiscordSocketClient>();

            commands = new CommandService();
            //_map.Add(commands);

            commands.AddTypeReader(typeof(MopsBot.Data.Tracker.BaseTracker), new Module.TypeReader.TrackerTypeReader());
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);

            await loadCustomCommands();
            client.MessageReceived += Client_MessageReceived;
            client.MessageReceived += HandleCommand;
            client.UserJoined += Client_UserJoined;
        }

        /// <summary>
        /// Manages experience gain whenever a message is recieved
        /// </summary>
        /// <param name="arg">The recieved message</param>
        private async Task Client_MessageReceived(SocketMessage arg)
        {
            //User Experience
            Task.Run(() =>
            {
                if (!arg.Author.IsBot && !arg.Content.StartsWith(GetGuildPrefixAsync(((ITextChannel)(arg.Channel)).GuildId).Result))
                {
                    //MopsBot.Data.Entities.User.ModifyUserAsync(arg.Author.Id, x => x.Experience += arg.Content.Length).Wait();
                }
            });
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

        /// <summary>
        /// Checks if message is a command, and executes it
        /// </summary>
        /// <param name="parameterMessage">The message to check</param>
        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            Task.Run(async () =>
            {
                // Don't handle the command if it is a system message
                var message = parameterMessage as SocketUserMessage;
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

                //StaticBase.people.AddStat(parameterMessage.Author.Id, 0, "experience");

                if (char.IsWhiteSpace(message.Content[argPos]))
                    argPos += 1;

                if (message.HasCharPrefix('?', ref argPos))
                {
                    await getCommands(parameterMessage, prefix);
                    return;
                }

                if(message.Content.StartsWith("[ProcessBotMessage]"))
                    argPos = "[ProcessBotMessage]".Length;

                // Create a Command Context
                var context = new SocketCommandContext(client, message);

                //Execute if command exists
                if (commands.Search(context, argPos).IsSuccess)
                {
                    await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"executed command: {parameterMessage.Content.Substring(argPos)}"));
                    var result = await commands.ExecuteAsync(context, argPos, _provider);
                    MopsBot.Module.Moderation.CustomCaller.Remove(message.Channel.Id);

                    // If the command failed, notify the user
                    if (!result.IsSuccess && !result.ErrorReason.Equals(""))
                    {
                        if (result.ErrorReason.Contains("The input text has too many parameters"))
                            await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}\nIf your parameter contains spaces, please wrap it around quotation marks like this: `\"A Parameter\"`.");
                        else if(!result.ErrorReason.Contains("Command not found"))
                            await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
                    }
                }

                //Else execute custom commands
                else if (!message.Author.IsBot && CustomCommands.ContainsKey(context.Guild.Id) && CustomCommands[context.Guild.Id].Commands.ContainsKey(context.Message.Content.Substring(argPos).Split(" ").First()))
                {
                    await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"executed command: {parameterMessage.Content.Substring(argPos)}"));
                    await commands.Commands.First(x => x.Name.Equals("UseCustomCommand")).ExecuteAsync(context, new List<object> { $"{context.Message.Content.Substring(argPos)}" }, new List<object> { }, _provider);
                }
            });
        }

        /// <summary>
        /// Creates help message as well as command information, and sends it
        /// </summary>
        /// <param name="msg">The message recieved</param>
        public async Task getCommands(SocketMessage msg, string prefix)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithDescription("For more information regarding a **specific command** or **command group***,\nplease use **?<command>** or " +
                              $"**{prefix}help <command>**")
             .WithColor(Discord.Color.Blue)
             .WithAuthor(async x =>
             {
                 x.IconUrl = (await ((IDiscordClient)Program.Client).GetGuildAsync(435919579005321237)).IconUrl;
                 x.Name = "Click to join the Support Server!";
                 x.Url = "https://discord.gg/wZFE2Zs";
             });

            string message = msg.Content.Replace("?", "").ToLower();
            Embed embed = getHelpEmbed(message, prefix, e).Build();

            if (embed != null)
                await msg.Channel.SendMessageAsync("", embed: embed);
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
                CommandInfo curCommand = commands.Commands.First(x => x.Name.ToLower().Equals(commandName) && (moduleNames.Length > 0 ? x.Module.Name.ToLower().Equals(moduleNames.LastOrDefault()) : true));

                if (curCommand.Summary.Equals(""))
                {
                    throw new Exception("Command not found");
                }
                output += $"`{prefix}{string.Join(" ", moduleNames.Append(curCommand.Name))}";

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
                        preconditions += prec;
                    }
                }

                e = createHelpEmbed($"{string.Join(" ", moduleNames.Append(curCommand.Name))}", output, curCommand.Summary, e, preconditions);
                // if(curCommand.Parameters.Any(x=> x.IsOptional)){
                //     output +="\n\n**Default Values**:";
                //     foreach(var p in curCommand.Parameters.Where(x=>x.IsOptional))
                //         output+=$"\n    {p.Name}: {p.DefaultValue}";
                // }

            }
            else
            {
                var module = Program.Handler.commands.Modules.FirstOrDefault(x => x.Name.ToLower().Equals(moduleNames.First().ToLower()));
                foreach(var mod in moduleNames.Skip(1)){
                    module = module.Submodules.FirstOrDefault(x => x.Name.Equals(mod, StringComparison.InvariantCultureIgnoreCase));
                }
                if(module == null) throw new Exception("Command not found");

                string moduleInformation = "";
                moduleInformation += string.Join(", ", module.Commands.Where(x => !x.Preconditions.OfType<HideAttribute>().Any()).Select(x => $"[{x.Name}]({CommandHandler.GetCommandHelpImage($"{string.Join(" ", moduleNames)} {x.Name}")})"));
                moduleInformation += "\n";

                moduleInformation += string.Join(", ", module.Submodules.Select(x => $"[{x.Name}\\*]({CommandHandler.GetCommandHelpImage($"{string.Join(" ", moduleNames)} {x.Name}")})"));

                e.AddField($"**{module.Name}**", moduleInformation);
            }
            return e;
        }

        public static string GetCommandHelpImage(string command)
        {
            return $"http://5.45.104.29/mops_example_usage/{command.ToLower()}.PNG?rand={StaticBase.ran.Next(0, 999999999)}".Replace(" ", "%20");
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
    }
}
