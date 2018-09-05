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
        [Group("Fight")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public class FightGame : ModuleBase
        {
            [Command("Start")]
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

            [Command("Write")]
            [Summary("Lists all available enemies, or specific information on one enemy.")]
            public async Task write(){
                Data.Entities.ItemMove moveTest = new Data.Entities.ItemMove(){Name = "Slash", DamageModifier = 1, RageConsumption = 0, DefenceModifier = 1};
                Data.Entities.Item test = new Data.Entities.Item(){Id = 1, Name = "Sword", BaseDamage = 2, BaseDefence = 1, Moveset = new List<Data.Entities.ItemMove>(){moveTest}};
                Data.Entities.Enemy enemyTest = new Data.Entities.Enemy(){Name = "Skeleton", Damage = 2, Defence = 1, Health = 20, MoveList = new List<Data.Entities.ItemMove>(){moveTest}, LootChance = new Dictionary<int, int>(){{1, 50}}};

                StaticBase.Database.GetCollection<Data.Entities.Item>("Items").InsertOne(test);
                StaticBase.Database.GetCollection<Data.Entities.Enemy>("Enemies").InsertOne(enemyTest);
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