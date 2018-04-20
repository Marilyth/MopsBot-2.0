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
    public class TwitterTracker : ITracker
    {
        public long lastMessage;
        private Task<IEnumerable<ITweet>> fetchTweets;

        public TwitterTracker() : base(300000)
        {
        }

        public TwitterTracker(string twitterName) : base(300000, 0)
        {
            Name = twitterName;
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            Tweetinvi.Parameters.IUserTimelineParameters parameters = Timeline.CreateUserTimelineParameter();
            if(lastMessage != 0) parameters.SinceId = lastMessage;
            parameters.MaximumNumberOfTweetsToRetrieve = 5;
            try{
                ITweet[] newTweets = Timeline.GetUserTimeline(Name, parameters).Reverse().ToArray();
            
                if(newTweets.Length != 0){
                 lastMessage = newTweets[newTweets.Length -1].Id;
                 StaticBase.trackers["twitter"].SaveJson();
                }
            
                foreach(ITweet newTweet in newTweets){
                    foreach(ulong channel in ChannelIds)
                        await OnMajorChangeTracked(channel, createEmbed(newTweet), "~Tweet Tweet~");
                    
                    System.Threading.Thread.Sleep(5000);
                }
            }catch (Exception e)
            {
                Console.WriteLine($"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }
    
        private EmbedBuilder createEmbed(ITweet tweet)
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

            return e;
        }
    }
}