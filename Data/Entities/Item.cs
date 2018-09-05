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
        public int Id;
        public string Name;
        public string ImageUrl;
        public int BaseDamage, BaseDefence;
        public List<ItemMove> Moveset;

        public Embed ItemEmbed(){
            EmbedBuilder e = new EmbedBuilder();
            e.WithAuthor(Name);
            e.WithCurrentTimestamp().WithColor(Discord.Color.Blue).WithThumbnailUrl(ImageUrl);

            e.AddField("Skills", string.Join("\n", Moveset.Select(x => $"[**{x.Name}**], {x.DamageModifier * BaseDamage}dmg, Rage Cost: {x.RageConsumption}")), true);
            
            return e.Build();
        }
    }

    public class ItemMove
    {
        [BsonId]
        public string Name;
        public double DamageModifier, DefenceModifier, HealthModifier;
        public int RageConsumption;
    }
}
