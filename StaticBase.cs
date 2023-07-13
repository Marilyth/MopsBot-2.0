using System;
using Discord;
using Discord.Rest;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data;
using MopsBot.Data.Tracker;
using MopsBot.Data.Interactive;
using Tweetinvi;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace MopsBot
{
    public class StaticBase
    {
        public static MongoClient DatabaseClient = new MongoClient($"{Program.Config["DatabaseURL"]}");
        public static IMongoDatabase Database = DatabaseClient.GetDatabase("Mops");
        public static System.Net.Http.HttpClient HttpClient = new System.Net.Http.HttpClient(new System.Net.Http.SocketsHttpHandler()
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                // Use DNS to look up the IP address(es) of the target host
                IPHostEntry ipHostEntry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host);

                // Filter for IPv4 addresses only
                IPAddress ipAddress = ipHostEntry
                    .AddressList
                    .FirstOrDefault(i => i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                // Fail the connection if there aren't any IPV4 addresses
                if (ipAddress == null)
                {
                    throw new Exception($"No IP4 address for {context.DnsEndPoint.Host}");
                }

                // Open the connection to the target host/port
                System.Net.Sockets.TcpClient tcp = new();
                await tcp.ConnectAsync(ipAddress, context.DnsEndPoint.Port, cancellationToken);

                // Return the NetworkStream to the caller
                return tcp.GetStream();
            }
        });

        //public static AuthDiscordBotListApi DiscordBotList = new AuthDiscordBotListApi(305398845389406209, Program.Config["DiscordBotListKey"]);
        public static Random ran = new Random();
        public static List<string> Playlist = new List<string>();
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public static Dictionary<ulong, Data.Entities.WelcomeMessage> WelcomeMessages;
        public static Dictionary<ulong, Data.Entities.CustomCommands> CustomCommands;
        public static ReactionGiveaway ReactGiveaways;
        public static ReactionRoleJoin ReactRoleJoin;
        public static ReactionPoll Poll;
        //public static Crosswords Crosswords;
        public static Dictionary<BaseTracker.TrackerType, TrackerWrapper> Trackers;
        public static Dictionary<ulong, MopsBot.Data.Entities.TwitchUser> TwitchUsers;
        public static Dictionary<ulong, MopsBot.Data.Entities.TwitchGuild> TwitchGuilds;
        public static Dictionary<ulong, MopsBot.Data.Entities.ChannelJanitor> ChannelJanitors;
        public static double GetMopsRAM() => ((System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024) / 1024);

        public static bool init = false;

        /// <summary>
        /// Initialises and loads all trackers
        /// </summary>
        public static void initTracking()
        {
            if (!init)
            {
                //Disable trackers without keys provided
                var twitterKeys = new List<string>() { "TwitterKey", "TwitterSecret", "TwitterToken", "TwitterAccessSecret" };
                if (twitterKeys.Any(key => !Program.Config.ContainsKey(key) || string.IsNullOrEmpty(Program.Config[key])))
                    Program.TrackerLimits["Twitter"]["TrackersPerServer"] = 0;

                var youtubeKeys = new List<string>() { "YoutubeKey" };
                if (youtubeKeys.Any(key => !Program.Config.ContainsKey(key) || string.IsNullOrEmpty(Program.Config[key])))
                {
                    Program.TrackerLimits["Youtube"]["TrackersPerServer"] = 0;
                    Program.TrackerLimits["YoutubeLive"]["TrackersPerServer"] = 0;
                }

                var twitchKeys = new List<string>() { "TwitchKey", "TwitchSecret" };
                if (twitchKeys.Any(key => !Program.Config.ContainsKey(key) || string.IsNullOrEmpty(Program.Config[key])))
                {
                    Program.TrackerLimits["Twitch"]["TrackersPerServer"] = 0;
                    Program.TrackerLimits["TwitchClip"]["TrackersPerServer"] = 0;
                }

                var osuKeys = new List<string>() { "OsuKey" };
                if (osuKeys.Any(key => !Program.Config.ContainsKey(key) || string.IsNullOrEmpty(Program.Config[key])))
                    Program.TrackerLimits["Osu"]["TrackersPerServer"] = 0;

                var steamKeys = new List<string>() { "SteamKey" };
                if (steamKeys.Any(key => !Program.Config.ContainsKey(key) || string.IsNullOrEmpty(Program.Config[key])))
                    Program.TrackerLimits["Steam"]["TrackersPerServer"] = 0;

                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");
                ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpClient.DefaultRequestHeaders.ConnectionClose = true;
                HttpClient.Timeout = TimeSpan.FromSeconds(60);
                ServicePointManager.DefaultConnectionLimit = 100;
                ServicePointManager.MaxServicePointIdleTime = 10000;

                if (Program.TrackerLimits["Twitter"]["TrackersPerServer"] > 0)
                {
                    /* Twitter API is now paid, so we can't use it anymore.
                    Auth.SetUserCredentials(Program.Config["TwitterKey"], Program.Config["TwitterSecret"],
                                            Program.Config["TwitterToken"], Program.Config["TwitterAccessSecret"]);
                    TweetinviConfig.CurrentThreadSettings.TweetMode = TweetMode.Extended;
                    TweetinviConfig.ApplicationSettings.TweetMode = TweetMode.Extended;
                    Tweetinvi.ExceptionHandler.SwallowWebExceptions = false;
                    Tweetinvi.RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackOnly;
                    TweetinviEvents.QueryBeforeExecute += Data.Tracker.TwitterTracker.QueryBeforeExecute;
                    Tweetinvi.Logic.JsonConverters.JsonPropertyConverterRepository.JsonConverters.Remove(typeof(Tweetinvi.Models.Language));
                    Tweetinvi.Logic.JsonConverters.JsonPropertyConverterRepository.JsonConverters.Add(typeof(Tweetinvi.Models.Language), new CustomJsonLanguageConverter());
                    */
                }

                Trackers = new Dictionary<BaseTracker.TrackerType, Data.TrackerWrapper>();
                foreach (var tracker in Enum.GetValues(typeof(BaseTracker.TrackerType)).Cast<BaseTracker.TrackerType>())
                {
                    // Twitter API is now paid, so we can't use it anymore.
                    if(tracker == BaseTracker.TrackerType.Twitter)
                        continue;

                    if (!Program.TrackerLimits.ContainsKey(tracker.ToString()))
                    {
                        Program.TrackerLimits[tracker.ToString()] = new Dictionary<string, int>();
                        Program.TrackerLimits[tracker.ToString()]["PollInterval"] = 900000;
                        Program.TrackerLimits[tracker.ToString()]["UpdateInterval"] = 120000;
                        Program.TrackerLimits[tracker.ToString()]["TrackersPerServer"] = 20;
                    }

                    var trackerType = Type.GetType($"MopsBot.Data.Tracker.{tracker}Tracker");
                    var wrapperType = typeof(TrackerHandler<>).MakeGenericType(trackerType);
                    var pollInterval = Program.TrackerLimits[tracker.ToString()]["PollInterval"];
                    var updateInterval = Program.TrackerLimits[tracker.ToString()]["UpdateInterval"];
                    Trackers[tracker] = (TrackerWrapper)Activator.CreateInstance(wrapperType, new object[] { pollInterval, updateInterval });
                }

                foreach (var tracker in Trackers)
                {
                    var trackerType = tracker.Key;
                    if (Program.TrackerLimits[trackerType.ToString()]["TrackersPerServer"] <= 0)
                    {
                        Program.MopsLog(new LogMessage(LogSeverity.Error, "Handler init", $"Disabled {trackerType.ToString()}-Tracker, due to either missing API Keys or no trackers per server allowed!")).Wait();
                        continue;
                    }

                    if (tracker.Key == BaseTracker.TrackerType.Twitch)
                    {
                        Task.Run(() => TwitchTracker.ObtainTwitchToken());
                        Task.Run(() =>
                        {
                            tracker.Value.PostInitialisation();
                            Trackers[BaseTracker.TrackerType.TwitchGroup].PostInitialisation();
                            TwitchGuilds = Database.GetCollection<Data.Entities.TwitchGuild>("TwitchGuilds").FindSync(x => true).ToEnumerable().ToDictionary(x => x.DiscordId);
                            TwitchUsers = Database.GetCollection<Data.Entities.TwitchUser>("TwitchUsers").FindSync(x => true).ToEnumerable().ToDictionary(x => x.GuildPlusDiscordId);
                            foreach (var user in TwitchUsers) user.Value.PostInitialisation();
                        });
                    }
                    else if (tracker.Key == BaseTracker.TrackerType.YoutubeLive)
                    {
                        Task.Run(() =>
                        {
                            tracker.Value.PostInitialisation();
                            YoutubeLiveTracker.fetchChannelsBatch().Wait();
                        });
                    }
                    else if (tracker.Key != BaseTracker.TrackerType.TwitchGroup)
                        Task.Run(() => tracker.Value.PostInitialisation());

                    Program.MopsLog(new LogMessage(LogSeverity.Info, "Tracker init", $"Initialising {trackerType.ToString()}"));
                    Task.Delay((int)(60000 / Trackers.Count)).Wait();
                }

                try
                {
                    //ChannelJanitors = MopsBot.Data.Entities.ChannelJanitor.GetJanitors().Result;
                    //Program.MopsLog(new LogMessage(LogSeverity.Info, "React init", $"Janitors started")).Wait();
                    //WelcomeMessages = Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").FindSync(x => true).ToEnumerable().ToDictionary(x => x.GuildId);
                    //Program.MopsLog(new LogMessage(LogSeverity.Info, "React init", $"Welcome messages loaded")).Wait();
                    //ReactRoleJoin = new ReactionRoleJoin();
                    //Program.MopsLog(new LogMessage(LogSeverity.Info, "React init", $"React role joins loaded")).Wait();
                    //ReactGiveaways = new ReactionGiveaway();
                    //Program.MopsLog(new LogMessage(LogSeverity.Info, "React init", $"React giveaways loaded")).Wait();
                    //Poll = new ReactionPoll();
                    //Program.MopsLog(new LogMessage(LogSeverity.Info, "React init", $"React polls loaded")).Wait();
                }
                catch (Exception e)
                {
                    Program.MopsLog(new LogMessage(LogSeverity.Info, "React init", $"Weird thing happened", e)).Wait();
                }

                init = true;

            }
        }

        private static DateTime lastReset = DateTime.UtcNow;
        public static async Task ResetHttpClient()
        {
            //Youtube uses some kind of session information to block all further requests from a bot. 
            //It must be reset completely.
            if ((DateTime.UtcNow - lastReset).TotalHours > 0)
            {
                lastReset = DateTime.UtcNow;
                await Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"Client is blocked by YouTube, resetting (evil)."));
                HttpClient.Dispose();
                HttpClient = new System.Net.Http.HttpClient(new System.Net.Http.SocketsHttpHandler()
                {
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        // Use DNS to look up the IP address(es) of the target host
                        IPHostEntry ipHostEntry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host);

                        // Filter for IPv4 addresses only
                        IPAddress ipAddress = ipHostEntry
                            .AddressList
                            .FirstOrDefault(i => i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                        // Fail the connection if there aren't any IPV4 addresses
                        if (ipAddress == null)
                        {
                            throw new Exception($"No IP4 address for {context.DnsEndPoint.Host}");
                        }

                        // Open the connection to the target host/port
                        System.Net.Sockets.TcpClient tcp = new();
                        await tcp.ConnectAsync(ipAddress, context.DnsEndPoint.Port, cancellationToken);

                        // Return the NetworkStream to the caller
                        return tcp.GetStream();
                    }
                });
                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");
                HttpClient.DefaultRequestHeaders.ConnectionClose = true;
                HttpClient.Timeout = TimeSpan.FromSeconds(60);
            }
        }

        public static async Task InsertOrUpdatePrefixAsync(ulong guildId, string prefix)
        {
            bool hasEntry = (await Database.GetCollection<Data.Entities.MongoKVP<ulong, string>>("GuildPrefixes").FindAsync(x => x.Key == guildId)).ToList().Count == 1;

            if (!hasEntry)
                await Database.GetCollection<Data.Entities.MongoKVP<ulong, string>>("GuildPrefixes").InsertOneAsync(new Data.Entities.MongoKVP<ulong, string>(guildId, prefix));
            else
                await Database.GetCollection<Data.Entities.MongoKVP<ulong, string>>("GuildPrefixes").ReplaceOneAsync(x => x.Key == guildId, new Data.Entities.MongoKVP<ulong, string>(guildId, prefix));
        }

        public static async Task<string> GetGuildPrefixAsync(ulong guildId)
        {
            string prefix = (await Database.GetCollection<Data.Entities.MongoKVP<ulong, string>>("GuildPrefixes").FindAsync(x => x.Key == guildId)).FirstOrDefault()?.Value;
            return prefix ?? "!";
        }

        /// <summary>
        /// Updates the guild count, by displaying it as an activity.
        /// </summary>
        /// <returns>A Task that sets the activity</returns>
        public static async Task UpdateServerCount()
        {
            if (Program.Client.LoginState == LoginState.LoggedIn)
            {
                await Program.Client.SetActivityAsync(new Game($"{Program.Client.Guilds.Count} servers", ActivityType.Watching));

                /*try
                {
                    if (Program.Client.CurrentUser.Id == 305398845389406209)
                        await DiscordBotList.UpdateStats(Program.Client.Guilds.Count);
                }
                catch (Exception e)
                {
                    await Program.MopsLog(new LogMessage(LogSeverity.Error, "", "discord bot list api failed", e));
                }*/
            }

            await SendHeartbeat();
            await Task.Delay(30000);
            foreach (var client in Program.Client.Shards.Where(x => x.LoginState == LoginState.LoggedIn))
                await client.SetActivityAsync(new Game($"{client.Latency}ms Latency", ActivityType.Listening));
        }

        /*public static async Task UserVoted(ulong userId)
        {
            var user = await GetUserAsync(userId);
            await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"User {user.ToString()}({userId}) voted. Adding 10 VP to balance!"));
            await MopsBot.Data.Entities.User.ModifyUserAsync(userId, x => x.Money += 10);
            try
            {
                if (Program.Client.CurrentUser.Id == 305398845389406209)
                    await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("Thanks for voting for me!\nI have added 10 Votepoints to your balance!");
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", "messaging voter failed", e));
            }
        }*/

        public static async Task<RestUser> GetUserAsync(ulong userId)
        {
            return await Program.Client.Rest.GetUserAsync(userId);
        }

        /// <summary>
        /// Displays the tracker counts one after another.
        /// </summary>
        /// <returns>A Task that sets the activity</returns>
        public static async Task UpdateStatusAsync()
        {
            if (!init)
            {
                try
                {
                    if (Program.Client.LoginState == LoginState.LoggedIn)
                        await Program.Client.SetActivityAsync(new Game("Currently Restarting!", ActivityType.Playing));

                    await SendHeartbeat();
                    await Task.Delay(30000);
                }
                catch { }

                int status = Enum.GetNames(typeof(BaseTracker.TrackerType)).Length;
                DateTime LastGC = default(DateTime);
                while (true)
                {
                    try
                    {
                        //Collect garbage when over 2GB of RAM is used
                        if (((System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024) / 1024) > 2200 && (DateTime.UtcNow - LastGC).TotalMinutes > 1)
                        {
                            await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"GC triggered."));
                            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                            System.GC.Collect();
                            LastGC = DateTime.UtcNow;
                        }

                        BaseTracker.TrackerType type = (BaseTracker.TrackerType)status++;
                        if (Program.TrackerLimits[type.ToString()]["TrackersPerServer"] <= 0)
                            continue;

                        //Skip everything after GW2, as this is hidden
                        if (type.ToString().Equals("GW2"))
                        {
                            status = Enum.GetNames(typeof(BaseTracker.TrackerType)).Length;
                            continue;
                        }

                        var trackerCount = Trackers[type].GetTrackers().Count;

                        if (Program.Client.LoginState == LoginState.LoggedIn)
                            await Program.Client.SetActivityAsync(new Game($"{trackerCount} {type.ToString()} Trackers", ActivityType.Watching));
                    }
                    catch
                    {
                        //Trackers were not initialised yet, or status exceeded trackertypes
                        //Show servers instead
                        status = 0;
                        try
                        {
                            await UpdateServerCount();
                        }
                        catch { }
                    }
                    finally
                    {
                        await SendHeartbeat();
                    }
                    await Task.Delay(30000);
                }
            }
        }

        public static async Task SendHeartbeat()
        {
            var messageReport = string.Join(" ", CommandHandler.MessagesPerGuild.OrderByDescending(x => x.Value).Take(1).Select(x => $"Guild {Program.Client.GetGuild(x.Key).Name} ({x.Key}) sent {x.Value} messages."));
            await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Heartbeat. I am still alive :)\nRatio: {MopsBot.Module.Information.FailedRequests} failed vs {MopsBot.Module.Information.SucceededRequests} succeeded requests\nSpamcheck: {messageReport}"));
            CommandHandler.MessagesPerGuild = new Dictionary<ulong, int>();
            MopsBot.Module.Information.FailedRequests = 0;
            MopsBot.Module.Information.SucceededRequests = 0;
        }
    }
    public class CustomJsonLanguageConverter : Tweetinvi.Logic.JsonConverters.JsonLanguageConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            return reader.Value != null
                ? base.ReadJson(reader, objectType, existingValue, serializer)
                : Tweetinvi.Models.Language.English;
        }
    }
}
