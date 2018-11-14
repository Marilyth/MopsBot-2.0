using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using Discord.WebSocket;
using Discord;

namespace MopsBot.Data.Entities
{
    public class CustomCommands
    {
        [BsonId]
        public ulong GuildId;

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, string> Commands;

        public CustomCommands(ulong guildId){
            GuildId = guildId;
            Commands = new Dictionary<string, string>();
        }

        public async Task AddCommandAsync(string command, string message){
            Commands[command] = message;
            await InsertOrUpdateAsync();
        }

        public async Task RemoveCommandAsync(string command){
            if(Commands.Count == 1){
                await RemoveFromDBAsync();
                StaticBase.CustomCommands.Remove(GuildId);
            } else {
                Commands.Remove(command);
                await InsertOrUpdateAsync();
            }
        }

        public async Task InsertOrUpdateAsync(){
            bool hasEntry = (await StaticBase.Database.GetCollection<CustomCommands>(this.GetType().Name).FindAsync(x => x.GuildId == GuildId)).ToList().Count == 1;
            
            if(!hasEntry)
                await StaticBase.Database.GetCollection<CustomCommands>(this.GetType().Name).InsertOneAsync(this);
            else
                await StaticBase.Database.GetCollection<CustomCommands>(this.GetType().Name).ReplaceOneAsync(x => x.GuildId == GuildId, this);
        }

        public async Task RemoveFromDBAsync(){
            await StaticBase.Database.GetCollection<CustomCommands>(this.GetType().Name).DeleteOneAsync(x => x.GuildId == GuildId);
        }
    }
}
