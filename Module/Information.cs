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

namespace MopsBot.Module
{
    public class Information : ModuleBase
    {

        [Command("howLong")]
        [Summary("Returns the date you joined the Guild")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task howLong()
        {
            await ReplyAsync(((SocketGuildUser)Context.User).JoinedAt.Value.Date.ToString("d"));
        }

        [Command("invite")]
        [Summary("Provides link to make me join your Server")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task joinServer()
        {
            await ReplyAsync($"https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=271969344&scope=bot");
        }

        [Command("define")]
        [Summary("Searches dictionaries for a definition of the specified word or expression")]
        public async Task define([Remainder] string text)
        {
            try
            {

                string query = Task.Run(() => ReadURLAsync($"http://api.wordnik.com:80/v4/word.json/{text}/definitions?limit=1&includeRelated=false&sourceDictionaries=all&useCanonical=true&includeTags=false&api_key={Program.Config["Wordnik"]}")).Result;

                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);

                tempDict = tempDict[0];
                await ReplyAsync($"__**{tempDict["word"]}**__\n\n``{tempDict["text"]}``");

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" +  $"[ERROR] by define at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        [Command("translate")]
        [Summary("Translates your text from srcLanguage to tgtLanguage.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task translate(string srcLanguage, string tgtLanguage, [Remainder] string text)
        {
            try
            {

                string query = Task.Run(() => ReadURLAsync($"https://translate.googleapis.com/translate_a/single?client=gtx&sl={srcLanguage}&tl={tgtLanguage}&dt=t&q={text}")).Result;
                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
                await ReplyAsync(tempDict[0][0][0].ToString());

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" +  $"[ERROR] by translate at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
                await ReplyAsync("Error happened");
            }
        }

        [Command("Wolfram", RunMode=RunMode.Async)]
        [Summary("Sends a query to wolfram alpha.")]
        public async Task wolf([Remainder]string query){
            var result = await ReadURLAsync($"https://api.wolframalpha.com/v2/query?input={System.Web.HttpUtility.UrlEncode(query)}&format=image,plaintext&podstate=Step-by-step%20solution&output=JSON&appid={Program.Config["WolframAlpha"]}");
            var jsonResult = JsonConvert.DeserializeObject<Data.Tracker.APIResults.Wolfram.WolframResult>(result);
            for(int i = 0; i < 2 && i < jsonResult.queryresult.pods.Count; i++){
                var image = jsonResult.queryresult.pods[i].subpods.FirstOrDefault(x => x.title=="Possible intermediate steps")?.img.src ?? jsonResult.queryresult.pods[i].subpods.First()?.img.src;
                var embed = new EmbedBuilder().WithTitle(jsonResult.queryresult.pods[i].title).WithDescription(query).WithImageUrl(image);
                await ReplyAsync("", embed: embed.Build());
            }
        }

        public async static Task<dynamic> GetRandomWordAsync()
        {
            try
            {

                string query = await ReadURLAsync($"http://api.wordnik.com:80/v4/words.json/randomWord?hasDictionaryDef=true&excludePartOfSpeech=given-name&minCorpusCount=10000&maxCorpusCount=-1&minDictionaryCount=4&maxDictionaryCount=-1&minLength=3&maxLength=13&api_key={Program.Config["Wordnik"]}");
                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
                return tempDict["word"];

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" +  $"[ERROR] by GetRandomWordAsync at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
            return null;
        }

        public static async Task<string> ReadURLAsync(string URL, params KeyValuePair<string, string>[] headers)
        {
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(URL);
            foreach(var header in headers)
                request.Headers.Add(header.Key, header.Value);

            request.KeepAlive = false;
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
            
            using (var response = await request.GetResponseAsync())
            using (var content = response.GetResponseStream())
            using (var reader = new System.IO.StreamReader(content))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public static async Task<Gfycat.Gfy> ConvertToGifAsync(string url)
        {
            var status = await StaticBase.gfy.CreateGfyAsync(url);
            return await status.GetGfyWhenCompleteAsync();
        }
    }
}
