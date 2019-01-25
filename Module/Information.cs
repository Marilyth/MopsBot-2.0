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
    public class Information : ModuleBase
    {

        [Command("HowLong")]
        [Summary("Returns the date you joined the Guild")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task howLong()
        {
            await ReplyAsync(((SocketGuildUser)Context.User).JoinedAt.Value.Date.ToString("d"));
        }

        [Command("Invite")]
        [Summary("Provides link to make me join your Server")]
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
        }

        [Command("Define", RunMode = RunMode.Async)]
        [Summary("Searches dictionaries for a definition of the specified word or expression")]
        [Ratelimit(1, 10, Measure.Seconds)]
        public async Task define([Remainder] string text)
        {
            using (Context.Channel.EnterTypingState())
            {
                try
                {
                    string query = Task.Run(() => GetURLAsync($"http://api.wordnik.com:80/v4/word.json/{text}/definitions?limit=1&includeRelated=false&sourceDictionaries=all&useCanonical=true&includeTags=false&api_key={Program.Config["Wordnik"]}")).Result;

                    dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);

                    tempDict = tempDict[0];
                    await ReplyAsync($"__**{tempDict["word"]}**__\n\n``{tempDict["text"]}``");

                }
                catch (Exception e)
                {
                    Console.WriteLine("\n" + $"[ERROR] by define at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
                }
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
                try
                {
                    string query = Task.Run(() => GetURLAsync($"https://translate.googleapis.com/translate_a/single?client=gtx&sl={srcLanguage}&tl={tgtLanguage}&dt=t&q={text}")).Result;
                    dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
                    await ReplyAsync(tempDict[0][0][0].ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n" + $"[ERROR] by translate at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
                    await ReplyAsync("Error happened");
                }
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
                for (int i = 0; i < 2 && i < jsonResult.queryresult.pods.Count; i++)
                {
                    var image = jsonResult.queryresult.pods[i].subpods.FirstOrDefault(x => x.title == "Possible intermediate steps")?.img.src ?? jsonResult.queryresult.pods[i].subpods.First()?.img.src;
                    var embed = new EmbedBuilder().WithTitle(jsonResult.queryresult.pods[i].title).WithDescription(query).WithImageUrl(image);
                    await ReplyAsync("", embed: embed.Build());
                }
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
                Console.WriteLine("\n" + $"[ERROR] by GetRandomWordAsync at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
            return null;
        }

        public static async Task<string> PostURLAsync(string URL, params KeyValuePair<string, string>[] headers)
        {
            using (var response = await StaticBase.HttpClient.PostAsync(URL, new FormUrlEncodedContent(headers)))
            {
                try
                {
                    return await response.Content.ReadAsStringAsync();
                }
                catch (System.Net.WebException e)
                {
                    return e.Message;
                }
            }
        }

        public static async Task<string> GetURLAsync(string URL, params KeyValuePair<string, string>[] headers)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, URL))
            {
                try
                {
                    foreach(var kvp in headers)
                        request.Headers.Add(kvp.Key, kvp.Value);
                    return await (await StaticBase.HttpClient.SendAsync(request)).Content.ReadAsStringAsync();
                }
                catch (System.Net.WebException e)
                {
                    return e.Message;
                }
            }
        }

        public static async Task<Gfycat.Gfy> ConvertToGifAsync(string url)
        {
            var status = await StaticBase.gfy.CreateGfyAsync(url);
            return await status.GetGfyWhenCompleteAsync();
        }
    }
}
