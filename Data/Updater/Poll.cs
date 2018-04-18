using System;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Data.Updater
{
    public class Poll
    {
        public string question;
        public string[] answers;
        public bool isPrivate;
        public Dictionary<string, List<ulong>> results;
        public List<IGuildUser> participants;

        /// <summary>
        /// Saves the plot as a .png and returns the URL.
        /// </summary>
        /// <returns>The URL</returns>
        public string DrawPlot()
        {
            return Plot.CreateBarDiagram<KeyValuePair<string, List<ulong>>>(
                                        answers.Length, 
                                        x => x.Value.Count, 
                                        x => isPrivate ? x.Key : String.Join(", ", x.Value.Select(y => Program.client.GetUser(y).Username)), 
                                        results.ToList());
        }

        /// <summary>
        /// Adds a Value to the plot, to its' current Title
        /// </summary>
        /// <param name="value">The Value to add to the plot</param>
        public void AddValue(string answer, ulong participant)
        {
            results[answer].Add(participant);
        }

        public Poll(string q, string[] a, IGuildUser[] p)
        {
            question = q;
            answers = a;
            results = new Dictionary<string, List<ulong>>();
            foreach(string answer in a){
                results.Add(answer, new List<ulong>());
            }
            participants = p.ToList();
        }
    }
}
