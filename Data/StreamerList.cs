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

            using (StreamReader read = new StreamReader(new FileStream("mopsdata//streamers.txt", FileMode.OpenOrCreate)))
            {
                string s = "";
                while ((s = read.ReadLine()) != null)
                {
                    try
                    {
                        Session.TwitchTracker streamer = null;
                        var trackerInformation = s.Split('|');
                        if (!streamers.ContainsKey(trackerInformation[0]))
                        {
                            streamer = new Session.TwitchTracker(trackerInformation[0], ulong.Parse(trackerInformation[1]), trackerInformation[2], Boolean.Parse(trackerInformation[3].ToLower()));
                            streamers.Add(streamer.name, streamer);
                            StaticBase.TrackerHandle.addTracker(streamer);
                        }

                        else
                        {
                            streamer.ChannelIds.Add(ulong.Parse(trackerInformation[1]), trackerInformation[2]);
                            Console.Out.WriteLine($"Added {trackerInformation[1]} to {trackerInformation[0]}");
                        }

                        if (trackerInformation[3].Equals("True"))
                        {
                            var channel = Program.client.GetChannel(ulong.Parse(trackerInformation[1]));
                            var message = ((Discord.ITextChannel)channel).GetMessageAsync(ulong.Parse(trackerInformation[4])).Result;
                            streamer.toUpdate.Add(ulong.Parse(trackerInformation[1]), (Discord.IUserMessage)message);
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
            using (StreamWriter write = new StreamWriter(new FileStream("mopsdata//streamers.txt", FileMode.Create)))
                foreach (Session.TwitchTracker tr in streamers.Values)
                {
                    foreach (var channel in tr.ChannelIds)
                    {
                        if (tr.toUpdate.ContainsKey(channel.Key))
                            write.WriteLine($"{tr.name}|{channel.Key}|{channel.Value}|{tr.isOnline}|{tr.toUpdate[channel.Key].Id}");
                        else
                            write.WriteLine($"{tr.name}|{channel.Key}|{channel.Value}|{tr.isOnline}|0");
                    }
                }
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
