using System;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module
{
    public class Game : ModuleBase
    {
        [Group("dungeon")]
        public class Dungeon : ModuleBase
        {
            [Command("start")]
            [Summary("Crawl through a dungeon")]
            public async Task start(uint lengthInMinutes)
            {
                if(lengthInMinutes > 0){
                    Discord.IUserMessage updateMessage = await Context.Channel.SendMessageAsync("Generating dungeon.");

                    Data.Session.IdleDungeon test = new Data.Session.IdleDungeon(updateMessage, Context.User.Id, (int)lengthInMinutes);
                    StaticBase.dungeonCrawler.Add(test);
                }
                else
                    await ReplyAsync("Fuck you.");
            }

            [Command("buy")]
            [Summary("Buy equipment")]
            public async Task buy(int valueOfItem)
            {
                var user = StaticBase.people.users[Context.User.Id];
                if(valueOfItem <= user.Score)
                {
                    user.getEquipment(Context.User.Id);
                    user.equipment.Add(new Data.AllItems().getItem(valueOfItem));
                    await ReplyAsync($"Successfully purchased **{new Data.AllItems().getItem(valueOfItem).name}**");
                    user.saveEquipment(Context.User.Id);
                    StaticBase.people.addStat(Context.User.Id, -valueOfItem, "score");
                }
                else
                    await ReplyAsync("Not enough money.");
            }

            [Command("stock")]
            [Summary("See all items you could buy")]
            public async Task stock()
            {
                var eligable = new Data.AllItems().getEligable(StaticBase.people.users[Context.User.Id].Score).Select(x => $"${x.Value}: **{x.Key}**");
                string output = String.Join("\n", eligable);
                await ReplyAsync(output.Count() > 0 ? output : "There is nothing you could buy.");
            }
        }

        [Group("salad")]
        public class Salad : ModuleBase
        {
            [Command("create")]
            [Summary("Creates a letter salad consisting of the parameter words.\nrandom to use Randomly generated english words.")]
            public async Task start([Remainder] string words)
            {
                string[] wordArray = words.Split(" ");
                StaticBase.crosswords = new MopsBot.Data.Session.Crosswords(wordArray);
                await ReplyAsync(StaticBase.crosswords.drawMap());
            }
            [Command("guess")]
            [Summary("Guess the location of a word, by providing the beginning and ending position.\nPosition: HorizontalValue:VerticalValue")]
            public async Task guess(string beginning, string ending)
            {
                await ReplyAsync(StaticBase.crosswords.guessWord(Context.User.Id, beginning + " " + ending));
            }
        }
        
        [Group("Blackjack")]
        public class Blackjack : ModuleBase
        {
            [Command("join")]
            [Summary("Joins a game of blackjack")]
            public async Task join(int betAmount)
            {
                if (StaticBase.blackjack == null || !StaticBase.blackjack.active)
                {
                    StaticBase.blackjack = new Data.Session.Blackjack(Context.Client.CurrentUser);
                    StaticBase.blackjack.toEdit = await ReplyAsync("Table set up. Woof");
                }

                if (betAmount <= StaticBase.people.users[Context.User.Id].Score && betAmount > 0)
                    StaticBase.blackjack.userJoin(Context.User, betAmount);

                else
                    await ReplyAsync("You can't bet that much.");
            }

            [Command("start")]
            [Summary("Starts the game of Blackjack")]
            public Task start()
            {
                if(!StaticBase.blackjack.active)
                    StaticBase.blackjack.start();

                return Task.CompletedTask;
            }

            [Command("hit")]
            [Summary("You get another card")]
            public Task hit()
            {
                if(StaticBase.blackjack.active)
                    StaticBase.blackjack.drawCard(Context.User, true);

                return Task.CompletedTask;
            }

            [Command("skip")]
            [Summary("You skip the round")]
            public Task skip()
            {
                if(StaticBase.blackjack.active)
                    StaticBase.blackjack.skipRound(Context.User);

                return Task.CompletedTask;
            }
        }
    }
}
