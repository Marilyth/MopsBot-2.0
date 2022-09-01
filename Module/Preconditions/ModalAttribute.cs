using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;

namespace MopsBot.Module.Preconditions{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    /// <summary>
    /// Marks a command as containing a modal response. 
    /// The CommandHandler will not automatically respond to modal commands.
    /// </summary>
    public class ModalAttribute : Attribute
    {
    }
}