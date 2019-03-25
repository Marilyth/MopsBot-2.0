using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults.TwitchClip;
using System.Threading.Tasks;
using System.Xml;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class TwitchClipTracker : BaseTracker
    {
        public uint ViewThreshold;
        
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<DateTime, KeyValuePair<int, double>> TrackedClips;
        public TwitchClipTracker() : base()
        {
        }

        public TwitchClipTracker(Dictionary<string, string> args) : base(){
            ViewThreshold = uint.Parse(args["ViewThreshold"]);
            base.SetBaseValues(args, true);

            //Check if Name ist valid
            try{
                new TwitchClipTracker(Name).Dispose();
                TrackedClips = new Dictionary<DateTime, KeyValuePair<int, double>>();
                ChannelMessages = new Dictionary<ulong, string>();
                SetTimer();
            } catch (Exception e){
                this.Dispose();
                throw e;
            }

            if(StaticBase.Trackers[TrackerType.TwitchClip].GetTrackers().ContainsKey(Name)){
                this.Dispose();

                args["Id"] = Name;
                var curTracker = StaticBase.Trackers[TrackerType.TwitchClip].GetTrackers()[Name];
                curTracker.ChannelMessages[ulong.Parse(args["Channel"].Split(":")[1])] = args["Notification"];
                StaticBase.Trackers[TrackerType.TwitchClip].UpdateContent(new Dictionary<string, Dictionary<string, string>>{{"NewValue", args}, {"OldValue", args}}).Wait();

                throw new ArgumentException($"Tracker for {args["_Name"]} existed already, updated instead!");
            }
        }

        public TwitchClipTracker(string streamerName) : base()
        {
            Name = streamerName;
            TrackedClips = new Dictionary<DateTime, KeyValuePair<int, double>>();
            ChannelMessages = new Dictionary<ulong, string>();
            ViewThreshold = 2;

            try
            {
                var checkExists = FetchJSONDataAsync<APIResults.Twitch.Channel>($"https://api.twitch.tv/kraken/channels/{Name}?client_id={Program.Config["Twitch"]}").Result;
                var test = checkExists.broadcaster_language;
                SetTimer();
            }
            catch (Exception)
            {
                Dispose();
                throw new Exception($"Streamer {TrackerUrl()} could not be found on Twitch!");
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                TwitchClipResult clips = await getClips();
                foreach (var datetime in TrackedClips.Keys.ToList())
                {
                    if (datetime.AddMinutes(30) <= DateTime.UtcNow){
                        TrackedClips.Remove(datetime);
                        await StaticBase.Trackers[TrackerType.TwitchClip].UpdateDBAsync(this);
                    }
                }

                foreach (Clip clip in clips.clips)
                {
                    var embed = createEmbed(clip);
                    foreach (ulong channel in ChannelMessages.Keys.ToList())
                    {
                        await OnMajorChangeTracked(channel, embed, ChannelMessages[channel]);
                    }
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
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
                var tmpResult = await FetchJSONDataAsync<TwitchClipResult>($"https://api.twitch.tv/kraken/clips/top?client_id={Program.Config["Twitch"]}&channel={name}&period=day{(!cursor.Equals("") ? $"&cursor={cursor}" : "")}", acceptHeader);

                if (tmpResult.clips != null)
                {
                    foreach (var clip in tmpResult.clips.Where(p => !TrackedClips.ContainsKey(p.created_at) && p.created_at > DateTime.UtcNow.AddMinutes(-30) && p.views >= ViewThreshold))
                    {
                        if(clip.vod != null && !TrackedClips.Any(x => {
                                double matchingDuration = 0;

                                if(clip.vod.offset < x.Value.Key)
                                    matchingDuration = (clip.vod.offset + clip.duration > x.Value.Key + x.Value.Value) ? x.Value.Value : clip.vod.offset + clip.duration - x.Value.Key;
                                else
                                    matchingDuration = (x.Value.Key + x.Value.Value > clip.vod.offset + clip.duration) ? clip.duration : x.Value.Key + x.Value.Value - clip.vod.offset;

                                double matchingPercentage = matchingDuration / clip.duration;
                                return matchingPercentage > 0.2;
                            })){

                            TrackedClips.Add(clip.created_at, new KeyValuePair<int, double>(clip.vod.offset, clip.duration));
                            clips.clips.Add(clip);
                        } else if (clip.vod == null){
                            TrackedClips.Add(clip.created_at, new KeyValuePair<int, double>(-60, clip.duration));
                            clips.clips.Add(clip);
                        }
                        
                        await StaticBase.Trackers[TrackerType.TwitchClip].UpdateDBAsync(this);
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
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
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
            e.AddField("Game", (clip.game == null || clip.game.Equals("")) ? "Nothing" : clip.game, true);
            e.AddField("Creator", $"[{clip.curator.name}]({clip.curator.channel_url})", true);

            return e.Build();
        }

        public override string TrackerUrl(){
            return $"https://www.twitch.tv/{Name}/clips";
        }

        public override Dictionary<string, object> GetParameters(ulong guildId)
        {
            var parentParameters = base.GetParameters(guildId);
            (parentParameters["Parameters"] as Dictionary<string, object>)["ViewThreshold"] = 1;
            return parentParameters;
        }

        public override object GetAsScope(ulong channelId){
            return new ContentScope(){
                Id = this.Name,
                _Name = this.Name,
                Notification = this.ChannelMessages[channelId],
                Channel = "#" + ((SocketGuildChannel)Program.Client.GetChannel(channelId)).Name + ":" + channelId,
                ViewThreshold = this.ViewThreshold
            };
        }

        public override void Update(Dictionary<string, Dictionary<string, string>> args){
            base.Update(args);
            ViewThreshold = uint.Parse(args["NewValue"]["ViewThreshold"]);
        }

        public new struct ContentScope
        {
            public string Id;
            public string _Name;
            public string Notification;
            public string Channel;
            public uint ViewThreshold;
        }
    }
}
