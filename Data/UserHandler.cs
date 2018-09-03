using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using MongoDB.Driver;
using MopsBot.Data.Entities;

namespace MopsBot.Data
{
    /// <summary>
    /// Class containing all Mops Users
    /// </summary>
    public class UserHandler
    {
        private readonly string COLLECTIONNAME = "Users";
        private Dictionary<ulong, User> Users = new Dictionary<ulong, User>();

        /// <summary>
        /// Reads data from text file, and fills Dictionary of Users with it
        /// </summary>
        public UserHandler()
        {
            var collection = StaticBase.Database.GetCollection<User>(COLLECTIONNAME).FindSync<User>(x => true).ToList();
            Users = collection.ToDictionary(x => x.Id);
        }

        public User GetUser(ulong Id){
            if(!Users.ContainsKey(Id)){
                Users[Id] = new User(Id);
                AddToDBAsync(Id).Wait();
            }

            return Users[Id];
        }

        private async Task UpdateDBAsync(ulong Id){
            await StaticBase.Database.GetCollection<User>(COLLECTIONNAME).ReplaceOneAsync(x => x.Id == Id, Users[Id]);
        }

        private async Task AddToDBAsync(ulong Id){
            await StaticBase.Database.GetCollection<User>(COLLECTIONNAME).InsertOneAsync(Users[Id]);
        }

        public async Task ModifyStatAsync(ulong Id, Action<User> action){
            if(Users.ContainsKey(Id)){
                action(Users[Id]);
                await UpdateDBAsync(Id);
            }
            else{
                Users[Id] = new User(Id);
                action(Users[Id]);
                await AddToDBAsync(Id);
            }
        }
    }
}