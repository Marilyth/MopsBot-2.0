using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data
{
    class Statistics
    {
        public List<Day> days = new List<Day>();
        public DateTime today = DateTime.Today;

        public Statistics()
        {
            
            StreamReader read = new StreamReader(new FileStream("data//statistics.txt", FileMode.Open));
            
            string s = "";

            while ((s = read.ReadLine()) != null)
            {
                string[] data = s.Split(':');
                days.Add(new Day(data[0], int.Parse(data[1])));
            }
            
            read.Dispose();
        }

        public void addValue(int increase)
        {
            today = DateTime.Today;

            if (days.Exists(x => x.date.Equals(today)))
                days.Find(x => x.date.Equals(today)).value += increase;

            else days.Add(new Day(today.ToString("dd/MM/yyyy"), increase));

            saveData();
        }

        private void saveData()
        {
            StreamWriter write = new StreamWriter(new FileStream("data//statistics.txt", FileMode.Create));
            write.AutoFlush=true;
            foreach(Day cur in days)
            {
                write.WriteLine($"{cur.date.ToString("dd/MM/yyyy")}:{cur.value}");
            }

            write.Dispose();
            
        }

        public string drawDiagram(int count)
        {
            days = days.OrderByDescending(x => x.date).ToList();

            List<Day> tempDays = days.Take(count).ToList();
            tempDays = tempDays.OrderByDescending(x => x.value).ToList();

            int maximum = tempDays[0].value;

            string[] lines = new string[count];

            for(int i = 0; i < count; i++)
            {
                lines[i] = $"{days[i].date.ToString("dd/MM/yyyy")}|";
                double relPercent = days[i].value / ((double)maximum / 10);
                for(int j = 0; j < relPercent; j++)
                {
                    lines[i] += "■";
                }
                lines[i] += $" {days[i].value}";
            }

            string output = "```coq\n" + string.Join("\n", lines) + "```";

            return output;
        }
    }

    class Day
    {
        public DateTime date;
        public int value;

        public Day(string pDate, int pValue)
        {
            date = DateTime.ParseExact(pDate, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            value = pValue;
        }
    }
}
