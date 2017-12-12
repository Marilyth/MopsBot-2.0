using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

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
            string query = Task.Run(() => readURL($"http://api.wordnik.com:80/v4/word.json/{text}/definitions?limit=1&includeRelated=false&sourceDictionaries=all&useCanonical=true&includeTags=false&api_key=a2a73e7b926c924fad7001ca3111acd55af2ffabf50eb4ae5")).Result;

            dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);

            tempDict = tempDict[0];
            await ReplyAsync($"__**{tempDict["word"]}**__\n\n``{tempDict["text"]}``");
        }

        [Command("translate")]
        [Summary("Translates your text from srcLanguage to tgtLanguage.")]
        public async Task translate(string srcLanguage, string tgtLanguage, [Remainder] string text)
        {
            string query = Task.Run(() => readURL($"https://translate.googleapis.com/translate_a/single?client=gtx&sl={srcLanguage}&tl={tgtLanguage}&dt=t&q={text}")).Result;
            dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);

            await ReplyAsync(tempDict[0][0][0].ToString());
        }

        [Command("dayDiagram")]
        [Summary("Returns the total characters send for the past limit days")]
        public async Task dayDiagram(int limit)
        {
            await ReplyAsync(StaticBase.stats.drawDiagram(limit));
        }

        [Command("getStats")]
        [Summary("Returns your experience and all that stuff")]
        public async Task getStats()
        {
            await ReplyAsync(StaticBase.people.users.Find(x => x.ID.Equals(Context.User.Id)).statsToString());
        }

        [Command("ranking")]
        [Summary("Returns the top limit ranks of level")]
        public async Task ranking(int limit)
        {
            await ReplyAsync(StaticBase.people.drawDiagram(limit, Data.UserScore.DiagramType.Level));
        }

        public static dynamic getRandomWord()
        {
            string query = readURL("http://api.wordnik.com:80/v4/words.json/randomWord?hasDictionaryDef=true&excludePartOfSpeech=given-name&minCorpusCount=10000&maxCorpusCount=-1&minDictionaryCount=4&maxDictionaryCount=-1&minLength=3&maxLength=13&api_key=a2a73e7b926c924fad7001ca3111acd55af2ffabf50eb4ae5");

            dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
            return tempDict["word"];
        }

        public static string readURL(string URL)
        {
            string s = "";
            try
            {
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(URL);
                using (var response = request.GetResponseAsync().Result)
                using (var content = response.GetResponseStream())
                using (var reader = new System.IO.StreamReader(content))
                {
                    s = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return s;
        }
    }
}
