using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Tweetinvi;
using Tweetinvi.Models;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MongoDB.Driver;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class TwitterTracker : BaseTracker
    {
        private static long DBCOUNT = StaticBase.Database.GetCollection<TwitterTracker>("TwitterTracker").CountDocuments(x => true);
        public long UserId, lastMessage;
        public int FailCount = 0;
        public static readonly string SHOWREPLIES = "ShowReplies", SHOWRETWEETS = "ShowRetweets", SHOWMAIN = "ShowMain", RETWEETNOTIFICATION = "RetweetNotification", REPLYNOTIFICATION = "ReplyNotification";

        public TwitterTracker() : base()
        {
        }

        public TwitterTracker(string twitterName) : base()
        {
            Name = twitterName;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                var user = Tweetinvi.User.GetUserFromScreenName(Name);
                UserId = user.UserIdentifier.Id;
                var tweets = getNewTweets();

                if(!tweets.Any())
                    throw new ArgumentException($"Could not find any tweets for {twitterName}.\nMake sure to have at least 1 tweet before setting up tracking.");

                lastMessage = tweets.Last().Id;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"{TrackerUrl()} could not be found on Twitter!", e);
            }
        }

        public override void PostInitialisation(object info = null)
        {
            if (UserId == 0)
            {
                UserId = Tweetinvi.User.GetUserFromScreenName(Name).Id;
                UpdateTracker();
            }
        }

        public async override void PostChannelAdded(ulong channelId)
        {
            base.PostChannelAdded(channelId);
            ChannelConfig[channelId][SHOWRETWEETS] = true;
            ChannelConfig[channelId][SHOWREPLIES] = true;
            ChannelConfig[channelId][SHOWMAIN] = true;

            ChannelConfig[channelId][RETWEETNOTIFICATION] = ChannelConfig[channelId]["Notification"];
            ChannelConfig[channelId][REPLYNOTIFICATION] = ChannelConfig[channelId]["Notification"];
        }

        public async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                if (lastMessage != 0)
                {
                    await checkMissedTweets();
                }

                //checkForChange.Dispose();
            }
            catch (Exception e)
            {
                //Make Trackerhandle not crash
            }
        }

        private async Task TweetReceived(ITweet tweet, bool updateDB = true)
        {
            try
            {
                var tweets = new List<ITweet>() { tweet };

                if (lastMessage >= tweet.Id) return;
                if (updateDB) lastMessage = tweet.Id;

                if (!tweet.CreatedBy.Id.Equals(UserId)) return;
                foreach (ulong channel in ChannelConfig.Keys.ToList())
                {
                    if (tweet.InReplyToScreenName != null)
                    {
                        if ((bool)ChannelConfig[channel][SHOWREPLIES]){
                            await OnMajorChangeTracked(channel, createEmbed(tweet), (string)ChannelConfig[channel][REPLYNOTIFICATION]);
                        }
                    }
                    else if (tweet.IsRetweet)
                    {
                        if ((bool)ChannelConfig[channel][SHOWRETWEETS]){
                            await OnMajorChangeTracked(channel, createEmbed(tweet), (string)ChannelConfig[channel][RETWEETNOTIFICATION]);
                        }
                    }
                    else
                    {
                        await OnMajorChangeTracked(channel, createEmbed(tweet), (string)ChannelConfig[channel]["Notification"]);
                    }
                }

                if (updateDB) await UpdateTracker();
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"error by {Name}", e));
            }
        }

        private ITweet[] getNewTweets(long since = 0, long before = 0)
        {
            try
            {
                Tweetinvi.Parameters.IUserTimelineParameters parameters = Timeline.CreateUserTimelineParameter();
                if (since != 0) parameters.SinceId = since;
                if (before != 0) parameters.MaxId = before;
                parameters.MaximumNumberOfTweetsToRetrieve = 10;

                var tweets = Timeline.GetUserTimeline(Name, parameters);

                FailCount = 0;
                return tweets.Reverse().ToArray();
            }
            catch (Exception e)
            {
                FailCount++;
                UpdateTracker();
                return new ITweet[0];
            }
        }

        private async Task checkMissedTweets(long beforeId = 0)
        {
            //if (!hasChecked)
            //{
            var missedTweets = getNewTweets(lastMessage, beforeId);
            int i = 0;
            foreach (var curTweet in missedTweets)
            {
                i++;
                if (i != missedTweets.Length)
                    await TweetReceived(curTweet, false);
                else
                    await TweetReceived(curTweet);
            }
            //}
        }

        private Embed createEmbed(ITweet tweet)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0, 163, 243);
            e.Title = "Tweet-Link";
            e.Url = tweet.Url;
            e.Timestamp = tweet.CreatedAt;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://upload.wikimedia.org/wikipedia/de/thumb/9/9f/Twitter_bird_logo_2012.svg/1259px-Twitter_bird_logo_2012.svg.png";
            footer.Text = "Twitter";
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = tweet.CreatedBy.Name;
            author.Url = tweet.CreatedBy.Url;
            author.IconUrl = tweet.CreatedBy.ProfileImageUrl;
            e.Author = author;

            e.ThumbnailUrl = tweet.CreatedBy.ProfileImageUrl;
            foreach (Tweetinvi.Models.Entities.IMediaEntity cur in tweet.Media)
                if (cur.MediaType.Equals("photo"))
                    e.ImageUrl = cur.MediaURL;

            e.Description = tweet.FullText;

            return e.Build();
        }

        public static void QueryBeforeExecute(object sender, Tweetinvi.Events.QueryBeforeExecuteEventArgs args)
        {
            var queryRateLimits = RateLimit.GetQueryRateLimit(args.QueryURL);
            // Some methods are not RateLimited. Invoking such a method will result in the queryRateLimits to be null
            if (queryRateLimits != null)
            {
                if (queryRateLimits.Remaining > 0)
                {
                    // We have enough resource to execute the query
                    return;
                }

                // Strategy #1 : Wait for RateLimits to be available
                Console.WriteLine("Waiting for RateLimits until : {0}", queryRateLimits.ResetDateTime.ToLongTimeString());
                args.Cancel = true;
            }
        }

        public override string TrackerUrl()
        {
            return "https://twitter.com/" + Name;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(true);
        }

        public override async Task UpdateTracker(){
            await StaticBase.Trackers[TrackerType.Twitter].UpdateDBAsync(this);
        }
    }
}