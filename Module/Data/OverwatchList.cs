using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;

namespace MopsBot.Module.Data
{
    class OverwatchList
    {
        public Dictionary<string, Session.OverwatchTracker> owPlayers;

        public OverwatchList()
        {
            owPlayers = new Dictionary<string, Session.OverwatchTracker>();

            Task.Run(() =>
            {
                string s = "";
                using (StreamReader read = new StreamReader(new FileStream("data//overwatchid.txt", FileMode.OpenOrCreate)))
                {
                    while ((s = read.ReadLine()) != null)
                    {
                        try
                        {

                            var trackerInformation = s.Split('|');
                            if (!owPlayers.ContainsKey(trackerInformation[0]))
                            {
                                owPlayers.Add(trackerInformation[0], new Session.OverwatchTracker(trackerInformation[0]));
                            }

                            owPlayers[trackerInformation[0]].ChannelIds.Add(ulong.Parse(trackerInformation[1]));
                            System.Threading.Thread.Sleep(6000);

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            });
        }

        public void writeList()
        {
            StreamWriter write = new StreamWriter(new FileStream("data//overwatchid.txt", FileMode.Create));
            write.AutoFlush = true;
            foreach (Session.OverwatchTracker tr in owPlayers.Values)
            {
                foreach (var channel in tr.ChannelIds)
                {
                    write.WriteLine($"{tr.name}|{channel}");
                }
            }
            write.Dispose();
        }
    }
}
