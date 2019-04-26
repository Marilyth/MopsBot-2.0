using System;
using System.Runtime.CompilerServices;
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
using Discord.Addons.Interactive;

namespace MopsBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Task.Run(() => BuildWebHost(args).Run());
            new Program().Start().GetAwaiter().GetResult();
        }
        public static DiscordSocketClient Client;
        public static Dictionary<string, string> Config;
        public static CommandHandler Handler { get; private set; }
        public static ReactionHandler ReactionHandler { get; private set; }

        private async Task Start()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true
            });
            var test = new List<int>(){0, 1, 2, 3, 4, 5, 6, 7};
            //var tost = test[1..3];
            
            using (StreamReader sr = new StreamReader(new FileStream("mopsdata//Config.json", FileMode.Open)))
                Config = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());

            await Client.LoginAsync(TokenType.Bot, Config["Discord"]);
            await Client.StartAsync();

            Client.Log += ClientLog;
            Client.Ready += onClientReady;

            var map = new ServiceCollection().AddSingleton(Client)
                // .AddSingleton(new AudioService())
                .AddSingleton(new ReliabilityService(Client, ClientLog))
                .AddSingleton(new InteractiveService(Client));

            var provider = map.BuildServiceProvider();

            Handler = new CommandHandler();
            await Handler.Install(provider);

            ReactionHandler = new ReactionHandler();
            ReactionHandler.Install(provider);

            await Task.Delay(-1);
        }

        public static async Task ClientLog(LogMessage msg)
        {
            await MopsLog(msg, "", msg.Source, -1);
        }

        public static async Task MopsLog(LogMessage msg, [CallerMemberName] string callerName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            string message = $"\n[{msg.Severity}] at {DateTime.Now}\nsource: {Path.GetFileNameWithoutExtension(callerPath)}.{callerName}, line: {callerLine}\nmessage: {msg.Message}";
            if(msg.Exception != null && !msg.Exception.Message.Contains("The SSL connection could not be established")){
                message += $"\nException: {msg.Exception?.Message ?? ""}\nStacktrace: {msg.Exception?.StackTrace ?? ""}";
            }

            Console.WriteLine(message);
        }

        private Task onClientReady()
        {
            Task.Run(() => {
                StaticBase.UpdateStatusAsync();
                StaticBase.initTracking();
            });
            return Task.CompletedTask;
        }
        
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://0.0.0.0:5000/")
                .ConfigureServices(x => x.AddCors(options => options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials();
                    })))
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
    }
}
