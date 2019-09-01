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
using MopsBot.Data.Entities;

namespace MopsBot.Data.Tracker
{
    [BsonIgnoreExtraElements]
    public class TwitchGroupTracker : BaseUpdatingTracker
    {
        private List<TwitchTracker> trackers;
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, ulong> RankChannels;

        public TwitchGroupTracker() : base()
        {
        }

        public TwitchGroupTracker(string name) : base()
        {
            Name = name;
            FetchTrackers();
            RankChannels = new Dictionary<ulong, ulong>();
            SetTimer(60000);
        }

        public async override void PostInitialisation(object info = null)
        {
            if (RankChannels == null) RankChannels = new Dictionary<ulong, ulong>();
            FetchTrackers();
            if(trackers.Count != 0) 
                SetTimer(60000);
        }

        public void FetchTrackers()
        {
            try{
                var guildTrackers = StaticBase.Trackers[TrackerType.Twitch].GetGuildTrackers(ulong.Parse(Name)).Select(x => x as TwitchTracker).ToList();
                trackers = guildTrackers;
            } catch {
                var guildTrackers = new List<TwitchTracker>();
                trackers = guildTrackers;
            }
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                List<KeyValuePair<string, Tuple<string, int>>> viewers = new List<KeyValuePair<string, Tuple<string, int>>>();
                foreach (var tracker in trackers)
                {
                    if (tracker.IsOnline)
                    {
                        viewers.Add(KeyValuePair.Create(tracker.Name, Tuple.Create(tracker.CurGame, (int)tracker.ViewerGraph.PlotDataPoints.LastOrDefault().Value.Value)));
                    }
                }

                if (viewers.Count > 0)
                {
                    viewers = viewers.OrderByDescending(x => x.Value.Item2).ToList();
                }

                foreach (var channel in ChannelConfig)
                {
                    if (RankChannels.ContainsKey(channel.Key))
                    {
                        var rankUsers = StaticBase.TwitchGuilds[ulong.Parse(Name)].GetUsers(RankChannels[channel.Key]);
                        var role = (Program.Client.GetChannel(channel.Key) as SocketTextChannel).Guild.GetRole(RankChannels[channel.Key]);
                        var embed = createEmbed(viewers.Where(x => rankUsers.Any(y => y.TwitchName.ToLower().Equals(x.Key.ToLower()))).ToList(), role.Name);
                        await OnMajorChangeTracked(channel.Key, embed, (string)channel.Value["Notification"]);
                    }
                    else
                    {
                        var embed = createEmbed(viewers);
                        await OnMajorChangeTracked(channel.Key, embed, (string)channel.Value["Notification"]);
                    }
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error by {Name}", e));
            }
        }

        public Embed createEmbed(List<KeyValuePair<string, Tuple<string, int>>> viewerCount, string tier = "")
        {
            var builderList = new List<EmbedBuilder>();
            builderList.Add(new EmbedBuilder().WithCurrentTimestamp().WithTitle($"TwitchTracker summary {(tier.Equals("") ? "" : $"({tier})")}").WithColor(new Color(0x6441A4))
                                              .WithFooter(x =>
                                              {
                                                  x.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
                                                  x.Text = "TwitchGrouping";
                                              }));

            var currentBuilder = builderList.First();

            if (viewerCount.Count > 0)
            {
                int i = 0;
                foreach (var user in viewerCount)
                {
                    if (++i % 26 == 0)
                    {
                        builderList.Add(new EmbedBuilder().WithCurrentTimestamp().WithTitle("Twitch tracker summary").WithColor(new Color(0x6441A4))
                                                  .WithFooter(x =>
                                                  {
                                                      x.IconUrl = "https://media-elerium.cursecdn.com/attachments/214/576/twitch.png";
                                                      x.Text = "TwitchGrouping";
                                                  }));
                        currentBuilder = builderList.Last();
                    }

                    currentBuilder.AddField($"<:twitch:564798112762429440>", $"**[{string.Join("", user.Key.Take(Math.Min(18, user.Key.Length)))}](https://www.twitch.tv/{user.Key}) | {user.Value.Item2}**\n{string.Join("", user.Value.Item1.Take(Math.Min(25, user.Value.Item1.Length)))}", true);
                }
            }
            else
            {
                currentBuilder.WithDescription("Nobody is streaming :c");
            }

            return builderList.First().Build();
        }
    }
}