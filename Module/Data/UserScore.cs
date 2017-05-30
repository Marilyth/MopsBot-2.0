using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

namespace MopsBot.Module.Data
{
    class UserScore
    {
        public List<Individual.User> users = new List<Individual.User>();

        public UserScore()
        {
            StreamReader read = new StreamReader(new FileStream("data//scores.txt", FileMode.Open));

            string fs = "";
            while ((fs = read.ReadLine()) != null)
            {
                string[] s = fs.Split(':');
                users.Add(new Individual.User(ulong.Parse(s[0]),int.Parse(s[1]), int.Parse(s[2]), int.Parse(s[3]), int.Parse(s[4]), int.Parse(s[5])));
            }
            read.Dispose();
            users = users.OrderByDescending(u => u.Experience).ToList();
        }

        public void writeScore()
        {
            users = users.OrderByDescending(u => u.Experience).ToList();

            StreamWriter write = new StreamWriter(new FileStream("data//scores.txt",FileMode.Open));
            write.AutoFlush=true;
            foreach (Individual.User that in users)
            {
                write.WriteLine($"{that.ID}:{that.Score}:{that.Experience}:{that.punched}:{that.hugged}:{that.kissed}");
            }

            write.Dispose();
        }

        public void addStat(ulong id, int value, string stat)
        {
            switch (stat.ToLower())
            {
                case "experience":
                    users.Find(x => x.ID.Equals(id)).Experience += value;
                    break;
                case "score":
                    users.Find(x => x.ID.Equals(id)).Score += value;
                    break;
                case "hug":
                    users.Find(x => x.ID.Equals(id)).hugged += value;
                    break;
                case "kiss":
                    users.Find(x => x.ID.Equals(id)).kissed += value;
                    break;
                case "punch":
                    users.Find(x => x.ID.Equals(id)).punched += value;
                    break;
                default:
                    return;
            }
            writeScore();
        }

        public string drawDiagram(int count, DiagramType type)
        {
            List<Individual.User> tempUsers = users.Take(count).ToList();

            int maximum = 0;
            string[] lines = new string[count];

            switch (type)
            {
                case DiagramType.Experience:
                    tempUsers = tempUsers.OrderByDescending(x => x.Experience).ToList();

                    maximum = tempUsers[0].Experience;

                    for (int i = 0; i < count; i++)
                    {
                        lines[i] = (i + 1).ToString().Length < 2 ? $"#{i + 1} |" : $"#{i + 1}|";
                        double relPercent = users[i].Experience / ((double)maximum / 10);
                        for (int j = 0; j < relPercent; j++)
                        {
                            lines[i] += "■";
                        }
                        lines[i] += $"  ({users[i].Experience} / {Program.client.GetUser(users[i].ID).Username})";
                    }
                    break;

                case DiagramType.Level:
                    tempUsers = tempUsers.OrderByDescending(x => x.Level).ToList();

                    maximum = tempUsers[0].Level;

                    for (int i = 0; i < count; i++)
                    {
                        lines[i] = (i + 1).ToString().Length < 2 ? $"#{i + 1} |" : $"#{i + 1}|";
                        double relPercent = users[i].Level / ((double)maximum / 10);
                        for (int j = 0; j < relPercent; j++)
                        {
                            lines[i] += "■";
                        }
                        lines[i] += $"  ({users[i].Level} / {(Program.client.GetUser(users[i].ID) == null ? "" + users[i].ID : Program.client.GetUser(users[i].ID).Username)})";
                    }
                    break;

                case DiagramType.Score:
                    tempUsers = tempUsers.OrderByDescending(x => x.Score).ToList();

                    maximum = tempUsers[0].Score;

                    for (int i = 0; i < count; i++)
                    {
                        lines[i] = (i + 1).ToString().Length < 2 ? $"#{i+1} |" : $"#{i+1}|";
                        double relPercent = users[i].Score / ((double)maximum / 10);
                        for (int j = 0; j < relPercent; j++)
                        {
                            lines[i] += "■";
                        }
                        lines[i] += $"  ({users[i].Score} / {Program.client.GetUser(users[i].ID).Username})";
                    }
                    break;
            }

            string output = "```" + string.Join("\n", lines) + "```";

            return output;
        }

        public enum DiagramType{Experience, Level, Score}
    }
}
