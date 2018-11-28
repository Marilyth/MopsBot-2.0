using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MopsBot.Data.Interactive
{
    public abstract class IUpdater : IDisposable
    {
        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        public event UpdateEventHandler OnUpdateHappened;
        public delegate Task UpdateEventHandler(ulong channelID, EmbedBuilder embed, string messageText="");
        public Dictionary<ulong, ulong> ChannelMessages;
        
        public IUpdater(int interval, bool ran = true){
            ChannelMessages = new Dictionary<ulong, ulong>();
            // Console.WriteLine("\n" + $"{DateTime.Now} Started a {this.GetType().Name}");
        }

        protected async Task OnUpdated(ulong channelID, EmbedBuilder embed, string notificationText=""){
            if(OnUpdateHappened != null)
               await OnUpdateHappened(channelID, embed, notificationText);
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
      
            if (disposing) {
                handle.Dispose();
            }
      
            disposed = true;
        }
    }
}
