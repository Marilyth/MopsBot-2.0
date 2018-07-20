using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MopsBot.Data.Individual;
using MopsBot.Data;
using MopsBot.Data.Tracker;
using Newtonsoft.Json;
using System;

namespace MopsBot.Api.Controllers
{
    [Route("api/[controller]")]
    public class TrackerController : Controller
    {
        public TrackerController()
        {

        }

        [HttpGet("{channel}")]
        public IActionResult GetTracks(ulong channel)
        {
            var result = new Dictionary<string, string>();
            var fields = typeof(StaticBase).GetFields().Where(x => x.FieldType.Name.Contains("TrackerHandler"));
            foreach (var field in fields)
            {

                try
                {
                    Type t = typeof(TrackerHandler<>).MakeGenericType(field.FieldType.GenericTypeArguments.First());
                    var obj = Convert.ChangeType(field.GetValue(null), t);
                    var value = obj.GetType().GetMethod("getTracker").Invoke(obj, new[] { (object)channel }).ToString();
                    if (value != "")
                    {
                        string name = obj.GetType().GetMethod("getTrackerType").Invoke(obj, new object[0]).ToString();
                        result.Add(name, value);
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("\n" + e);
                };

            }

            if (!result.Any())
                return BadRequest();
            return new ObjectResult(result);

        }

        [HttpGet()]
        public IActionResult GetTracks()
        {
            Dictionary<string, string[]> parameters = HttpContext.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToArray());
            bool allTypes = !parameters.ContainsKey("type");
            bool allChannels = !parameters.ContainsKey("channel");

            Dictionary<string, TrackerWrapper> allTrackers = StaticBase.Trackers;
            HashSet<ITracker> allResults = new HashSet<ITracker>();

            allTrackers = allTrackers.
                Where(x => allTypes || parameters["type"].Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);

            foreach (var key in allTrackers.Keys)
                allResults = allResults.Concat(allTrackers[key].GetTrackerSet().
                    Where(x => allChannels ||
                    parameters["channel"].Any(y => x.ChannelIds.Contains(ulong.Parse(y)))))
                    .ToHashSet();

            var result = allResults.GroupBy(x => x.GetType()).ToDictionary(x => x.Key.Name,
                x => x.ToDictionary(y => y.Name, y => y.ChannelIds.
                Where(z => allChannels || parameters["channel"].Contains(z.ToString()))));

            return new ObjectResult(JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        [HttpGet("{channel}/{type}")]
        public IActionResult GetTracks(ulong channel, string type)
        {
            string result = "";
            var fields = typeof(StaticBase).GetFields().Where(x => x.FieldType.Name.Contains("TrackerHandler"));
            foreach (var field in fields)
            {

                try
                {
                    Type t = typeof(TrackerHandler<>).MakeGenericType(field.FieldType.GenericTypeArguments.First());
                    var obj = Convert.ChangeType(field.GetValue(null), t);
                    var value = obj.GetType().GetMethod("getTracker").Invoke(obj, new[] { (object)channel }).ToString();
                    string name = obj.GetType().GetMethod("getTrackerType").Invoke(obj, new object[0]).ToString().ToLower();
                    if (name.Contains(type))
                    {
                        result += value;
                        break;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("\n" + e);
                };

            }

            if (result.Equals(""))
                return BadRequest();
            return new ObjectResult(result);
        }

        [HttpGet("add/{channel}/{type}/{name}/{notification}")]
        public IActionResult AddNewTracker(ulong channel, string type, string name, string notification)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "http://5.45.104.29");
            try
            {
                StaticBase.Trackers[type].AddTracker(name, channel, notification);
            }
            catch (Exception e)
            {
                return new ObjectResult(e.InnerException?.Message ?? e.Message);
            }
            return new ObjectResult("Success");
        }

        [HttpGet("remove/{channel}/{type}/{name}")]
        public IActionResult RemoveTracker(ulong channel, string type, string name)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "http://5.45.104.29");
            try
            {
                var result = StaticBase.Trackers[type].TryRemoveTracker(name, channel);
                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                return new ObjectResult(e.Message);
            }
        }
    }
}