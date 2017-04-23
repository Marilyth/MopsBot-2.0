using System;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data.Session
{
    public class TwitchTracker
    {
        private System.Timers.Timer checkForChange;
        private Boolean isOnline;
        internal string name;
        internal List<ulong> ChannelIds;

        public TwitchTracker(string streamerName, ulong pChannel)
        {
            ChannelIds = new List<ulong>();
            ChannelIds.Add(pChannel);
            name = streamerName;
            isOnline = false;
            checkForChange = new System.Timers.Timer(6000);
            checkForChange.Elapsed += CheckForChange_Elapsed;
            checkForChange.Enabled = true;
        }

        private void CheckForChange_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            dynamic information = streamerInformation();
            Boolean isStreaming = information["stream"] != null;

            if(isOnline != isStreaming)
            {
                if (isOnline)
                {
                    isOnline = false;

                    foreach (var channel in ChannelIds)
                    {
                        ((SocketTextChannel)Program.client.GetChannel(channel)).SendMessageAsync($"{name} hat aufgehört zu streamen.");
                    }
                }
                else
                {
                    isOnline = true;
                    sendTwitchNotification(information);
                }
            }

            checkForChange.Interval = 60000;
        }

        private dynamic streamerInformation()
        {
            string query = Task.Run(() => Information.readURL($"https://api.twitch.tv/kraken/streams/{name}?client_id={Program.twitchId}")).Result;

            var jss = new JavaScriptSerializer();
            dynamic tempDict = jss.Deserialize<dynamic>(query);
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
            e.ImageUrl = streamInformation["stream"]["preview"]["medium"];

            e.AddInlineField("Spiel", streamInformation["stream"]["game"]);
            e.AddInlineField("Zuschauer", streamInformation["stream"]["viewers"]);

            foreach(var channel in ChannelIds)
            {
                await ((SocketTextChannel)Program.client.GetChannel(channel)).SendMessageAsync($"{name} streamt gerade!", false, e);
            }
        }
    }
}
