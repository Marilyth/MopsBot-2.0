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

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);

            GuildPrefix = new Dictionary<ulong, string>();
            fillPrefix();
            loadCustomCommands();
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
            if (!arg.Author.IsBot)
            {
                await StaticBase.Users.ModifyUserAsync(arg.Author.Id, x => x.Experience += arg.Content.Length);
            }
        }

        /// <summary>
        /// Greets a user when he joins a Guild
        /// </summary>
        /// <param name="User">The User who joined</param>
        private async Task Client_UserJoined(SocketGuildUser User)
        {
            if(StaticBase.WelcomeMessages.ContainsKey(User.Guild.Id)){
                await WelcomeMessages[User.Guild.Id].SendWelcomeMessageAsync(User);
            }
        }

        /// <summary>
        /// Checks if message is a command, and executes it
        /// </summary>
        /// <param name="parameterMessage">The message to check</param>
        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null || message.Author.IsBot) return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;

            //Determines if the Guild has set a special prefix, if not, ! is used
            ulong id = 0;
            if (message.Channel is Discord.IDMChannel) id = message.Channel.Id;
            else id = ((SocketGuildChannel)message.Channel).Guild.Id;
            var prefix = GuildPrefix.ContainsKey(id) ? GuildPrefix[id] : "!";

            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasStringPrefix(prefix, ref argPos) || message.HasCharPrefix('?', ref argPos))) return;

            //StaticBase.people.AddStat(parameterMessage.Author.Id, 0, "experience");

            if (char.IsWhiteSpace(message.Content[argPos]))
                argPos += 1;

            if (message.HasCharPrefix('?', ref argPos))
            {
                await getCommands(parameterMessage, prefix);
                return;
            }

            // Create a Command Context
            var context = new SocketCommandContext(client, message);
            // Execute the Command, store the result
            var result = await commands.ExecuteAsync(context, argPos, _provider);

            // If the command failed, notify the user
            if (!result.IsSuccess && !result.ErrorReason.Equals(""))
            {
                if(result.ErrorReason.Contains("Object reference not set to an instance of an object"))
                    await message.Channel.SendMessageAsync($"**Error:** Mops just restarted and needs to initialise things first.\nTry again in a minute!");
                else if(result.ErrorReason.Contains("The input text has too many parameters"))
                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}\nIf your parameter contains spaces, please wrap it around quotation marks like this: `\"A Parameter\"`.");
                else if(!result.ErrorReason.Contains("Unknown command"))
                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
                else{
                    await commands.Commands.First(x => x.Name.Equals("UseCustomCommand")).ExecuteAsync(context, new List<object>{$"{context.Message.Content.Substring(argPos)}"}, new List<object>{}, _provider);
                }
            }
        }

        /// <summary>
        /// Creates help message as well as command information, and sends it
        /// </summary>
        /// <param name="msg">The message recieved</param>
        public async Task getCommands(SocketMessage msg, string prefix)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithDescription("For more information regarding a **specific command**, please use **?<command>**\n" +
                              "To see the commands of a **submodule\\***, please use **help <submodule>**.")
             .WithColor(Discord.Color.Blue)
             .WithAuthor(async x => {
                 x.IconUrl = (await ((IDiscordClient)Program.Client).GetGuildAsync(435919579005321237)).IconUrl;
                 x.Name = "Click to join the Support Server!";
                 x.Url = "https://discord.gg/wZFE2Zs";
             });
             
            string message = msg.Content.Replace("?", "").ToLower();
            Embed embed = getHelpEmbed(message, prefix, e).Build();
            
            if(embed != null)
                await msg.Channel.SendMessageAsync("", embed: embed);
        }


        /// <summary>
        /// Creates the embed that is sent whenever ?command is called
        /// </summary>
        /// <param name="command">The command to create the embed for</param>
        /// <param name="usage">The usage example to include in the embed</param>
        /// <param name="description">The desciption to include in the embed</param>
        private EmbedBuilder createHelpEmbed(string command, string usage, string description, EmbedBuilder e){
            //EmbedBuilder e = new EmbedBuilder();
            e.Title = command;
            e.ImageUrl = GetCommandHelpImage(command);

            e.AddField("Example usage", usage);
            e.Description = description;

            return e;
        }

        public EmbedBuilder getHelpEmbed(string message, string prefix, EmbedBuilder e = null){
            if(e is null){
                e = new EmbedBuilder();
                e.Color = new Color(0x0099ff);
            }

            var output = "";
            string commandName = message, moduleName = message;
            string[] tempMessages;
            if ((tempMessages = message.Split(' ')).Length > 1)
            {
                moduleName = tempMessages[0];
                commandName = tempMessages[1];
            }

            if (commands.Commands.ToList().Exists(x => x.Name.ToLower().Equals(commandName)))
            {
                CommandInfo curCommand = commands.Commands.First(x => x.Name.ToLower().Equals(commandName));
                if (!moduleName.Equals(commandName))
                {
                    curCommand = commands.Modules.First(x => x.Name.ToLower().Equals(moduleName)).Commands.First(x => x.Name.ToLower().Equals(commandName));
                }

                if(curCommand.Summary.Equals("")){
                    throw new Exception("Command not found");
                }
                output += $"`{prefix}{(curCommand.Module.IsSubmodule ? curCommand.Module.Name + " " + curCommand.Name : curCommand.Name)}";
                foreach (Discord.Commands.ParameterInfo p in curCommand.Parameters)
                {
                    output += $" {(p.IsOptional ? $"[Optional: {p.Name}]" : $"<{p.Name}>")}";
                }
                output += "`";

                e = createHelpEmbed($"{(curCommand.Module.IsSubmodule ? curCommand.Module.Name + " " + curCommand.Name : curCommand.Name)}", output, curCommand.Summary, e);
                // if(curCommand.Parameters.Any(x=> x.IsOptional)){
                //     output +="\n\n**Default Values**:";
                //     foreach(var p in curCommand.Parameters.Where(x=>x.IsOptional))
                //         output+=$"\n    {p.Name}: {p.DefaultValue}";
                // }

            }
            else
            {
                var module = Program.Handler.commands.Modules.First(x => x.Name.ToLower().Equals(moduleName.ToLower()));
                
                string moduleInformation = "";
                moduleInformation += string.Join(", ", module.Commands.Where(x => !x.Preconditions.OfType<HideAttribute>().Any()).Select(x => $"[{x.Name}]({CommandHandler.GetCommandHelpImage($"{module.Name} {x.Name}")})"));
                moduleInformation += "\n";

                moduleInformation += string.Join(", ", module.Submodules.Select(x => $"{x.Name}\\*"));

                e.AddField($"**{module.Name}**", moduleInformation);
            }
            return e;
        }

        public static string GetCommandHelpImage(string command){
            return $"http://5.45.104.29/mops_example_usage/{command.ToLower()}.PNG?rand={StaticBase.ran.Next(0, 999999999)}".Replace(" ", "%20");
        }

        /// <summary>
        /// Reads all custom commands and saves them as a Dictionary
        /// </summary>
        private void loadCustomCommands()
        {
            
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//CustomCommands.json", FileMode.OpenOrCreate)))
            {
                try
                {
                    CustomCommands = JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<string, string>>>(read.ReadToEnd()) ?? new Dictionary<ulong, Dictionary<string, string>>();
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n" +  e.Message + e.StackTrace);
                }
            }
        }

        /// <summary>
        /// Reads all guild prefixes and saves them as a dictionary
        /// </summary>
        private void fillPrefix()
        {
            string s = "";
            using (StreamReader read = new StreamReader(new FileStream("mopsdata//guildprefixes.txt", FileMode.OpenOrCreate)))
            {
                while ((s = read.ReadLine()) != null)
                {
                    try
                    {
                        var trackerInformation = s.Split('|');
                        var prefix = trackerInformation[1];
                        var guildID = ulong.Parse(trackerInformation[0]);
                        if (!GuildPrefix.ContainsKey(guildID))
                        {
                            GuildPrefix.Add(guildID, prefix);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\n" +  $"[ERROR] by GuildPrefixes at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }
    }
}
