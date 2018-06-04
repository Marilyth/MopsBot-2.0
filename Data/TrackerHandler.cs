using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using MopsBot.Data.Tracker;

namespace MopsBot.Data
{
    public abstract class TrackerWrapper {
        public abstract void SaveJson();
        public abstract void removeTracker(string name, ulong channelID);
        public abstract void addTracker(string name, ulong channelID, string notification="");
        public abstract Dictionary<string, Tracker.ITracker> getTracker();
        public abstract HashSet<Tracker.ITracker> getTrackerSet();
        public abstract string getTracker(ulong channelID);
        public abstract Type getTrackerType();

    }

    /// <summary>
    /// A class containing all Trackers
    /// </summary>
    public class TrackerHandler<T> : TrackerWrapper where T : Tracker.ITracker
    {
        public Dictionary<string, T> trackers;
        public TrackerHandler()
        {
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//{typeof(T).Name}.json", FileMode.OpenOrCreate)))
            {
                try{
                    trackers = JsonConvert.DeserializeObject<Dictionary<string, T>>(read.ReadToEnd());
                } catch(Exception e){
                    Console.WriteLine(e.Message + e.StackTrace);
                }
            }
            trackers = (trackers == null ? new Dictionary<string, T>() : trackers);
            foreach(KeyValuePair<string, T> cur in trackers){
                cur.Value.PostInitialisation();
                cur.Value.OnMinorEventFired += OnMinorEvent;
                cur.Value.OnMajorEventFired += OnMajorEvent;
            }
        }

        public override void SaveJson()
        {
            string dictAsJson = JsonConvert.SerializeObject(trackers, Formatting.Indented);
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//{typeof(T).Name}.json", FileMode.Create)))
                write.Write(dictAsJson);
        }

        public override void removeTracker(string name, ulong channelID){
            if(trackers.ContainsKey(name) && trackers[name].ChannelIds.Contains(channelID)){
                if(trackers.First().Value.GetType() == typeof(Tracker.TwitchTracker))
                        foreach(var channel in (trackers[name] as Tracker.TwitchTracker).ToUpdate.Where(x => x.Key.Equals(channelID)))
                            Program.reactionHandler.clearHandler((IUserMessage)((ITextChannel)Program.client.GetChannel(channelID)).GetMessageAsync(channel.Value).Result).Wait();

                if(trackers[name].ChannelIds.Count > 1){
                    trackers[name].ChannelIds.Remove(channelID);
                    if(trackers.First().Value.GetType() == typeof(Tracker.TwitchTracker)){
                        (trackers[name] as Tracker.TwitchTracker).ChannelMessages.Remove(channelID);
                    }
                }

                else{
                    trackers[name].Dispose();
                    trackers.Remove(name);
                }
                
                SaveJson();
            }
        }

        public override void addTracker(string name, ulong channelID, string notification=""){
            if(trackers.ContainsKey(name)){
                if(!trackers[name].ChannelIds.Contains(channelID))
                    trackers[name].ChannelIds.Add(channelID);
            }
            else{
                trackers.Add(name, (T)Activator.CreateInstance(typeof(T), new object[] { name }));
                trackers[name].ChannelIds.Add(channelID);
                trackers[name].OnMajorEventFired += OnMajorEvent;
                trackers[name].OnMinorEventFired += OnMinorEvent;
            }
            if(trackers.First().Value.GetType() == typeof(Tracker.TwitchTracker)){
                (trackers[name] as Tracker.TwitchTracker).ChannelMessages.Add(channelID, notification);
            }
            else if(trackers.First().Value.GetType() == typeof(Tracker.TwitterTracker)){
                (trackers[name] as Tracker.TwitterTracker).SetNotification(channelID, notification.Split("|")[0], notification.Split("|")[1]);
            }

            SaveJson();
        }

        public override string getTracker(ulong channelID){
            return string.Join(", ", trackers.Where(x => x.Value.ChannelIds.Contains(channelID)).Select(x => x.Key));
        }
        public override Dictionary<string, ITracker> getTracker()
        {
            return trackers.Select(x=> new KeyValuePair<string, ITracker>(x.Key, (ITracker)x.Value)).ToDictionary(x=>x.Key, x=>x.Value);
        }

        public override HashSet<ITracker> getTrackerSet()
        {
            return trackers.Values.Select(x => (ITracker)x).ToHashSet();
        }

        public override Type getTrackerType(){
            return typeof(T);
        }


        /// <summary>
        /// Event that is called when the Tracker fetches new data containing no Embed
        /// </summary>
        /// <returns>A Task that can be awaited</returns>
        private async Task OnMinorEvent(ulong channelID, Tracker.ITracker parent, string notification)
        {
            await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification);
        }

        /// <summary>
        /// Event that is called when the Tracker fetches new data containing an Embed
        /// Updates or creates the notification message with it
        /// </summary>
        /// <returns>A Task that can be awaited</returns>
        private async Task OnMajorEvent(ulong channelID, Embed embed, Tracker.ITracker parent, string notification)
        {
            if(parent is Tracker.TwitchTracker){
                Tracker.TwitchTracker parentHandle = parent as Tracker.TwitchTracker;

                if(parentHandle.ToUpdate.ContainsKey(channelID))
                    await ((IUserMessage)((ITextChannel)Program.client.GetChannel(channelID)).GetMessageAsync(parentHandle.ToUpdate[channelID]).Result).ModifyAsync(x => {
                        x.Content = notification;
                        x.Embed = embed;
                    });

                else{
                    var message = await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, embed:embed);
                    parentHandle.ToUpdate.Add(channelID, message.Id);
                    await parentHandle.setReaction((IUserMessage)message);
                    SaveJson();
                }
            }
            else
                await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, embed:embed);
        }


    }
}
