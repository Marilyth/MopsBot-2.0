using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data.Individual
{
    class Items
    {
        internal int vitality, attack, dropChance;

        public Items(string item, int dropChance)
        {
            vitality = 0;
            attack = 0;

            switch (item.ToLower())
            {
                case "fists":
                    attack = 1;
                    break;
                case "club":
                    attack = 2;
                    break;
                case "mace":
                    attack = 3;
                    break;
                case "sword":
                    attack = 4;
                    break;
                case "greatsword":
                    attack = 5;
                    vitality = -1;
                    break;
                case "leather armor":
                    vitality = 2;
                    break;
                case "spiked armor":
                    vitality = 2;
                    attack = 1;
                    break;
                case "hard leather armor":
                    vitality = 3;
                    break;
                case "copper armor":
                    vitality = 4;
                    break;
            }
        }
    }
}