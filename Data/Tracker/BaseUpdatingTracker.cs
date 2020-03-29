using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults;
using OxyPlot;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;

namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public abstract class BaseUpdatingTracker : BaseTracker
    {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, ulong> ToUpdate;

        public BaseUpdatingTracker() : base(){
            ToUpdate = new Dictionary<ulong, ulong>();
        }

        public async virtual Task setReaction(IUserMessage message){}

        public override void Dispose()
        {
            ChannelConfig.Clear();
            ToUpdate.Clear();
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
