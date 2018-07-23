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
        public abstract bool TrySetNotification(string name, ulong channelID, string notificationMessage);
        public abstract void AddTracker(string name, ulong channelID, string notification = "");
        public abstract HashSet<Tracker.ITracker> GetTrackerSet();
        public abstract Dictionary<string, Tracker.ITracker> GetTrackers();
        public abstract IEnumerable<ITracker> GetTrackers(ulong channelID);
        public abstract ITracker GetTracker(ulong channelID, string name);
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
            //         Console.WriteLine("\n" +  e.Message + e.StackTrace);
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
                    Console.WriteLine("\n" +  e.Message + e.StackTrace);
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

        public override bool TryRemoveTracker(string name, ulong channelId)
        {
            if (trackers.ContainsKey(name) && trackers[name].ChannelIds.Contains(channelId))
            {
                if (trackers.First().Value.GetType() == typeof(Tracker.TwitchTracker))
                    foreach (var channel in (trackers[name] as Tracker.TwitchTracker).ToUpdate.Where(x => x.Key.Equals(channelId)))
                        try
                        {
                            Program.ReactionHandler.ClearHandler((IUserMessage)((ITextChannel)Program.Client.GetChannel(channelId)).GetMessageAsync(channel.Value).Result).Wait();
                        }
                        catch
                        {
                        }

                if (trackers[name].ChannelIds.Count > 1)
                {
                    trackers[name].ChannelMessages.Remove(channelId);
                    trackers[name].ChannelIds.Remove(channelId);

                    if (trackers.First().Value.GetType() == typeof(Tracker.TwitchTracker))
                    {
                        (trackers[name] as Tracker.TwitchTracker).ToUpdate.Remove(channelId);
                    }

                    Console.WriteLine("\n" + $"{DateTime.Now} Removed a {trackers.First().Value.GetType().Name} for {name}\nChannel: {channelId}");
                }

                else
                {
                    trackers[name].Dispose();
                    trackers.Remove(name);
                    Console.WriteLine("\n" + $"{DateTime.Now} Removed a {trackers.First().Value.GetType().Name} for {name}\nChannel: {channelId}; Last channel left.");
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

            trackers[name].ChannelMessages.Add(channelID, notification);
            Console.WriteLine("\n" + $"{DateTime.Now} Started a new {typeof(T).Name} for {name}\nChannels: {string.Join(",", trackers[name].ChannelIds)}\nMessage: {notification}");

            SaveJson();
        }

        public override bool TrySetNotification(string name, ulong channelID, string notificationMessage)
        {
            var tracker = GetTracker(channelID, name);

            if (tracker != null)
            {
                tracker.ChannelMessages[channelID] = notificationMessage;
                SaveJson();
                return true;
            }

            return false;
        }

        public override IEnumerable<ITracker> GetTrackers(ulong channelID)
        {
            return trackers.Select(x => x.Value).Where(x => x.ChannelIds.Contains(channelID));
        }

        public override Dictionary<string, ITracker> GetTrackers()
        {
            return trackers.Select(x => new KeyValuePair<string, ITracker>(x.Key, (ITracker)x.Value)).ToDictionary(x => x.Key, x => x.Value);
        }

        public override ITracker GetTracker(ulong channelID, string name)
        {
            return trackers.FirstOrDefault(x => x.Key.Equals(name) && x.Value.ChannelIds.Contains(channelID)).Value;
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
        private async Task OnMinorEvent(ulong channelID, Tracker.ITracker sender, string notification)
        {
            if (!Program.Client.ConnectionState.Equals(Discord.ConnectionState.Connected))
                return;
            try
            {
                await ((Discord.WebSocket.SocketTextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync(notification);
            }
            catch
            {
                if (Program.Client.GetChannel(channelID) == null || (await ((IGuildChannel)Program.Client.GetChannel(channelID)).Guild.GetCurrentUserAsync()) == null)
                {
                    TryRemoveTracker(sender.Name, channelID);
                    Console.WriteLine("\n" + $"Removed Tracker: {sender.Name} Channel {channelID} is missing");
                }
                else
                {
                    var permission = (await ((IGuildChannel)Program.Client.GetChannel(channelID)).Guild.GetCurrentUserAsync()).GetPermissions(((IGuildChannel)Program.Client.GetChannel(channelID)));
                    if (!permission.SendMessages || !permission.ViewChannel || !permission.ReadMessageHistory)
                    {
                        TryRemoveTracker(sender.Name, channelID);
                        Console.WriteLine("\n" + $"Removed a tracker for {sender.Name} from Channel {channelID} due to missing Permissions");
                        if (permission.SendMessages)
                        {
                            await ((ITextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync($"Removed tracker for `{sender.Name}` due to missing Permissions");
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
        private async Task OnMajorEvent(ulong channelID, Embed embed, Tracker.ITracker sender, string notification)
        {
            if (!Program.Client.ConnectionState.Equals(Discord.ConnectionState.Connected))
                return;
            try
            {
                if (sender is Tracker.TwitchTracker)
                {
                    Tracker.TwitchTracker tracker = sender as Tracker.TwitchTracker;
                    if (tracker.ToUpdate.ContainsKey(channelID))
                    {
                        var message = ((IUserMessage)((ITextChannel)Program.Client.GetChannel(channelID)).GetMessageAsync(tracker.ToUpdate[channelID]).Result);
                        if (message != null)
                            await message.ModifyAsync(x =>
                            {
                                x.Content = notification;
                                x.Embed = embed;
                            });
                        else
                        {
                            var newMessage = await ((Discord.WebSocket.SocketTextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync(notification, embed: embed);
                            tracker.ToUpdate[channelID] = newMessage.Id;
                            await tracker.setReaction((IUserMessage)message);
                            SaveJson();
                        }
                    }
                    else
                    {
                        var message = await ((Discord.WebSocket.SocketTextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync(notification, embed: embed);
                        tracker.ToUpdate.Add(channelID, message.Id);
                        await tracker.setReaction((IUserMessage)message);
                        SaveJson();
                    }
                }
                else
                    await ((Discord.WebSocket.SocketTextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync(notification, embed: embed);
            }
            catch
            {
                //Check if channel still exists, or existing only in cache
                if (Program.Client.GetChannel(channelID) == null || (await ((IGuildChannel)Program.Client.GetChannel(channelID)).Guild.GetCurrentUserAsync()) == null)
                {
                    TryRemoveTracker(sender.Name, channelID);
                    Console.WriteLine("\n" + $"Removed Tracker: {sender.Name} Channel {channelID} is missing");
                }
                //Check if permissions were modified, to an extend of making the tracker unusable
                else
                {
                    var permission = (await ((IGuildChannel)Program.Client.GetChannel(channelID)).Guild.GetCurrentUserAsync()).GetPermissions(((IGuildChannel)Program.Client.GetChannel(channelID)));
                    if (!permission.SendMessages || !permission.ViewChannel || !permission.ReadMessageHistory || (sender is Tracker.TwitchTracker && (!permission.AddReactions || !permission.ManageMessages)))
                    {
                        TryRemoveTracker(sender.Name, channelID);
                        Console.WriteLine("\n" + $"Removed a tracker for {sender.Name} from Channel {channelID} due to missing Permissions");
                        if (permission.SendMessages)
                        {
                            await ((ITextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync($"Removed tracker for `{sender.Name}` due to missing Permissions");
                        }
                    }
                }
            }
        }
    }
}
