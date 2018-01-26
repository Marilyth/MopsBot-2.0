using System;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Data.Session
{
    public class Poll
    {
        public string question;
        public string[] answers;
        public int[] results;
        public List<IGuildUser> participants;

        public Poll(string q, string[] a, IGuildUser[] p)
        {
            question = q;
            answers = a;
            participants = p.ToList();

            results = new int[answers.Length];
        }

        public Poll()
        {
        }

        public string pollToText()
        {
            string output = "";
            for(int i = 0; i < answers.Length; i++)
            {
                output += $"\n{answers[i]} -> {results[i]}";
            }

            return $"📄: {question}\n{output}";
        }
    }
}
