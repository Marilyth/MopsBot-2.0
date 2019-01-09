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

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class NewsTracker : BaseTracker
    {
        public string LastNews, Query, Source;

        public NewsTracker() : base(600000, ExistingTrackers * 2000)
        {
        }

        public NewsTracker(Dictionary<string, string> args) : base(600000, 60000){
            if(!StaticBase.Trackers[TrackerType.News].GetTrackers().ContainsKey(args["Name"] + "|" + args["Query"])){
                base.SetBaseValues(args);
                Name = args["Name"] + "|" + args["Query"];
                Query = args["Query"];
            } else {
                this.Dispose();
                var curTracker = StaticBase.Trackers[TrackerType.News].GetTrackers()[args["Name"] + "|||" + args["Regex"]];
                var curGuild = ((ITextChannel)Program.Client.GetChannel(ulong.Parse(args["Channel"]))).GuildId;

                var OldValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(curTracker.GetAsScope(curGuild)));
                StaticBase.Trackers[TrackerType.HTML].UpdateContent(new Dictionary<string, Dictionary<string, string>>{{"NewValues", args}, {"OldValues", OldValues}});
                throw new ArgumentException($"Tracker for {args["Name"]} existed already, updated instead!");
            }
        }

        public NewsTracker(string NewsQuery) : base(600000)
        {
            var request = NewsQuery.Split("|");
            Name = NewsQuery;

            Source = request[0];
            Query = request[1];

            //Check if query and source yield proper results, by forcing exceptions if not.
            try
            {
                var checkExists = getNews().Result;
                var test = checkExists[0];

                if (checkExists.GroupBy(x => x.Source.Name).Count() > 1)
                    throw new Exception();

                LastNews = test.PublishedAt.ToString();
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"`{Source}` didn't yield any proper result{(Query.Equals("") ? "" : $" for `{Query}`")}.");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                Article[] newArticles = await getNews();

                if (newArticles.Length > 0)
                {
                    LastNews = newArticles.First().PublishedAt.ToString();
                    await StaticBase.Trackers[TrackerType.News].UpdateDBAsync(this);
                }

                foreach (Article newArticle in newArticles)
                {
                    foreach (ulong channel in ChannelMessages.Keys.ToList())
                    {
                        await OnMajorChangeTracked(channel, createEmbed(newArticle), ChannelMessages[channel]);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" +  $"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<Article[]> getNews()
        {
            var result = await StaticBase.NewsClient.GetEverythingAsync(new EverythingRequest()
            {
                Q = Query,
                Sources = new List<string>() { Source },
                From = DateTime.Parse(LastNews ?? DateTime.MinValue.ToUniversalTime().ToString()).AddSeconds(1),
                SortBy = SortBys.PublishedAt
            });

            return result.Articles.ToArray();
            //return result.Articles.Where(x => x.Title.ToUpper().Contains(Query.ToUpper())).ToArray();
        }

        private Embed createEmbed(Article article)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(255, 255, 255);
            e.Title = article.Title;
            e.Url = article.Url;
            e.Timestamp = article.PublishedAt;
            e.ThumbnailUrl = article.UrlToImage;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://cdn5.vectorstock.com/i/1000x1000/82/39/the-news-icon-newspaper-symbol-flat-vector-5518239.jpg";
            footer.Text = article.Source.Name;
            e.Footer = footer;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = article.Author ?? "Unknown Author";

            e.Author = author;

            e.Description = article.Description;

            return e.Build();
        }

        public override Dictionary<string, object> GetParameters(ulong guildId)
        {
            var parameters = base.GetParameters(guildId);
            (parameters["Parameters"] as Dictionary<string, object>)["Name"] = "";
            (parameters["Parameters"] as Dictionary<string, object>)["Query"] = "";

            return parameters;
        }

        public override void Update(Dictionary<string, Dictionary<string, string>> args){
            base.Update(args);
            Query = args["NewValue"]["Query"];
        }

        public override object GetAsScope(ulong channelId){
            return new ContentScope(){
                Id = this.Name,
                Name = this.Source,
                Query = this.Query,
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId
            };
        }

        public new struct ContentScope
        {
            public string Id;
            public string Name;
            public string Query;
            public string Notification;
            public string Channel;
        }
    }
}
