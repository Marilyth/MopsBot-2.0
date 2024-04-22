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
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ServiceModel.Syndication;

namespace MopsBot.Data.Tracker
{
    /// <summary>
    /// A tracker which keeps track of a Subreddit
    /// </summary>
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class RedditTracker : BaseTracker
    {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, double> LastCheck = new Dictionary<ulong, double>();
        public static readonly string POSTAGE = "MinPostAgeInMinutes";
        public RedditTracker() : base()
        {
        }

        public RedditTracker(string name) : base()
        {
            Name = name;

            try
            {
                var test = FetchPosts().Result;
                if (!test.Items.Any())
                    throw new Exception("");
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"No results were found for Subreddit {TrackerUrl()}" +
                                    $"{(Name.Split(" ").Length > 1 ? $" with restriction(s) `{Name.Split(" ")[1]}`." : ".")}", e);
            }
        }

        public async override void PostChannelAdded(ulong channelId)
        {
            base.PostChannelAdded(channelId);

            var config = ChannelConfig[channelId];
            config[POSTAGE] = 0;
            LastCheck[channelId] = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public override async void Conversion(object obj = null)
        {
            bool save = false;
            foreach (var channel in ChannelConfig.Keys.ToList())
            {
                if (!LastCheck.ContainsKey(channel))
                {
                    LastCheck[channel] = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    save = true;
                }
            }
            if (save)
                await UpdateTracker();
        }

        /// <summary>
        /// Event for the Timer, to check for changed stats
        /// </summary>
        /// <param Name="stateinfo"></param>
        public async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var allThings = await FetchPosts();

                foreach (var channel in ChannelConfig)
                {
                    var minPostAge = (int)ChannelConfig[channel.Key][POSTAGE];
                    var newPosts = allThings.Items.TakeWhile(x => x.PublishDate.ToUnixTimeSeconds() > LastCheck[channel.Key]).ToList();
                    newPosts.RemoveAll(x => (DateTimeOffset.UtcNow - x.PublishDate).TotalMinutes < minPostAge);

                    if (newPosts.Count > 0)
                    {
                        LastCheck[channel.Key] = newPosts.Max(x => x.PublishDate.ToUnixTimeSeconds());
                        await UpdateTracker();

                        newPosts.Reverse();
                        foreach (var post in newPosts){
                            await OnMinorChangeTracked(channel.Key, $"{ChannelConfig[channel.Key]["Notification"]}\n{post.Links.First().Uri}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        public static async Task<List<Uri>> CheckReddit(string subreddit, string query = null, int limit = 1)
        {
            var results = await FetchRSSData($"https://www.reddit.com/r/{subreddit}/" +
                                                                        $"{(query != null ? $"search.rss?sort=new&restrict_sr=on&q={query}" : "new.rss?restrict_sr=on")}" + $"&limit={limit}");

            return results.Items.Select(x => x.Links.First().Uri).ToList();
        }

        private async Task<SyndicationFeed> FetchPosts()
        {
            var posts = await FetchRSSData($"https://www.reddit.com/r/{Name.Split(" ")[0]}/" +
                                                                        $"{(Name.Split(" ").Length > 1 ? $"search.rss?sort=new&restrict_sr=on&q={string.Join(" ", Name.Split(" ").Skip(1))}" : "new.rss?restrict_sr=on")}");

            return posts;
        }

        ///<summary>Builds an embed out of the changed stats, and sends it as a Discord message </summary>
        /// <param Name="RedditInformation">All fetched stats of the user </param>
        /// <param Name="changedStats">All changed stats of the user, together with a string presenting them </param>
        /// <param Name="mostPlayed">The most played Hero of the session, together with a string presenting them </param>
        private async static Task<Embed> createEmbed(Data2 redditPost)
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
                if(!string.IsNullOrEmpty(redditPost.url) && redditPost.url.Contains(".jpg", StringComparison.CurrentCultureIgnoreCase) || redditPost.url.Contains(".png", StringComparison.CurrentCultureIgnoreCase)){
                        e.ImageUrl = redditPost.url;
                }
                else if(!redditPost.thumbnail.Equals("self") && !redditPost.thumbnail.Equals("default") && !string.IsNullOrEmpty(redditPost.thumbnail)){
                        e.ImageUrl = redditPost.thumbnail;
                }
            }
            catch (Exception)
            {
                e.ImageUrl = null;
            }

            e.AddField("Score", redditPost.score, true);

            /*if (redditPost.media_embed != null && redditPost.media_embed.media_domain_url != null)
                e.ImageUrl = (await Module.Information.ConvertToGifAsync(redditPost.media_embed.media_domain_url)).Max5MbGif;
            else if (redditPost.media != null && redditPost.media.reddit_video != null)
                e.ImageUrl = (await Module.Information.ConvertToGifAsync(redditPost.media.reddit_video.fallback_url)).Max5MbGif;
            */

            return e.Build();
        }

        public override string TrackerUrl()
        {
            return "https://www.reddit.com/r/" + Name.Split(" ").First();
        }

        public override async Task UpdateTracker()
        {
            await StaticBase.Trackers[TrackerType.Reddit].UpdateDBAsync(this);
        }
    }
}
