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
        public enum TrackerType { Twitch, TwitchClip, Twitter, Osu, Overwatch, Youtube, YoutubeLive, Reddit, News, OSRS, HTML, RSS};
        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        protected System.Threading.Timer checkForChange;
        public event MainEventHandler OnMajorEventFired;
        public event MinorEventHandler OnMinorEventFired;
        public delegate Task MinorEventHandler(ulong channelID, BaseTracker self, string notificationText);
        public delegate Task MainEventHandler(ulong channelID, Embed embed, BaseTracker self, string notificationText = "");
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, string> ChannelMessages;

        [BsonId]
        public string Name;

        public BaseTracker(int interval, int gap = 5000)
        {
            ExistingTrackers++;
            ChannelMessages = new Dictionary<ulong, string>();
            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false),
                                                                                (gap % interval) + 5000, interval);
        }

        public virtual void PostInitialisation()
        {
        }

        public static async Task<T> FetchJSONDataAsync<T>(string url, params KeyValuePair<string, string>[] headers)
        {
            string query = await MopsBot.Module.Information.ReadURLAsync(url, headers);

            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<T>(query, _jsonWriter);
        }

        public static SyndicationFeed FetchRSSData(string url, params KeyValuePair<string, string>[] headers)
        {
            using(var reader = System.Xml.XmlReader.Create(url)){
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
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
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId
            };
        }

        public override void Update(Dictionary<string, Dictionary<string, string>> args)
        {
            if (args["NewValue"].ContainsKey("Notification")) SetBaseValues(args["NewValue"]);

            var newChannelId = ulong.Parse(args["NewValue"]["Channel"].Split(":")[1]);
            var oldChannelId = ulong.Parse(args["OldValue"]["Channel"].Split(":")[1]);
            if (newChannelId != oldChannelId)
                ChannelMessages.Remove(oldChannelId);
        }

        public void SetBaseValues(Dictionary<string, string> args, bool setName = false)
        {
            try
            {
                if (setName) Name = args["_Name"];
                var newChannelId = ulong.Parse(args["Channel"].Split(":")[1]);
                ChannelMessages[newChannelId] = args["Notification"];
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
    }
}
