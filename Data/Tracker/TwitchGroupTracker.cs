using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MopsBot.Data.Tracker.APIResults.Twitch;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using TwitchLib;
using TwitchLib.Api;

namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public class TwitchGroupTracker : BaseUpdatingTracker
    {
        private List<TwitchTracker> trackers;

        public TwitchGroupTracker() : base()
        {
        }

        public TwitchGroupTracker(string name) : base()
        {
            Name = name;
            FetchTrackers();
            SetTimer(60000);
        }

        public async override void PostInitialisation(object info = null)
        {
            FetchTrackers();
        }

        public void FetchTrackers(){
            //Must be changed for rank roles
            var channels = Program.Client.GetGuild(ulong.Parse(Name)).TextChannels;
            var allTrackers = StaticBase.Trackers[TrackerType.Twitch].GetTrackers().Select(x => x.Value as TwitchTracker).ToList();
            var guildTrackers = allTrackers.Where(x => x.ChannelMessages.Keys.Any(y => channels.Select(z => z.Id).Contains(y))).ToList();
            trackers = guildTrackers;
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                List<KeyValuePair<string, Tuple<string, int>>> viewers = new List<KeyValuePair<string, Tuple<string, int>>>();
                foreach(var tracker in trackers){
                    if(tracker.IsOnline){
                        viewers.Add(KeyValuePair.Create(tracker.Name, Tuple.Create(tracker.CurGame, (int)tracker.ViewerGraph.PlotDataPoints.LastOrDefault().Value.Value)));
                    }
                }

                if(viewers.Count > 0){
                    var embed = createEmbed(viewers);
                    foreach(var channel in ChannelMessages){
                        await OnMajorChangeTracked(channel.Key, embed, channel.Value);
                    }
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        public Embed createEmbed(List<KeyValuePair<string, Tuple<string, int>>> viewerCount)
        {
            var builderList = new List<EmbedBuilder>();
            builderList.Add(new EmbedBuilder().WithCurrentTimestamp().WithTitle("Twitch tracker summary").WithColor(new Color(0x6441A4))
                                              .WithFooter(x => {x.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
                                                                x.Text = "TwitchGrouping";}));

            var currentBuilder = builderList.First();

            int i = 0;
            foreach(var user in viewerCount){
                if(++i % 26 == 0){
                    builderList.Add(new EmbedBuilder().WithCurrentTimestamp().WithTitle("Twitch tracker summary").WithColor(new Color(0x6441A4))
                                              .WithFooter(x => {x.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
                                                                x.Text = "TwitchGrouping";}));
                    currentBuilder = builderList.Last();
                }

                currentBuilder.AddField($"<:twitch:564798112762429440>", $"**[{string.Join("", user.Key.Take(Math.Min(18, user.Key.Length)))}](https://www.twitch.tv/{user.Key}) | {user.Value.Item2}**\n{string.Join("", user.Value.Item1.Take(Math.Min(25, user.Value.Item1.Length)))}", true);
            }

            return builderList.First().Build();
        }
    }
}