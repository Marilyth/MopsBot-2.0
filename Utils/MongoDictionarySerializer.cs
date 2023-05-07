using System.Collections.Generic;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace MopsBot.Utils{
    /// <summary>
    /// A dictionary serializer for MongoDB which uses TryAdd instead of Add.
    /// For now this only supports dictionaries which are represented as documents.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class MongoDictionarySerializer<K, V> : DictionarySerializerBase<Dictionary<K, V>>{
        /// <inheritdoc />
        protected override Dictionary<K, V> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;
            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Document:
                    return DocumentToDictionary(context);
                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <summary>
        /// Converts a bson document to a dictionary using TryAdd instead of Add.
        /// </summary>
        /// <param name="context">The BsonDeserializationContext.</param>
        /// <returns>The the dictionary.</returns>
        private Dictionary<K, V> DocumentToDictionary(BsonDeserializationContext context){
            var dictionary = CreateInstance();
            var bsonReader = context.Reader;
            bsonReader.ReadStartDocument();

            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var keyString = bsonReader.ReadName();

                // This might not work for types other than string, actually. I don't know how their names are represented in the bson.
                var key = Newtonsoft.Json.JsonConvert.DeserializeObject<K>($"\"{keyString}\"");
                var value = BsonSerializer.Deserialize<V>(bsonReader);

                dictionary.TryAdd(key, value);
            }

            bsonReader.ReadEndDocument();
            return dictionary;
        }

        /// <inheritdoc />
        protected override Dictionary<K, V> CreateInstance(){
            return new Dictionary<K, V>();
        }
    }
}