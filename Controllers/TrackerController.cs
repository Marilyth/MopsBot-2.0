using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MopsBot.Data.Individual;
using MopsBot.Data;
using MopsBot.Data.Tracker;
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
                    System.Console.Out.WriteLine(e);
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
            string[] type = new string[0];
            string[] channel = new string[0];
            bool alltypes = false;
            bool allchannels = false;
            if (parameters.Any(x => x.Key == "type"))
                type = parameters["type"];
            else
                alltypes = true;
            if (parameters.Any(x => x.Key == "channel"))
                channel = parameters["channel"];
            else
                allchannels = true;
            Results.TrackerResult.RootObject result = new Results.TrackerResult.RootObject();
            if (type.Length.Equals(0))
                alltypes = true;
            if (channel.Length.Equals(0))
                allchannels = true;
            foreach (var pair in StaticBase.trackers)
            {
                Dictionary<string, ITracker> dict = new Dictionary<string, ITracker>();
                if (!alltypes && !type.Any(x => pair.Key.Equals(x)))
                    continue;
                Results.TrackerResult.Type handler = new Results.TrackerResult.Type(pair.Key);
                if (allchannels)
                    dict = pair.Value.getTracker();
                else if (!(channel.Length == 0))
                {
                    var whichIds = new HashSet<string>();
                    channel.ToList().ForEach(c =>
                    {
                        whichIds.Concat(pair.Value.getTracker(ulong.Parse(c)).Split(','));
                    });
                    foreach (string id in whichIds)
                        dict.Concat(pair.Value.getTracker().Where(x=> x.Key.Equals(id)).ToDictionary(x=> x.Key,x=> x.Value));
                }
                else
                    return BadRequest();
                if (!dict.Any())
                    continue;
                foreach (var key in dict.Keys)
                {
                    Results.TrackerResult.Id id = new Results.TrackerResult.Id(key);
                    if (!allchannels)
                    {
                        handler.ids.Add(id);
                        continue;
                    }
                    var value = dict[key];
                    HashSet<ulong> channels = value.ChannelIds;
                    channels.ToList().ForEach(x => id.channels.Add(new Results.TrackerResult.Channel(x)));
                    handler.ids.Add(id);
                }
                result.types.Add(handler);
            }
            return new ObjectResult(result);
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
                    System.Console.Out.WriteLine(e);
                };

            }

            if (result.Equals(""))
                return BadRequest();
            return new ObjectResult(result);
        }

        [HttpPost("{channel}/{type}/{identificator}")]
        public IActionResult AddTracker(ulong channel, string type, string identificator)
        {

            var result = new Dictionary<string, string>();
            var fields = typeof(StaticBase).GetFields().Where(x => x.FieldType.Name.Contains("TrackerHandler"));
            foreach (var field in fields)
            {

                try
                {
                    Type t = typeof(TrackerHandler<>).MakeGenericType(field.FieldType.GenericTypeArguments.First());
                    var obj = Convert.ChangeType(field.GetValue(null), t);
                    string name = obj.GetType().GetMethod("getTrackerType").Invoke(obj, new object[0]).ToString();
                    if (name.Contains(type))
                    {
                        if (!obj.GetType().GetMethod("testIdentificator").Invoke(obj, new[] { (object)identificator }).Equals(true))
                            return NotFound(identificator);

                    }
                }
                catch (Exception e)
                {
                    System.Console.Out.WriteLine(e);
                };

            }

            if (!result.Any())
                return BadRequest();
            return new ObjectResult(result);
        }
    }
}