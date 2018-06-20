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
    public abstract class TrackerWrapper
    {
        public abstract void SaveJson();
        public abstract bool TryRemoveTracker(string name, ulong channelID);
        public abstract void AddTracker(string name, ulong channelID, string notification = "");
        public abstract Dictionary<string, Tracker.ITracker> GetTracker();
        public abstract HashSet<Tracker.ITracker> GetTrackerSet();
        public abstract string GetTracker(ulong channelID);
        public abstract Type GetTrackerType();
        public abstract void postInitialisation();

    }

    /// <summary>
    /// A class containing all Trackers
    /// </summary>
    public class TrackerHandler<T> : TrackerWrapper where T : Tracker.ITracker
    {
        public Dictionary<string, T> trackers;
        public TrackerHandler()
        {
            // using (StreamReader read = new StreamReader(new FileStream($"mopsdata//{typeof(T).Name}.json", FileMode.OpenOrCreate)))
            // {
            //     try
            //     {
            //         trackers = JsonConvert.DeserializeObject<Dictionary<string, T>>(read.ReadToEnd());
            //     }
            //     catch (Exception e)
            //     {
            //         Console.WriteLine(e.Message + e.StackTrace);
            //     }
            // }
            // trackers = (trackers == null ? new Dictionary<string, T>() : trackers);
            // foreach(KeyValuePair<string, T> cur in trackers){
            //     cur.Value.PostInitialisation();
            //     cur.Value.OnMinorEventFired += OnMinorEvent;
            //     cur.Value.OnMajorEventFired += OnMajorEvent;
            // }
        }

        public override void postInitialisation()
        {
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//{typeof(T).Name}.json", FileMode.OpenOrCreate)))
            {
                try
                {
                    trackers = JsonConvert.DeserializeObject<Dictionary<string, T>>(read.ReadToEnd());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + e.StackTrace);
                }
            }
            trackers = (trackers == null ? new Dictionary<string, T>() : trackers);

            for (int i = trackers.Count - 1; i >= 0; i--)
            {
                var cur = trackers[trackers.Keys.ElementAt(i)];
                cur.PostInitialisation();
                cur.OnMinorEventFired += OnMinorEvent;
                cur.OnMajorEventFired += OnMajorEvent;
            }
            // foreach(KeyValuePair<string, T> cur in trackers){
            //     cur.Value.PostInitialisation();
            //     cur.Value.OnMinorEventFired += OnMinorEvent;
            //     cur.Value.OnMajorEventFired += OnMajorEvent;
            // }
        }

        public override void SaveJson()
        {
            string dictAsJson = JsonConvert.SerializeObject(trackers, Formatting.Indented);
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//{typeof(T).Name}.json", FileMode.Create)))
                write.Write(dictAsJson);
        }

        public override bool TryRemoveTracker(string name, ulong channelID)
        {
            if (trackers.ContainsKey(name) && trackers[name].ChannelIds.Contains(channelID))
            {
                if (trackers.First().Value.GetType() == typeof(Tracker.TwitchTracker))
                    foreach (var channel in (trackers[name] as Tracker.TwitchTracker).ToUpdate.Where(x => x.Key.Equals(channelID)))
                        try
                        {
                            Program.reactionHandler.clearHandler((IUserMessage)((ITextChannel)Program.client.GetChannel(channelID)).GetMessageAsync(channel.Value).Result).Wait();
                        }
                        catch
                        {
                        }

                if (trackers[name].ChannelIds.Count > 1)
                {
                    trackers[name].ChannelIds.Remove(channelID);
                    if (trackers.First().Value.GetType() == typeof(Tracker.TwitchTracker))
                    {
                        (trackers[name] as Tracker.TwitchTracker).ChannelMessages.Remove(channelID);
                    }
                }

                else
                {
                    trackers[name].Dispose();
                    trackers.Remove(name);
                }

                SaveJson();
                return true;
            }
            return false;
        }

        public override void AddTracker(string name, ulong channelID, string notification = "")
        {
            if (trackers.ContainsKey(name))
            {
                if (!trackers[name].ChannelIds.Contains(channelID))
                    trackers[name].ChannelIds.Add(channelID);
            }
            else
            {
                trackers.Add(name, (T)Activator.CreateInstance(typeof(T), new object[] { name }));
                trackers[name].ChannelIds.Add(channelID);
                trackers[name].OnMajorEventFired += OnMajorEvent;
                trackers[name].OnMinorEventFired += OnMinorEvent;
            }
            if (trackers.First().Value.GetType() == typeof(Tracker.TwitchTracker))
            {
                (trackers[name] as Tracker.TwitchTracker).ChannelMessages.Add(channelID, notification);
            }
            else if (trackers.First().Value.GetType() == typeof(Tracker.TwitterTracker))
            {
                (trackers[name] as Tracker.TwitterTracker).SetNotification(channelID, notification.Split("|")[0], notification.Split("|")[1]);
            }

            SaveJson();
        }

        public override string GetTracker(ulong channelID)
        {
            return string.Join(", ", trackers.Where(x => x.Value.ChannelIds.Contains(channelID)).Select(x => x.Key));
        }
        public override Dictionary<string, ITracker> GetTracker()
        {
            return trackers.Select(x => new KeyValuePair<string, ITracker>(x.Key, (ITracker)x.Value)).ToDictionary(x => x.Key, x => x.Value);
        }

        public override HashSet<ITracker> GetTrackerSet()
        {
            return trackers.Values.Select(x => (ITracker)x).ToHashSet();
        }

        public override Type GetTrackerType()
        {
            return typeof(T);
        }


        /// <summary>
        /// Event that is called when the Tracker fetches new data containing no Embed
        /// </summary>
        /// <returns>A Task that can be awaited</returns>
        private async Task OnMinorEvent(ulong channelID, Tracker.ITracker parent, string notification)
        {
            try
            {
                await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification);
            }
            catch
            {
                if (Program.client.GetChannel(channelID) == null){
                    TryRemoveTracker(parent.Name, channelID);
                    Console.Out.WriteLine($"Removed Tracker: {parent.Name} Channel {channelID} is missing");
                }
                else{
                    var permission = (await ((IGuildChannel)Program.client.GetChannel(channelID)).Guild.GetCurrentUserAsync()).GetPermissions( ((IGuildChannel)Program.client.GetChannel(channelID)));
                    if(!permission.SendMessages || !permission.ViewChannel || !permission.ReadMessageHistory){
                        TryRemoveTracker(parent.Name, channelID);
                        Console.Out.WriteLine($"Removed a tracker for {parent.Name} due to missing Permissions");
                        if(permission.SendMessages){
                            await ((ITextChannel)Program.client.GetChannel(channelID)).SendMessageAsync($"Removed tracker for `{parent.Name}` due to missing Permissions");
                        }
                    }           
                }

            }
        }

        /// <summary>
        /// Event that is called when the Tracker fetches new data containing an Embed
        /// Updates or creates the notification message with it
        /// </summary>
        /// <returns>A Task that can be awaited</returns>
        private async Task OnMajorEvent(ulong channelID, Embed embed, Tracker.ITracker parent, string notification)
        {
            try
            {
                if (parent is Tracker.TwitchTracker)
                {
                    Tracker.TwitchTracker parentHandle = parent as Tracker.TwitchTracker;
                    if (parentHandle.ToUpdate.ContainsKey(channelID))
                    {
                        var message = ((IUserMessage)((ITextChannel)Program.client.GetChannel(channelID)).GetMessageAsync(parentHandle.ToUpdate[channelID]).Result);
                        if(message !=null)
                            await message.ModifyAsync(x =>
                            {
                                x.Content = notification;
                                x.Embed = embed;
                            });
                        else{
                            var newMessage = await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, embed: embed);
                            parentHandle.ToUpdate[channelID]=newMessage.Id;
                            await parentHandle.setReaction((IUserMessage)message);
                            SaveJson();
                        }
                    }
                    else
                    {
                        var message = await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, embed: embed);
                        parentHandle.ToUpdate.Add(channelID, message.Id);
                        await parentHandle.setReaction((IUserMessage)message);
                        SaveJson();
                    }
                }
                else
                    await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, embed: embed);
            }
            catch
            {
                if (Program.client.GetChannel(channelID) == null){
                    TryRemoveTracker(parent.Name, channelID);
                    Console.Out.WriteLine($"Removed Tracker: {parent.Name} Channel {channelID} is missing");
                }
                else{
                    var permission = (await ((IGuildChannel)Program.client.GetChannel(channelID)).Guild.GetCurrentUserAsync()).GetPermissions( ((IGuildChannel)Program.client.GetChannel(channelID)));
                    if(!permission.SendMessages || !permission.ViewChannel || !permission.ReadMessageHistory || (parent is Tracker.TwitchTracker && (!permission.AddReactions || !permission.ManageMessages))){
                        TryRemoveTracker(parent.Name, channelID);
                        Console.Out.WriteLine($"Removed a tracker for {parent.Name} due to missing Permissions");
                        if(permission.SendMessages){
                            await ((ITextChannel)Program.client.GetChannel(channelID)).SendMessageAsync($"Removed tracker for `{parent.Name}` due to missing Permissions");
                        }
                    }           
                }
            }
        }


    }
}
