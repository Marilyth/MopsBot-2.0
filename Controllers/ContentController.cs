using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MopsBot.Data.Entities;
using MopsBot.Data;
using MopsBot.Data.Tracker;
using Newtonsoft.Json;
using System;

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

            if(parameters["type"].First().Contains("Tracker")){
                result = StaticBase.Trackers.First(x => parameters["type"].Any(y => y.Split("Tracker")[0].Equals(x.Key.ToString())))
                             .Value.GetContent(0, ulong.Parse(parameters["guild"].First()));
            }
            else if(parameters["type"].First().Contains("Giveaway")){

            }
            else if(parameters["type"].First().Contains("Poll")){
                
            }
            else if(parameters["type"].First().Contains("RoleInvite")){
                
            }

            return new ObjectResult(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
    }
}