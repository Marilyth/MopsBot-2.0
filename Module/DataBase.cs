/*using System;
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
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task hug(SocketGuildUser person)
        {
            if (!person.Id.Equals(Context.User.Id))
            {
                StaticBase.people.AddStat(person.Id, 1, "hug");
                await ReplyAsync($"Aww. **{person.Username}** got hugged by **{Context.User.Username}**.\n" +
                                 $"They have already been hugged {StaticBase.people.Users[person.Id].hugged} times!");
            }
            else
                await ReplyAsync("Go ahead.");
        }

        [Command("kiss")]
        [Summary("Smooches the specified person")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task kiss(SocketGuildUser person)
        {
            if (!person.Id.Equals(Context.User.Id))
            {
                StaticBase.people.AddStat(person.Id, 1, "kiss");
                await ReplyAsync($"Hmpf. Cute, I guess? **{person.Username}** got kissed by **{Context.User.Username}**.\n" +
                                 $"They have already been kissed {StaticBase.people.Users[person.Id].kissed} times!");
            }
            else
                await ReplyAsync("That's sad.");
        }

        [Command("punch")]
        [Summary("Fucks the specified person up")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task punch(SocketGuildUser person)
        {
            if (!person.Id.Equals(Context.User.Id))
            {
                StaticBase.people.AddStat(person.Id, 1, "punch");
                await ReplyAsync($"DAAMN! **{person.Username}** just got fucked up by **{Context.User.Username}**.\n" +
                                 $"That's {StaticBase.people.Users[person.Id].punched} times, they have been fucked up now.");
            }
            else
                await ReplyAsync("Please don't fuck yourself up. That's unhealthy.");
        }
    }
}
*/