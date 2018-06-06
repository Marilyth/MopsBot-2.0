using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults;
using OxyPlot;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data.Tracker
{
    public abstract class ITracker : IDisposable
    {
        //Avoid ratelimit by placing a gap between all trackers.
        public static int ExistingTrackers = 0;
        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        protected System.Threading.Timer checkForChange;
        public event MainEventHandler OnMajorEventFired;
        public event MinorEventHandler OnMinorEventFired;
        public delegate Task MinorEventHandler(ulong channelID, ITracker self, string notificationText);
        public delegate Task MainEventHandler(ulong channelID, Embed embed, ITracker self, string notificationText = "");
        public HashSet<ulong> ChannelIds;
        public string Name;

        public ITracker(int interval, int gap = 5000)
        {
            ExistingTrackers++;
            ChannelIds = new HashSet<ulong>();
            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false),
                                                                                gap, interval);
            Console.Out.WriteLine($"{DateTime.Now} Started a {this.GetType().Name}");
        }

        public virtual void PostInitialisation()
        {
        }

        protected abstract void CheckForChange_Elapsed(object stateinfo);

        protected async Task OnMajorChangeTracked(ulong channelID, Embed embed, string notificationText = "")
        {
            if (OnMajorEventFired != null)
                await OnMajorEventFired(channelID, embed, this, notificationText);
        }
        protected async Task OnMinorChangeTracked(ulong channelID, string notificationText)
        {
            if (OnMinorEventFired != null)
                await OnMinorEventFired(channelID, this, notificationText);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                checkForChange.Dispose();
            }

            disposed = true;
        }
    }
}
