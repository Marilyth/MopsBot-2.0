using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data
{
    /// <summary>
    /// Class containing and handling all Twitter Users to track
    /// </summary>
    public class TwitterList : IDisposable
    {
        bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        public Dictionary<string, Session.TwitterTracker> twitters;

        /// <summary>
        /// Applies credentials from a config.txt
        /// Then reads Twitter users from a text file and fills a dictionary with them, with Trackers as value
        /// </summary>
        public TwitterList()
        {
            Auth.SetUserCredentials(Program.twitterAuth[0], Program.twitterAuth[1], Program.twitterAuth[2], Program.twitterAuth[3]);
            TweetinviConfig.CurrentThreadSettings.TweetMode = TweetMode.Extended;
            TweetinviConfig.ApplicationSettings.TweetMode = TweetMode.Extended;

            twitters = new Dictionary<string, Session.TwitterTracker>();

            StreamReader read = new StreamReader(new FileStream("mopsdata//twitters.txt", FileMode.OpenOrCreate));

            string s = "";
            while((s = read.ReadLine()) != null)
            {
                try{

                    var trackerInformation = s.Split('|');
                    if (!twitters.ContainsKey(trackerInformation[0]))
                    {
                        twitters.Add(trackerInformation[0], new Session.TwitterTracker(trackerInformation[0], long.Parse(trackerInformation[2]))); 
                    }
                   
                    twitters[trackerInformation[0]].ChannelIds.Add(ulong.Parse(trackerInformation[1]));

                }catch(Exception e){
                    Console.WriteLine(e.Message);
                }
            }

            read.Dispose();
        }

        /// <summary>
        /// Writes all tracked Twitter Users into a text file
        /// </summary>
        public void writeList()
        {
            StreamWriter write = new StreamWriter(new FileStream("mopsdata//twitters.txt", FileMode.Create));
            write.AutoFlush=true;
            foreach(Session.TwitterTracker tr in twitters.Values)
            {
                foreach(var channel in tr.ChannelIds)
                {
                    write.WriteLine($"{tr.name}|{channel}|{tr.lastMessage}");
                }
            }
            write.Dispose();
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
                foreach(Session.TwitterTracker t in twitters.Values)
                    t.Dispose();
            }
      
            twitters = new Dictionary<string, Session.TwitterTracker>();
            disposed = true;
        }
    }
}
