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

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class TwitterTracker : ITracker
    {
        public long lastMessage;

        public TwitterTracker() : base(600000, ExistingTrackers * 2000)
        {
        }

        public TwitterTracker(string twitterName) : base(600000)
        {
            Name = twitterName;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                if (twitterName.Contains(" ")) throw new Exception();
                lastMessage = getNewTweets().Last().Id;
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"No tweets from {TrackerUrl()} could be found on Twitter!\nI only track people who tweeted at least once.");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                ITweet[] newTweets = getNewTweets();

                foreach (ITweet newTweet in newTweets)
                {
                    foreach (ulong channel in ChannelMessages.Keys.ToList())
                    {
                        if (newTweet.InReplyToScreenName == null && !newTweet.IsRetweet)
                        {
                            if (!ChannelMessages[channel].Split("|")[0].Equals("NONE"))
                                await OnMajorChangeTracked(channel, createEmbed(newTweet), ChannelMessages[channel].Split("|")[0]);
                        }
                        else if (!ChannelMessages[channel].Split("|")[1].Equals("NONE"))
                            await OnMajorChangeTracked(channel, createEmbed(newTweet), ChannelMessages[channel].Split("|")[1]);
                    }
                }

                if (newTweets.Length != 0)
                {
                    lastMessage = newTweets[newTweets.Length - 1].Id;
                    await StaticBase.Trackers[TrackerType.Twitter].UpdateDBAsync(this);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + $"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private ITweet[] getNewTweets()
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

        public override Dictionary<string, object> GetParameters(ulong guildId)
        {
            var parentParameters = base.GetParameters(guildId);
            (parentParameters["Parameters"] as Dictionary<string, object>)["TrackMainTweets"] = new bool[]{true, false};
            (parentParameters["Parameters"] as Dictionary<string, object>)["TrackNonMainTweets"] = new bool[]{true, false};
            return parentParameters;
        }

        public override object GetAsScope(ulong channelId)
        {
            return new ContentScope()
            {
                Name = this.Name,
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId,
                TrackMainTweets = !this.ChannelMessages[channelId].Split("|")[0].Equals("NONE"),
                TrackNonMainTweets = !this.ChannelMessages[channelId].Split("|")[1].Equals("NONE")
            };
        }

        public override void Update(params string[] args)
        {
            var channelId = ulong.Parse(args[2].Split(":")[1]);
            Name = args[0];
            ChannelMessages[channelId] = args[1];

            if (bool.Parse(args[3]))
                ChannelMessages[channelId] = args[1] + "|" + ChannelMessages[channelId].Split("|")[1];
            else
                ChannelMessages[channelId] = "NONE|" + ChannelMessages[channelId].Split("|")[1];

            if (bool.Parse(args[4]))
                ChannelMessages[channelId] = ChannelMessages[channelId].Split("|")[0] + "|" + args[1];
            else
                ChannelMessages[channelId] = ChannelMessages[channelId].Split("|")[0] + "|NONE";
        }

        public new struct ContentScope
        {
            public string Name;
            public string Notification;
            public string Channel;
            public bool TrackMainTweets;
            public bool TrackNonMainTweets;
        }
    }
}