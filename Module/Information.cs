using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Xml.Serialization;
using MopsBot.Module.Preconditions;

namespace MopsBot.Module
{
    public class Information : ModuleBase<ShardedCommandContext>
    {
        public static int FailedRequests = 0, SucceededRequests = 0, FailedRequestsTotal = 0, SucceededRequestsTotal = 0;

        [Command("HowLong")]
        [Summary("Returns the date you joined the Guild")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task howLong([Remainder]SocketGuildUser user = null)
        {
            if (user == null)
                user = (SocketGuildUser)Context.User;
            await ReplyAsync(user.JoinedAt.Value.Date.ToString("d"));
        }

        [Command("BotInfo", RunMode = RunMode.Async)]
        [Hide]
        [Summary("Returns information about the bot.")]
        [RequireBotManage]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task BotInfo()
        {
            using (Context.Channel.EnterTypingState())
            {
                using (var prc = new System.Diagnostics.Process())
                {
                    prc.StartInfo.FileName = "convert";
                    prc.StartInfo.Arguments = $"-set density 300 \"//var//www//html//StreamCharts//MopsKillerPlot.pdf\" \"//var//www//html//StreamCharts//MopsKillerPlot.png\"";

                    prc.Start();

                    prc.WaitForExit();
                }

                var embed = new EmbedBuilder();

                embed.WithColor(Discord.Color.Blue).WithCurrentTimestamp().WithTitle("Mops Statistics");

                embed.AddField(x =>
                {
                    x.WithName("Shards").WithValue(string.Join("\n", Program.Client.Shards.Select(y => (y.ConnectionState.Equals(ConnectionState.Connected) ? new Emoji("🟢") : new Emoji("🔴")) + $" Shard {y.ShardId} ({y.Guilds.Count} Servers, {y.Latency}ms)")));
                    x.IsInline = true;
                });

                embed.AddField(x =>
                {
                    var mopsbot = Process.GetCurrentProcess();
                    var runtime = DateTime.Now - mopsbot.StartTime;
                    x.WithName("Stats").WithValue($"Runtime: {(int)runtime.TotalHours}h:{runtime.ToString(@"m\m\:s\s")}\nHandleCount: {mopsbot.HandleCount}\nThreads: {mopsbot.Threads.Count}\nRAM: {(mopsbot.WorkingSet64 / 1024) / 1024}\nFailed Requests: {Information.FailedRequestsTotal}\nSucceeded Requests: {Information.SucceededRequestsTotal}");
                    x.IsInline = true;
                });

                embed.AddField(x => x.WithName("_ _").WithValue("_ _"));
                var third = (int)Math.Ceiling(StaticBase.Trackers.Count / 3.0);
                var sorted = StaticBase.Trackers.OrderByDescending(x => x.Value.GetTrackers().Count);
                for (int i = 0; i < StaticBase.Trackers.Count; i += third)
                {
                    embed.AddField(x =>
                    {
                        x.WithName("Trackers").WithValue(string.Join("\n", sorted.Skip(i).Take(third).Select(y => $"{y.Key}: {y.Value.GetTrackers().Count}")));
                        x.IsInline = true;
                    });
                }

                embed.WithImageUrl($"{Program.Config["ServerAddress"]}/StreamCharts/MopsKillerPlot.png?rand={StaticBase.ran.Next(0, 99)}");

                await Context.Channel.SendMessageAsync(embed: embed.Build());
            }
        }

        [Command("TrackerInfo", RunMode = RunMode.Async)]
        [Hide]
        [RequireBotManage]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task TrackerInfo()
        {
            using (Context.Channel.EnterTypingState())
            {
                var embeds = new List<Embed>();
                foreach (var handler in StaticBase.Trackers.OrderByDescending(x => x.Value.GetTrackers().Count))
                {
                    embeds.Add(await handler.Value.GetEmbed());
                }
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, embeds);
            }
        }

        /*[Command("Invite")]
        [Summary("Provides a link to make me join your Server")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task joinServer()
        {
            await ReplyAsync($"https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=271969344&scope=bot");
        }

        [Command("Vote")]
        [Summary("Provides link to vote for me!")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Vote()
        {
            await ReplyAsync($"https://discordbots.org/bot/{Program.Client.CurrentUser.Id}/vote");
        }*/

        [Command("Define", RunMode = RunMode.Async)]
        [Summary("Searches dictionaries for a definition of the specified word or expression")]
        [Ratelimit(1, 10, Measure.Seconds)]
        public async Task define([Remainder] string text)
        {
            using (Context.Channel.EnterTypingState())
            {

                string query = await GetURLAsync($"http://api.wordnik.com:80/v4/word.json/{text}/definitions?includeRelated=false&sourceDictionaries=all&useCanonical=true&includeTags=false&api_key={Program.Config["Wordnik"]}");
                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);

                var strings = new List<string>();
                foreach (var result in tempDict)
                {
                    strings.Add($"__**{result["word"]}**__\n\n``{result["text"]}``");
                }

                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel.Id, strings);
            }
        }

        [Command("Translate", RunMode = RunMode.Async)]
        [Summary("Translates your text from srcLanguage to tgtLanguage.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Ratelimit(1, 10, Measure.Seconds)]
        public async Task translate(string srcLanguage, string tgtLanguage, [Remainder] string text)
        {
            using (Context.Channel.EnterTypingState())
            {
                string query = await GetURLAsync($"https://translate.googleapis.com/translate_a/single?client=gtx&ie=UTF-8&oe=UTF-8&sl={srcLanguage}&tl={tgtLanguage}&dt=t&q={text}");
                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
                await ReplyAsync(tempDict[0][0][0].ToString());
            }
        }

        [Command("Wolfram", RunMode = RunMode.Async)]
        [Summary("Sends a query to wolfram alpha.")]
        [Ratelimit(1, 10, Measure.Seconds)]
        public async Task wolf([Remainder]string query)
        {
            using (Context.Channel.EnterTypingState())
            {
                var result = await GetURLAsync($"https://api.wolframalpha.com/v2/query?input={System.Web.HttpUtility.UrlEncode(query)}&format=image,plaintext&podstate=Step-by-step%20solution&output=JSON&appid={Program.Config["WolframAlpha"]}");
                var jsonResult = JsonConvert.DeserializeObject<Data.Tracker.APIResults.Wolfram.WolframResult>(result);
                var embeds = new List<Embed>();
                foreach (var pod in jsonResult.queryresult.pods)
                {
                    var image = pod.subpods.FirstOrDefault(x => x.title == "Possible intermediate steps")?.img.src ?? pod.subpods.First()?.img.src;
                    embeds.Add(new EmbedBuilder().WithTitle(pod.title).WithDescription(query).WithImageUrl(image).Build());
                }

                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(Context.Channel, embeds);
            }
        }

        public async static Task<dynamic> GetRandomWordAsync()
        {
            try
            {
                string query = await GetURLAsync($"http://api.wordnik.com:80/v4/words.json/randomWord?hasDictionaryDef=true&excludePartOfSpeech=given-name&minCorpusCount=10000&maxCorpusCount=-1&minDictionaryCount=4&maxDictionaryCount=-1&minLength=3&maxLength=13&api_key={Program.Config["Wordnik"]}");
                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
                return tempDict["word"];
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"Getting random word failed", e));
            }
            return null;
        }

        public static async Task<string> PostURLAsync(string URL, string body = "", params KeyValuePair<string, string>[] headers)
        {
            if (FailedRequests >= 10 && SucceededRequests / FailedRequests < 1)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Warning, "HttpRequests", $"More Failed requests {FailedRequests} than succeeded ones {SucceededRequests}. Waiting"));
                return "";
            }

            HttpRequestMessage test = new HttpRequestMessage(HttpMethod.Post, URL);
            test.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            foreach (var header in headers)
                test.Headers.TryAddWithoutValidation(header.Key, header.Value);

            using (var response = await StaticBase.HttpClient.SendAsync(test))
            {
                try
                {
                    string value = await response.Content.ReadAsStringAsync();
                    SucceededRequests++;
                    SucceededRequestsTotal++;
                    return value;
                }
                catch (Exception e)
                {
                    FailedRequests++;
                    FailedRequestsTotal++;
                    await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"error for sending post request to {URL}", e.GetBaseException()));
                    throw e;
                }
            }
        }

        public static async Task<string> GetURLAsync(string URL, params KeyValuePair<string, string>[] headers)
        {
            if (FailedRequests >= 10 && SucceededRequests / FailedRequests < 1)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Warning, "HttpRequests", $"More Failed requests {FailedRequests} than succeeded ones {SucceededRequests}. Waiting"));
                return "";
            }

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, URL))
                {
                    foreach (var kvp in headers)
                        request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                    using (var response = await StaticBase.HttpClient.SendAsync(request))
                    {
                        using (var content = response.Content)
                        {
                            string value = "";
                            if ((content?.Headers?.ContentType?.CharSet?.ToLower().Contains("utf8") ?? false) || (content?.Headers?.ContentType?.CharSet?.ToLower().Contains("utf-8") ?? false))
                                value = System.Text.Encoding.UTF8.GetString(await content.ReadAsByteArrayAsync());
                            else
                                value = await content.ReadAsStringAsync();

                            SucceededRequests++;
                            SucceededRequestsTotal++;
                            return value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FailedRequests++;
                FailedRequestsTotal++;
                if (!e.GetBaseException().Message.Contains("the remote party has closed the transport stream") && !e.GetBaseException().Message.Contains("The server returned an invalid or unrecognized response"))
                    await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"error for sending request to {URL}", e.GetBaseException()));
                else if (e.GetBaseException().Message.Contains("the remote party has closed the transport stream"))
                    await Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"Remote party closed the transport stream: {URL}."));
                else
                    await Program.MopsLog(new LogMessage(LogSeverity.Debug, "", $"Osu API messed up again: {URL}"));
                throw e;
            }
        }
    }
}

