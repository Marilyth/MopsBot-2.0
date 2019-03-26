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
    public class TwitterRealtimeTracker : BaseTracker
    {
        public static Tweetinvi.Streaming.IFilteredStream STREAM = Tweetinvi.Stream.CreateFilteredStream();
        private static long DBCOUNT = StaticBase.Database.GetCollection<TwitterRealtimeTracker>("TwitterRealtimeTracker").CountDocuments(x => true);
        public long UserId;

        public TwitterRealtimeTracker() : base()
        {
        }

        /*public TwitterRealtimeTracker(Dictionary<string, string> args) : base(){
            Update(new Dictionary<string, Dictionary<string, string>>(){{"NewValue", args}, {"OldValue", args}});
            base.SetBaseValues(args, true);

            //Check if Name ist valid
            try{
                var test = new TwitterTracker(Name);
                test.Dispose();
                lastMessage = test.lastMessage;
                SetTimer();
            } catch (Exception e){
                this.Dispose();
                throw e;
            }

            if(StaticBase.Trackers[TrackerType.Twitter].GetTrackers().ContainsKey(Name)){
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.Twitter].GetTrackers()[Name];
                curTracker.ChannelMessages[ulong.Parse(args["Channel"].Split(":")[1])] = args["Notification"];
                StaticBase.Trackers[TrackerType.Twitter].UpdateContent(new Dictionary<string, Dictionary<string, string>>{{"NewValue", args}, {"OldValue", args}}).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }*/

        public TwitterRealtimeTracker(string twitterName) : base()
        {
            Name = twitterName;

            //Check if person exists by forcing Exceptions if not.
            try
            {                
                var user = Tweetinvi.User.GetUserFromScreenName(Name);
                UserId = user.UserIdentifier.Id;

                if(DBCOUNT > 0) STREAM.StopStream();
                STREAM.AddFollow(UserId, TweetRecieved);
                STREAM.StartStreamMatchingAllConditionsAsync();
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"{TrackerUrl()} could not be found on Twitter!");
            }
        }

        public override void PostInitialisation(){
            STREAM.AddFollow(UserId, TweetRecieved);
            if(STREAM.FollowingUserIds.Keys.Count >= DBCOUNT && STREAM.StreamState == StreamState.Stop)
                STREAM.StartStreamMatchingAllConditionsAsync();

            checkForChange.Dispose();
        }

        protected async override void CheckForChange_Elapsed(object stateinfo){}

        private async void TweetRecieved(ITweet tweet)
        {
            try
            {
                foreach (ulong channel in ChannelMessages.Keys.ToList())
                {
                    if (tweet.InReplyToScreenName == null && !tweet.IsRetweet)
                    {
                        if (!ChannelMessages[channel].Split("|")[0].Equals("NONE"))
                            await OnMajorChangeTracked(channel, createEmbed(tweet), ChannelMessages[channel].Split("|")[0]);
                    }
                    else if (!ChannelMessages[channel].Split("|")[1].Equals("NONE")){
                        await OnMajorChangeTracked(channel, createEmbed(tweet), ChannelMessages[channel].Split("|")[1]);
                    }
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"error by {Name}", e));
            }
        }

        /*private ITweet[] getNewTweets()
        {
            var twitterKey = Program.Config["TwitterKey"];
            var twitterSecret = Program.Config["TwitterSecret"];
            var twitterToken = Program.Config["TwitterToken"];
            var twitterAccessSecret = Program.Config["TwitterAccessSecret"];

            Program.Config["TwitterKey"] = Program.Config["TwitterKey2"];
            Program.Config["TwitterSecret"] = Program.Config["TwitterSecret2"];
            Program.Config["TwitterToken"] = Program.Config["TwitterToken2"];
            Program.Config["TwitterAccessSecret"] = Program.Config["TwitterAccessSecret2"];

            Program.Config["TwitterKey2"] = twitterKey;
            Program.Config["TwitterSecret2"] = twitterSecret;
            Program.Config["TwitterToken2"] = twitterToken;
            Program.Config["TwitterAccessSecret2"] = twitterAccessSecret;

            Auth.SetUserCredentials(Program.Config["TwitterKey"], Program.Config["TwitterSecret"],
                                        Program.Config["TwitterToken"], Program.Config["TwitterAccessSecret"]);
            Tweetinvi.Parameters.IUserTimelineParameters parameters = Timeline.CreateUserTimelineParameter();
            if (lastMessage != 0) parameters.SinceId = lastMessage;
            parameters.MaximumNumberOfTweetsToRetrieve = 10;

            var tweets = Timeline.GetUserTimeline(Name, parameters);
            return tweets.Reverse().ToArray();
        }*/

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

        public override Dictionary<string, object> GetParameters(ulong guildId)
        {
            var parentParameters = base.GetParameters(guildId);
            (parentParameters["Parameters"] as Dictionary<string, object>)["MainNotification"] = "New main tweet!";
            (parentParameters["Parameters"] as Dictionary<string, object>)["NonMainNotification"] = "New reply or retweet!";
            (parentParameters["Parameters"] as Dictionary<string, object>)["TrackMainTweets"] = new bool[]{true, false};
            (parentParameters["Parameters"] as Dictionary<string, object>)["TrackNonMainTweets"] = new bool[]{true, false};
            (parentParameters["Parameters"] as Dictionary<string, object>).Remove("Notification");
            return parentParameters;
        }

        public override object GetAsScope(ulong channelId)
        {
            return new ContentScope()
            {
                Id = this.Name,
                _Name = this.Name,
                MainNotification = this.ChannelMessages[channelId].Split("|")[0],
                NonMainNotification = this.ChannelMessages[channelId].Split("|")[1],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId,
                TrackMainTweets = !this.ChannelMessages[channelId].Split("|")[0].Equals("NONE"),
                TrackNonMainTweets = !this.ChannelMessages[channelId].Split("|")[1].Equals("NONE")
            };
        }

        public override void Update(Dictionary<string, Dictionary<string, string>> args)
        {
            base.Update(args);
            var channelId = ulong.Parse(args["NewValue"]["Channel"].Split(":")[1]);
            var newChannelId = ulong.Parse(args["NewValue"]["Channel"].Split(":")[1]);
            ChannelMessages[newChannelId] = args["NewValue"]["MainNotification"] + "|" + args["NewValue"]["NonMainNotification"];

            if (!bool.Parse(args["NewValue"]["TrackMainTweets"]))
                ChannelMessages[channelId] = "NONE|" + ChannelMessages[channelId].Split("|")[1];

            if (!bool.Parse(args["NewValue"]["TrackNonMainTweets"]))
                ChannelMessages[channelId] = ChannelMessages[channelId].Split("|")[0] + "|NONE";
        }

        public new struct ContentScope
        {
            public string Id;
            public string _Name;
            public string MainNotification;
            public string NonMainNotification;
            public string Channel;
            public bool TrackMainTweets;
            public bool TrackNonMainTweets;
        }
    }
}