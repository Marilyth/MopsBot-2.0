using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;

namespace MopsBot
{
    class Program
    {
        public static void Main(string[] args) =>
            new Program().Start().GetAwaiter().GetResult();

        public static DiscordSocketClient client;
        public static string twitchId;
        private CommandHandler handler;
        
        public async Task Start()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
            });
            
            StreamReader sr = new StreamReader(new FileStream("data//config.txt", FileMode.Open));

            var token = sr.ReadLine();
            twitchId = sr.ReadLine();          
            
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            client.Log += Client_Log;
            client.Ready += onClientReady;

            var map = new ServiceCollection().AddSingleton(client).AddSingleton(new AudioService());
            var provider = map.BuildServiceProvider();

            handler = new CommandHandler();
            await handler.Install(provider);

            var ids = sr.ReadLine();
            foreach(var id in ids.Split(':')){
                StaticBase.BotManager.Add(ulong.Parse(id));
            }

            sr.Dispose();

            await Task.Delay(-1);
        }

        private Task Client_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task onClientReady(){
            await Task.Run(() => StaticBase.initTracking());
        }
    }
}