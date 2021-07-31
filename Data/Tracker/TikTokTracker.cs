using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Threading.Tasks;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class TikTokTracker : BaseTracker
    {
        public List<string> TrackedVideos;

        public TikTokTracker() : base()
        {
        }

        public TikTokTracker(string name) : base()
        {
            Name = name;
            TrackedVideos = new List<string>();

            try
            {
                (var plainText, var links) = FetchHTMLDataAsync($"https://www.tiktok.com/@{Name}").Result;
                var test = plainText.Select(x => x.Last()).Contains("Followers");
                if(!test) throw new Exception("Could not find any data.");
                TrackedVideos = getClips().Result.Select(x => x.First()).ToList();
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Channel {TrackerUrl()} could not be found on TikTok!", e);
            }
        }
        
        public async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var clips = await getClips();
                var difference = clips.TakeWhile(x => x.First() != TrackedVideos.FirstOrDefault()).ToList();
                TrackedVideos = clips.Select(x => x.First()).ToList();

                foreach (var clip in difference)
                {
                    foreach (ulong channel in ChannelConfig.Keys.ToList())
                    {
                        await OnMinorChangeTracked(channel, (string)ChannelConfig[channel]["Notification"] + $"\n{clip.Last()}\n{clip.First()}");
                    }
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        private async Task<List<List<string>>> getClips()
        {
            return await GetClips(Name);
        }

        public static async Task<List<List<string>>> GetClips(string name){
            var result = await FetchHTMLDataAsync($"https://www.tiktok.com/@{name}", @"(?:{\""id\"":\""(\d+)\"",\""desc\"":\""(.+?)\"")");
            foreach(var clip in result){
                clip[0] = $"https://www.tiktok.com/@{name}" + "/video/" + clip[0];
            }
            return result;
        }

        public override string TrackerUrl(){
            return $"https://www.tiktok.com/@{Name}";
        }

        public override async Task UpdateTracker(){
            await StaticBase.Trackers[TrackerType.TikTok].UpdateDBAsync(this);
        }
    }
}
