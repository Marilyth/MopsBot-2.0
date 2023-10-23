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
using MongoDB.Driver;
using MopsBot.Data.Entities;
using Microsoft.Win32;

namespace MopsBot.Data
{
    public abstract class TrackerWrapper
    {
        public abstract void PauseLoops(int minutes = 10);
        public abstract Task UpdateDBAsync(BaseTracker tracker);
        public abstract Task RemoveFromDBAsync(BaseTracker tracker);
        public abstract Task<bool> TryRemoveTrackerAsync(string name, ulong channelID);
        public abstract Task<bool> TrySetNotificationAsync(string name, ulong channelID, string notificationMessage);
        public abstract Task<BaseTracker> AddTrackerAsync(string name, ulong channelID, string notification = "");
        public abstract Task<Embed> GetEmbed();
        public abstract HashSet<Tracker.BaseTracker> GetTrackerSet();
        public abstract Dictionary<string, Tracker.BaseTracker> GetTrackers();
        public abstract IEnumerable<BaseTracker> GetTrackers(ulong channelID);
        public abstract IEnumerable<BaseTracker> GetGuildTrackers(ulong guildId);
        public abstract IEnumerable<Embed> GetTrackersEmbed(ulong channelID, bool searchServer = false, string name = null);
        public abstract BaseTracker GetTracker(ulong channelID, string name);
        public abstract Type GetTrackerType();
        public abstract void PostInitialisation();
    }

    /// <summary>
    /// A class containing all Trackers
    /// </summary>
    public class TrackerHandler<T> : TrackerWrapper where T : Tracker.BaseTracker
    {
        public Dictionary<string, T> trackers = new Dictionary<string, T>();
        public DatePlot IncreaseGraph;
        private int trackerInterval, updateInterval;
        private System.Threading.Timer nextTracker, nextUpdate;
        public TrackerHandler(int trackerInterval = 900000, int updateInterval = 120000)
        {
            this.trackerInterval = trackerInterval;
            this.updateInterval = updateInterval;
        }

        public override void PostInitialisation()
        {
            var collection = StaticBase.Database.GetCollection<T>(typeof(T).Name).FindSync<T>(x => true).ToList();
            IncreaseGraph = StaticBase.Database.GetCollection<DatePlot>("TrackerHandler").FindSync<DatePlot>(x => x.ID.Equals(typeof(T).Name + "Handler")).FirstOrDefault();
            IncreaseGraph?.InitPlot("Date", "Tracker Increase", "dd-MMM", false);

            trackers = collection.ToDictionary(x => x.Name);

            trackers = (trackers == null ? new Dictionary<string, T>() : trackers);

            if (collection.Count > 0)
            {
                int gap = trackerInterval / collection.Count;

                for (int i = trackers.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var cur = trackers[trackers.Keys.ElementAt(i)];
                        //cur.SetTimer(trackerInterval, gap * (i + 1) + 20000);
                        bool save = cur.ChannelConfig.Count == 0;
                        cur.Conversion(trackers.Count - i);
                        cur.PostInitialisation(trackers.Count - i);
                        if (save) UpdateDBAsync(cur).Wait();
                        cur.OnMinorEventFired += OnMinorEvent;
                        cur.OnMajorEventFired += OnMajorEvent;
                    }
                    catch (Exception e)
                    {
                        Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"Error on PostInitialisation, {e.Message}", e));
                    }
                }
            }

            nextTracker = new System.Threading.Timer(LoopTrackers);
            loopQueue = trackers.Values.ToList();
            if (typeof(T).IsSubclassOf(typeof(BaseUpdatingTracker)))
            {
                nextUpdate = new System.Threading.Timer(LoopTrackersUpdate);
                loopQueue = trackers.Where(x => (x.Value as BaseUpdatingTracker).ToUpdate.Count == 0).Select(x => x.Value).ToList();
                updateQueue = trackers.Where(x => (x.Value as BaseUpdatingTracker).ToUpdate.Count > 0).Select(x => x.Value).ToList();
            }

            PauseLoops(1);
        }

        public override void PauseLoops(int minutes){
            nextTracker.Change(minutes * 60 * 1000, trackerInterval / (loopQueue.Count > 0 ? loopQueue.Count : 1));
            
            if(nextUpdate != null)
                nextUpdate.Change(minutes * 60 * 1000, updateInterval / (updateQueue.Count > 0 ? updateQueue.Count : 1));
        }

        private int trackerTurn;
        private List<T> loopQueue;
        public async void LoopTrackers(object state)
        {
            if (trackerTurn < loopQueue.Count)
            {
                BaseTracker curTracker = null;
                try
                {
                    curTracker = loopQueue[trackerTurn];
                    trackerTurn++;
                    curTracker.CheckForChange_Elapsed(null);
                }
                catch (Exception e)
                {
                    await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"Error on checking for change for {curTracker?.Name ?? "Unknown"}", e));
                }
            }

            else
            {
                if (typeof(T).IsSubclassOf(typeof(BaseUpdatingTracker)))
                {
                    loopQueue = trackers.Where(x => (x.Value as BaseUpdatingTracker).ToUpdate.Count == 0).Select(x => x.Value).ToList();
                }
                else
                {
                    loopQueue = trackers.Values.ToList();
                }
                var gap = trackerInterval / (loopQueue.Count > 0 ? loopQueue.Count : 1);
                nextTracker.Change(loopQueue.Count > 0 ? 0 : gap, gap);
                trackerTurn = 0;
            }
        }

        private int updateTurn;
        private List<T> updateQueue;
        public async void LoopTrackersUpdate(object state)
        {
            if (updateTurn < updateQueue.Count)
            {
                BaseTracker curTracker = null;
                try
                {
                    curTracker = updateQueue[updateTurn];
                    updateTurn++;
                    curTracker.CheckForChange_Elapsed(null);
                }
                catch (Exception e)
                {
                    await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"Error on checking for change for {curTracker?.Name ?? "Unknown"}", e));
                }
            }

            else{
                updateQueue = trackers.Where(x => (x.Value as BaseUpdatingTracker).ToUpdate.Count > 0).Select(x => x.Value).ToList();
                var gap = updateInterval / (updateQueue.Count > 0 ? updateQueue.Count : 1);
                nextUpdate.Change(updateQueue.Count > 0 ? 0 : gap, gap);
                updateTurn = 0;
            }
        }

        public override async Task UpdateDBAsync(BaseTracker tracker)
        {
            lock (tracker.ChannelConfig)
            {
                try
                {
                    StaticBase.Database.GetCollection<BaseTracker>(typeof(T).Name).ReplaceOneAsync(x => x.Name.Equals(tracker.Name), tracker, new UpdateOptions { IsUpsert = true }).Wait();
                }
                catch (Exception e)
                {
                    Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"Error on upsert for {tracker.Name}, {e.Message}", e)).Wait();
                }
            }
        }

        public override async Task RemoveFromDBAsync(BaseTracker tracker)
        {
            lock (tracker.ChannelConfig)
            {
                try
                {
                    StaticBase.Database.GetCollection<T>(typeof(T).Name).DeleteOneAsync(x => x.Name.Equals(tracker.Name)).Wait();
                }
                catch (Exception e)
                {
                    Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"Error on removing for {tracker.Name}, {e.Message}", e)).Wait();
                }
            }
        }

        public override async Task<bool> TryRemoveTrackerAsync(string name, ulong channelId)
        {
            if (trackers.ContainsKey(name) && trackers[name].ChannelConfig.ContainsKey(channelId))
            {
                if (trackers[name].ChannelConfig.Keys.Count > 1)
                {
                    trackers[name].ChannelConfig.Remove(channelId);

                    if (typeof(T).IsSubclassOf(typeof(BaseUpdatingTracker)))
                    {
                        (trackers[name] as BaseUpdatingTracker).ToUpdate.Remove(channelId);
                    }

                    await UpdateDBAsync(trackers[name]);
                    await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"Removed a {typeof(T).FullName} for {name}\nChannel: {channelId}"));
                }

                else
                {
                    var worked = loopQueue.Remove(trackers[name]);
                    var uWorked = updateQueue?.Remove(trackers[name]) ?? false;
                    trackers[name].Dispose();
                    await RemoveFromDBAsync(trackers[name]);
                    trackers.Remove(name);
                    await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"Removed a {typeof(T).FullName} for {name}\nChannel: {channelId}; Last channel left."));
                    await updateGraph(-1);
                }

                return true;
            }
            return false;
        }

        public override async Task<BaseTracker> AddTrackerAsync(string name, ulong channelID, string notification = "")
        {
            if (trackers.ContainsKey(name))
            {
                if (!trackers[name].ChannelConfig.ContainsKey(channelID))
                {
                    trackers[name].PostChannelAdded(channelID);
                    trackers[name].ChannelConfig[channelID]["Notification"] = notification;
                    await UpdateDBAsync(trackers[name]);
                }
            }
            else
            {
                var tracker = (T)Activator.CreateInstance(typeof(T), new object[] { name });
                name = tracker.Name;

                // The name might have changed in the constructor. Check if it already exists again.
                // E.g. YoutubeTracker will convert the name to the channel id.
                if(trackers.ContainsKey(name))
                    return await AddTrackerAsync(name, channelID, notification);

                trackers.Add(name, tracker);
                tracker.PostChannelAdded(channelID);
                tracker.PostInitialisation();
                trackers[name].ChannelConfig[channelID]["Notification"] = notification;
                trackers[name].LastActivity = DateTime.Now;
                trackers[name].OnMajorEventFired += OnMajorEvent;
                trackers[name].OnMinorEventFired += OnMinorEvent;
                //trackers[name].SetTimer(trackerInterval);
                await UpdateDBAsync(trackers[name]);
                await updateGraph(1);
                loopQueue.Add(tracker);
            }

            await Program.MopsLog(new LogMessage(LogSeverity.Info, "", $"Started a new {typeof(T).Name} for {name}\nChannels: {string.Join(",", trackers[name].ChannelConfig.Keys)}\nMessage: {notification}"));

            return trackers[name];
        }

        private async Task updateGraph(int increase = 1)
        {
            double dateValue = OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Today);

            if (IncreaseGraph == null || IncreaseGraph.PlotDataPoints.Count == 0)
            {
                IncreaseGraph = new DatePlot(typeof(T).Name + "Handler", "Date", "Tracker Increase", "dd-MMM", false);
                IncreaseGraph.AddValue("Value", 0, DateTime.Today.AddMilliseconds(-1));
                IncreaseGraph.AddValue("Value", increase, DateTime.Today);
            }

            else
            {
                if (IncreaseGraph.PlotDataPoints.Last().Value.Key < dateValue)
                {
                    //Only show past year
                    IncreaseGraph.PlotDataPoints = IncreaseGraph.PlotDataPoints.SkipWhile(x => (DateTime.Today - OxyPlot.Axes.DateTimeAxis.ToDateTime(x.Value.Key)).Days >= 365).ToList();
                    for (int i = (int)(dateValue - IncreaseGraph.PlotDataPoints.Last().Value.Key) - 1; i > 0; i--)
                        IncreaseGraph.AddValue("Value", 0, DateTime.Today.AddDays(-i));
                    IncreaseGraph.AddValue("Value", increase, DateTime.Today);
                }
                else
                {
                    IncreaseGraph.AddValue("Value", IncreaseGraph.PlotDataPoints.Last().Value.Value + increase, DateTime.Today, replace: true);
                }
            }

            await StaticBase.Database.GetCollection<DatePlot>("TrackerHandler").ReplaceOneAsync(x => x.ID.Equals(typeof(T).Name + "Handler"), IncreaseGraph, new UpdateOptions { IsUpsert = true });
        }

        public override async Task<Embed> GetEmbed()
        {
            var embed = new EmbedBuilder();

            embed.WithTitle(typeof(T).Name + "Handler").WithDescription($"Currently harboring {this.trackers.Count} {typeof(T).Name}s");

            await updateGraph(0);
            embed.WithImageUrl(IncreaseGraph.DrawPlot());

            return embed.Build();
        }

        public override async Task<bool> TrySetNotificationAsync(string name, ulong channelID, string notificationMessage)
        {
            var tracker = GetTracker(channelID, name);

            if (tracker != null)
            {
                tracker.ChannelConfig[channelID]["Notification"] = notificationMessage;
                await UpdateDBAsync(tracker);
                return true;
            }

            return false;
        }

        public override IEnumerable<BaseTracker> GetTrackers(ulong channelID)
        {
            return trackers.Select(x => x.Value).Where(x => x.ChannelConfig.ContainsKey(channelID));
        }

        public override IEnumerable<BaseTracker> GetGuildTrackers(ulong guildId)
        {
            try
            {
                var channels = Program.Client.GetGuild(guildId).TextChannels;
                var allTrackers = trackers.Select(x => x.Value).ToList();
                var guildTrackers = allTrackers.Where(x => x.ChannelConfig.Keys.Any(y => channels.Select(z => z.Id).Contains(y))).ToList();
                return guildTrackers;
            }
            catch
            {
                return new List<BaseTracker>();
            }
        }

        public async Task<bool> TryModifyTrackerAsync(string name, ulong channelId, Action<T> modifier)
        {
            var tracker = GetTracker(channelId, name) as T;
            if (tracker != null)
            {
                modifier(tracker);
                await UpdateDBAsync(tracker);
                return true;
            }
            else
                return false;
        }

        public override IEnumerable<Embed> GetTrackersEmbed(ulong channelID, bool searchServer = false, string name = null)
        {
            var guild = (Program.Client.GetChannel(channelID) as SocketGuildChannel).Guild;

            var foundTrackers = (searchServer ? GetGuildTrackers(guild.Id) : trackers.Where(x => x.Value.ChannelConfig.ContainsKey(channelID)).Select(x => x.Value));
            if (name != null) foundTrackers = foundTrackers.Where(x => x.Name.Equals(name));

            var trackerStrings = foundTrackers.Select(x => x.TrackerUrl() != null ? $"[``{x.Name}``]({x.TrackerUrl()}) [{string.Join(" ", x.ChannelConfig.Keys.Where(y => guild.GetTextChannel(y) != null).Select(y => (Program.Client.GetChannel(y) as SocketTextChannel).Mention))}]\n"
                                                                                  : $"``{x.Name}`` [{string.Join(" ", x.ChannelConfig.Keys.Where(y => guild.GetTextChannel(y) != null).Select(y => (Program.Client.GetChannel(y) as SocketTextChannel).Mention))}]\n");
            var embeds = new List<EmbedBuilder>() { new EmbedBuilder().WithTitle(typeof(T).Name).WithCurrentTimestamp().WithColor(Discord.Color.Blue) };

            foreach (var tracker in trackerStrings)
            {
                if ((embeds.Last().Description?.Length ?? 0) + tracker.Length > 2048)
                {
                    embeds.Add(new EmbedBuilder());
                    embeds.Last().WithTitle(typeof(T).Name).WithCurrentTimestamp().WithColor(Discord.Color.Blue);
                }
                embeds.Last().Description += tracker;
            }

            return embeds.Select(x => x.Build());
        }

        public override Dictionary<string, BaseTracker> GetTrackers()
        {
            return trackers.Select(x => new KeyValuePair<string, BaseTracker>(x.Key, (BaseTracker)x.Value)).ToDictionary(x => x.Key, x => x.Value);
        }

        public override BaseTracker GetTracker(ulong channelID, string name)
        {
            return trackers.FirstOrDefault(x => x.Key.Equals(name) && x.Value.ChannelConfig.ContainsKey(channelID)).Value;
        }

        public override HashSet<BaseTracker> GetTrackerSet()
        {
            return trackers.Values.Select(x => (BaseTracker)x).ToHashSet();
        }

        public override Type GetTrackerType()
        {
            return typeof(T);
        }


        /// <summary>
        /// Event that is called when the Tracker fetches new data containing no Embed
        /// </summary>
        /// <returns>A Task that can be awaited</returns>
        private async Task OnMinorEvent(ulong channelID, Tracker.BaseTracker sender, string notification)
        {
            if (!Program.GetShardFor(channelID)?.ConnectionState.Equals(Discord.ConnectionState.Connected) ?? true)
                return;
            try
            {
                if (!notification.Equals(""))
                    await ((Discord.WebSocket.SocketTextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync(notification);

                if((DateTime.Now - sender.LastActivity).TotalSeconds > 10){
                    sender.LastActivity = DateTime.Now;
                    await sender.UpdateTracker();
                }
            }
            catch(Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"A {typeof(T).Name} for {sender.Name} got an error:", e));
                if (Program.Client.GetChannel(channelID) == null || (await ((IGuildChannel)Program.Client.GetChannel(channelID)).Guild.GetCurrentUserAsync()) == null)
                {
                    await TryRemoveTrackerAsync(sender.Name, channelID);
                    await Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"Removed Tracker: {sender.Name} Channel {channelID} is missing"));
                }
                else
                {
                    var permission = (await ((IGuildChannel)Program.Client.GetChannel(channelID)).Guild.GetCurrentUserAsync()).GetPermissions(((IGuildChannel)Program.Client.GetChannel(channelID)));
                    if (!permission.SendMessages)
                    {
                        await TryRemoveTrackerAsync(sender.Name, channelID);
                        var perms = string.Join(", ", permission.ToList().Select(x => x.ToString() + ": " + permission.Has(x)));
                        await Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"Removed a {typeof(T).Name} for {sender.Name} from Channel {channelID} due to missing Permissions:\n{perms}", e));
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
        private async Task OnMajorEvent(ulong channelID, Embed embed, Tracker.BaseTracker sender, string notification)
        {
            if (!Program.GetShardFor(channelID)?.ConnectionState.Equals(Discord.ConnectionState.Connected) ?? true)
                return;
            try
            {
                if (sender is BaseUpdatingTracker)
                {
                    BaseUpdatingTracker tracker = sender as BaseUpdatingTracker;
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
                            await UpdateDBAsync(tracker);
                        }
                    }
                    else
                    {
                        var message = await ((Discord.WebSocket.SocketTextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync(notification, embed: embed);
                        tracker.ToUpdate.Add(channelID, message.Id);
                        await tracker.setReaction((IUserMessage)message);
                        await UpdateDBAsync(tracker);
                    }
                }
                else
                    await ((Discord.WebSocket.SocketTextChannel)Program.Client.GetChannel(channelID)).SendMessageAsync(notification, embed: embed);
                
                if((DateTime.Now - sender.LastActivity).TotalSeconds > 10){
                    sender.LastActivity = DateTime.Now;
                    await sender.UpdateTracker();
                }
            }
            catch(Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"A {typeof(T).Name} for {sender.Name} got an error:", e));
                //Check if channel still exists, or existing only in cache
                if (Program.Client.GetChannel(channelID) == null || (await ((IGuildChannel)Program.Client.GetChannel(channelID)).Guild.GetCurrentUserAsync()) == null)
                {
                    //await TryRemoveTrackerAsync(sender.Name, channelID);
                    await Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"Removed {typeof(T).Name}: {sender.Name} Channel {channelID} is missing"));
                }
                //Check if permissions were modified, to an extend of making the tracker unusable
                else
                {
                    var permission = (await ((IGuildChannel)Program.Client.GetChannel(channelID)).Guild.GetCurrentUserAsync()).GetPermissions(((IGuildChannel)Program.Client.GetChannel(channelID)));
                    if (!permission.SendMessages || (sender is Tracker.BaseUpdatingTracker && (!permission.ManageMessages || !permission.ReadMessageHistory)))
                    {
                        await TryRemoveTrackerAsync(sender.Name, channelID);
                        var perms = string.Join(", ", permission.ToList().Select(x => x.ToString() + ": " + permission.Has(x)));
                        await Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"Removed a {typeof(T).Name} for {sender.Name} from Channel {channelID} due to missing Permissions:\n{perms}", e));
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
