using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults.Reddit;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data.Tracker
{
    /// <summary>
    /// A tracker which keeps track of a Subreddit
    /// </summary>
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class RedditTracker : BaseTracker
    {
        public double lastCheck;

        /// <summary>
        /// Initialises the tracker by setting attributes and setting up a Timer with a 10 minutes interval
        /// </summary>
        /// <param Name="OWName"> The Name-Battletag combination of the player to track </param>
        public RedditTracker() : base(600000, ExistingTrackers * 2000)
        {
        }

        public RedditTracker(Dictionary<string, string> args) : base(600000, 60000){
            base.SetBaseValues(args, true);

            //Check if Name ist valid
            try{
                new RedditTracker(Name).Dispose();
            } catch (Exception e){
                this.Dispose();
                throw e;
            }

            if(StaticBase.Trackers[TrackerType.Reddit].GetTrackers().ContainsKey(Name)){
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.Reddit].GetTrackers()[Name];
                curTracker.ChannelMessages[ulong.Parse(args["Channel"].Split(":")[1])] = args["Notification"];
                StaticBase.Trackers[TrackerType.Reddit].UpdateContent(new Dictionary<string, Dictionary<string, string>>{{"NewValue", args}, {"OldValue", args}}).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }

        public RedditTracker(string name) : base(600000)
        {
            lastCheck = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Name = name;

            try
            {
                var test = fetchPosts().Result;
                if (test.data.children.Count == 0)
                    throw new Exception("");
            }
            catch (Exception)
            {
                Dispose();
                Console.WriteLine("\n" +  "");
                throw new Exception($"No results were found for Subreddit {TrackerUrl()}" +
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
                var allThings = await fetchPosts();
                var newPosts = allThings.data.children.TakeWhile(x => x.data.created_utc > lastCheck).ToArray();

                if (newPosts.Length > 0)
                {
                    lastCheck = newPosts.Max(x => x.data.created_utc);
                    await StaticBase.Trackers[TrackerType.Reddit].UpdateDBAsync(this);

                    newPosts = newPosts.Reverse().ToArray();
                    foreach (var post in newPosts)
                        foreach (ulong channel in ChannelMessages.Keys.ToList())
                        {
                            await OnMajorChangeTracked(channel, await createEmbed(post.data), "");
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" +  $"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<RedditResult> fetchPosts()
        {
            return await FetchJSONDataAsync<RedditResult>($"https://www.reddit.com/r/{Name.Split(" ")[0]}/" +
                                                                        $"{(Name.Split(" ").Length > 1 ? $"search.json?sort=new&restrict_sr=on&q={Name.Split(" ")[1]}" : "new.json?restrict_sr=on")}");
        }

        ///<summary>Builds an embed out of the changed stats, and sends it as a Discord message </summary>
        /// <param Name="RedditInformation">All fetched stats of the user </param>
        /// <param Name="changedStats">All changed stats of the user, together with a string presenting them </param>
        /// <param Name="mostPlayed">The most played Hero of the session, together with a string presenting them </param>
        private async Task<Embed> createEmbed(Data2 redditPost)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(255, 49, 0);
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
            e.Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)redditPost.created_utc).DateTime.AddHours(2);
            e.Footer = footer;

            try
            {
                e.ThumbnailUrl = !redditPost.thumbnail.Equals("self") && !redditPost.thumbnail.Equals("default") ? redditPost.thumbnail : null;
            }
            catch (Exception)
            {
                e.ThumbnailUrl = null;
            }

            e.AddField("Score", redditPost.score, true);

            /*if (redditPost.media_embed != null && redditPost.media_embed.media_domain_url != null)
                e.ImageUrl = (await Module.Information.ConvertToGifAsync(redditPost.media_embed.media_domain_url)).Max5MbGif;
            else if (redditPost.media != null && redditPost.media.reddit_video != null)
                e.ImageUrl = (await Module.Information.ConvertToGifAsync(redditPost.media.reddit_video.fallback_url)).Max5MbGif;
            */
            
            return e.Build();
        }

        public override string TrackerUrl(){
            return "https://www.reddit.com/r/" + Name.Split(" ").First();
        }
    }
}
