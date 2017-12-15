using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using MopsBot.Module.Preconditions;

namespace MopsBot
{
    public class CommandHandler
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider _provider;
        public async Task Install(IServiceProvider provider)
        {
            // Create Command Service, inject it into Dependency Map
            _provider = provider;
            client = _provider.GetService<DiscordSocketClient>();

            commands = new CommandService();
            //_map.Add(commands);

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            client.MessageReceived += HandleCommand;
            client.UserJoined += Client_UserJoined;
            client.MessageReceived += Client_MessageReceived;
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            //Poll
            if (arg.Channel.Name.Contains((arg.Author.Username)) && StaticBase.poll != null)
            {
                if (StaticBase.poll.participants.ToList().Select(x => x.Id).ToArray().Contains(arg.Author.Id))
                {
                    StaticBase.poll.results[int.Parse(arg.Content) - 1]++;
                    await arg.Channel.SendMessageAsync("Vote accepted!");
                    StaticBase.poll.participants.RemoveAll(x => x.Id == arg.Author.Id);
                }

            }

            //Daily Statistics & User Experience
            if (!arg.Author.IsBot && !arg.Content.StartsWith("!"))
            {
                StaticBase.people.addStat(arg.Author.Id, arg.Content.Length, "experience");
                StaticBase.stats.addValue(arg.Content.Length);
            }
        }

        private async Task Client_UserJoined(SocketGuildUser User)
        {
            //PhunkRoyalServer Begruessung
            if (User.Guild.Id.Equals(205130885337448469))
                await User.Guild.GetTextChannel(305443055396192267).SendMessageAsync($"Willkommen im **{User.Guild.Name}** Server, {User.Mention}!" +
                $"\n\nBevor Du vollen Zugriff auf den Server hast, möchten wir Dich auf die Regeln des Servers hinweisen, die Du hier findest:" +
                $" {User.Guild.GetTextChannel(305443033296535552).Mention}\nSobald Du fertig bist, kannst Du Dich an einen unserer Moderatoren zu Deiner" +
                $" rechten wenden, die Dich alsbald zum Mitglied ernennen.\n\nHave a very mopsig day\nDein heimlicher Verehrer Mops");
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
            
            StaticBase.people.addStat(parameterMessage.Author.Id, 0, "experience");

            if (message.Content.Contains("help") || message.HasCharPrefix('?', ref argPos))
            {
                await getCommands(parameterMessage);
                return;
            }

            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the Command, store the result
            var result = await commands.ExecuteAsync(context, argPos, _provider);

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
                        output += $"`{module.Name}*` ";
                    }
                    else
                    {
                        output += $"\n**{module.Name}**: ";
                        foreach (var command in module.Commands)
                            if(!command.Preconditions.OfType<HideAttribute>().Any())
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
