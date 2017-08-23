using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data.Individual
{
    class User
    {
        public ulong ID;
        public int Score, Experience, Level, punched, hugged, kissed;
        public List<Items> equipment;

        public User(ulong userID, int userScore, int XP, int punch, int hug, int kiss, string[] items)
        {
            equipment = new List<Items>();

            foreach(string item in items)
                equipment.Add(new Items(item, 0));

            ID = userID;
            Score = userScore;
            Experience = XP;
            punched = punch;
            hugged = hug;
            kissed = kiss;
            Level = calcLevel();
        }

        public User(ulong userID, int userScore, int XP)
        {
            ID = userID;
            Score = userScore;
            Experience = XP;
            Level = calcLevel();
        }

        public User(ulong userID, int userScore)
        {
            ID = userID;
            Score = userScore;
        }

        internal delegate double del(int i);
        internal static del levelCalc = x => (200*(x*x));

        private int calcLevel()
        {
            int i = 0;
            while(Experience > levelCalc(i))
            {
                i++;
            }
            return (i - 1);
        }

        internal string calcNextLevel()
        {
            double expCurrentHold = Experience - levelCalc(Level);
            string output = "", TempOutput = "";
            double diffExperience = levelCalc(Level + 1) - levelCalc(Level);
            for (int i = 0; i < Math.Floor(expCurrentHold/(diffExperience/10)); i++)
            {
                output += "■";
            }
            for (int i = 0; i < 10-output.Length; i++)
            {
                TempOutput += "□";
            }
            return output + TempOutput;
        }

        internal string statsToString()
        {
            string output = $"${Score}\n" +
                            $"Level: {Level} (Experience Bar: {calcNextLevel()})\n" +
                            $"EXP: {Experience}\n\n" +
                            $"Been kissed {kissed} times\n" +
                            $"Been hugged {hugged} times\n" +
                            $"Been punched {punched} times";

            return output;
        }
    }
}
