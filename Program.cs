using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Threading;
using System.Net;
using Newtonsoft.Json;

namespace MopsBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Task.Run(() => BuildWebHost(args).Run());
            new Program().Start().GetAwaiter().GetResult();

        }
        public static DiscordSocketClient client;
        public static Dictionary<string, string> Config;
        public static CommandHandler handler {get; private set;}
        public static ReactionHandler reactionHandler {get; private set;}

        public async Task Start()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
            });

            using(StreamReader sr = new StreamReader(new FileStream("mopsdata//Config.json", FileMode.Open)))
                Config = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());

            await client.LoginAsync(TokenType.Bot, Config["Discord"]);
            await client.StartAsync();

            client.Log += Client_Log;
            client.Ready += onClientReady;
            client.Disconnected += onClientDC;

            var map = new ServiceCollection().AddSingleton(client)
                .AddSingleton(new AudioService())
                .AddSingleton(new ReliabilityService(client, Client_Log));

            var provider = map.BuildServiceProvider();

            handler = new CommandHandler();
            await handler.Install(provider);

            reactionHandler = new ReactionHandler();
            await reactionHandler.Install(provider);

            await Task.Delay(-1);
        }

        private Task Client_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task onClientReady()
        {
            var test = client;
            Task.Run(() => StaticBase.initTracking());
            Task.Run(() => StaticBase.UpdateGameAsync());
            return Task.CompletedTask;
        }

        private async Task onClientDC(Exception e)
        {
            await Task.Run(() => StaticBase.disconnected());
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://0.0.0.0:5000/")
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
    }
}
