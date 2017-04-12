using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace MopsBot
{
    public class CommandHandler
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IDependencyMap map;

        public async Task Install(IDependencyMap _map)
        {
            // Create Command Service, inject it into Dependency Map
            client = _map.Get<DiscordSocketClient>();
            commands = new CommandService();
            //_map.Add(commands);
            map = _map;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            client.MessageReceived += HandleCommand;
        }

        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos) || message.HasCharPrefix('?', ref argPos))) return;

            if (message.Content.Contains("help") || message.HasCharPrefix('?', ref argPos))
            {
                await getCommands(parameterMessage);
                return;
            }

            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the Command, store the result
            var result = await commands.ExecuteAsync(context, argPos, map);

            // If the command failed, notify the user
            if (!result.IsSuccess)
                await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
        }

        public Task getCommands(SocketMessage msg)
        {
            string output = "";

            if (msg.Content.Contains("help"))
            {
                output += "For more information regarding a specific command, please use ?<command>";

                foreach (var module in commands.Modules)
                {
                    if (module.IsSubmodule)
                    {
                        output += $"`{module.Name}*`";
                    }
                    else
                    {
                        output += $"\n**{module.Name}**: ";
                        foreach (var command in module.Commands)
                            output += $"`{command.Name}` ";
                    }
                }
            }

            else
            {
                string message = msg.Content.Replace("?", "").ToLower();

                string commandName = message, moduleName = message;

                string[] tempMessages;
                if ((tempMessages = message.Split(' ')).Length > 1)
                {
                    moduleName = tempMessages[0];
                    commandName = tempMessages[1];
                }

                if(commands.Commands.ToList().Exists(x => x.Name.ToLower().Equals(commandName)))
                {
                    CommandInfo curCommand = commands.Commands.First(x => x.Name.ToLower().Equals(commandName));
                    if (!moduleName.Equals(commandName))
                    {
                        curCommand = commands.Modules.First(x => x.Name.ToLower().Equals(moduleName)).Commands.First(x => x.Name.ToLower().Equals(commandName));
                    }

                    output += $"`{curCommand.Name}`:\n";
                    output += curCommand.Summary;
                    output += $"\n\n**Usage**: `!{(curCommand.Module.IsSubmodule ? curCommand.Module.Name + " " + curCommand.Name : curCommand.Name)}";
                    foreach (Discord.Commands.ParameterInfo p in curCommand.Parameters)
                    {
                        output += $" <{p.Name}>";
                    }
                    output += "`";
                }
                else
                {
                    ModuleInfo curModule = commands.Modules.First(x => x.Name.ToLower().Equals(moduleName));

                    output += $"**{curModule.Name}**:";

                    foreach (CommandInfo curCommand in curModule.Commands)
                        output += $" `{curCommand.Name}`";
                }
            }

            msg.Channel.SendMessageAsync(output);

            return Task.CompletedTask;
        }
    }
}
