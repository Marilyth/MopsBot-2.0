using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data.Individual
{
    class Treasure
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