using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
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
        public static DateTime baseDate = new DateTime(1, 1, 1, 1, 0, 0);
        public DateTime? LastFeed;
        public string LastTitle;

        public RSSTracker() : base()
        {
        }

        public RSSTracker(Dictionary<string, string> args) : base()
        {
            base.SetBaseValues(args, true);

            //Check if Name ist valid
            try
            {
                var test = new RSSTracker(Name);
                test.Dispose();
                LastFeed = test.LastFeed;
                LastTitle = test.LastTitle;
                SetTimer();
            }
            catch (Exception e)
            {
                this.Dispose();
                throw e;
            }

            if (StaticBase.Trackers[TrackerType.RSS].GetTrackers().ContainsKey(Name))
            {
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.RSS].GetTrackers()[Name];
                curTracker.ChannelConfig[ulong.Parse(args["Channel"].Split(":")[1])]["Notification"] = args["Notification"];
                StaticBase.Trackers[TrackerType.RSS].UpdateContent(new Dictionary<string, Dictionary<string, string>> { { "NewValue", args }, { "OldValue", args } }).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }

        public RSSTracker(string url) : base()
        {
            Name = url;

            //Check if query and source yield proper results, by forcing exceptions if not.
            try
            {
                var checkExists = getFeed().Result;

                try{
                    LastFeed = checkExists.Items.OrderByDescending(x => x.PublishDate.DateTime).FirstOrDefault()?.PublishDate.UtcDateTime ?? DateTime.UtcNow;
                    if(LastFeed.Value.Year == 1){ LastFeed = null; throw new Exception("No date set");}
                    SetTimer();
                } catch (Exception e){
                    LastFeed = null;
                    LastTitle = checkExists.Items.FirstOrDefault()?.Title?.Text ?? "";
                }
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"The url did not provide any valid data!\nIs it an RSS feed?");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var feed = await getFeed();
                List<SyndicationItem> feedItems;
                if(LastFeed != null){
                    feedItems = feed.Items.Where(x => x.PublishDate.UtcDateTime > LastFeed?.AddSeconds(1)).OrderBy(x => x.PublishDate).ToList();
                } else {
                    feedItems = feed.Items.TakeWhile(x => !x.Title.Text.Equals(LastTitle))?.Reverse().ToList();
                }

                if (feedItems.Count() > 0)
                {
                    if(LastFeed != null) LastFeed = feedItems.Last().PublishDate.UtcDateTime;
                    else LastTitle = feedItems.Last().Title?.Text;
                    await StaticBase.Trackers[TrackerType.RSS].UpdateDBAsync(this);
                }

                foreach (var newFeed in feedItems)
                {
                    foreach (ulong channel in ChannelConfig.Keys.ToList())
                    {
                        await OnMajorChangeTracked(channel, createEmbed(newFeed, feed), (string)ChannelConfig[channel]["Notification"]);
                    }
                }

            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        private async Task<SyndicationFeed> getFeed()
        {
            return await FetchRSSData(Name);
        }

        private static Embed createEmbed(SyndicationItem feedItem, SyndicationFeed parent)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(255, 255, 255);
            e.Title = feedItem.Title?.Text;
            e.Url = feedItem.Links?.FirstOrDefault(x => !isImageUrl(x.Uri?.AbsoluteUri ?? ""))?.Uri?.AbsoluteUri ?? feedItem.BaseUri?.AbsoluteUri;
            
            try{
                e.Timestamp = feedItem.PublishDate.UtcDateTime.Year > 1 ? feedItem.PublishDate.UtcDateTime : feedItem.LastUpdatedTime.UtcDateTime;
            } catch (Exception ex){
                e.Timestamp = DateTime.UtcNow;
            }

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://cdn5.vectorstock.com/i/1000x1000/82/39/the-news-icon-newspaper-symbol-flat-vector-5518239.jpg";
            footer.Text = "RSS feed";
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = (feedItem.Authors?.FirstOrDefault()?.Name ?? feedItem.Authors?.FirstOrDefault()?.Email) ?? parent.Title?.Text;
            author.Url = feedItem.Authors?.FirstOrDefault()?.Uri;
            e.Author = author;

            e.ThumbnailUrl = parent.ImageUrl?.AbsoluteUri;

            e.Description = (new string(HtmlToPlainText(feedItem.Summary?.Text ?? "", out string htmlImage).Take(Math.Min(2000, feedItem.Summary.Text.Length)).ToArray()));
            e.ImageUrl = !string.IsNullOrEmpty(htmlImage) ? htmlImage : feedItem.Links?.FirstOrDefault(x => isImageUrl(x.Uri?.AbsoluteUri ?? ""))?.Uri?.AbsoluteUri;
            if (e.Description.Length >= 2000) e.Description += " [...]";

            return e.Build();
        }

        private static bool isImageUrl(string URL)
        {
            try
            {
                var req = System.Net.HttpWebRequest.Create(URL);
                req.Method = "HEAD";
                using (var resp = req.GetResponse())
                {
                    return resp.ContentType.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                               .StartsWith("image/");
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static string HtmlToPlainText(string html, out string image)
        {
            const string images = "img src=[\"'](.*?)[\"']";//matches images
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR|p|P)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var imagesRegex = new Regex(images, RegexOptions.Multiline);
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            image = imagesRegex.Match(text).Groups.Last().Value;
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

        public static async Task<Embed> GetFeed(string url){
            var data = await FetchRSSData(url);
            return createEmbed(data.Items.First(), data);
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
