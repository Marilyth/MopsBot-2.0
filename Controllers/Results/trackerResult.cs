using System;
using System.Collections.Generic;

namespace MopsBot.Api.Controllers.Results.TrackerResult
{
    public class Channel
    {
        public ulong id { get; set; }
        
        public Channel(ulong pId){
            id = pId;
        }
    }

    public class Id
    {
        public string id { get; set; }
        public List<Channel> channels { get; set; }

        public Id(string pId){
            id = pId;
            channels = new List<Channel>();
        }
    }

    public class Type
    {
        public string type { get; set; }
        public List<Id> ids { get; set; }

        public Type(string pType){
            type = pType;
            ids = new List<Id>();
        }

    }

    public class RootObject
    {
        public List<Type> types { get; set; }
        public RootObject(){
            types = new List<Type>();
        }
    }
}