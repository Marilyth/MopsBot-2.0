using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;

namespace MopsBot.Module.Data
{
    class TwitterList
    {
        public Dictionary<string, Session.TwitterTracker> twitters;

        public TwitterList()
        {
            Auth.SetUserCredentials(Program.twitterAuth[0], Program.twitterAuth[1], Program.twitterAuth[2], Program.twitterAuth[3]);
            TweetinviConfig.CurrentThreadSettings.TweetMode = TweetMode.Extended;
            TweetinviConfig.ApplicationSettings.TweetMode = TweetMode.Extended;

            twitters = new Dictionary<string, Session.TwitterTracker>();

            StreamReader read = new StreamReader(new FileStream("data//twitters.txt", FileMode.OpenOrCreate));

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

        public void writeList()
        {
            StreamWriter write = new StreamWriter(new FileStream("data//twitters.txt", FileMode.Create));
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
    }
}
