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
    public class Enemy
    {
        [BsonId]
        public string Name;
        public int Health, Damage, Defence, Rage;
        public string ImageUrl;

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<int, int> LootChance;
        public List<ItemMove> MoveList;

        public ItemMove GetNextMove(){
            MoveList = MoveList.OrderByDescending(x => x.RageConsumption).ToList();
            foreach(var move in MoveList){
                if(Rage >= move.RageConsumption){
                    var roll = StaticBase.ran.Next(0, 2);

                    if(roll == 0){
                        return move;
                    }
                }
            }

            return MoveList.Last();
        }

        public Embed StatEmbed(){
            EmbedBuilder e = new EmbedBuilder();
            e.WithAuthor(Name);
            e.WithCurrentTimestamp().WithColor(Discord.Color.DarkRed).WithThumbnailUrl(ImageUrl);

            e.AddField("Stats", $"Health: {Health}HP\nExperience: {Health * Damage * 10}");
            e.AddField("Skills", string.Join("\n", MoveList.Select(x => $"[**{x.Name}**], {x.DamageModifier * Damage}dmg, Rage Cost: {x.RageConsumption}")), true);

            e.AddField("Loot", string.Join("\n", LootChance.Select(x => {
                    Item i = StaticBase.Database.GetCollection<Item>("Items").FindSync(y => y.Id == x.Key).First();
                    return $"`{i.Name}` ({i.Moveset.Count} Skill(s)), Dropchance: {x.Value}%";}
                )), true);
            
            return e.Build();
        }

        public List<Item> GetLoot(){
            List<Item> loot = new List<Item>();

            foreach(var item in LootChance){
                var roll = StaticBase.ran.Next(1, 101);
                if(roll <= item.Value)
                    loot.Add(StaticBase.Database.GetCollection<Item>("Items").FindSync(y => y.Id == item.Key).First());
            }

            return loot;
        }
    }
}
