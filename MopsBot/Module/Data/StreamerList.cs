using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data
{
    class StreamerList
    {
        public List<Session.TwitchTracker> streamers;

        public StreamerList()
        {
            streamers = new List<Session.TwitchTracker>();

            StreamReader read = new StreamReader(new FileStream("data//streamers.txt", FileMode.Open));

            string s = "";
            while((s = read.ReadLine()) != null)
            {
                var trackerInformation = s.Split(':');
                if (!streamers.Exists(x => x.name.ToLower().Equals(trackerInformation[0].ToLower())))
                {
                    streamers.Add(new Session.TwitchTracker(trackerInformation[0], ulong.Parse(trackerInformation[1])));
                }
                else
                    streamers.Find(x => x.name.ToLower().Equals(trackerInformation[0].ToLower())).ChannelIds.Add(ulong.Parse(trackerInformation[1]));
            }

            read.Dispose();
        }

        public void writeList()
        {
            StreamWriter write = new StreamWriter(new FileStream("data//streamers.txt", FileMode.Open));

            foreach(Session.TwitchTracker tr in streamers)
            {
                foreach(ulong id in tr.ChannelIds)
                {
                    write.WriteLine($"{tr.name}:{id}");
                }
            }
            write.Dispose();
        }
    }
}
