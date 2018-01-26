using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Data
{
    public class AllEnemies
    {
        public Dictionary<string, int> enemyLevel = new Dictionary<string, int>();
        public Random ran = new Random();

        public AllEnemies()
        {
            enemyLevel.Add("Rat", 0);
            enemyLevel.Add("Snake", 10);
            enemyLevel.Add("Spider", 15);
            enemyLevel.Add("Bat", 20);
            enemyLevel.Add("Skeleton", 25);
            enemyLevel.Add("Phoenix", 30);
            enemyLevel.Add("The Wall", 40);

            enemyLevel.OrderBy(x => x.Value);
        }

        public List<string> getEligable(int level)
        {
            return enemyLevel.Where(x => x.Value <= level && x.Value >= level - 10).ToDictionary(x => x.Key, x => x.Value).Keys.ToList();
        }

        public string getEnemy(int level)
        {
            List<string> enemies = getEligable(level);
            return enemies[ran.Next(0, enemies.Count)];
        }
    }
}
