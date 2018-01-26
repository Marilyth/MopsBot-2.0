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
    /// A class containing all Twitch streamers to track
    /// </summary>
    public class StreamerList : IDisposable
    {
        bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);


        public Dictionary<string, Session.TwitchTracker> streamers;

        /// <summary>
        /// Reads frmo a text file, and fills a Dictionary with Streamer names as key, and trackers as value
        /// Also adds events
        /// </summary>
        public StreamerList()
        {
            streamers = new Dictionary<string, Session.TwitchTracker>();

            using (StreamReader read = new StreamReader(new FileStream("data//streamers.txt", FileMode.OpenOrCreate)))
            {
                string s = "";
                while ((s = read.ReadLine()) != null)
                {
                    try
                    {

                        var trackerInformation = s.Split('|');
                        if (!streamers.ContainsKey(trackerInformation[0]))
                        {
                            Session.TwitchTracker streamer = new Session.TwitchTracker(trackerInformation[0], ulong.Parse(trackerInformation[1]), trackerInformation[2], Boolean.Parse(trackerInformation[3].ToLower()), trackerInformation[4]);
                            streamer.StreamerGameChanged += onGameChanged;
                            streamer.StreamerStatusChanged += onStatusChanged;
                            streamer.StreamerWentOnline += onWentOnline;
                            streamer.StreamerWentOffline += onWentOffline;

                            streamers.Add(trackerInformation[0], streamer);
                        }

                        else
                        {
                            streamers[trackerInformation[0]].ChannelIds.Add(ulong.Parse(trackerInformation[1]), trackerInformation[2]);
                            Console.Out.WriteLine($"Added {trackerInformation[1]} to {trackerInformation[0]}");
                        }

                        if (trackerInformation[3].Equals("True"))
                        {
                            var channel = Program.client.GetChannel(ulong.Parse(trackerInformation[1]));
                            var message = ((Discord.ITextChannel)channel).GetMessageAsync(ulong.Parse(trackerInformation[5])).Result;
                            streamers[trackerInformation[0]].toUpdate.Add(ulong.Parse(trackerInformation[1]), (Discord.IUserMessage)message);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Writes all information of each Streamer into a text file
        /// </summary>
        public void writeList()
        {
            using (StreamWriter write = new StreamWriter(new FileStream("data//streamers.txt", FileMode.Create)))
                foreach (Session.TwitchTracker tr in streamers.Values)
                {
                    foreach (var channel in tr.ChannelIds)
                    {
                        if (tr.toUpdate.ContainsKey(channel.Key))
                            write.WriteLine($"{tr.name}|{channel.Key}|{channel.Value}|{tr.isOnline}|{tr.curGame}|{tr.toUpdate[channel.Key].Id}");
                        else
                            write.WriteLine($"{tr.name}|{channel.Key}|{channel.Value}|{tr.isOnline}|{tr.curGame}|0");
                    }
                }
        }

        /// <summary>
        /// Event that is called when the Streamer changes games
        /// </summary>
        /// <param name="streamer">The tracker object of the Streamer who invoked the event</param>
        /// <returns>A Task that can be awaited</returns>
        private async Task onGameChanged(Session.TwitchTracker streamer)
        {
            foreach (var channel in streamer.ChannelIds)
                await ((Discord.WebSocket.SocketTextChannel)Program.client.GetChannel(channel.Key)).SendMessageAsync($"{streamer.name} spielt jetzt **{streamer.curGame}**!");

            writeList();
        }

        /// <summary>
        /// Event that is called when the Tracker fetches new data
        /// Updates or creates the notification message with it
        /// </summary>
        /// <param name="streamer">The tracker object of the Streamer who invoked the event</param>
        /// <returns>A Task that can be awaited</returns>
        private async Task onStatusChanged(Session.TwitchTracker streamer)
        {
            var e = streamer.createEmbed();

            foreach (var channel in streamer.ChannelIds)
            {
                if (!streamer.toUpdate.ContainsKey(channel.Key))
                {
                    streamer.toUpdate.Add(channel.Key, await ((SocketTextChannel)Program.client.GetChannel(channel.Key)).SendMessageAsync(channel.Value, false, e));
                    Console.Out.WriteLine($"{DateTime.Now} {streamer.name} toUpdate added {streamer.toUpdate[channel.Key].Id}");
                    StaticBase.streamTracks.writeList();
                }

                else
                    await streamer.toUpdate[channel.Key].ModifyAsync(x =>
                    {
                        x.Content = channel.Value;
                        x.Embed = (Embed)e;
                    });
            }
        }

        /// <summary>
        /// Event that is called when the Streamer goes online
        /// </summary>
        /// <param name="streamer">The tracker object of the Streamer who invoked the event</param>
        /// <returns>A Task that can be awaited</returns>
        private Task onWentOnline(Session.TwitchTracker streamer)
        {
            Console.WriteLine($"Streamer {streamer.name} went online");

            writeList();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Event that is called when the Streamer goes offline
        /// </summary>
        /// <param name="streamer">The tracker object of the Streamer who invoked the event</param>
        /// <returns>A Task that can be awaited</returns>
        private Task onWentOffline(Session.TwitchTracker streamer)
        {
            Console.WriteLine($"{DateTime.Now} Streamer {streamer.name} went online");

            writeList();
            return Task.CompletedTask;
        }

        public void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);           
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return; 
      
            if (disposing) {
                handle.Dispose();
                foreach(Session.TwitchTracker t in streamers.Values)
                    t.Dispose();
            }
      
            streamers = new Dictionary<string, Session.TwitchTracker>();
            disposed = true;
        }


    }
}
