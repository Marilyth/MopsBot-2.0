using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using Discord;

namespace MopsBot.Data
{
    /// <summary>
    /// A class containing all Updaters
    /// </summary>

    public class UpdaterHander<T> where T: Updater.IUpdater
    {
        private Dictionary<String, T> updaters;

        public UpdaterHander(){
            updaters = new Dictionary<String, T>();
        }

        public void addUpdater(string key, T updater){
            updaters.Add(key, updater);
        }

        public void removeUpdater(string key){
            T item = updaters.GetValueOrDefault(key);
            updaters.Remove(key);
            item.Dispose();
        }

    }
}   