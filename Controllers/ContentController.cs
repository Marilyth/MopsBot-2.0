using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
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

        [HttpGet]
        public IActionResult Get()
        {         
            Dictionary<string, string[]> parameters = HttpContext.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToArray());
            if(parameters.ContainsKey("name") && System.IO.File.Exists($"mopsdata//Images//{parameters["name"].First()}")){
                Byte[] b = System.IO.File.ReadAllBytes($"mopsdata//Images//{parameters["name"].First()}");          
                return File(b, $"image/{(parameters["name"].First().Contains("png") ? "png" : "jpeg")}");
            } else {        
                return new BadRequestResult();
            }
        }
    }
}