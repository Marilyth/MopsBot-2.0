using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using Discord;

namespace MopsBot.Data.Entities
{
    public class Item
    {
        [BsonId]
        public string Name;
        public int Damage;
        public int Defence;
    }
}
