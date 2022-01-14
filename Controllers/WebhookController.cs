using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Newtonsoft.Json;
using MopsBot.Data.Entities;
using System.Threading.Tasks;
using System.IO;
using System;
using Discord;
using MopsBot.Data.Tracker;
using System.ServiceModel.Syndication;

namespace MopsBot.Api.Controllers
{
    [Route("api/[controller]")]
    public class WebhookController : Controller
    {
        public WebhookController()
        {

        }

        [HttpGet("youtube")]
        public async Task<IActionResult> ReplyChallengeYT()
        {
            Dictionary<string, string[]> parameters = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToArray());
            if (parameters.ContainsKey("hub.challenge"))
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Received a YT challenge, containing {string.Join("\n", parameters.Select(x => x.Key + ": " + string.Join(", ", x.Value)))}"));
                var channel = parameters["hub.topic"].FirstOrDefault().Split("channel_id=").LastOrDefault();
                if(!parameters["hub.mode"].FirstOrDefault().Contains("unsubscribe")){
                    MopsBot.Data.Tracker.YoutubeTracker tracker = StaticBase.Trackers[Data.Tracker.BaseTracker.TrackerType.Youtube].GetTrackers()[channel] as MopsBot.Data.Tracker.YoutubeTracker;
                    tracker.WebhookExpire = DateTime.Now.AddDays(4);
                    await tracker.UpdateTracker();
                }
                return new OkObjectResult(parameters["hub.challenge"].FirstOrDefault());
            }
            else
            {
                return new BadRequestResult();
            }
        }

        [HttpPost("twitch")]
        public async Task<string> WebhookReceived()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var headers = Request.Headers;

            await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Received a webhook message\n" + body));
            var update = JsonConvert.DeserializeObject<dynamic>(body);
            try{
                string id = update["subscription"]["condition"]["broadcaster_user_id"];
                TwitchTracker tracker = (TwitchTracker)StaticBase.Trackers[BaseTracker.TrackerType.Twitch].GetTrackers().FirstOrDefault(x => 
                    (x.Value as TwitchTracker).TwitchId.ToString().Equals(id)).Value;
                    
                if(update.ContainsKey("challenge")){
                    if(tracker is not null){
                        tracker.Callback = update["subscription"]["transport"]["callback"];
                        tracker.CallbackId = update["subscription"]["id"];
                        await tracker.UpdateTracker();
                    }
                    string challenge = update["challenge"];
                    return update["challenge"];
                }

                await tracker.CheckStreamerInfoAsync();
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by Twitch Webhook, someone went offline, probably.", e));
            }

            return "";
        }

        [HttpPost("youtube")]
        public async Task<IActionResult> WebhookReceivedYT()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var bodyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
            var headers = Request.Headers;

            await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Received a YT webhook message\n" + body));
            try
            {
                MopsBot.Data.Tracker.APIResults.Youtube.YoutubeNotification data;

                if(body.Contains("at:deleted-entry")) data = ConvertAtomToSyndicationTemporaryFix(body);
                else data = ConvertAtomToSyndication(bodyStream);
                
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

        /*[HttpPost("dbl")]
        public async Task<IActionResult> VoteWebhookReceived()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var headers = Request.Headers;

            await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Received a vote webhook message\n" + body));
            if (headers["Authorization"].Equals(Program.Config["DiscordBotListPassword"]))
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Webhook had correct password, processing"));

                var update = JsonConvert.DeserializeObject<dynamic>(body);
                try
                {
                    ulong voterId = update["user"];
                    //await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Voter id is: {voterId}"));
                    await StaticBase.UserVoted(voterId);
                }
                catch (Exception e)
                {
                    await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by voter Webhook.", e));
                }
            }

            return new OkResult();
        }*/

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
                    Published = item.PublishDate,
                    Updated = item.LastUpdatedTime
                };
            }
        }

        public static MopsBot.Data.Tracker.APIResults.Youtube.YoutubeNotification ConvertAtomToSyndicationTemporaryFix(string body)
        {
            return new MopsBot.Data.Tracker.APIResults.Youtube.YoutubeNotification()
            {
                ChannelId = body.Split("<uri>https://www.youtube.com/channel/", 2).Last().Split("<", 2).First(),
                VideoId = body.Split("ref=\"yt:video:", 2).Last().Split("\"", 2).First(),
                Title = "Unknown",
                Published = DateTimeOffset.Parse(body.Split("when=\"", 2).Last().Split("\"", 2).First()),
                Updated = DateTimeOffset.Parse(body.Split("when=\"", 2).Last().Split("\"", 2).First())
            };
        }

        public static string GetElementExtensionValueByOuterName(SyndicationItem item, string outerName)
        {
            if (item.ElementExtensions.All(x => x.OuterName != outerName)) return null;
            return item.ElementExtensions.Single(x => x.OuterName == outerName).GetObject<System.Xml.Linq.XElement>().Value;
        }
    }
}