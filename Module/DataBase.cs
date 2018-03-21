using System;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module
{
    public class DataBase : ModuleBase
    {
        [Command("hug")]
        [Summary("Hugs the specified person")]
        public async Task hug(string person)
        {
            IGuildUser mentioned = Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ElementAt(0)).Result;

            if (!mentioned.Id.Equals(Context.User.Id))
            {
                StaticBase.people.AddStat(mentioned.Id, 1, "hug");
                await ReplyAsync($"Aww. **{mentioned.Username}** got hugged by **{Context.User.Username}**.\n" +
                                 $"They have already been hugged {StaticBase.people.Users[mentioned.Id].hugged} times!");
            }
            else
                await ReplyAsync("Go ahead.");
        }

        [Command("kiss")]
        [Summary("Smooches the specified person")]
        public async Task kiss(string person)
        {
            IGuildUser mentioned = Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ElementAt(0)).Result;

            if (!mentioned.Id.Equals(Context.User.Id))
            {
                StaticBase.people.AddStat(mentioned.Id, 1, "kiss");
                await ReplyAsync($"Hmpf. Cute, I guess? **{mentioned.Username}** got kissed by **{Context.User.Username}**.\n" +
                                 $"They have already been kissed {StaticBase.people.Users[mentioned.Id].kissed} times!");
            }
            else
                await ReplyAsync("That's sad.");
        }

        [Command("punch")]
        [Summary("Fucks the specified person up")]
        public async Task punch(string person)
        {
            IGuildUser mentioned = Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ElementAt(0)).Result;

            if (!mentioned.Id.Equals(Context.User.Id))
            {
                StaticBase.people.AddStat(mentioned.Id, 1, "punch");
                await ReplyAsync($"DAAMN! **{mentioned.Username}** just got fucked up by **{Context.User.Username}**.\n" +
                                 $"That's {StaticBase.people.Users[mentioned.Id].punched} times, they have been fucked up now.");
            }
            else
                await ReplyAsync("Please don't fuck yourself up. That's unhealthy.");
        }
    }
}
