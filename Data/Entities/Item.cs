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
        public string Name, ImageUrl;
        public int BaseDamage, BaseDefence;
        public List<ItemMove> Moveset;

        public Embed ItemEmbed(){
            EmbedBuilder e = new EmbedBuilder();
            e.WithAuthor(Name);
            e.WithCurrentTimestamp().WithColor(Discord.Color.Blue).WithThumbnailUrl(ImageUrl);

            e.AddField("Skills", string.Join("\n", Moveset.Select(x => x.ToString(BaseDamage, BaseDefence, 0))), true);
            
            return e.Build();
        }
    }

    public class ItemMove
    {
        [BsonId]
        public string Name;
        public double DamageModifier, DefenceModifier, HealthModifier, DeflectModifier;
        public int RageConsumption;

        public string ToString(int BaseDamage, int BaseDefence, int IncomingDamage){
            List<string> stats = new List<string>();
                if(DamageModifier != 0) stats.Add(DamageModifier * BaseDamage + "dmg");
                if(DeflectModifier != 0) stats.Add(DeflectModifier * IncomingDamage + "dmg");
                if(DefenceModifier != 0) stats.Add(DefenceModifier * BaseDefence + "def");
                if(HealthModifier != 0) stats.Add(HealthModifier + "hp");
                
                return $"[**{Name}**]: {string.Join(", ", stats)}, Rage Cost: {RageConsumption}";
        }
    }
}
