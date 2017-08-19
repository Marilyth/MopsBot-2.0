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

        internal int HP, curHP, dmg, exp;
        public string name;
        public static string[] enemies = new string[]{"Rat","Snake","Bat","Spider","Skeleton"};

        public Enemy(string enemy)
        {
            switch (enemy)
            {
                case "Rat":
                    HP = 2;
                    dmg = 1;
                    break;
                case "Snake":
                    HP = 2;
                    dmg = 2;
                    break;
                case "Bat":
                    HP = 3;
                    dmg = 1;
                    break;
                case "Spider":
                    HP = 3;
                    dmg = 2;
                    break;
                case "Skeleton":
                    HP = 5;
                    dmg = 2;
                    break;
            }
            curHP = HP;
            name = enemy;
        }
    }
}