using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data
{
    class AllItems
    {
        public Dictionary<string, int> itemValue = new Dictionary<string, int>();
        public Random ran = new Random();

        public AllItems()
        {
            itemValue.Add("Club", 100);
            itemValue.Add("Mace", 500);
            itemValue.Add("sword", 1000);
            itemValue.Add("greatsword", 2500);
            itemValue.Add("leather armor", 200);
            itemValue.Add("spiked armor", 3000);
            itemValue.Add("hard leather armor", 2000);
            itemValue.Add("copper armor", 5000);

            itemValue.OrderBy(x => x.Value);
        }

        public Dictionary<string, int> getEligable(int value)
        {
            return itemValue.Where(x => x.Value <= value).ToDictionary(x => x.Key, x => x.Value);
        }

        public Individual.Items getItem(int value)
        {
            return new Individual.Items(itemValue.First(x => x.Value.Equals(value)).Key);
        }
    }
}