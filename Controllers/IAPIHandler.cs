using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Api
{
    public interface IAPIHandler
    {
        Task AddContent(Dictionary<string, string> args);
        Task UpdateContent(Dictionary<string, Dictionary<string, string>> args);
        Task RemoveContent(Dictionary<string, string> args);
        Dictionary<string, object> GetContent(ulong userId, ulong guildId);
    }

    public abstract class BaseAPIContent
    {
        //Defines the ContentScope attributes along with their default values
        public abstract Dictionary<string, object> GetParameters(ulong guildId);

        //Returns self as ContentScope, please define own version
        public virtual object GetAsScope(){ return null;}

        //Returns self as ContentScope, if channel is required. Please define own version
        public abstract object GetAsScope(ulong channelId);

        public abstract void Update(Dictionary<string, Dictionary<string, string>> args);

        //Defines which attributes can be seen/modified, please define own version
        public struct ContentScope{
            public string Id;
        }
    }
}
