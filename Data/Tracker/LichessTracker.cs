using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MopsBot.Data.Tracker.APIResults.Chess;
using System.Xml;

namespace MopsBot.Data.Tracker
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class LichessTracker : BaseTracker
    {
        public LichessUser PreviousResults;

        public LichessTracker() : base()
        {
        }

        public LichessTracker(string name) : base()
        {
            Name = name;

            //Check if person exists by forcing Exceptions if not.
            try
            {
                PreviousResults = fetchUser().Result;
                var test = PreviousResults.username;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception($"Channel {TrackerUrl()} could not be found on Youtube!\nPerhaps you used the channel-name instead?", e);
            }
        }

        private async Task<LichessUser> fetchUser()
        {
            return await FetchJSONDataAsync<LichessUser>($"https://lichess.org/api/user/{Name}");
        }

        public async Task<(string pgn, string moves)> fetchGamePGN()
        {
            var tmpResult = await MopsBot.Module.Information.GetURLAsync($"https://lichess.org/api/games/user/{Name}?max=1");
            
            var moves = tmpResult.Split("\n").FirstOrDefault(x => x.StartsWith("1."));

            return (tmpResult, moves);
        }

        protected async override void CheckForChange_Elapsed(object stateinfo)
        {
            try
            {
                var newResults = await fetchUser();
                if(PreviousResults.count.win != newResults.count.win || PreviousResults.count.loss != newResults.count.loss){
                    var game = await fetchGamePGN();
                    var embed = await createGameEmbed(game.pgn, game.moves);
                    PreviousResults = newResults;
                    foreach(var channel in ChannelConfig.Keys){
                        await OnMajorChangeTracked(channel, embed, (string)ChannelConfig[channel]["Notification"]);
                    }

                    await UpdateTracker();
                }
            }
            catch (Exception e)
            {
                await Program.MopsLog(new LogMessage(LogSeverity.Verbose, "", $"Found no videos by {Name}"));
            }
        }

        public async Task<Embed> createGameEmbed(string pgn, string moves){
            EmbedBuilder e = new EmbedBuilder();
            e.WithCurrentTimestamp();
            var segments = pgn.Split("\n");
            var white = string.Join("", segments.First(x => x.StartsWith("[White")).Skip(8).SkipLast(2));
            var whiteELO = string.Join("", segments.First(x => x.StartsWith("[WhiteElo")).Skip(11).SkipLast(2));
            var black = string.Join("", segments.First(x => x.StartsWith("[Black")).Skip(8).SkipLast(2));
            var blackElo = string.Join("", segments.First(x => x.StartsWith("[BlackElo")).Skip(11).SkipLast(2));
            var result = string.Join("", segments.First(x => x.StartsWith("[Result")).Skip(9).SkipLast(2));
            var gameId = string.Join("", segments.First(x => x.StartsWith("[Site")).Skip(7).SkipLast(2)).Split("/").Last();
            e.WithTitle($"{white} ({whiteELO}) vs {black} ({blackElo})");
            e.WithDescription(string.Join("", moves.Take(Math.Min(moves.Length, 2048))));
            e.AddField("Result", "White " + result + " Black");
            e.WithColor(255, 255, 255);

            e.WithImageUrl($"https://lichess.org/game/export/png/{gameId}.png");
            e.WithAuthor(new EmbedAuthorBuilder(){
                Name = Name,
                Url = TrackerUrl(),
            });
            e.WithUrl($"https://lichess.org/{gameId}");

            e.WithFooter(new EmbedFooterBuilder(){
                IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/af/Lichess_Logo.svg/2000px-Lichess_Logo.svg.png",
                Text = "Lichess",
            });

            return e.Build();
        }

        public override string TrackerUrl(){
            return PreviousResults.url;
        }

        public override async Task UpdateTracker(){
            await StaticBase.Trackers[TrackerType.Chess].UpdateDBAsync(this);
        }
    }
}