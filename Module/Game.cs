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
            public async Task start(int lenghtInMinutes)
            {
                Discord.IUserMessage updateMessage = Context.Channel.SendMessageAsync("Generating dungeon.").Result;

                Data.Session.IdleDungeon test = new Data.Session.IdleDungeon(updateMessage, StaticBase.people.users.Find(x => x.ID.Equals(Context.User.Id)), lenghtInMinutes);
                StaticBase.dungeonCrawler.Add(test);
            }

            [Command("buy")]
            [Summary("Buy equipment")]
            public async Task buy(int valueOfItem)
            {
                var user = StaticBase.people.users.First(x => x.ID == Context.User.Id);
                if(valueOfItem <= user.Score)
                {
                    user.getEquipment();
                    user.equipment.Add(new Data.AllItems().getItem(valueOfItem));
                    await ReplyAsync($"Successfully purchased **{new Data.AllItems().getItem(valueOfItem).name}**");
                    user.saveEquipment();
                    StaticBase.people.addStat(user.ID, -valueOfItem, "score");
                }
                else
                    await ReplyAsync("Not enough money.");
            }

            [Command("stock")]
            [Summary("See all items you could buy")]
            public async Task stock()
            {
                var eligable = new Data.AllItems().getEligable(StaticBase.people.users.First(x => x.ID == Context.User.Id).Score).Select(x => $"${x.Value}: **{x.Key}**");
                string output = String.Join("\n", eligable);
                await ReplyAsync(output.Count() > 0 ? output : "There is nothing you could buy.");
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
                    await ReplyAsync("Table set up. Woof");
                }
                if (betAmount <= StaticBase.people.users.Find(x => x.ID.Equals(Context.User.Id)).Score && betAmount > 0)
                    await ReplyAsync(StaticBase.blackjack.userJoin(Context.User, betAmount));
                else
                    await ReplyAsync("You can't bet that much.");
            }

            [Command("start")]
            [Summary("Starts the game of Blackjack")]
            public async Task start()
            {
                if(!StaticBase.blackjack.active)
                    await ReplyAsync(StaticBase.blackjack.showCards() + "\n\n" + StaticBase.blackjack.endRound());
            }

            [Command("hit")]
            [Summary("You get another card")]
            public async Task hit()
            {
                if(StaticBase.blackjack.active)
                    await ReplyAsync(StaticBase.blackjack.drawCard(Context.User, true));
            }

            [Command("skip")]
            [Summary("You skip the round")]
            public async Task skip()
            {
                if(StaticBase.blackjack.active)
                    await ReplyAsync(StaticBase.blackjack.skipRound(Context.User));
            }
        }
    }
}