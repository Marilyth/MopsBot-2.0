using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data
{
    class AllEnemies
    {
        public Dictionary<string, int> enemyLevel = new Dictionary<string, int>();
        public Random ran = new Random();

        public AllEnemies()
        {
            enemyLevel.Add("Rat", 1);
            enemyLevel.Add("Snake", 7);
            enemyLevel.Add("Bat", 15);
            enemyLevel.Add("Skeleton", 20);
            enemyLevel.Add("Phoenix", 30);
            enemyLevel.Add("Spider", 5);

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