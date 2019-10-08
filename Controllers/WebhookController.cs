using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Newtonsoft.Json;
using MopsBot.Data.Entities;
using System.Threading.Tasks;
using System.IO;
using System;

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
            Console.WriteLine("Received a challenge, responding with " + parameters["hub.challenge"]);
            return new OkObjectResult(parameters["hub.challenge"]);
        }

        [HttpPost("twitch")]
        public async Task<IActionResult> WebhookReceived()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var headers = Request.Headers;
            
            Console.WriteLine("Received a webhook message: " + Request.Body);
            var update = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            
            return new OkObjectResult("Yay");
        }
    }
}