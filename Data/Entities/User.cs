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
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        public ulong Id;
        public int Money, Experience, Punched, Hugged, Kissed;
        public int WeaponId;
        public List<int> Inventory;

        public User(ulong pId)
        {
            Id = pId;
        }

        public int CalcExperience(int level)
        {
            return 200*level*level;
        }

        public int CalcCurLevel(){
            return (int)Math.Sqrt(Experience/200.0);
        }

        public async Task ModifyAsync(Action<User> modification){
            await StaticBase.Users.ModifyUserAsync(Id, modification);
        }

        private string DrawProgressBar()
        {
            int Level = CalcCurLevel();
            double expCurrentHold = Experience - CalcExperience(Level);
            string output = "", TempOutput = "";
            double diffExperience = CalcExperience(Level + 1) - CalcExperience(Level);
            for (int i = 0; i < Math.Floor(expCurrentHold / (diffExperience / 10)); i++)
            {
                output += "■";
            }
            for (int i = 0; i < 10 - output.Length; i++)
            {
                TempOutput += "□";
            }
            return output + TempOutput;
        }

        public Embed StatEmbed()
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithAuthor(Program.Client.GetUser(Id).Username, Program.Client.GetUser(Id).GetAvatarUrl());
            e.WithCurrentTimestamp().WithColor(Discord.Color.Blue);

            e.AddField("Level", $"{CalcCurLevel()} ({Experience}/{CalcExperience(CalcCurLevel() + 1)}xp)\n{DrawProgressBar()}", true);
            e.AddField("Interactions", $"**Kissed** {Kissed} times\n**Hugged** {Hugged} times\n**Punched** {Punched} times", true);
            e.AddField("Money", Money, false);

            return e.Build();
        }
    }
}
