using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data.Individual
{
    class Enemy
    {
        internal int axisX { get; set; }
        internal int axisY { get; set; }

        internal List<Items> DropList;

        internal int HP, curHP, dmg, exp;

        public Enemy(int x, int y, Boolean boss, string enemy)
        {
            DropList = new List<Items>();

            switch (enemy)
            {
                case "Rat":
                    HP = 2;
                    dmg = 1;
                    exp = 25;
                    break;
                case "Snake":
                    HP = 20;
                    dmg = 3;
                    exp = 50;
                    break;
                case "Bat":
                    HP = 10;
                    dmg = 5;
                    exp = 60;
                    break;
                case "Spider":
                    HP = 100;
                    dmg = 10;
                    exp = 370;
                    break;
                case "Ghost":
                    HP = 30;
                    dmg = 4;
                    exp = 100;
                    break;
                case "Skeleton":
                    HP = 500;
                    dmg = 20;
                    exp = 1800;
                    break;
                case "Phoenix":
                    HP = 200;
                    dmg = 60;
                    exp = 3500;
                    break;
                case "Vampire":
                    HP = 2000;
                    dmg = 100;
                    exp = 13500;
                    break;
                case "Dragon":
                    HP = 50000;
                    dmg = 200;
                    exp = 180000;
                    break;
            }
            curHP = HP;
        }

        public Treasure uponDeath()
        {
            Random ran = new Random();
            Items Drop = null;

            int decider = ran.Next(0, 101);

            foreach (Items possibDrop in DropList)
                if (decider <= possibDrop.dropChance)
                    Drop = possibDrop;

            return new Treasure(Drop, ran.Next(1, dmg));
        }
    }
}