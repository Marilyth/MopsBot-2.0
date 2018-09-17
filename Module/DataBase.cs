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
        [Command("Hug")]
        [Summary("Hugs the specified person")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task hug(SocketGuildUser person)
        {
            if (!person.Id.Equals(Context.User.Id))
            {
                await StaticBase.Users.ModifyUserAsync(person.Id, x => x.Hugged++);
                await ReplyAsync($"Aww, **{person.Username}** got hugged by **{Context.User.Username}**.\n" +
                                 $"They have already been hugged {StaticBase.Users.GetUser(person.Id).Hugged} times!");
            }
            else
                await ReplyAsync("Go ahead.");
        }

        [Command("Kiss")]
        [Summary("Smooches the specified person")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task kiss(SocketGuildUser person)
        {
            if (!person.Id.Equals(Context.User.Id))
            {
                await StaticBase.Users.ModifyUserAsync(person.Id, x => x.Kissed++);
                await ReplyAsync($"Mwaaah, **{person.Username}** got kissed by **{Context.User.Username}**.\n" +
                                 $"They have already been kissed {StaticBase.Users.GetUser(person.Id).Kissed} times!");
            }
            else
                await ReplyAsync("That's sad.");
        }

        [Command("Punch")]
        [Summary("Fucks the specified person up")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task punch(SocketGuildUser person)
        {
            if (!person.Id.Equals(Context.User.Id))
            {
                await StaticBase.Users.ModifyUserAsync(person.Id, x => x.Punched++);
                await ReplyAsync($"DAAMN! **{person.Username}** just got fucked up by **{Context.User.Username}**.\n" +
                                 $"That's {StaticBase.Users.GetUser(person.Id).Punched} times, they have been fucked up now.");
            }
            else
                await ReplyAsync("Please don't fuck yourself up. That's unhealthy.");
        }

        
        [Command("GetStats")]
        [Summary("Returns your or another persons experience and all that stuff")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task GetStats(SocketGuildUser user = null)
        {
            await ReplyAsync("", embed: StaticBase.Users.GetUser(user?.Id ?? Context.User.Id).StatEmbed());
        }

        /*[Command("ranking")]
        [Summary("Returns the top 10 list of level")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task ranking(int limit, string stat = "level")
        {
            
        }*/
    }
}