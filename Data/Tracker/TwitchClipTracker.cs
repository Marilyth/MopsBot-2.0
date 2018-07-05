using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public uint ViewThreshold;
        public List<DateTime> TrackedClips;
        public TwitchClipTracker() : base(600000, (ExistingTrackers * 2000 + 500) % 600000)
        {
        }

        public TwitchClipTracker(string streamerName) : base(600000)
        {
            Console.Out.WriteLine($"{DateTime.Now} Started TwitchClipTracker for {streamerName}");
            Name = streamerName;
            TrackedClips = new List<DateTime>();
            ChannelMessages = new Dictionary<ulong, string>();
            ViewThreshold = 2;

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
                foreach (var datetime in TrackedClips.ToList())
                {
                    if (datetime.AddMinutes(30) <= DateTime.UtcNow){
                        TrackedClips.Remove(datetime);
                        StaticBase.Trackers["twitchclips"].SaveJson();
                    }
                }

                foreach (Clip clip in clips.clips)
                {
                    var embed = createEmbed(clip);
                    foreach (ulong channel in ChannelMessages.Keys)
                    {
                        await OnMajorChangeTracked(channel, embed, ChannelMessages[channel]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Error] by {Name} at {DateTime.Now}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        private async Task<TwitchClipResult> getClips()
        {
            return await NextPage(Name);
        }

        private async Task<TwitchClipResult> NextPage(string name, TwitchClipResult clips = null, string cursor = "")
        {
            if (clips == null)
            {
                clips = new TwitchClipResult();
                clips.clips = new List<Clip>();
            }
            try
            {
                var acceptHeader = new KeyValuePair<string, string>("Accept", "application/vnd.twitchtv.v5+json");
                string query = await MopsBot.Module.Information.ReadURLAsync($"https://api.twitch.tv/kraken/clips/top?client_id={Program.Config["Twitch"]}&channel={name}&period=day{(!cursor.Equals("") ? $"&cursor={cursor}" : "")}", acceptHeader);

                JsonSerializerSettings _jsonWriter = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var tmpResult = JsonConvert.DeserializeObject<TwitchClipResult>(query, _jsonWriter);
                if (tmpResult.clips != null)
                {
                    foreach (var clip in tmpResult.clips.Where(p => !TrackedClips.Contains(p.created_at) && p.created_at > DateTime.UtcNow.AddMinutes(-30) && p.views >= ViewThreshold))
                    {
                        clips.clips.Add(clip);
                        TrackedClips.Add(clip.created_at);
                        StaticBase.Trackers["twitchclips"].SaveJson();
                    }
                    if (!tmpResult._cursor.Equals(""))
                    {
                        return await NextPage(name, clips, tmpResult._cursor);
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

        private Embed createEmbed(Clip clip)
        {
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

            e.ImageUrl = clip.thumbnails.medium;

            e.AddField("Length", clip.duration + " seconds", true);
            e.AddField("Views", clip.views, true);
            e.AddField("Game", clip.game ?? "Nothing", true);
            e.AddField("Creator", $"[{clip.curator.name}]({clip.curator.channel_url})", true);

            return e.Build();
        }
    }
}