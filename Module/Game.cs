using System;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MopsBot.Module
{
    public class Game : ModuleBase
    {
        [Group("RPG")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public class RPG : ModuleBase
        {
            [Command("Fight")]
            [Summary("Starts a fight with the specified Enemy.")]
            public async Task Start([Remainder]string enemy){
                var message = await ReplyAsync("Generating fight.");
                new Data.Updater.Fight(Context.User.Id, enemy, message);
            }

            [Command("Enemies")]
            [Summary("Lists all available enemies, or specific information on one enemy.")]
            public async Task Enemies([Remainder]string enemy = null){
                if(enemy == null)
                    await ReplyAsync(string.Join(", ", StaticBase.Database.GetCollection<Data.Entities.Enemy>("Enemies").FindSync(x => true).ToList().Select(y => y.Name)));
                else
                    await ReplyAsync("", embed: StaticBase.Database.GetCollection<Data.Entities.Enemy>("Enemies").FindSync(x => x.Name.Equals(enemy)).First().StatEmbed());
            }
        }

        /*[Group("salad")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public class Salad : ModuleBase
        {
            [Command("create")]
            [Summary("Creates a letter salad consisting of the parameter words.\nrandom to use Randomly generated english words.")]
            public async Task start([Remainder] string words)
            {
                string[] wordArray = words.Split(" ");
                StaticBase.Crosswords = new MopsBot.Data.Updater.Crosswords(wordArray);
                StaticBase.Crosswords.setToUpdate(await ReplyAsync(StaticBase.Crosswords.drawMap()));
            }
            [Command("guess")]
            [Summary("Guess a word.")]
            public async Task guess(string guess)
            {
                StaticBase.Crosswords.guessWord(Context.User.Id, guess);
                await Context.Message.DeleteAsync();
            }
        }*/
    }
}