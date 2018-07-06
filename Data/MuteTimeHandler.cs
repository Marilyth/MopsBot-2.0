using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using MopsBot.Data.Tracker;

namespace MopsBot.Data
{
    /// <summary>
    /// A class containing all Timed out people
    /// </summary>
    public class MuteTimeHandler
    {
        //Dictionary of who was timed out in which server, and a tuple of how long and the role to remove.
        public Dictionary<ulong, int> ToUnmute;
        public Dictionary<ulong, ulong> WhereToUnmute;
        public Dictionary<ulong, string> WhatRole;
        private List<System.Threading.Timer> timers;
        public MuteTimeHandler()
        {
            timers = new List<System.Threading.Timer>();
            ToUnmute = new Dictionary<ulong, int>();
            WhereToUnmute = new Dictionary<ulong, ulong>();
            WhatRole = new Dictionary<ulong, string>();
        }

        public void SetTimers(){
            foreach(var cur in ToUnmute){
                timers.Add(new System.Threading.Timer(OnTimerElapsed, new Tuple<ulong, int>(cur.Key, timers.Count), 60000, 60000));
            }
        }

        public void SaveJson()
        {
            string dictAsJson = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//MuteTimerHandler.json", FileMode.Create)))
                write.Write(dictAsJson);
        }

        public async Task AddMute(SocketGuildUser person, ulong guildId, int length, string role){
            await person.AddRoleAsync(Program.Client.GetGuild(guildId).Roles.First(x => x.Name.ToLower().Equals(role.ToLower())));
            
            if(ToUnmute == null)
                ToUnmute = new Dictionary<ulong, int>();
            if(WhereToUnmute == null)
                WhereToUnmute = new Dictionary<ulong, ulong>();
            if(WhatRole == null)
                WhatRole = new Dictionary<ulong, string>();
            
            ToUnmute.Add(person.Id, length);
            WhereToUnmute.Add(person.Id, guildId);
            WhatRole.Add(person.Id, role);
            timers.Add(new System.Threading.Timer(OnTimerElapsed, new Tuple<ulong, int>(person.Id, timers.Count), 60000, 60000));
            
            SaveJson();
        }

        /// <summary>
        /// Event that is called when the Tracker fetches new data containing no Embed
        /// </summary>
        /// <returns>A Task that can be awaited</returns>
        private async void OnTimerElapsed(object stateInfo)
        {
            ulong userID = ((Tuple<ulong, int>)stateInfo).Item1;
            ToUnmute[userID]--;

            if(ToUnmute[userID] <= 0){
                var guild = Program.Client.GetGuild(WhereToUnmute[userID]);
                var role = guild.Roles.First(x => x.Name.ToLower().Equals(WhatRole[userID].ToLower()));
                await (Program.Client.GetGuild(WhereToUnmute[userID])).GetUser(userID).RemoveRoleAsync(role);
                ToUnmute.Remove(userID);
                WhereToUnmute.Remove(userID);
                WhatRole.Remove(userID);
                timers[((Tuple<ulong, int>)stateInfo).Item2].Dispose();
            }

            SaveJson();
        }
    }
}
