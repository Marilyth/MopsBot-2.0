using System;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MopsBot.Data.Entities;
using MopsBot.Module.Preconditions;

namespace MopsBot.Module
{
    public class DataBase : ModuleBase
    {
        [Command("Hug", RunMode = RunMode.Async)]
        [Summary("Hugs the specified person")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Ratelimit(5, 1, Measure.Hours)]
        public async Task hug([Remainder]SocketGuildUser person)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (!person.Id.Equals(Context.User.Id))
                {
                    await User.ModifyUserAsync(person.Id, x => x.Hugged++);
                    await ReplyAsync($"Aww, **{person.Username}** got hugged by **{Context.User.Username}**.\n" +
                                     $"They have already been hugged {(await User.GetUserAsync(person.Id)).Hugged} times!");
                }
                else
                    await ReplyAsync("Go ahead.");
            }
        }

        [Command("Kiss", RunMode = RunMode.Async)]
        [Summary("Smooches the specified person")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Ratelimit(5, 1, Measure.Hours)]
        public async Task kiss([Remainder]SocketGuildUser person)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (!person.Id.Equals(Context.User.Id))
                {
                    await User.ModifyUserAsync(person.Id, x => x.Kissed++);
                    await ReplyAsync($"Mwaaah, **{person.Username}** got kissed by **{Context.User.Username}**.\n" +
                                     $"They have already been kissed {(await User.GetUserAsync(person.Id)).Kissed} times!");
                }
                else
                    await ReplyAsync("That's sad.");
            }
        }

        [Command("Punch", RunMode = RunMode.Async)]
        [Summary("Punches the specified person")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Ratelimit(5, 1, Measure.Hours)]
        public async Task punch([Remainder]SocketGuildUser person)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (!person.Id.Equals(Context.User.Id))
                {
                    await User.ModifyUserAsync(person.Id, x => x.Punched++);
                    await ReplyAsync($"DAAMN! **{person.Username}** just got punched by **{Context.User.Username}**.\n" +
                                     $"They have been punched {(await User.GetUserAsync(person.Id)).Punched} times.");
                }
                else
                    await ReplyAsync("Please don't punch yourself. That's unhealthy.");
            }
        }


        [Command("GetStats", RunMode = RunMode.Async)]
        [Summary("Returns your or another persons experience and all that stuff")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task GetStats([Remainder]SocketGuildUser user = null)
        {
            using (Context.Channel.EnterTypingState())
            {
                await ReplyAsync("", embed: await (await User.GetUserAsync(user?.Id ?? Context.User.Id)).StatEmbed());
            }
        }

        [Command("GetLeaderboard", RunMode = RunMode.Async)]
        [Summary("Returns an embed, representing the level leaderboard of the current server.\n" +
                 "stat: Can be Punch, Hug, Kiss, Experience, Level or Votepoints (default is Level)\n" +
                 "begin: The index of which to begin the leaderboard (default is 1)\n" +
                 "end: The index of which to end the leaderboard (default is 10)")]
        [Ratelimit(1, 10, Measure.Seconds, RatelimitFlags.GuildwideLimit)]
        public async Task GetLeaderboard(User.Stats stat = User.Stats.Level, uint begin = 1, uint end = 10, bool isGlobal = false)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (begin >= end) throw new Exception("Begin was bigger than, or equal to end.");
                if (begin == 0 || end == 0) throw new Exception("Begin or end was 0.");
                if (end - begin >= 5000) throw new Exception("Range must be smaller than 5000! (performance)");

                long userCount = await User.GetDBUserCount((isGlobal ? null : (ulong?)Context.Guild.Id));

                if (end > userCount)
                    end = (uint)userCount;

                Func<User, double> toSort = null;
                switch (stat)
                {
                    case User.Stats.Experience:
                        toSort = x => x.CharactersSent;
                        break;
                    case User.Stats.Hug:
                        toSort = x => x.Hugged;
                        break;
                    case User.Stats.Kiss:
                        toSort = x => x.Kissed;
                        break;
                    case User.Stats.Punch:
                        toSort = x => x.Punched;
                        break;
                    case User.Stats.Level:
                        toSort = x => x.CalcCurLevelDouble();
                        break;
                    case User.Stats.Votepoints:
                        toSort = x => x.Money;
                        break;
                }

                await ReplyAsync("", embed: await User.GetLeaderboardAsync(isGlobal ? null : (ulong?)Context.Guild.Id, toSort, begin: (int)begin, end: (int)end));
            }
        }

        /*[Command("ranking")]
        [Summary("Returns the top 10 list of level")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task ranking(int limit, string stat = "level")
        {
            
        }*/
    }
}