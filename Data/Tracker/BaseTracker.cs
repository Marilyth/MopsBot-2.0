using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
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
using System.ServiceModel.Syndication;

namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public abstract class BaseTracker : MopsBot.Api.BaseAPIContent, IDisposable
    {
        //Avoid ratelimit by placing a gap between all trackers.
        public static int ExistingTrackers = 0;
        public enum TrackerType { Twitch, TwitchGroup, TwitchClip, Twitter, Osu, Overwatch, /*Tibia,*/ Youtube, YoutubeLive, Reddit, JSON, OSRS, HTML, RSS, Steam };
        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        protected System.Threading.Timer checkForChange;
        public event MainEventHandler OnMajorEventFired;
        public event MinorEventHandler OnMinorEventFired;
        public delegate Task MinorEventHandler(ulong channelID, BaseTracker self, string notificationText);
        public delegate Task MainEventHandler(ulong channelID, Embed embed, BaseTracker self, string notificationText = "");
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, Dictionary<string, object>> ChannelConfig;

        [BsonId]
        public string Name;

        public BaseTracker()
        {
            ExistingTrackers++;
            ChannelConfig = new Dictionary<ulong, Dictionary<string, object>>();
            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed);
        }

        public virtual void PostInitialisation(object info = null){}

        public virtual void Conversion(object info = null){}

        public virtual void PostChannelAdded(ulong channelId){
            ChannelConfig.Add(channelId, new Dictionary<string, object>());
            ChannelConfig[channelId]["Notification"] = "";
        }

        public void SetTimer(int interval = 600000, int delay = -1)
        {
            checkForChange.Change(delay == -1 ? StaticBase.ran.Next(5000, interval) : delay, interval);
        }

        public static async Task<T> FetchJSONDataAsync<T>(string url, params KeyValuePair<string, string>[] headers)
        {
            string query = await MopsBot.Module.Information.GetURLAsync(url, headers);

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<T>(query, _jsonWriter);
        }

        public async static Task<SyndicationFeed> FetchRSSData(string url, params KeyValuePair<string, string>[] headers)
        {
            try
            {
                var settings = new System.Xml.XmlReaderSettings();
                settings.DtdProcessing = System.Xml.DtdProcessing.Parse;
                using (var reader = System.Xml.XmlReader.Create(url, settings))
                {
                    
                    SyndicationFeed feed = SyndicationFeed.Load(reader);
                    return feed;
                }
            }
            catch (Exception e)
            {
                return await FetchRSSDataUTF8(url, headers);
            }
        }

        private async static Task<SyndicationFeed> FetchRSSDataUTF8(string url, params KeyValuePair<string, string>[] headers)
        {
            var content = await MopsBot.Module.Information.GetURLAsync(url, headers);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content?.Replace("utf8", "utf-8") ?? ""));
            var settings = new System.Xml.XmlReaderSettings();
            settings.DtdProcessing = System.Xml.DtdProcessing.Parse;
            using (var reader = System.Xml.XmlReader.Create(stream, settings))
            {
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                return feed;
            }
        }

        protected abstract void CheckForChange_Elapsed(object stateinfo);

        public virtual string TrackerUrl()
        {
            return null;
        }

        protected async Task OnMajorChangeTracked(ulong channelID, Embed embed, string notificationText = "")
        {
            if (OnMajorEventFired != null)
                await OnMajorEventFired(channelID, embed, this, notificationText);
        }
        protected async Task OnMinorChangeTracked(ulong channelID, string notificationText)
        {
            if (OnMinorEventFired != null)
                await OnMinorEventFired(channelID, this, notificationText);
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                checkForChange.Dispose();
            }

            disposed = true;
        }

        public override Dictionary<string, object> GetParameters(ulong guildId)
        {
            string[] channels = Program.Client.GetGuild(guildId).TextChannels.Select(x => $"#{x.Name}:{x.Id}").ToArray();

            return new Dictionary<string, object>(){
                {"Parameters", new Dictionary<string, object>(){
                                {"_Name", ""},
                                {"Notification", "New content!"},
                                {"Channel", channels}}},
                {"Permissions", GuildPermission.ManageChannels}
            };
        }

        public override object GetAsScope(ulong channelId)
        {
            return new ContentScope()
            {
                Id = this.Name,
                _Name = this.Name,
                Notification = (string)this.ChannelConfig[channelId]["Notification"],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId
            };
        }

        public override void Update(Dictionary<string, Dictionary<string, string>> args)
        {
            if (args["NewValue"].ContainsKey("Notification")) SetBaseValues(args["NewValue"]);

            var newChannelId = ulong.Parse(args["NewValue"]["Channel"].Split(":")[1]);
            var oldChannelId = ulong.Parse(args["OldValue"]["Channel"].Split(":")[1]);
            if (newChannelId != oldChannelId)
                ChannelConfig.Remove(oldChannelId);
        }

        public void SetBaseValues(Dictionary<string, string> args, bool setName = false)
        {
            try
            {
                if (setName) Name = args["_Name"];
                var newChannelId = ulong.Parse(args["Channel"].Split(":")[1]);
                ChannelConfig[newChannelId]["Notification"] = args["Notification"];
            }
            catch (Exception e)
            {
                Dispose();
                throw e;
            }
        }

        public new struct ContentScope
        {
            public string Id;
            public string _Name;
            public string Notification;
            public string Channel;
        }

        public class Config{
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
            public Dictionary<string, object> Values = new Dictionary<string, object>();

            public Config(){}

            public Config(params KeyValuePair<string, object>[] values){
                foreach(var value in values){
                    Values.Add(value.Key, value.Value);
                }
            }
        }
    }
}
