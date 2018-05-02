using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace MopsBot.Data
{
    public class Giveaway
    {
        public Dictionary<string, HashSet<ulong>> Giveaways = new Dictionary<string, HashSet<ulong>>();

        public Giveaway(){
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//Giveaways.json", FileMode.OpenOrCreate)))
            {
                try{
                    Giveaways = JsonConvert.DeserializeObject<Dictionary<string, HashSet<ulong>>>(read.ReadToEnd());
                } catch(Exception e){
                    Console.WriteLine(e.Message + e.StackTrace);
                }
            }
            Giveaways = Giveaways ?? new Dictionary<string, HashSet<ulong>>();
        }

        public void SaveJson(){
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//Giveaways.json", FileMode.Create)))
                write.Write(JsonConvert.SerializeObject(Giveaways, Formatting.Indented));
        }

        public void AddGiveaway(string name){
            name = name.ToLower();

            if(!Giveaways.ContainsKey(name)){
                Giveaways.Add(name, new HashSet<ulong>());
                SaveJson();
            }

            else
                throw new Exception("A Giveaway with the same name already exists.\nPlease try another name.");
        }

        public void JoinGiveaway(string name, ulong id){
            if(Giveaways.ContainsKey(name)){
                Giveaways[name].Add(id);
                SaveJson();
            }

            else
                throw new Exception("The Giveaway does not seem to exist.");
        }

        public ulong DrawGiveaway(string name){
            name = name.ToLower();

            if(Giveaways.ContainsKey(name)){
                if(Giveaways[name].Count > 1){
                    ulong toReturn = Giveaways[name].ToList()[StaticBase.ran.Next(1, Giveaways[name].Count)];
                    Giveaways.Remove(name);
                    SaveJson();
                    return toReturn;
                }
                else{
                    Giveaways.Remove(name);
                    SaveJson();
                    throw new Exception("There was nobody to draw. Deleting Giveaway still.");
                }
            }

            throw new Exception("The Giveaway does not exist.");
        }
    }
}