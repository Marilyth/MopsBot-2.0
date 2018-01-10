using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MopsBot.Module.Data.Individual
{
    class User
    {
        public int Score, Experience, punched, hugged, kissed;
        public List<Items> equipment;

        public User(int userScore, int XP, int punch, int hug, int kiss)
        {
            Score = userScore;
            Experience = XP;
            punched = punch;
            hugged = hug;
            kissed = kiss;
        }

        public User(int userScore, int XP)
        {
            Score = userScore;
            Experience = XP;
        }

        public User(int userScore)
        {
            Score = userScore;
        }

        private delegate int del(int i);
        private del levelCalc = x => (200 * (x * x));

        public int calcLevel()
        {
            int i = 0;
            while (Experience >= levelCalc(i))
            {
                i++;
            }
            return (i - 1);
        }
        
        public string calcNextLevel()
        {
            int Level = calcLevel();
            double expCurrentHold = Experience - levelCalc(Level);
            string output = "", TempOutput = "";
            double diffExperience = levelCalc(Level + 1) - levelCalc(Level);
            for (int i = 0; i < Math.Floor(expCurrentHold / (diffExperience / 10)); i++)
            {
                output += "■";
            }
            for (int i = 0; i < 10 - output.Length; i++)
            {
                TempOutput += "□";
            }
            return output + TempOutput;
        }

        public string statsToString()
        {
            string output = $"${Score}\n" +
                            $"Level: {calcLevel()} (Experience Bar: {calcNextLevel()})\n" +
                            $"EXP: {Experience}\n\n" +
                            $"Been kissed {kissed} times\n" +
                            $"Been hugged {hugged} times\n" +
                            $"Been punched {punched} times";

            return output;
        }

        public void getEquipment(ulong ID)
        {
            equipment = new List<Items>();

            StreamReader read = new StreamReader(new FileStream("data//dungeonItems.txt", FileMode.OpenOrCreate));

            string fs = "";
            string fullFile = read.ReadToEnd();

            if (!fullFile.Contains(ID.ToString()))
            {
                equipment.Add(new Items("Fists"));
                saveEquipment(ID);
            }

            read.Dispose();
            read = new StreamReader(new FileStream("data//dungeonItems.txt", FileMode.OpenOrCreate));

            while (!(fs = read.ReadLine()).Contains(ID.ToString()))
            {

            }

            string[] items = fs.Split(':');
            items = items.Skip(1).ToArray();

            foreach (string item in items)
                equipment.Add(new Items(item));

            read.Dispose();
        }

        public void saveEquipment(ulong ID)
        {
            List<string> allLines = File.ReadAllLines("data//dungeonItems.txt").ToList();
            bool hasLine = false;

            for (int i = 0; i < allLines.Count; i++)
                if (allLines[i].Contains(ID.ToString()))
                {
                    hasLine = true;
                    allLines[i] = $"{ID}:{String.Join(":", equipment.Select(x => x.name))}";
                }

            if (!hasLine)
                allLines.Add($"{ID}:{String.Join(":", equipment.Select(x => x.name))}");


            File.WriteAllLines("data//dungeonItems.txt", allLines);
        }
    }
}
