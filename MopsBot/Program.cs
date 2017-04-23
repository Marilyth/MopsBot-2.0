using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
            });

            StreamReader sr = new StreamReader(new FileStream("data//config.txt", FileMode.Open));

            var token = sr.ReadLine();
            twitchId = sr.ReadLine();

            sr.Close();

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            client.Log += Client_Log;

            var map = new DependencyMap();
            map.Add(client);

            handler = new CommandHandler();
            await handler.Install(map);

            new StaticBase();

            await Task.Delay(-1);
        }

        private Task Client_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
