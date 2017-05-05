using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
#if NET40
    using System.Web.Script.Serialization;
#else
    using Newtonsoft.Json;
#endif

namespace MopsBot.Module.Data.Session
{
    public class TwitchTracker
    {
        private System.Threading.Timer checkForChange;
        private Boolean isOnline;
        internal string name;
        internal Dictionary<ulong, string> ChannelIds;

        public TwitchTracker(string streamerName, ulong pChannel, string notificationText)
        {
            ChannelIds = new Dictionary<ulong, string>();
            ChannelIds.Add(pChannel, notificationText);
            name = streamerName;
            isOnline = false;
            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), 6000, 60000);
        }

        private void CheckForChange_Elapsed(object stateinfo)
        {
            dynamic information = streamerInformation();
            Boolean isStreaming = information["stream"] != null;

            if(isOnline != isStreaming)
            {
                if (isOnline)
                {
                    isOnline = false;
                }
                else
                {
                    isOnline = true;
                    sendTwitchNotification(information);
                }
            }

        }

        private dynamic streamerInformation()
        {
            string query = Task.Run(() => Information.readURL($"https://api.twitch.tv/kraken/streams/{name}?client_id={Program.twitchId}")).Result;

            #if NET40
                var jss = new JavaScriptSerializer();
                dynamic tempDict = jss.Deserialize<dynamic>(query);
            #else
                dynamic tempDict = JsonConvert.DeserializeObject<dynamic>(query);
            #endif
            return tempDict;
        }

        private async void sendTwitchNotification(dynamic streamInformation)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = streamInformation["stream"]["channel"]["status"];
            e.Url = streamInformation["stream"]["channel"]["url"];

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = name;
            author.Url = streamInformation["stream"]["channel"]["url"];
            author.IconUrl = streamInformation["stream"]["channel"]["logo"];
            e.Author = author;

            e.ThumbnailUrl = streamInformation["stream"]["channel"]["logo"];
            e.ImageUrl = $"{streamInformation["stream"]["preview"]["medium"]}?rand={StaticBase.ran.Next(0,99999999)}";

            e.AddInlineField("Spiel", streamInformation["stream"]["game"]);
            e.AddInlineField("Zuschauer", streamInformation["stream"]["viewers"]);

            foreach(var channel in ChannelIds)
            {
                await ((SocketTextChannel)Program.client.GetChannel(channel.Key)).SendMessageAsync(channel.Value, false, e);
            }
        }
    }
}
