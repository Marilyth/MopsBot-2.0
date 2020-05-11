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
using Discord.Rest;
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
            Task.Run(() => BuildWebHost(args).Run());
            new Program().Start().GetAwaiter().GetResult();
        }
        public static DiscordShardedClient Client;
        public static Dictionary<string, string> Config;
        public static CommandHandler Handler { get; private set; }
        public static ReactionHandler ReactionHandler { get; private set; }
        private static ServiceProvider provider;
        private static List<ReliabilityService> failsafe = new List<ReliabilityService>();

        private async Task Start()
        {
            Client = new DiscordShardedClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                //TotalShards = 2,
                LargeThreshold = 50,
                MessageCacheSize = 0,
                AlwaysDownloadUsers = false,
            });

            using (StreamReader sr = new StreamReader(new FileStream("mopsdata//Config.json", FileMode.Open)))
                Config = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());

            await Client.LoginAsync(TokenType.Bot, Config["Discord"]);
            await Client.StartAsync();

            Client.Log += ClientLog;
            Client.ShardReady += onShardReady;

            await Task.Delay(-1);
        }

        public static async Task ClientLog(LogMessage msg)
        {
            await MopsLog(msg, "", msg.Source, -1);
        }

        public static async Task MopsLog(LogMessage msg, [CallerMemberName] string callerName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            string message = $"\n[{msg.Severity}] at {DateTime.Now}\nsource: {Path.GetFileNameWithoutExtension(callerPath)}.{callerName}, line: {callerLine}\nmessage: {msg.Message}";
            if (msg.Exception != null && !msg.Exception.Message.Contains("The SSL connection could not be established"))
            {
                message += $"\nException: {msg.Exception?.Message ?? ""}\nStacktrace: {msg.Exception?.StackTrace ?? ""}";
            }

            Console.WriteLine(message);
        }

        private static int shardsReady = 0;
        private DateTime LastGC = default(DateTime);
        private async Task onShardReady(DiscordSocketClient client)
        {
            shardsReady++;
            await MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Shard {shardsReady} is ready."));

            if((DateTime.UtcNow - LastGC).TotalMinutes > 1 && (((System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024) / 1024) > 2000
                || shardsReady == Client.Shards.Count)){
                LastGC = DateTime.UtcNow;
                await MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Shard {shardsReady} caused GC."));
                System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                System.GC.Collect();
            }

            if (shardsReady == 1)
            {
                Task.Run(() =>
                {
                    var map = new ServiceCollection().AddSingleton(Client)
                                                     .AddSingleton(new InteractiveService(Client));

                    foreach(var shard in Client.Shards){
                        failsafe.Add(new ReliabilityService(shard, ClientLog));
                    }

                    provider = map.BuildServiceProvider();

                    ReactionHandler = new ReactionHandler();
                    ReactionHandler.Install(provider);
                    Handler = new CommandHandler();
                    Handler.Install(provider).Wait();
                });
            }

            if (shardsReady == Client.Shards.Count)
            {
                Task.Run(() =>
                {
                    StaticBase.UpdateStatusAsync();
                    StaticBase.initTracking();
                });
            }
        }

        public static DiscordSocketClient GetShardFor(ulong channelId)
        {
            if(Client.GetChannel(channelId) != null)
                return Client.GetShardFor((Client.GetChannel(channelId) as SocketGuildChannel).Guild);
            return null;
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
