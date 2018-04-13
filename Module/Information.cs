using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using System.Net.NetworkInformation;

namespace MopsBot.Module
{
    public class Information : ModuleBase
    {

        [Command("howLong")]
        [Summary("Returns the date you joined the Guild")]
        public async Task howLong()
        {
            await ReplyAsync(((SocketGuildUser)Context.User).JoinedAt.Value.Date.ToString("d"));
        }

        [Command("joinServer")]
        [Summary("Provides link to make me join your Server")]
        public async Task joinServer()
        {
            await ReplyAsync($"https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot");
        }

        [Command("define")]
        [Summary("Searches dictionaries for a definition of the specified word or expression")]
        public async Task define([Remainder] string text)
        {
            try{

                string query = Task.Run(() => readURL($"http://api.wordnik.com:80/v4/word.json/{text}/definitions?limit=1&includeRelated=false&sourceDictionaries=all&useCanonical=true&includeTags=false&api_key=a2a73e7b926c924fad7001ca3111acd55af2ffabf50eb4ae5")).Result;

                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);

                tempDict = tempDict[0];
                await ReplyAsync($"__**{tempDict["word"]}**__\n\n``{tempDict["text"]}``");

            } catch(Exception e){
                Console.WriteLine($"[ERROR] by define at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        [Command("translate")]
        [Summary("Translates your text from srcLanguage to tgtLanguage.")]
        public async Task translate(string srcLanguage, string tgtLanguage, [Remainder] string text)
        {
            try{

                string query = Task.Run(() => readURL($"https://translate.googleapis.com/translate_a/single?client=gtx&sl={srcLanguage}&tl={tgtLanguage}&dt=t&q={text}")).Result;
                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
                await ReplyAsync(tempDict[0][0][0].ToString());

            } catch(Exception e){
                Console.WriteLine($"[ERROR] by translate at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
                await ReplyAsync("Error happened");
            }
        }

        [Command("dayDiagram")]
        [Summary("Returns the total characters send for the past limit days")]
        public async Task dayDiagram(int limit)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.ImageUrl = StaticBase.stats.DrawDiagram(limit);
            await ReplyAsync("", embed:e);
        }

        [Command("getStats")]
        [Summary("Returns your experience and all that stuff")]
        public async Task getStats()
        {
            await ReplyAsync(StaticBase.people.Users[Context.User.Id].statsToString());
        }

        [Command("ranking")]
        [Summary("Returns the top limit ranks of level\nOr if specified {experience, money, hug, punch, kiss}")]
        public async Task ranking(int limit, string stat = "level")
        {
            Func<MopsBot.Data.Individual.User, int> sortParameter = x => x.calcLevel();
            switch(stat.ToLower()){
                case "experience":
                    sortParameter = x => x.Experience;
                    break;
                case "money":
                    sortParameter = x => x.Score;
                    break;
                case "hug":
                    sortParameter = x => x.hugged;
                    break;
                case "punch":
                    sortParameter = x => x.punched;
                    break;
                case "kiss":
                    sortParameter = x => x.kissed;
                    break;
            }
            await ReplyAsync(StaticBase.people.DrawDiagram(limit, sortParameter));
        }

        public static dynamic getRandomWord()
        {
            try{

                string query = readURL("http://api.wordnik.com:80/v4/words.json/randomWord?hasDictionaryDef=true&excludePartOfSpeech=given-name&minCorpusCount=10000&maxCorpusCount=-1&minDictionaryCount=4&maxDictionaryCount=-1&minLength=3&maxLength=13&api_key=a2a73e7b926c924fad7001ca3111acd55af2ffabf50eb4ae5");
                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
                return tempDict["word"];

            } catch(Exception e){
                Console.WriteLine($"[ERROR] by getRandomWord at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
                return null;
        }

        public static string readURL(string URL)
        {
            string s = "";
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(URL);
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
            using (var response = request.GetResponseAsync().Result)
            using (var content = response.GetResponseStream())
            using (var reader = new System.IO.StreamReader(content))
            {
                s = reader.ReadToEnd();
            }
            return s;
        }
    }
}
