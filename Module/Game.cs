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
            [Command("Fight", RunMode = RunMode.Async)]
            [Summary("Starts a fight with the specified Enemy.")]
            public async Task Start([Remainder]string enemy){
                var message = await ReplyAsync("Generating fight.");
                try{
                    new Data.Updater.Fight(Context.User.Id, enemy, message);
                } catch(ArgumentException e) {
                    await ReplyAsync(e.Message);
                }
            }

            [Command("Enemies")]
            [Summary("Lists all available enemies, or specific information on one enemy.")]
            public async Task Enemies([Remainder]string enemy = null){
                if(enemy == null)
                    await ReplyAsync(string.Join(", ", StaticBase.Database.GetCollection<Data.Entities.Enemy>("Enemies").FindSync(x => true).ToList().Select(y => string.Format("`{0}`", y.Name))));
                else
                    await ReplyAsync("", embed: StaticBase.Database.GetCollection<Data.Entities.Enemy>("Enemies").FindSync(x => x.Name.Equals(enemy)).First().StatEmbed());
            }

            [Command("Items")]
            [Summary("Lists all items, or specific information on one item.")]
            public async Task Items([Remainder]string item = null){
                if(item == null)
                    await ReplyAsync(string.Join(", ", StaticBase.Database.GetCollection<Data.Entities.Item>("Items").FindSync(x => true).ToList().Select(y => string.Format("`{0}`", y.Name))));
                else
                    await ReplyAsync("", embed: StaticBase.Database.GetCollection<Data.Entities.Item>("Items").FindSync(x => x.Name.Equals(item)).First().ItemEmbed());
            }

            [Command("Inventory")]
            [Summary("Lists all items in your inventory.")]
            public async Task Inventory(){
                await ReplyAsync(string.Join("\n", (StaticBase.Users.GetUser(Context.User.Id).Inventory ?? new List<int>())
                                .Select(x => { var Item = StaticBase.Database.GetCollection<Data.Entities.Item>("Items").FindSync(y => y.Id.Equals(x)).First();
                                                return $"Id: [**{Item.Id}**], {Item.Name}";})));
            }

            [Command("Equip")]
            [Summary("Equip an Item from your inventory.")]
            public async Task Equip(int itemId){
                var User = StaticBase.Users.GetUser(Context.User.Id);
                var Inventory = User.Inventory ?? new List<int>();

                if(Inventory.Contains(itemId)){
                    await User.ModifyAsync(x => {x.Inventory.Remove(itemId);
                                                 x.Inventory.Add(x.WeaponId);
                                                 x.WeaponId = itemId;});
                    await ReplyAsync($"Your equipped the [{itemId}] item and put your old item into your inventory.");
                }

                else
                    await ReplyAsync($"No Item with the Id {itemId} could be found.");
            }

            /*[Command("Sell")]
            [Summary("Sell an Item from your inventory.")]
            public async Task Sell(int itemId){
                var User = StaticBase.Users.GetUser(Context.User.Id);
                var Inventory = User.Inventory ?? new List<int>();

                if(Inventory.Contains(itemId)){
                    await User.ModifyAsync(x => {x.Inventory.Remove(itemId);
                                                 x.Inventory.Add(x.WeaponId);
                                                 x.WeaponId = itemId;});
                    await ReplyAsync($"Your equipped the [{itemId}] item and put your old item into your inventory.");
                }

                else
                    await ReplyAsync($"No Item with the Id {itemId} could be found.");*/
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
        }
    }*/
}