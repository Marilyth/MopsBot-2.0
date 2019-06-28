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
    public class TrackerController : Controller
    {
        public static List<string> Parameters = new List<string>(){"Name", "Notification", "Channel"};

        public TrackerController()
        {

        }

        [HttpGet()]
        public IActionResult GetTrackers()
        {
            //HttpContext.Response.Headers.Append(new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("Access-Control-Allow-Origin", "http://0.0.0.0"));
            Dictionary<string, string[]> parameters = HttpContext.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToArray());
            bool allTypes = !parameters.ContainsKey("type");
            bool allChannels = !parameters.ContainsKey("channel");
            IEnumerable<ulong> channels = parameters.ContainsKey("channel") ? parameters["channels"].Select(x => ulong.Parse(x)) :
                                          Program.Client.GetGuild(ulong.Parse(parameters["guild"].First())).Channels.Select(x => x.Id);

            IEnumerable<IContent> allResults = new List<IContent>();

            allResults = StaticBase.Trackers.First(x => parameters["type"].Any(y => y.Equals(x.Key.ToString())))
                        .Value.GetTrackers().Where(x => channels.Any(y => x.Value.ChannelConfig.ContainsKey(y)))
                        .Select(x => new IContent(){Name=x.Value.Name, 
                                                    Channel=x.Value.ChannelConfig.First(y => channels.ToList().Contains(y.Key)).Key,
                                                    Notification=(string)x.Value.ChannelConfig.First(y => channels.ToList().Contains(y.Key)).Value["Notification"]});
            
            ParameterPair<IEnumerable<IContent>> result = new ParameterPair<IEnumerable<IContent>>(Parameters, allResults);

            return new ObjectResult(JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        /*[HttpGet("add/{token}/{channel}/{type}/{name}/{notification}")]
        public IActionResult AddNewTracker(string token, ulong channel, string type, string name, string notification)
        {
            if (token.Equals(Program.Config["MopsAPI"]))
            {
                Response.Headers.Add("Access-Control-Allow-Origin", "http://5.45.104.29");
                try
                {
                    StaticBase.Trackers[type].AddTrackerAsync(name, channel, notification);
                }
                catch (Exception e)
                {
                    return new ObjectResult(e.InnerException?.Message ?? e.Message);
                }
                return new ObjectResult("Success");
            }
            return new ObjectResult("Wrong token");
        }*/

        /*[HttpGet("remove/{token}/{channel}/{type}/{name}")]
        public IActionResult RemoveTracker(string token, ulong channel, string type, string name)
        {
            if (token.Equals(Program.Config["MopsAPI"]))
            {
                Response.Headers.Add("Access-Control-Allow-Origin", "http://5.45.104.29");
                try
                {
                    var result = StaticBase.Trackers[type].TryRemoveTrackerAsync(name, channel);
                    return new ObjectResult(result);
                }
                catch (Exception e)
                {
                    return new ObjectResult(e.Message);
                }
            }
            return new ObjectResult("Wrong token");
        }*/
    }

    public class IContent{
        public string Name;
        public ulong Channel;
        public string Notification;
    }

    public class ParameterPair<T>{
        public T Content;
        public List<string> Parameters;
        public ParameterPair(List<string> parameters, T values){
            Content = values;
            Parameters = parameters;
        }
    }
}