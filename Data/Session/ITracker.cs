using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Session.APIResults;
using OxyPlot;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data.Session
{
    public abstract class ITracker : IDisposable
    {
        public event MainEventHandler OnMajorEventFired;
        public event MinorEventHandler OnMinorEventFired;
        public delegate Task MinorEventHandler(ulong channelID, ITracker self, string notificationText);
        public delegate Task MainEventHandler(ulong channelID, EmbedBuilder embed, ITracker self, string notificationText="");
        public HashSet<ulong> ChannelIds;
        
        protected abstract void CheckForChange_Elapsed(object stateinfo);

        protected async void OnMajorChangeTracked(ulong channelID, EmbedBuilder embed, string notificationText=""){
            if(OnMajorEventFired != null)
               await OnMajorEventFired(channelID, embed, this, notificationText);
        }
        protected async void OnMinorChangeTracked(ulong channelID, string notificationText){
            if(OnMinorEventFired != null)
               await OnMinorEventFired(channelID, this, notificationText);
        }

        public abstract string[] GetInitArray();

        public abstract void Dispose();

        protected abstract void Dispose(bool disposing);
    }
}
