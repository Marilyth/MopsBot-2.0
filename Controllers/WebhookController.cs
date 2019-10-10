using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Newtonsoft.Json;
using MopsBot.Data.Entities;
using System.Threading.Tasks;
using System.IO;
using System;
using Discord;

namespace MopsBot.Api.Controllers
{
    [Route("api/[controller]")]
    public class WebhookController : Controller
    {
        public WebhookController(){

        }

        [HttpGet("twitch")]
        public async Task<IActionResult> ReplyChallenge()
        {
            Dictionary<string, string[]> parameters = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToArray());
            if(parameters.ContainsKey("hub.challenge")){
                Console.WriteLine("Received a challenge, responding with " + parameters["hub.challenge"].FirstOrDefault());
                return new OkObjectResult(parameters["hub.challenge"].FirstOrDefault());
            } else {
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
            try{
                string name = update["data"][0]["user_name"].ToString().ToLower();

                MopsBot.Data.Tracker.TwitchTracker tracker = StaticBase.Trackers[Data.Tracker.BaseTracker.TrackerType.Twitch].GetTrackers()[name] as MopsBot.Data.Tracker.TwitchTracker;
                await tracker.CheckStreamerInfoAsync();
            } catch(Exception e) {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by Twitch Webhook, someone went offline, probably.", e));
            }
            
            return new OkResult();
        }
    }
}