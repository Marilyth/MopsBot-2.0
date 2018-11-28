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

namespace MopsBot.Data.Entities
{
    [BsonIgnoreExtraElements]
    public class UserEvent
    {
        public List<ulong> pastList;
        public static event UserHasVoted UserVoted;
        public delegate Task UserHasVoted(IDblEntity voter);

        public async Task CheckUsersVotedLoop()
        {
            while (true)
            {
                var voterList = (await StaticBase.DiscordBotList.GetVotersAsync()).Select(x => x.Id).ToList();
                voterList.Reverse();

                var newVoters = new List<IDblEntity>();

                if (pastList == null)
                {
                    pastList = (await StaticBase.Database.GetCollection<UserEvent>("Voters").FindAsync(x => true)).FirstOrDefault()?.pastList;
                    if (pastList == null)
                    {
                        await StaticBase.Database.GetCollection<UserEvent>("Voters").InsertOneAsync(new UserEvent() { pastList = voterList });
                        pastList = voterList;
                    }

                }

                var test = pastList;

                if (voterList.Count >= 999)
                {
                    int startIndex = pastList.FindIndex(x => x == voterList[0]);

                    for (int i = startIndex; i < voterList.Count; i++)
                    {
                        if (pastList.Count < i || pastList[i] != voterList[i])
                            newVoters.Add(await StaticBase.DiscordBotList.GetUserAsync(voterList[i]));
                    }
                }
                else
                {
                    for (int i = pastList.Count; i < voterList.Count; i++)
                    {
                        newVoters.Add(await StaticBase.DiscordBotList.GetUserAsync(voterList[i]));
                    }
                }

                pastList = voterList;
                await StaticBase.Database.GetCollection<UserEvent>("Voters").ReplaceOneAsync(x => true, this);

                if (UserVoted != null)
                    foreach (var user in newVoters)
                        await UserVoted.Invoke(user);

                await Task.Delay(60000);
            }
        }
    }
}
