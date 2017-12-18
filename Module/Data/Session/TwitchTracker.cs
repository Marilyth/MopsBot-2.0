using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Module.Data.Session.APIResults;

namespace MopsBot.Module.Data.Session
{
    public class TwitchTracker
    {
        private System.Threading.Timer checkForChange;
        public Dictionary<ulong, Discord.IUserMessage> toUpdate;
        public Boolean isOnline;
        public string name, curGame;
        public Dictionary<ulong, string> ChannelIds;
        

        public TwitchTracker(string streamerName, ulong pChannel, string notificationText, Boolean pIsOnline, string pGame)
        {
            toUpdate = new Dictionary<ulong, IUserMessage>();
            ChannelIds = new Dictionary<ulong, string>();
            ChannelIds.Add(pChannel, notificationText);
            name = streamerName;
            isOnline = pIsOnline;
            curGame = pGame;

            checkForChange = new System.Threading.Timer(CheckForChange_Elapsed, new System.Threading.AutoResetEvent(false), StaticBase.ran.Next(6,59)*1000, 60000);
        }

        private void CheckForChange_Elapsed(object stateinfo)
        {
            TwitchResult information;
            try{
                information = streamerInformation();
            }catch{
                return;
            }
            if(information == null) return;
            Boolean isStreaming = information.stream != null;

            if(isOnline != isStreaming)
            {
                if (isOnline)
                {
                    isOnline = false;
                }
                else
                {
                    isOnline = true;
                    toUpdate = new Dictionary<ulong, IUserMessage>();
                    curGame = (information.stream == null) ? "Nothing" : information.stream.game;
                    sendTwitchNotification(information);

                }
                StaticBase.streamTracks.writeList();
            }

            if(isOnline)
                sendTwitchNotification(information);
                if(information.stream != null && curGame.CompareTo(information.stream.game) != 0 && !information.stream.game.Equals("")){
                    curGame = information.stream.game;

                    foreach(var channel in ChannelIds)
                        ((SocketTextChannel)Program.client.GetChannel(channel.Key)).SendMessageAsync($"{name} spielt jetzt **{curGame}**!");
                    
                    StaticBase.streamTracks.writeList();
                }
        }

        private TwitchResult streamerInformation()
        {
            string query = Information.readURL($"https://api.twitch.tv/kraken/streams/{name}?client_id={Program.twitchId}");
            
            JsonSerializerSettings _jsonWriter = new JsonSerializerSettings {
                                 NullValueHandling = NullValueHandling.Ignore
                             };

            return JsonConvert.DeserializeObject<TwitchResult>(query, _jsonWriter);
        }

        private async void sendTwitchNotification(TwitchResult streamInformation)
        {
            Channel streamer = streamInformation.stream.channel;

            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = streamer.status;
            e.Url = streamer.url;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = name;
            author.Url = streamer.url;
            author.IconUrl = streamer.logo;
            e.Author = author;

            e.ThumbnailUrl = streamer.logo;
            e.ImageUrl = $"{streamInformation.stream.preview.medium}?rand={StaticBase.ran.Next(0,99999999)}";

            e.AddInlineField("Spiel", (streamer.game=="")?"no Game":streamer.game);
            e.AddInlineField("Zuschauer", streamInformation.stream.viewers);

            foreach(var channel in ChannelIds)
            {
                if(!toUpdate.ContainsKey(channel.Key)){
                    toUpdate.Add(channel.Key, ((SocketTextChannel)Program.client.GetChannel(channel.Key)).SendMessageAsync(channel.Value, false, e).Result);
                    StaticBase.streamTracks.writeList();
                }
                    
                else    
                    await toUpdate[channel.Key].ModifyAsync(x => {
                        x.Content = channel.Value;
                        x.Embed = (Embed)e;
                    });
            }
        }
    }
}
