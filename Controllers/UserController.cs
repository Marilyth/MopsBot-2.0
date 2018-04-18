using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MopsBot.Data.Individual;

namespace MopsBot.Api.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        public UserController(){

        }
        [HttpGet]
        public IEnumerable<KeyValuePair<ulong,User>> GetAll()
        {
            return StaticBase.people.Users.ToList();
        }

        [HttpGet("{id}", Name = "GetUser")]
        public IActionResult GetById(ulong id)
        {   
            User item = null;
            try{
                 item = StaticBase.people.Users.First(x=> x.Key==id).Value;
            }catch{}
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(item);

        }
    }
}