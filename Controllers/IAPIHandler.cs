using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Api
{
    public interface IAPIHandler
    {
        Task TryAddContent(params string[] args);
        Task TryUpdateContent(string[] newArgs, string[] oldArgs);
        Task TryRemoveContent(params string[] args);
        Dictionary<string, object> GetContent(ulong userId, ulong guildId);
    }

    public abstract class IAPIContent
    {
        //Defines the ContentScope attributes along with their default values
        public abstract Dictionary<string, object> GetParameters(ulong guildId);

        //Returns self as ContentScope, please define own version
        public virtual object GetAsScope(){
            return new ContentScope(){Content = this};
        }

        //Returns self as ContentScope, if channel is required. Please define own version
        public virtual object GetAsScope(ulong channelId){
            return new ContentScope(){Content = this};
        }

        public abstract void Update(params string[] args);

        //Defines which attributes can be seen/modified, please define own version
        public struct ContentScope{
            public IAPIContent Content;
        }
    }
}
