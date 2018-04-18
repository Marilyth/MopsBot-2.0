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
        public TrackerController(){

        }
        
        [HttpGet("get/{channel}")]
        public IActionResult GetTracks(ulong channel){
            var result = new Dictionary<string, string>();
            var fields = typeof(StaticBase).GetFields().Where(x=> x.FieldType.Name.Contains("TrackerHandler"));
            foreach(var field in fields){

                try{
                    Type t = typeof(TrackerHandler<>).MakeGenericType(field.FieldType.GenericTypeArguments.First());
                    var obj = Convert.ChangeType(field.GetValue(null), t);
                    var value = obj.GetType().GetMethod("getTracker").Invoke(obj, new[] {(object)channel}).ToString();
                    System.Console.Out.WriteLine(value);
                    if(value!=""){
                        string name = obj.GetType().GetMethod("getTrackerType").Invoke(obj, new object[0]).ToString();
                        result.Add(name,value);
                    }
                }catch(Exception e){
                    System.Console.Out.WriteLine(e);
                };

            }

            if(!result.Any())
                return BadRequest();
            return new ObjectResult(result);

        }

        [HttpGet("get/{channel}/{type}")]
        public IActionResult GetTracks(ulong channel, string type){
            string result = "";
            var fields = typeof(StaticBase).GetFields().Where(x=> x.FieldType.Name.Contains("TrackerHandler"));
            foreach(var field in fields){

                try{
                    Type t = typeof(TrackerHandler<>).MakeGenericType(field.FieldType.GenericTypeArguments.First());
                    var obj = Convert.ChangeType(field.GetValue(null), t);
                    var value = obj.GetType().GetMethod("getTracker").Invoke(obj, new[] {(object)channel}).ToString();
                    System.Console.Out.WriteLine(value);
                    string name = obj.GetType().GetMethod("getTrackerType").Invoke(obj, new object[0]).ToString().ToLower();
                    if(name.Contains(type)){
                        result += value;
                        break;
                    }
                }catch(Exception e){
                    System.Console.Out.WriteLine(e);
                };

            }

            if(result.Equals(""))
                return BadRequest();
            return new ObjectResult(result);
        }
    }
}