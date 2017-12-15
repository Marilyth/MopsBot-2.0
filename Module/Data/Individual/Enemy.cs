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
        private List<Items> DropList;
        public string name;
        public int HP, curHP, dmg, exp;

        public Enemy(string enemy)
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
                    HP = 2;
                    dmg = 2;
                    exp = 50;
                    break;
                case "Bat":
                    HP = 3;
                    dmg = 1;
                    exp = 60;
                    break;
                case "Spider":
                    HP = 4;
                    dmg = 1;
                    exp = 70;
                    break;
                case "Skeleton":
                    HP = 6;
                    dmg = 2;
                    exp = 100;
                    break;
                case "Ghost":
                    HP = 12;
                    dmg = 3;
                    exp = 1800;
                    break;
                case "Phoenix":
                    HP = 17;
                    dmg = 3;
                    exp = 3500;
                    break;
            }
            curHP = HP;
            name = enemy;
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