using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Data
{
    /// <summary>
    /// A class that keeps track of how many characters have been recieved each day
    /// </summary>
    public class Statistics
    {
        public List<Day> days = new List<Day>();
        public DateTime today = DateTime.Today;

        /// <summary>
        /// Initialises Statistics, by reading from a text file containing Date and Characters Count and adding them to a List
        /// </summary>
        public Statistics()
        {
            
            StreamReader read = new StreamReader(new FileStream("data//statistics.txt", FileMode.OpenOrCreate));
            
            string s = "";

            while ((s = read.ReadLine()) != null)
            {
                string[] data = s.Split(':');
                days.Add(new Day(data[0], int.Parse(data[1])));
            }
            
            read.Dispose();
        }

        /// <summary>
        /// Adds the "increase" parameter to todays value
        /// </summary>
        /// <param name="increase">Integer repesenting how many characters have been recieved</param>
        public void addValue(int increase)
        {
            today = DateTime.Today;

            if (days.Exists(x => x.date.Equals(today)))
                days.Find(x => x.date.Equals(today)).value += increase;

            else days.Add(new Day(today.ToString("dd/MM/yyyy"), increase));

            saveData();
        }

        /// <summary>
        /// Writes all days and values into a text file
        /// </summary>
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

        /// <summary>
        /// Creates an ASCII chart presenting the past "count" days and their values
        /// </summary>
        /// <param name="count">Integer, representing how many days should be shown</param>
        /// <returns></returns>
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

    /// <summary>
    /// Class representing a single Day for the Statistics
    /// </summary>
    public class Day
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
