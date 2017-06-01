using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Discord.WebSocket;

namespace MopsBot.Module.Data{
    public class MeetUps
    {
        public List<MeetUp> upcoming;

        public MeetUps()
        {
            upcoming = new List<MeetUp>();

            StreamReader sr = new StreamReader(new FileStream("data//meetups.txt", FileMode.Open));

            string s = "";

            while ((s = sr.ReadLine()) != null)
            {
                string[] data = s.Split('|');
                string[] participants = data[3].Split(';');
                upcoming.Add(new MeetUp(data[0], data[1], data[2], participants));
            }
            
            sr.Dispose();
        }

        public void addMeetUp(string information, SocketGuildUser creator)
        {
            string[] data = information.Split(';');

            upcoming.Add(new MeetUp(data[0], data[1], data[2], new string[]{creator.Id.ToString()}));

            writeData();
        }

        public void blowMeetUp(int id, SocketGuildUser user)
        {
            if(upcoming[id-1].who[0].Equals(user.Id))
            {
                upcoming.RemoveAt(id-1);
                writeData();
            }
        }

        public string meetupToString()
        {
            string output = "";

            upcoming = upcoming.OrderBy(x => x.when).ToList();

            int index = 1;
            foreach(MeetUp cur in upcoming)
            {
                List<string> people = new List<string>();
                foreach(ulong id in cur.who)
                    people.Add(Program.client.GetUser(id).Username);

                output += $"**ID**: `{index}`"+ 
                "\n```"+
                $"\nWhen: {cur.when.ToString("dd.MM.yyyy HH:mm")}"+
                $"\nWhat: {cur.what}"+
                $"\nWhere: {cur.where}"+
                $"\nWho: {String.Join(", ", people)}"+
                "\n```\n\n";

                index++;
            }

            return output;
        }

        public void writeData()
        {
            StreamWriter sw = new StreamWriter(new FileStream("data//meetups.txt", FileMode.Create));

            foreach(MeetUp cur in upcoming)
            {
                if(cur.when > DateTime.Now)
                {
                    sw.WriteLine($"{cur.when.ToString("dd.MM.yyyy HH:mm")}|{cur.what}|{cur.where}|{String.Join(";", cur.who)}");
                }
            }

            sw.Dispose();
        }
    }

    public class MeetUp
    {
        public DateTime when;
        public string what, where;
        public List<ulong> who;
        public MeetUp(string pWhen, string pWhat, string pWhere, string[] pWho)
        {
            when = DateTime.ParseExact(pWhen, "dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            what = pWhat;
            where = pWhere;
            who = new List<ulong>();
            foreach(string id in pWho)
            {
                who.Add(ulong.Parse(id));
            }
        }

         public void addParticipant(ulong id)
         {
            who.Add(id);
            StaticBase.meetups.writeData();
         }

         public void removeParticipant(ulong id)
         {
            who.Remove(id);
            StaticBase.meetups.writeData();
         }
    }
}