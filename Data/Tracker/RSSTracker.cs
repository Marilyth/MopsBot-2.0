using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using NewsAPI;
using NewsAPI.Constants;
using NewsAPI.Models;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class RSSTracker : BaseTracker
    {
        public DateTime LastFeed;

        public RSSTracker() : base(600000, ExistingTrackers * 2000)
        {
        }

        public RSSTracker(Dictionary<string, string> args) : base(600000, 60000)
        {
            base.SetBaseValues(args);

            //Check if Name ist valid
            try
            {
                new RSSTracker(Name).Dispose();
            }
            catch (Exception e)
            {
                this.Dispose();
                throw e;
            }

            if (StaticBase.Trackers[TrackerType.News].GetTrackers().ContainsKey(Name))
            {
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.News].GetTrackers()[Name];
                curTracker.ChannelMessages[ulong.Parse(args["Channel"].Split(":")[1])] = args["Notification"];
                StaticBase.Trackers[TrackerType.News].UpdateContent(new Dictionary<string, Dictionary<string, string>> { { "NewValue", args }, { "OldValue", args } }).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }

        public RSSTracker(string url) : base(600000)
        {
            Name = url;

            //Check if query and source yield proper results, by forcing exceptions if not.
            try
            {
                var checkExists = getFeed();

                LastFeed = checkExists.Items.OrderByDescending(x => x.PublishDate.DateTime).FirstOrDefault()?.PublishDate.UtcDateTime ?? DateTime.UtcNow;
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"The url did not provide any valid data!\nIs it an RSS feed?");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var feed = getFeed();
                var feedItems = feed.Items.Where(x => x.PublishDate.UtcDateTime > LastFeed.AddSeconds(1)).OrderBy(x => x.PublishDate).ToArray();

                if (feedItems.Length > 0)
                {
                    LastFeed = feedItems.Last().PublishDate.UtcDateTime;
                    await StaticBase.Trackers[TrackerType.RSS].UpdateDBAsync(this);
                }

                foreach (var newFeed in feedItems)
                {
                    foreach (ulong channel in ChannelMessages.Keys.ToList())
                    {
                        await OnMajorChangeTracked(channel, createEmbed(newFeed), ChannelMessages[channel]);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + $"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private SyndicationFeed getFeed()
        {
            return FetchRSSData(Name);
        }

        private Embed createEmbed(SyndicationItem feedItem)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(255, 255, 255);
            e.Title = feedItem.Title.Text;
            e.Url = feedItem.Links?.FirstOrDefault()?.Uri?.AbsoluteUri ?? feedItem.BaseUri.AbsoluteUri;
            e.Timestamp = feedItem.PublishDate.UtcDateTime;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://cdn5.vectorstock.com/i/1000x1000/82/39/the-news-icon-newspaper-symbol-flat-vector-5518239.jpg";
            footer.Text = "RSS feed";
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = feedItem.Authors?.FirstOrDefault()?.Name;
            author.Url = feedItem.Authors?.FirstOrDefault()?.Uri;
            e.Author = author;

            var image = feedItem.Links?.FirstOrDefault(x => x.RelationshipType.Contains("enclosure"))?.Uri?.AbsoluteUri;
            e.ImageUrl = image;
            e.Description = new string(feedItem.Summary?.Text?.Take(Math.Min(2000, feedItem.Summary.Text.Length)).ToArray() ?? new char[]{' '});

            return e.Build();
        }

        private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }

        public new struct ContentScope
        {
            public string Id;
            public string _Name;
            public string Query;
            public string Notification;
            public string Channel;
        }
    }
}
