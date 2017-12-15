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

namespace MopsBot.Module.Data.Session
{
    public class TwitterTracker
    {
        private System.Threading.Timer checkForChange;
        public string name;
        private IUserIdentifier ident;
        public long lastMessage;
        private Task<IEnumerable<ITweet>> fetchTweets;
        public HashSet<ulong> ChannelIds;
        

        public TwitterTracker(string twitterName, long pLastMessage)
        {
            lastMessage = pLastMessage;
            name = twitterName;
            ChannelIds = new HashSet<ulong>();

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6,59)*1000, 300000);
        }

        private void CheckForChange_Elapsed(object stateinfo)
        {
            Tweetinvi.Parameters.IUserTimelineParameters parameters = Timeline.CreateUserTimelineParameter();
            if(lastMessage != 0) parameters.SinceId = lastMessage;
            parameters.MaximumNumberOfTweetsToRetrieve = 5;

            ITweet[] newTweets = Timeline.GetUserTimeline(name, parameters).Reverse().ToArray();

            if(newTweets.Length != 0){
                 lastMessage = newTweets[newTweets.Length -1].Id;
                 StaticBase.twitterTracks.writeList();
            }
            
            foreach(ITweet newTweet in newTweets){
                sendTwitterNotification(newTweet);
                System.Threading.Thread.Sleep(5000);
            }
        }

        private void sendTwitterNotification(ITweet tweet)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = "Tweet-Link";
            e.Url = tweet.Url;
            e.Timestamp = tweet.CreatedAt.AddHours(-1);

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
            foreach(Tweetinvi.Models.Entities.IMediaEntity cur in tweet.Media)
                if(cur.MediaType.Equals("photo")) 
                    e.ImageUrl = cur.MediaURL;

            e.Description = tweet.FullText;

            foreach(var channel in ChannelIds)
            {
                ((SocketTextChannel)Program.client.GetChannel(channel)).SendMessageAsync("~ Tweet Tweet ~", false, e);
            }
        }
    }
}