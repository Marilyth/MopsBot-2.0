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
using System.Diagnostics;

namespace MopsBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Task.Run(() => BuildWebHost(args).Run());
            Task.Run(() => BuildTwitterWebHost(args).Run());
            new Program().Start().GetAwaiter().GetResult();
        }
        public static DiscordSocketClient Client;
        public static Dictionary<string, string> Config;
        public static CommandHandler Handler { get; private set; }
        public static ReactionHandler ReactionHandler { get; private set; }
        private System.Threading.Timer garbageCollector;
        private int ProcessId;
        private int OpenFilesLastRead;
        private int OpenFilesSameCount;
        private static int OpenFilesSameCounThreshold = 5;
        
        private async Task Start()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true
            });

            using (StreamReader sr = new StreamReader(new FileStream("mopsdata//Config.json", FileMode.Open)))
                Config = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());

            await Client.LoginAsync(TokenType.Bot, Config["Discord"]);
            await Client.StartAsync();

            Client.Log += Client_Log;
            Client.Ready += onClientReady;

            var map = new ServiceCollection().AddSingleton(Client)
                // .AddSingleton(new AudioService())
                .AddSingleton(new ReliabilityService(Client, Client_Log));

            var provider = map.BuildServiceProvider();

            Handler = new CommandHandler();
            await Handler.Install(provider);

            ReactionHandler = new ReactionHandler();
            ReactionHandler.Install(provider);

            garbageCollector = new System.Threading.Timer(collectGarbage, null, 1800000, 1800000);
            ProcessId = Process.GetCurrentProcess().Id;
            Console.Out.WriteLine(ProcessId);

            await Task.Delay(-1);
        }

        private Task Client_Log(LogMessage msg)
        {
            Console.WriteLine("\n" +  msg.ToString());
            return Task.CompletedTask;
        }


        private Task onClientReady()
        {
            Task.Run(() => StaticBase.initTracking());
            Task.Run(() => StaticBase.UpdateGameAsync());
            return Task.CompletedTask;
        }

        private void collectGarbage(object stateinfo)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void checkOpenFiles(){
            try{
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"ls -lisa /proc/{ProcessId}/fd | wc -l\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                int openFiles = Convert.ToInt32(result);
                if(OpenFilesLastRead == openFiles)
                    OpenFilesSameCount++;

                if(OpenFilesSameCount == OpenFilesSameCounThreshold)
                    Environment.Exit(-1);

                OpenFilesLastRead = openFiles;
                }catch(Exception e){
                    Environment.Exit(-1);
                }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://0.0.0.0:5000/")
                .ConfigureServices(x => x.AddCors(options => options.AddPolicy("AllowAllHeaders",
                    builder =>
                    {
                        builder.WithOrigins("*")
                               .AllowAnyHeader();
                    })))
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

        public static IWebHost BuildTwitterWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<TwitterStartup>()
                .UseUrls("http://0.0.0.0:8076/")
                .Build();
    }
}
