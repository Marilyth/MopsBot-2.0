using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Data.Individual
{
    public class Treasure
    {
        public Items drop;
        public int gold;

        public Treasure(Items pItem, int g)
        {
            drop = pItem;
            gold = g;
        }
    }
}