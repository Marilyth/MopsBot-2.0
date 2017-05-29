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
                await ReplyAsync(StaticBase.blackjack.showCards() + "\n\n" + StaticBase.blackjack.endRound());
            }

            [Command("hit")]
            [Summary("You get another card")]
            public async Task hit()
            {
                await ReplyAsync(StaticBase.blackjack.drawCard(Context.User, true));
            }

            [Command("skip")]
            [Summary("You skip the round")]
            public async Task skip()
            {
                await ReplyAsync(StaticBase.blackjack.skipRound(Context.User));
            }
        }
    }
}
