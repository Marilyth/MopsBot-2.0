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
using DiscordBotsList.Api.Objects;
using MopsBot.Data.Tracker;

namespace MopsBot.Data.Entities
{
    [BsonIgnoreExtraElements]
    public class TwitchGuild
    {
        [BsonId]
        public ulong DiscordId;
        //public List<TwitchUser> TwitchUsers;
        public ulong LiveRole;
        public ulong notifyChannel;
        public List<Tuple<int, ulong>> RankRoles;

        public TwitchGuild(ulong dId)
        {
            DiscordId = dId;
            //TwitchUsers = new List<TwitchUser>();
            RankRoles = new List<Tuple<int, ulong>>();
        }

        public async Task UpdateGuildAsync()
        {
            TwitchGuild user = (await StaticBase.Database.GetCollection<TwitchGuild>("TwitchGuilds").FindAsync(x => x.DiscordId == DiscordId)).FirstOrDefault();

            if (user == null)
            {
                await StaticBase.Database.GetCollection<TwitchGuild>("TwitchGuilds").InsertOneAsync(this);
            } else {
                await StaticBase.Database.GetCollection<TwitchGuild>("TwitchGuilds").ReplaceOneAsync(x => x.DiscordId == DiscordId, this);
            }
        }
    }
}
