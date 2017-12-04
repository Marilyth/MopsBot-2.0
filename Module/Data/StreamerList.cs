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
                try{

                    var trackerInformation = s.Split('|');
                    if (!streamers.Exists(x => x.name.ToLower().Equals(trackerInformation[0].ToLower())))
                    {
                        streamers.Add(new Session.TwitchTracker(trackerInformation[0], ulong.Parse(trackerInformation[1]), trackerInformation[2], Boolean.Parse(trackerInformation[3].ToLower()), trackerInformation[4])); 
                    }

                    else{                     
                        streamers.Find(x => x.name.ToLower().Equals(trackerInformation[0].ToLower())).ChannelIds.Add(ulong.Parse(trackerInformation[1]), trackerInformation[2]);
                    }

                    if(trackerInformation[3].Equals("True")){
                        var channel = Program.client.GetChannel(ulong.Parse(trackerInformation[1]));
                        var message = ((Discord.ITextChannel)channel).GetMessageAsync(ulong.Parse(trackerInformation[5])).Result;
                        streamers.Find(x => x.name.ToLower().Equals(trackerInformation[0].ToLower())).toUpdate.Add(ulong.Parse(trackerInformation[1]), (Discord.IUserMessage)message);
                    }

                }catch(Exception e){
                    Console.WriteLine(e.Message);
                }
            }

            read.Dispose();
        }

        public void writeList()
        {
            StreamWriter write = new StreamWriter(new FileStream("data//streamers.txt", FileMode.Create));
            write.AutoFlush=true;
            foreach(Session.TwitchTracker tr in streamers)
            {
                foreach(var channel in tr.ChannelIds)
                {
                    if(tr.toUpdate.ContainsKey(channel.Key))
                        write.WriteLine($"{tr.name}|{channel.Key}|{channel.Value}|{tr.isOnline}|{tr.curGame}|{tr.toUpdate[channel.Key].Id}");
                    else
                        write.WriteLine($"{tr.name}|{channel.Key}|{channel.Value}|{tr.isOnline}|{tr.curGame}|0");
                }
            }
            write.Dispose();
        }
    }
}
