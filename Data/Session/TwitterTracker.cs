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

namespace MopsBot.Data.Session
{
    public class TwitterTracker : ITracker
    {
        bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        
        private System.Threading.Timer checkForChange;
        public string name;
        private IUserIdentifier ident;
        public long lastMessage;
        private Task<IEnumerable<ITweet>> fetchTweets;

        public TwitterTracker(string twitterName)
        {
            name = twitterName;
            ChannelIds = new HashSet<ulong>();

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6,59)*1000, 300000);
        }

        public TwitterTracker(string[] initArray)
        {
            ChannelIds = new HashSet<ulong>();

            name = initArray[0];
            lastMessage = long.Parse(initArray[1]);
            foreach(string channel in initArray[2].Split(new char[]{'{','}',';'})){
                if(channel != "")
                    ChannelIds.Add(ulong.Parse(channel));
            }
            

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6,59)*1000, 300000);
        }

        protected override void CheckForChange_Elapsed(object stateinfo)
        {
            Tweetinvi.Parameters.IUserTimelineParameters parameters = Timeline.CreateUserTimelineParameter();
            if(lastMessage != 0) parameters.SinceId = lastMessage;
            parameters.MaximumNumberOfTweetsToRetrieve = 5;
            try{
                ITweet[] newTweets = Timeline.GetUserTimeline(name, parameters).Reverse().ToArray();
            
                if(newTweets.Length != 0){
                 lastMessage = newTweets[newTweets.Length -1].Id;
                 StaticBase.twitterTracks.writeList();
                }
            
                foreach(ITweet newTweet in newTweets){
                    foreach(ulong channel in ChannelIds)
                        OnMajorChangeTracked(channel, sendTwitterNotification(newTweet), "~Tweet Tweet~");
                    
                    System.Threading.Thread.Sleep(5000);
                }
            }catch{
                return;
            }
        }
    
        private EmbedBuilder sendTwitterNotification(ITweet tweet)
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

        public override string[] getInitArray(){
            string[] informationArray = new string[2 + ChannelIds.Count];
            informationArray[0] = name;
            informationArray[1] = lastMessage.ToString();
            informationArray[2] = "{" + string.Join(";", ChannelIds) + "}";

            return informationArray;
        }


        public override void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);           
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return; 
      
            if (disposing) {
                handle.Dispose();
                checkForChange.Dispose();
            }
      
            disposed = true;
        }
    }
}