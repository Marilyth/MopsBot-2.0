using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Newtonsoft.Json;
using MopsBot.Data.Entities;
using System.Threading.Tasks;
using System.IO;
using System;
using Discord;
using System.ServiceModel.Syndication;

namespace MopsBot.Api.Controllers
{
    [Route("api/[controller]")]
    public class WebhookController : Controller
    {
        public WebhookController()
        {

        }

        [HttpGet("twitch")]
        public async Task<IActionResult> ReplyChallenge()
        {
            Dictionary<string, string[]> parameters = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToArray());
            if (parameters.ContainsKey("hub.challenge"))
            {
                Console.WriteLine("Received a challenge, responding with " + parameters["hub.challenge"].FirstOrDefault());
                return new OkObjectResult(parameters["hub.challenge"].FirstOrDefault());
            }
            else
            {
                return new BadRequestResult();
            }
        }

        [HttpGet("youtube")]
        public async Task<IActionResult> ReplyChallengeYT()
        {
            Dictionary<string, string[]> parameters = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToArray());
            if (parameters.ContainsKey("hub.challenge"))
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Received a YT challenge, responding with " + parameters["hub.challenge"].FirstOrDefault()));
                return new OkObjectResult(parameters["hub.challenge"].FirstOrDefault());
            }
            else
            {
                return new BadRequestResult();
            }
        }

        [HttpPost("twitch")]
        public async Task<IActionResult> WebhookReceived()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var headers = Request.Headers;

            await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Received a webhook message\n" + body));
            var update = JsonConvert.DeserializeObject<dynamic>(body);
            try
            {
                string name = update["data"][0]["user_name"].ToString().ToLower();

                MopsBot.Data.Tracker.TwitchTracker tracker = StaticBase.Trackers[Data.Tracker.BaseTracker.TrackerType.Twitch].GetTrackers()[name] as MopsBot.Data.Tracker.TwitchTracker;
                await tracker.CheckStreamerInfoAsync();
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by Twitch Webhook, someone went offline, probably.", e));
            }

            return new OkResult();
        }

        [HttpPost("youtube")]
        public async Task<IActionResult> WebhookReceivedYT()
        {
            try
            {
                Request.Body.Position = 0;
                var data = ConvertAtomToSyndication(Request.Body);
                Request.Body.Position = 0;
                string body = new StreamReader(Request.Body).ReadToEnd();
                var headers = Request.Headers;

                await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Received a YT webhook message\n" + body));
                MopsBot.Data.Tracker.YoutubeTracker tracker = StaticBase.Trackers[Data.Tracker.BaseTracker.TrackerType.Youtube].GetTrackers()[data.ChannelId] as MopsBot.Data.Tracker.YoutubeTracker;
                await tracker.CheckInfoAsync(data);
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by Youtube Webhook.", e));
            }

            return new OkResult();
        }


        [HttpPost("mixer")]
        public async Task<IActionResult> MixerWebhookReceived()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var headers = Request.Headers;

            await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Received a mixer webhook message\n" + body));
            var update = JsonConvert.DeserializeObject<dynamic>(body);
            try
            {
                string name = update["data"][0]["user_name"].ToString().ToLower();

                MopsBot.Data.Tracker.TwitchTracker tracker = StaticBase.Trackers[Data.Tracker.BaseTracker.TrackerType.Twitch].GetTrackers()[name] as MopsBot.Data.Tracker.TwitchTracker;
                await tracker.CheckStreamerInfoAsync();
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by Mixer Webhook.", e));
            }

            return new OkResult();
        }

        public static MopsBot.Data.Tracker.APIResults.Youtube.YoutubeNotification ConvertAtomToSyndication(Stream stream)
        {
            using (var xmlReader = System.Xml.XmlReader.Create(stream))
            {
                SyndicationFeed feed = SyndicationFeed.Load(xmlReader);
                var item = feed.Items.FirstOrDefault();
                return new MopsBot.Data.Tracker.APIResults.Youtube.YoutubeNotification()
                {
                    ChannelId = GetElementExtensionValueByOuterName(item, "channelId"),
                    VideoId = GetElementExtensionValueByOuterName(item, "videoId"),
                    Title = item.Title.Text,
                    Published = item.PublishDate.ToString("dd/MM/yyyy"),
                    Updated = item.LastUpdatedTime.ToString("dd/MM/yyyy")
                };
            }
        }

        public static string GetElementExtensionValueByOuterName(SyndicationItem item, string outerName)
        {
            if (item.ElementExtensions.All(x => x.OuterName != outerName)) return null;
            return item.ElementExtensions.Single(x => x.OuterName == outerName).GetObject<System.Xml.Linq.XElement>().Value;
        }
    }
}