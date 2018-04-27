using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data.Tracker
{
    /// <summary>
    /// A tracker which keeps track of a Subreddit
    /// </summary>
    public class RedditTracker : ITracker
    {
        private OStatsResult information;
        public string lastPost = "";

        /// <summary>
        /// Initialises the tracker by setting attributes and setting up a Timer with a 10 minutes interval
        /// </summary>
        /// <param Name="OWName"> The Name-Battletag combination of the player to track </param>
        public RedditTracker() : base(60000, 2000)
        {
        }

        public RedditTracker(string name) : base(60000, 0)
        {
            Name = name;

            try{
                var test = fetchPosts().Result;
                if(test.data.children.Count == 0) 
                    throw new Exception("");
            } catch(Exception e){
                Dispose();
                Console.WriteLine("");
                throw new Exception($"No results were found for Subreddit `{Name.Split(" ")[0]}`"+
                                    $"{(Name.Split(" ").Length > 1 ? $" with restriction(s) `{Name.Split(" ")[1]}`." : ".")}");
            }
        }

        /// <summary>
        /// Event for the Timer, to check for changed stats
        /// </summary>
        /// <param Name="stateinfo"></param>
        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var allPosts = await fetchPosts();
                var allSubredditPosts = allPosts.data.children.Where(x => x.data.subreddit.ToLower().Equals(Name.Split(" ")[0].ToLower())).ToArray();

                if(lastPost == ""){
                    lastPost = allSubredditPosts.FirstOrDefault().data.title;
                    StaticBase.trackers["reddit"].SaveJson();
                    return;
                }

                var newPosts = allSubredditPosts.TakeWhile(x => x.data.title != lastPost).ToArray();

                if (newPosts.Length > 0)
                {
                    lastPost = newPosts[0].data.title;
                    foreach(var post in newPosts)
                        foreach(ulong channel in ChannelIds)
                            await OnMajorChangeTracked(channel, createEmbed(post.data), "");
                    StaticBase.trackers["reddit"].SaveJson();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<RedditResult> fetchPosts(){
            string query = await MopsBot.Module.Information.ReadURLAsync($"https://www.reddit.com/r/{Name.Split(" ")[0]}/"+
                                                                        $"{(Name.Split(" ").Length > 1 ? $"search.json?sort=new&q={Name.Split(" ")[1]}" : "new.json")}");

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<RedditResult>(query, _jsonWriter);
        }

        ///<summary>Builds an embed out of the changed stats, and sends it as a Discord message </summary>
        /// <param Name="RedditInformation">All fetched stats of the user </param>
        /// <param Name="changedStats">All changed stats of the user, together with a string presenting them </param>
        /// <param Name="mostPlayed">The most played Hero of the session, together with a string presenting them </param>
        private EmbedBuilder createEmbed(Data2 redditPost)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = redditPost.title.Length > 256 ? redditPost.title.Substring(0, 251) + "[...]" : redditPost.title;
            e.Url = "https://www.reddit.com" + redditPost.permalink;
            e.Description = redditPost.selftext.Length > 2048 ? redditPost.selftext.Substring(0, 2043) + "[...]" : redditPost.selftext;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = redditPost.author;
            author.Url = $"https://www.reddit.com/user/{redditPost.author}";
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "http://1000logos.net/wp-content/uploads/2017/05/Reddit-logo.png";
            footer.Text = "Reddit";
            e.Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)redditPost.created_utc).DateTime;
            e.Footer = footer;
            e.ThumbnailUrl = !redditPost.thumbnail.Equals("self") ? redditPost.thumbnail : null;
            if(redditPost.url.EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase))
                e.ImageUrl = redditPost.url;
            if(redditPost.url.ToLower().Contains("gfycat.com"))
                e.ImageUrl = redditPost.url.Replace("gfycat.com", "thumbs.gfycat.com") + "-size_restricted.gif";

            e.AddInlineField("Score", redditPost.score);

            return e;
        }
    }
}
