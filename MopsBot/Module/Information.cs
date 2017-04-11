using System;
using System.Web.Script.Serialization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MopsBot.Module
{
    public class Information : ModuleBase
    {

        [Command("howLong")]
        [Summary("Returns the date you joined the Guild")]
        public async Task howLong()
        {
            await ReplyAsync(((SocketGuildUser)Context.User).JoinedAt.Value.Date.ToShortDateString());
        }

        [Command("joinServer")]
        [Summary("Provides link to make me join your Server")]
        public async Task joinServer()
        {
            await ReplyAsync("https://discordapp.com/oauth2/authorize?client_id=212975561759391744&scope=bot&permissions=66186303");
        }

        [Command("define")]
        [Summary("Searches dictionaries for a definition of the specified word or expression")]
        public async Task define([Remainder] string text)
        {
            string query = Task.Run(() => readURL($"http://api.wordnik.com:80/v4/word.json/{text}/definitions?limit=1&includeRelated=false&sourceDictionaries=all&useCanonical=true&includeTags=false&api_key=a2a73e7b926c924fad7001ca3111acd55af2ffabf50eb4ae5")).Result;
            
            var jss = new JavaScriptSerializer();

            dynamic tempDict = jss.Deserialize<dynamic>(query);
            tempDict = tempDict[0];
            await ReplyAsync($"__**{tempDict["word"]}**__\n\n``{tempDict["text"]}``");
        }

        [Command("translate")]
        [Summary("Translates your text from srcLanguage to dstLanguage.\nUse the abbreviations noted here http://www.transltr.org/api/getlanguagesfortranslate")]
        public async Task translate(string srcLanguage, string dstLanguage, [Remainder] string text)
        {
            string query = Task.Run(() => readURL($"http://www.transltr.org/api/translate?text={text}&to={dstLanguage}&from={srcLanguage}")).Result;

            var jss = new JavaScriptSerializer();

            dynamic tempDict = jss.Deserialize<dynamic>(query);

            await ReplyAsync(tempDict["translationText"]);
        }

        [Command("dayDiagram")]
        [Summary("Returns the total characters send for the past limit days")]
        public async Task dayDiagram(int limit)
        {
            await ReplyAsync(StaticBase.stats.drawDiagram(limit));
        }

        public static dynamic getRandomWord()
        {
            string query = readURL("http://api.wordnik.com:80/v4/words.json/randomWord?hasDictionaryDef=true&excludePartOfSpeech=given-name&minCorpusCount=10000&maxCorpusCount=-1&minDictionaryCount=4&maxDictionaryCount=-1&minLength=3&maxLength=13&api_key=a2a73e7b926c924fad7001ca3111acd55af2ffabf50eb4ae5");

            var jss = new JavaScriptSerializer();

            dynamic tempDict = jss.Deserialize<dynamic>(query);

            return tempDict["word"];
        }

        public static string readURL(string URL)
        {
            string s = "";
            try
            {
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(URL);
                using (var response = request.GetResponse())
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
