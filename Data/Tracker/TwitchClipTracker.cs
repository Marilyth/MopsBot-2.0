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
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, DateTime> MatchedClips = new Dictionary<string, DateTime>();
        public static readonly string VIEWTHRESHOLD = "ViewerThreshold";
        public ulong TwitchId;
        public TwitchClipTracker() : base()
        {
        }

        public TwitchClipTracker(string streamerName) : base()
        {
            Name = streamerName;
            MatchedClips = new Dictionary<string, DateTime>();

            try
            {
                TwitchId = TwitchTracker.GetIdFromUsername(streamerName).Result;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Streamer {TrackerUrl()} could not be found on Twitch!", e);
            }
        }

        public async override void PostInitialisation(object info = null)
        {
            if(MatchedClips is null) MatchedClips = new Dictionary<string, DateTime>();
        }

        public async override void PostChannelAdded(ulong channelId)
        {
            base.PostChannelAdded(channelId);
            ChannelConfig[channelId][VIEWTHRESHOLD] = (uint)2;
        }
        
        public async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                TwitchClipResult clips = await getClips();
                foreach (var clipId in MatchedClips.Keys.ToList())
                {
                    if (MatchedClips[clipId].AddMinutes(30) <= DateTime.UtcNow){
                        MatchedClips.Remove(clipId);
                        await UpdateTracker();
                    }
                }

                foreach (TwitchClipInfo clip in clips?.data ?? new List<TwitchClipInfo>())
                {
                    var embed = createEmbed(clip);
                    foreach (ulong channel in ChannelConfig.Keys.ToList())
                    {
                        if(clip.view_count >= (uint)ChannelConfig[channel][VIEWTHRESHOLD])
                            await OnMajorChangeTracked(channel, embed, (string)ChannelConfig[channel]["Notification"]);
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
                clips.data = new List<TwitchClipInfo>();
            }
            try
            {
                if(TwitchId == 0){
                    TwitchId = await TwitchTracker.GetIdFromUsername(name);
                    await UpdateTracker();
                }

                var tmpResult = await FetchJSONDataAsync<TwitchClipResult>($"https://api.twitch.tv/helix/clips?broadcaster_id={TwitchId}&first=100&started_at={JsonConvert.SerializeObject(DateTime.UtcNow.AddDays(-1)).Replace("\"", "")}{(!cursor.Equals("") ? $"&after={cursor}" : "")}", TwitchTracker.GetHelixHeaders());

                if (tmpResult.data != null)
                {
                    foreach (var clip in tmpResult.data.Where(p => !MatchedClips.ContainsKey(p.id) && p.created_at > DateTime.UtcNow.AddMinutes(-30)))
                    {
                        MatchedClips.Add(clip.id, clip.created_at);
                        clips.data.Add(clip);
                        
                        await UpdateTracker();
                    }
                    if (!tmpResult.pagination.cursor?.Equals("") ?? false)
                    {
                        return await NextPage(name, clips, tmpResult.pagination.cursor);
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

        private Embed createEmbed(TwitchClipInfo clip)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Color = new Color(0x6441A4);
            e.Title = clip.title;
            e.Url = clip.url;
            e.Timestamp = clip.created_at;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = Name;
            author.Url = $"https://www.twitch.tv/{clip.broadcaster_name}";
            author.IconUrl = TwitchTracker.GetBroadcasterLogoUrl(clip.broadcaster_id).Result;
            e.Author = author;

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
            footer.Text = "Twitch";
            e.Footer = footer;

            e.ImageUrl = clip.thumbnail_url;

            string game = clip.game_id?.Length > 0 ? GetGameById(clip.game_id).Result : "Unknown";
            e.AddField("Length", clip.duration + " seconds", true);
            e.AddField("Views", clip.view_count, true);
            e.AddField("Game", (clip.game_id == null || game.Equals("")) ? "Nothing" : game, true);
            e.AddField("Creator", $"[{clip.creator_name}](https://www.twitch.tv/{clip.creator_name})", true);

            return e.Build();
        }

        private static Dictionary<string, string> GameNameCache = new Dictionary<string, string>();
        public async static Task<string> GetGameById(string gameId){
            if(!GameNameCache.ContainsKey(gameId)) GameNameCache[gameId] = (await FetchJSONDataAsync<TwitchGameResult>($"https://api.twitch.tv/helix/games?id={gameId}", TwitchTracker.GetHelixHeaders())).data.First().name;
            return GameNameCache[gameId];
        }

        public override string TrackerUrl(){
            return $"https://www.twitch.tv/{Name}/clips";
        }

        public override async Task UpdateTracker(){
            await StaticBase.Trackers[TrackerType.TwitchClip].UpdateDBAsync(this);
        }
    }
}
