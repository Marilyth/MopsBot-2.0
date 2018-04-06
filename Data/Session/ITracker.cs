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
        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        protected System.Threading.Timer checkForChange;
        public event MainEventHandler OnMajorEventFired;
        public event MinorEventHandler OnMinorEventFired;
        public delegate Task MinorEventHandler(ulong channelID, ITracker self, string notificationText);
        public delegate Task MainEventHandler(ulong channelID, EmbedBuilder embed, ITracker self, string notificationText="");
        public HashSet<ulong> ChannelIds;
        
        public ITracker(int interval){
            ChannelIds = new HashSet<ulong>();
            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6,59)*1000, interval);
            Console.Out.WriteLine($"{DateTime.Now} Started a {this.GetType().Name}");
        }

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

        public void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);           
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return; 
      
            if (disposing) {
                handle.Dispose();
                checkForChange.Dispose();
            }
      
            disposed = true;
        }
    }
}
