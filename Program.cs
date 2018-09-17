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
            //Task.Run(() => BuildWebHost(args).Run());
            new Program().Start().GetAwaiter().GetResult();
        }
        public static DiscordSocketClient Client;
        public static Dictionary<string, string> Config;
        public static CommandHandler Handler { get; private set; }
        public static ReactionHandler ReactionHandler { get; private set; }
        private System.Threading.Timer openFileChecker;

        //Open file handling
        private int ProcessId, OpenFilesCount, OpenFilesRepetition;
        private static int OpenFilesRepetitionThreshold = 5;

        private async Task Start()
        {
            ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            Console.Out.WriteLine(ProcessId);

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

            openFileChecker = new System.Threading.Timer(checkOpenFiles, null, 60000, 60000);

            await Task.Delay(-1);
        }

        private Task Client_Log(LogMessage msg)
        {
            Console.WriteLine("\n" + msg.ToString());
            return Task.CompletedTask;
        }

        private Task onClientReady()
        {
            var test = Client;
            Task.Run(() => StaticBase.initTracking());
            Task.Run(() => StaticBase.UpdateGameAsync());
            return Task.CompletedTask;
        }

        private void checkOpenFiles(object stateinfo)
        {
            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"ls -lisa /proc/{ProcessId}/fd | wc -l\"";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    process.WaitForExit();

                    string result = process.StandardOutput.ReadToEnd();
                    int openFiles = Convert.ToInt32(result);
                    Console.WriteLine("\n" + System.DateTime.Now.ToLongTimeString() + $" open files were {openFiles}");
                    if (OpenFilesCount == openFiles)
                        OpenFilesRepetition++;
                    
                    if (OpenFilesRepetition == OpenFilesRepetitionThreshold || OpenFilesCount > 600){
                        Console.WriteLine("\nShutting down due to deadlock or too many open files!");
                        Environment.Exit(-1);
                    }

                    OpenFilesCount = openFiles;
                }


            }
            catch (Exception e)
            {
                Console.WriteLine("\n[FILE READING ERROR]: " + System.DateTime.Now.ToLongDateString() + $" {e.Message}\n{e.StackTrace}");
                //Environment.Exit(-1);
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
    }
}
