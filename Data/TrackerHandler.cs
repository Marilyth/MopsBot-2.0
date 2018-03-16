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
    public class TrackerHandler
    {
        public HashSet<Session.ITracker> allTrackers;
        public TrackerHandler()
        {
            allTrackers = new HashSet<Session.ITracker>();
        }

        public void addTracker(Session.ITracker tracker){
            allTrackers.Add(tracker);
            tracker.OnMajorEventFired += OnMajorEvent;
            tracker.OnMinorEventFired += OnMinorEvent;
        }


        /// <summary>
        /// Event that is called when the Tracker fetches new data containing no Embed
        /// </summary>
        /// <returns>A Task that can be awaited</returns>
        private async Task OnMinorEvent(ulong channelID, Session.ITracker parent, string notification)
        {
            await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification);
        }

        /// <summary>
        /// Event that is called when the Tracker fetches new data containing an Embed
        /// Updates or creates the notification message with it
        /// </summary>
        /// <returns>A Task that can be awaited</returns>
        private async Task OnMajorEvent(ulong channelID, EmbedBuilder embed, Session.ITracker parent, string notification)
        {
            if(parent is Session.TwitchTracker){
                Session.TwitchTracker parentHandle = (Session.TwitchTracker)parent;

                if(parentHandle.toUpdate.ContainsKey(channelID))
                    await parentHandle.toUpdate[channelID].ModifyAsync(x => {
                        x.Content = notification;
                        x.Embed = (Embed)embed;
                    });

                else
                    parentHandle.toUpdate.Add(channelID, await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, false, embed));
            }
            
            else
                await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channelID)).SendMessageAsync(notification, false, embed);
        }
    }
}
