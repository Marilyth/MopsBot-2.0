using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data
{
    /// <summary>
    /// Class that handles and contains Twitch Clip tracking
    /// (Unfinished I think)
    /// </summary>
    public class ClipTracker : IDisposable
    {
        bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        
        Dictionary<string, List<ulong>> tracklist;
        public DateTime lastcheck;
        private System.Threading.Timer checkForChange;

        public ClipTracker()
        {
            tracklist = new Dictionary<string, List<ulong>>();
            try
            {
                StreamReader read = new StreamReader(new FileStream("mopsdata//lastcheck.txt", FileMode.OpenOrCreate));
                lastcheck = DateTime.Parse(read.ReadLine());
                read.Dispose();
            }
            catch
            {
                lastcheck = DateTime.MinValue;
                StreamWriter write = new StreamWriter(new FileStream("mopsdata//lastcheck.txt", FileMode.Create));
                write.AutoFlush = true;
                write.WriteLine(lastcheck.ToString());
                write.Dispose();
            }
            try
            {
                readList();
            }
            catch
            {
                writeList();
            }
            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6, 59) * 1000, 60000);
        }

        private void CheckForChange_Elapsed(object stateinfo)
        {
            string channel = "";
            foreach (KeyValuePair<string, List<ulong>> item in tracklist)
            {
                channel = (channel == "") ? item.Key : $"{channel},{item.Key}";
                foreach (dynamic clip in NextPage(new List<dynamic>(), channel, ""))
                {
                    var temp = clip["broadcaster"]["name"].ToString();
                    List<ulong> channels = tracklist[temp];
                    foreach (SocketTextChannel c in channels.Select(id => Program.client.GetChannel(id)))
                    {
                        c.SendMessageAsync(clip["url"].ToString());
                    }
                }
            }
        }

        public void readList()
        {
            StreamReader read = new StreamReader(new FileStream("mopsdata//clips.txt", FileMode.Open));
            tracklist = new Dictionary<string, List<ulong>>();
            string s = "";
            while ((s = read.ReadLine()) != null)
            {
                var trackerInformation = s.Split(':');
                tracklist.Add(trackerInformation[0], new List<ulong>());
                foreach (var item in trackerInformation.Skip(1))
                {
                    tracklist[trackerInformation[0]].Add(ulong.Parse(item));
                }
            }

            read.Dispose();
        }

        public void writeList()
        {
            StreamWriter write = new StreamWriter(new FileStream("mopsdata//clips.txt", FileMode.Create));
            foreach (var entry in tracklist)
            {
                write.WriteLine($"{entry.Key}:{String.Join(":", entry.Value)}");
            }
        }

        public void addTracker(string name, ulong channel)
        {
            if (!tracklist.ContainsKey(name))
                tracklist.Add(name, new List<ulong>());
            tracklist[name].Add(channel);
            writeList();

        }

        public List<dynamic> NextPage(List<dynamic> clips, string channel, string cursor)
        {
            dynamic tempDict;
            try
            {
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create($"https://api.twitch.tv/kraken/clips/top?channel={channel}&period=day&limit=100" + ((cursor != "") ? $"&cursor={cursor}" : ""));
                request.Headers["Accept"] = "application/vnd.twitchtv.v5+json";
                request.Headers["Client-ID"] = Program.twitchId;
                using (var response = request.GetResponseAsync().Result)
                using (var content = response.GetResponseStream())
                using (var reader = new System.IO.StreamReader(content))
                {
                    tempDict = JsonConvert.DeserializeObject<dynamic>(reader.ReadToEnd());
                }
                if (tempDict["clips"] != null)
                {
                    foreach (dynamic clip in ((IEnumerable<dynamic>)tempDict["clips"]).Where(p => DateTime.Parse(p.created_at.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind).CompareTo(lastcheck) > 0))
                    {
                        clips.Add(clip);
                    }
                    if (!tempDict["_cursor"].ToString().Equals(""))
                    {
                        return NextPage(clips, channel, tempDict["_cursor"].ToString());
                    }
                    else
                    {
                        if (clips.Count > 0)
                            lastcheck = DateTime.Now;
                        StreamWriter write = new StreamWriter(new FileStream("mopsdata//lastcheck.txt", FileMode.Create));
                        write.AutoFlush = true;
                        write.WriteLine(lastcheck.ToString());
                        write.Dispose();
                        return clips;
                    }
                }
                return clips;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new List<dynamic>();
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
                checkForChange.Dispose();
            }
      
            tracklist = new Dictionary<string, List<ulong>>();
            disposed = true;
        }

    }
}