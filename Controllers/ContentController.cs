using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MopsBot.Data.Entities;
using MopsBot.Data;
using MopsBot.Data.Tracker;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.IO;

namespace MopsBot.Api.Controllers
{
    [Route("api/[controller]")]
    public class ContentController : Controller
    {
        public ContentController()
        {

        }

        [HttpGet()]
        public IActionResult GetContent()
        {
            Dictionary<string, string[]> parameters = HttpContext.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToArray());

            IEnumerable<ulong> channels = parameters.ContainsKey("channel") ? parameters["channels"].Select(x => ulong.Parse(x)) :
                                          Program.Client.GetGuild(ulong.Parse(parameters["guild"].First())).Channels.Select(x => x.Id);

            Dictionary<string, object> result = new Dictionary<string, object>();

            if (parameters["type"].First().Contains("Tracker"))
            {
                result = StaticBase.Trackers.First(x => parameters["type"].Any(y => y.Split("Tracker")[0].Equals(x.Key.ToString())))
                             .Value.GetContent(0, ulong.Parse(parameters["guild"].First()));
            }
            else if (parameters["type"].First().Contains("Giveaway"))
            {

            }
            else if (parameters["type"].First().Contains("Poll"))
            {

            }
            else if (parameters["type"].First().Contains("RoleInvite"))
            {

            }

            return new ObjectResult(JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddContent()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var token = Request.Headers["Token"].ToString();
            var id = await GetUserViaTokenAsync(token);

            try
            {
                var add = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                if (!id.Equals(110431936635207680)
                    && !id.Equals(110429968252555264)) throw new Exception("You cannot use this service");
                if (Request.Headers["Type"].ToString().Contains("Tracker"))
                {
                    await StaticBase.Trackers.First(x => Request.Headers["Type"].Any(y => y.Split("Tracker")[0].Equals(x.Key.ToString())))
                             .Value.AddContent(add);
                }
                else if (Request.Headers["Type"].Contains("Giveaway"))
                {

                }
                else if (Request.Headers["Type"].Contains("Poll"))
                {

                }
                else if (Request.Headers["Type"].Contains("RoleInvite"))
                {

                }

                return new ObjectResult("Success");
            }
            catch (Exception e)
            {
                if(e.GetBaseException().Message.Contains("updated")) return new ObjectResult("Success");
                return new ObjectResult($"ERROR: {e.GetBaseException().Message}");
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateContent()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var token = Request.Headers["Token"].ToString();
            var id = await GetUserViaTokenAsync(token);

            try
            {
                var update = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(body);
                if (!id.Equals(110431936635207680)
                    && !id.Equals(110429968252555264)) throw new Exception("You cannot use this service");
                if (Request.Headers["Type"].ToString().Contains("Tracker"))
                {
                    await StaticBase.Trackers.First(x => Request.Headers["Type"].Any(y => y.Split("Tracker")[0].Equals(x.Key.ToString())))
                             .Value.UpdateContent(update);
                }
                else if (Request.Headers["Type"].Contains("Giveaway"))
                {

                }
                else if (Request.Headers["Type"].Contains("Poll"))
                {

                }
                else if (Request.Headers["Type"].Contains("RoleInvite"))
                {

                }

                return new ObjectResult("Success");
            }
            catch (Exception e)
            {
                return new ObjectResult($"ERROR: {e.Message}");
            }
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveContent()
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            var token = Request.Headers["Token"].ToString();
            var id = await GetUserViaTokenAsync(token);

            try
            {
                var remove = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                if (!id.Equals(110431936635207680)
                    && !id.Equals(110429968252555264)) throw new Exception("You cannot use this service");
                if (Request.Headers["Type"].ToString().Contains("Tracker"))
                {
                    await StaticBase.Trackers.First(x => Request.Headers["Type"].Any(y => y.Split("Tracker")[0].Equals(x.Key.ToString())))
                             .Value.RemoveContent(remove);
                }
                else if (Request.Headers["Type"].Contains("Giveaway"))
                {

                }
                else if (Request.Headers["Type"].Contains("Poll"))
                {

                }
                else if (Request.Headers["Type"].Contains("RoleInvite"))
                {

                }

                return new ObjectResult("Success");
            }
            catch (Exception e)
            {
                return new ObjectResult($"ERROR: {e.Message}");
            }
        }

        private async Task<ulong> GetUserViaTokenAsync(string token){
            var contentHeader = new KeyValuePair<string, string>("Content-Type", "application/json");
            var tokenHeader = new KeyValuePair<string, string>("Authorization", "Bearer " + token);
            var response = await MopsBot.Module.Information.ReadURLAsync("https://discordapp.com/api/v6/users/@me", tokenHeader, contentHeader);

            return JsonConvert.DeserializeObject<dynamic>(response)["id"];
        }
    }
}