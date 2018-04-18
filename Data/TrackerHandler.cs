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

namespace MopsBot.Data
{

    /// <summary>
    /// A class containing all Trackers
    /// </summary>
    public class TrackerHandler<T> where T : Tracker.ITracker
    {
        private Dictionary<String, T> trackers;
        public TrackerHandler(int sleepTime=0)
        {
            trackers = new Dictionary<string, T>();
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//{typeof(T).Name}.txt", FileMode.OpenOrCreate)))
            {
                string s = "";
                while ((s = read.ReadLine()) != null)
                {
                    try
                    {
                        var trackerInformation = s.Split('|');
                        trackers.Add(trackerInformation[0], (T)Activator.CreateInstance(typeof(T), new object[] { trackerInformation }));
                        trackers[trackerInformation[0]].OnMajorEventFired += OnMajorEvent;
                        trackers[trackerInformation[0]].OnMinorEventFired += OnMinorEvent;
                        System.Threading.Thread.Sleep(sleepTime);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        public void writeList()
        {
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//{typeof(T).Name}.txt", FileMode.Create)))
                foreach (T tr in trackers.Values)
                {
                    write.WriteLine(string.Join("|", tr.GetInitArray()));
                }
        }

        public void removeTracker(string name, ulong channelID){
            if(trackers.ContainsKey(name) && trackers[name].ChannelIds.Contains(channelID)){
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
                
                writeList();
            }
        }

        public void addTracker(string name, ulong channelID, string notification=""){
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

            writeList();
        }

        public string getTracker(ulong channelID){
            return string.Join(", ", trackers.Where(x => x.Value.ChannelIds.Contains(channelID)).Select(x => x.Key));
        }

        public Type getTrackerType(){
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
        private async Task OnMajorEvent(ulong channelID, EmbedBuilder embed, Tracker.ITracker parent, string notification)
        {
            if(parent is Tracker.TwitchTracker){
                Tracker.TwitchTracker parentHandle = parent as Tracker.TwitchTracker;

                if(parentHandle.ToUpdate.ContainsKey(channelID))
                    await parentHandle.ToUpdate[channelID].ModifyAsync(x => {
                        x.Content = notification;
                        x.Embed = (Embed)embed;
                    });

                else{
                    parentHandle.ToUpdate.Add(channelID, await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, embed:embed));
                    writeList();
                }
            }
            else
                await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, embed:embed);
        }
    }
}
