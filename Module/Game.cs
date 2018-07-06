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
    public class Game : ModuleBase
    {
        [Group("dungeon")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Dungeon : ModuleBase
        {
            [Command("start")]
            [Summary("Crawl through a dungeon")]
            public async Task start(uint lengthInMinutes)
            {
                if(lengthInMinutes > 0){
                    Discord.IUserMessage updateMessage = await Context.Channel.SendMessageAsync("Generating dungeon.");

                    Data.Updater.IdleDungeon test = new Data.Updater.IdleDungeon(updateMessage, Context.User.Id, (int)lengthInMinutes);
                    StaticBase.DungeonCrawler.Add(test);
                }
                else
                    await ReplyAsync("Fuck you.");
            }

            [Command("buy")]
            [Summary("Buy equipment")]
            public async Task buy(int valueOfItem)
            {
                var user = StaticBase.people.Users[Context.User.Id];
                if(valueOfItem <= user.Score)
                {
                    user.getEquipment(Context.User.Id);
                    user.equipment.Add(new Data.AllItems().getItem(valueOfItem));
                    await ReplyAsync($"Successfully purchased **{new Data.AllItems().getItem(valueOfItem).name}**");
                    user.saveEquipment(Context.User.Id);
                    StaticBase.people.AddStat(Context.User.Id, -valueOfItem, "score");
                }
                else
                    await ReplyAsync("Not enough money.");
            }

            [Command("stock")]
            [Summary("See all items you could buy")]
            public async Task stock()
            {
                var eligable = new Data.AllItems().getEligable(StaticBase.people.Users[Context.User.Id].Score).Select(x => $"${x.Value}: **{x.Key}**");
                string output = String.Join("\n", eligable);
                await ReplyAsync(output.Count() > 0 ? output : "There is nothing you could buy.");
            }
        }

        [Group("salad")]
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
        }
    }
}
