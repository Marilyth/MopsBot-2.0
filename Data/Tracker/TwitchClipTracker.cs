using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults;
using System.Threading.Tasks;
using System.Xml;

namespace MopsBot.Data.Tracker
{
    public class TwitchClipTracker : ITracker
    {
        public DateTime LastTime;
        public TwitchClipTracker() : base(60000, (ExistingTrackers * 2000+500) % 60000)
        {
        }

        public TwitchClipTracker(string streamerName) : base(60000)
        {
            Console.Out.WriteLine($"{DateTime.Now} Started TwitchClipTracker for {streamerName}");
            Name = streamerName;
            LastTime = DateTime.Now;
            ChannelMessages = new Dictionary<ulong, string>();

            try
            {
                string query = MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/channels/{Name}?client_id={Program.Config["Twitch"]}").Result;
                Channel checkExists = JsonConvert.DeserializeObject<Channel>(query);
                var test = checkExists.broadcaster_language;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Person `{Name}` could not be found on Twitch!");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                TwitchClipResult clips = await getClips();
                if(clips.clips.Count>0){
                    foreach( Clip clip in clips.clips){
                        var embed = createEmbed(clip);
                        foreach(ulong channel in ChannelMessages.Keys){
                            await OnMajorChangeTracked(channel, embed, ChannelMessages[channel]);
                        }
                    }
                }

            }catch(Exception e)
            {
                Console.WriteLine($"[Error] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<TwitchClipResult> getClips()
        {
            return await NextPage(Name);
        }

        private async Task<TwitchClipResult> NextPage(string name, TwitchClipResult clips=null,  string cursor = ""){
            if(clips.Equals(null)){
                clips = new TwitchClipResult();
                clips.clips = new List<Clip>();
            }
            try{
                string query = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/clips/topclient_id={Program.Config["Twitch"]}&channel={name}&limit=100&period=day{(!cursor.Equals("")?$"&cursor={cursor}":"")}");
                JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                TwitchClipResult tmpResult = JsonConvert.DeserializeObject<TwitchClipResult>(query, _jsonWriter);
                if(tmpResult.clips!=null){
                    foreach(var clip in tmpResult.clips.Where(p => p.created_at.CompareTo(LastTime) > 0)){
                        clips.clips.Add(clip);
                    }
                    if(!tmpResult._cursor.Equals("")){
                        return await NextPage(name,clips, tmpResult._cursor);
                    }else{
                        if(clips.clips.Count > 0)
                            LastTime = clips.clips.Max( p => p.created_at);
                    }
                }
                return clips;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new TwitchClipResult();
            }
        }

        private Embed createEmbed(Clip clip){
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = clip.title;
            e.Url = clip.url;
            e.Timestamp = clip.created_at;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            author.Url = clip.broadcaster.channel_url;
            author.IconUrl = clip.broadcaster.logo;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
            footer.Text = "Twitch";
            e.Footer = footer;

            e.ThumbnailUrl  = clip.thumbnails.medium;

            return e.Build();
        }
    }
}