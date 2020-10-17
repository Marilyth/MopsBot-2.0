using System;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Rest;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using MopsBot.Data;
using MopsBot.Data.Tracker;
using MopsBot.Data.Interactive;
using Tweetinvi;
using Tweetinvi.Logic;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace MopsBot
{
    public class StaticBase
    {
        public static MongoClient DatabaseClient = new MongoClient($"{Program.Config["DatabaseURLLocal"]}");
        public static IMongoDatabase Database = DatabaseClient.GetDatabase("Mops");
        public static readonly System.Net.Http.HttpClient HttpClient = new System.Net.Http.HttpClient();
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
                var test = Database;
                var tost = DatabaseClient;
                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");
                ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpClient.DefaultRequestHeaders.ConnectionClose = true;
                HttpClient.Timeout = TimeSpan.FromSeconds(10);
                ServicePointManager.DefaultConnectionLimit = 100;
                ServicePointManager.MaxServicePointIdleTime = 10000;

                Auth.SetUserCredentials(Program.Config["TwitterKey"], Program.Config["TwitterSecret"],
                                        Program.Config["TwitterToken"], Program.Config["TwitterAccessSecret"]);
                TweetinviConfig.CurrentThreadSettings.TweetMode = TweetMode.Extended;
                TweetinviConfig.ApplicationSettings.TweetMode = TweetMode.Extended;
                Tweetinvi.ExceptionHandler.SwallowWebExceptions = false;
                Tweetinvi.RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackOnly;
                TweetinviEvents.QueryBeforeExecute += Data.Tracker.TwitterTracker.QueryBeforeExecute;
                Tweetinvi.Logic.JsonConverters.JsonPropertyConverterRepository.JsonConverters.Remove(typeof(Tweetinvi.Models.Language));
                Tweetinvi.Logic.JsonConverters.JsonPropertyConverterRepository.JsonConverters.Add(typeof(Tweetinvi.Models.Language), new CustomJsonLanguageConverter());

                Trackers = new Dictionary<BaseTracker.TrackerType, Data.TrackerWrapper>();
                Trackers[BaseTracker.TrackerType.Twitter] = new TrackerHandler<TwitterTracker>(1800000);
                Trackers[BaseTracker.TrackerType.Youtube] = new TrackerHandler<YoutubeTracker>(3600000);
                Trackers[BaseTracker.TrackerType.Twitch] = new TrackerHandler<TwitchTracker>(3600000);
                Trackers[BaseTracker.TrackerType.YoutubeLive] = new TrackerHandler<YoutubeLiveTracker>(900000);
                Trackers[BaseTracker.TrackerType.Reddit] = new TrackerHandler<RedditTracker>();
                Trackers[BaseTracker.TrackerType.JSON] = new TrackerHandler<JSONTracker>(updateInterval: 600000);
                Trackers[BaseTracker.TrackerType.Osu] = new TrackerHandler<OsuTracker>();
                Trackers[BaseTracker.TrackerType.Overwatch] = new TrackerHandler<OverwatchTracker>(3600000);
                Trackers[BaseTracker.TrackerType.TwitchGroup] = new TrackerHandler<TwitchGroupTracker>(60000);
                Trackers[BaseTracker.TrackerType.TwitchClip] = new TrackerHandler<TwitchClipTracker>();
                Trackers[BaseTracker.TrackerType.OSRS] = new TrackerHandler<OSRSTracker>();
                Trackers[BaseTracker.TrackerType.HTML] = new TrackerHandler<HTMLTracker>();
                Trackers[BaseTracker.TrackerType.RSS] = new TrackerHandler<RSSTracker>(3600000);
                Trackers[BaseTracker.TrackerType.Steam] = new TrackerHandler<SteamTracker>();

                foreach (var tracker in Trackers)
                {
                    var trackerType = tracker.Key;

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

                try{
                    ChannelJanitors = MopsBot.Data.Entities.ChannelJanitor.GetJanitors().Result;
                    Program.MopsLog(new LogMessage(LogSeverity.Error, "React init", $"Janitors started")).Wait();
                    WelcomeMessages = Database.GetCollection<Data.Entities.WelcomeMessage>("WelcomeMessages").FindSync(x => true).ToEnumerable().ToDictionary(x => x.GuildId);
                    Program.MopsLog(new LogMessage(LogSeverity.Error, "React init", $"Welcome messages loaded")).Wait();
                    ReactRoleJoin = new ReactionRoleJoin();
                    Program.MopsLog(new LogMessage(LogSeverity.Error, "React init", $"React role joins loaded")).Wait();
                    ReactGiveaways = new ReactionGiveaway();
                    Program.MopsLog(new LogMessage(LogSeverity.Error, "React init", $"React giveaways loaded")).Wait();
                } catch (Exception e){
                    Program.MopsLog(new LogMessage(LogSeverity.Error, "React init", $"Weird thing happened", e)).Wait();
                }
                
                init = true;

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
            if(Program.Client.LoginState == LoginState.LoggedIn){
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

        public static async Task<SocketGuildUser> GetGuildUserAsync(ulong guildId, ulong userId)
        {
            var guild = Program.Client.GetGuild(guildId);
            if (!guild.HasAllMembers)
                await guild.DownloadUsersAsync();
            return guild.GetUser(userId);
        }

        public static async Task<RestUser> GetUserAsync(ulong userId)
        {
            return await Program.Client.Shards.First().Rest.GetUserAsync(userId);
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
                    if(Program.Client.LoginState == LoginState.LoggedIn)
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

                        //Skip everything after GW2, as this is hidden
                        if (type.ToString().Equals("GW2"))
                        {
                            status = Enum.GetNames(typeof(BaseTracker.TrackerType)).Length;
                            continue;
                        }

                        var trackerCount = Trackers[type].GetTrackers().Count;

                        if(Program.Client.LoginState == LoginState.LoggedIn)
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
