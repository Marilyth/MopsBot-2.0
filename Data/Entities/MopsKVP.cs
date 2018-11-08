using System.Collections;
using System.Collections.Generic;
using MongoDB;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Linq;

namespace MopsBot.Data.Entities
{
    public class MongoKVP<T1, T2>{
        [BsonId]
        public T1 Key;
        public T2 Value;

        public static implicit operator KeyValuePair<T1, T2>(MongoKVP<T1, T2> mKVP){
            return new KeyValuePair<T1, T2>(mKVP.Key, mKVP.Value);
        }

        public MongoKVP(){}

        public MongoKVP(T1 key, T2 value){
            Key = key;
            Value = value;
        }

        public static MongoKVP<T1, T2> KVPToMongoKVP(KeyValuePair<T1, T2> kvp){
            return new MongoKVP<T1, T2>(){Key = kvp.Key, Value = kvp.Value};
        }

        public static List<MongoKVP<T1, T2>> DictToMongoKVP(Dictionary<T1, T2> dict){
            return dict.Select(x => new MongoKVP<T1, T2>(){Key = x.Key, Value = x.Value}).ToList();
        }
    }
}