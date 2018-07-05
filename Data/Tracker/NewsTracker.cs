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
    public class NewsTracker : ITracker
    {
        public string LastNews, Query, Source;

        public NewsTracker() : base(600000, (ExistingTrackers * 2000 + 500) % 600000)
        {
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
                    StaticBase.Trackers["news"].SaveJson();
                }

                foreach (Article newArticle in newArticles)
                {
                    foreach (ulong channel in ChannelIds)
                    {
                        await OnMajorChangeTracked(channel, createEmbed(newArticle), ChannelMessages[channel]);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
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
            e.Color = new Color(0x0099ff);
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
    }
}
