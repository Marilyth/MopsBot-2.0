using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data.Individual
{
    class Dungeon
    {
        private Random ran = new Random();
        internal Field[,] mapset;
        public int enemyCount, bossCount, treasureCount, wallCount;

        public Dungeon(int lengthX, int lengthY)
        {
            mapset = new Field[lengthX, lengthY];

            for (int x = 0; x < mapset.GetLength(0); x++)
            {
                for (int y = 0; y < mapset.GetLength(1); y++)
                {
                    Field.Type type = Field.Type.Ground;
                    int decision = ran.Next(0, 400);

                    if (decision < 100)
                    {
                        type = Field.Type.Wall;
                        wallCount++;
                    }
                    else if (decision < 380)
                    {
                        type = Field.Type.Ground;
                    }
                    else if (decision < 390)
                    {
                        type = Field.Type.Enemy;
                        enemyCount++;
                    }
                    else if (decision < 395)
                    {
                        type = Field.Type.Boss;
                        bossCount++;
                    }
                    else if (decision < 400)
                    {
                        type = Field.Type.Treasure;
                        treasureCount++;
                    }

                    mapset[x, y] = new Field(x, y, type);
                }
            }
        }
    }
}