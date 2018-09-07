using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MongoDB.Driver;

namespace MopsBot.Data.Updater
{
    public class Fight
    {
        private Entities.User User;
        private Entities.Enemy Enemy;
        private IUserMessage Message;
        private int Health, Rage;
        private Entities.Item Weapon;
        private Entities.ItemMove NextEnemyMove;
        private List<string> Log = new List<string>();

        public Fight(ulong userId, string enemyName, IUserMessage message)
        {
            var enemies = StaticBase.Database.GetCollection<Entities.Enemy>("Enemies")
                               .FindSync(x => x.Name.Equals(enemyName)).ToList();
            Message = message;

            if (enemies.Count == 0)
            {
                message.DeleteAsync().Wait();
                throw new ArgumentException($"**Error**: No Enemy called {enemyName} could be found.\n" +
                                            $"Available Enemies are: `{string.Join(", ", StaticBase.Database.GetCollection<Entities.Enemy>("Enemies").FindSync(y => true).ToList().Select(y => y.Name))}`");
            }


            Enemy = enemies.First();
            User = StaticBase.Users.GetUser(userId);
            Weapon = StaticBase.Database.GetCollection<Entities.Item>("Items").FindSync(x => x.Id == User.WeaponId).First();
            Health = User.CalcCurLevel() + Weapon.BaseDefence + 10;

            for(int i = 0; i < Weapon.Moveset.Count; i++){
                int curIndex = i;
                Program.ReactionHandler.AddHandler(Message, (IEmote)ReactionPoll.EmojiDict[i], x => SkillUsed(x, curIndex), false).Wait();
            }

            Message.ModifyAsync(x => {x.Embed = FightEmbed(); x.Content = "";});
        }

        private async Task SkillUsed(ReactionHandlerContext context, int option)
        {
            if(context.Reaction.UserId == User.Id){
                if(Rage >= Weapon.Moveset[option].RageConsumption){
                    Log = new List<string>();

                    Rage -= Weapon.Moveset[option].RageConsumption;
                    Enemy.Rage -= (int)(NextEnemyMove.RageConsumption);

                    var userDamage = (int)(Weapon.Moveset[option].DamageModifier * Weapon.BaseDamage - NextEnemyMove.DefenceModifier * Enemy.Damage + NextEnemyMove.DamageModifier * Enemy.Damage * Weapon.Moveset[option].DeflectModifier);
                    var enemyDamage = (int)(NextEnemyMove.DamageModifier * Enemy.Damage - Weapon.Moveset[option].DefenceModifier * Weapon.BaseDefence + Weapon.Moveset[option].DamageModifier * Weapon.BaseDamage * NextEnemyMove.DeflectModifier);
                    if(enemyDamage < 0) enemyDamage = 0;
                    if(userDamage < 0) userDamage = 0;

                    enemyDamage -= (int)Weapon.Moveset[option].HealthModifier;
                    userDamage -= (int)NextEnemyMove.HealthModifier;

                    if(userDamage >= 0)
                        Log.Add($"You [{Weapon.Moveset[option].Name}] the {Enemy.Name} for {userDamage} damage.");
                    else
                        Log.Add($"The {Enemy.Name} [{NextEnemyMove.Name}] itself for {userDamage * (-1)} health.");
                    if(enemyDamage < 0)
                        Log.Add($"You [{Weapon.Moveset[option].Name}] yourself for {enemyDamage * (-1)} health.");
                    else
                        Log.Add($"The {Enemy.Name} [{NextEnemyMove.Name}] you for {enemyDamage} damage.");

                    Health -= (int)(enemyDamage);
                    Enemy.Health -= userDamage;

                    if(Enemy.Health <= 0){
                        await Program.ReactionHandler.ClearHandler(Message);

                        List<Entities.Item> loot = Enemy.GetLoot();
                        Log = new List<string>();
                        var tmpEnemy = StaticBase.Database.GetCollection<Entities.Enemy>("Enemies").FindSync(x => x.Name == Enemy.Name).First();
                        Log.Add($"You gained {tmpEnemy.Health * tmpEnemy.Damage * 10} Experience");
                        Log.Add($"You gained {tmpEnemy.Health}$");
                        if(loot.Count > 0) Log.Add($"You gained Loot: {string.Join(", ", loot.Select(x => string.Format("[{0}]", x.Name)))}");
                        await User.ModifyAsync(x => {x.Experience += tmpEnemy.Health * tmpEnemy.Damage * 10; 
                                                     x.Inventory = x.Inventory ?? new List<int>();
                                                     x.Inventory.AddRange(loot.Select(y => y.Id));
                                                     x.Money += tmpEnemy.Health;});

                        await Message.ModifyAsync(x => x.Embed = EndEmbed());
                        return;
                    }
                    else if(Health <= 0){
                        await Program.ReactionHandler.ClearHandler(Message);

                        Log = new List<string>();
                        Log.Add($"You died and lost {User.Experience/10} Experience");

                        await Message.ModifyAsync(x => x.Embed = EndEmbed());
                        await User.ModifyAsync(x => x.Experience -= x.Experience/10);
                        return;
                    }

                    await Message.ModifyAsync(x => x.Embed = FightEmbed());
                }
            }
        }

        private Embed FightEmbed()
        {
            NextEnemyMove = Enemy.GetNextMove();
            Log.Add($"The {Enemy.Name} is preparing to use [{NextEnemyMove.Name}]!");

            EmbedBuilder e = new EmbedBuilder();

            e.WithAuthor(Program.Client.GetUser(User.Id).Username, Program.Client.GetUser(User.Id).GetAvatarUrl());
            e.WithThumbnailUrl(Enemy.ImageUrl);
            
            e.AddField("Stats", $"HP: {Health}/{User.CalcCurLevel() + Weapon.BaseDefence + 10}\n" + 
                                $"Rage: {Rage}!!!", true);

            e.AddField("Enemy Stats", $"HP: {Enemy.Health}\n" + 
                                      $"Rage: {Enemy.Rage}!!!", true);

            StringBuilder options = new StringBuilder();
            int i = 0;
            foreach (var move in Weapon.Moveset)
            {   
                options.AppendLine($"{ReactionPoll.EmojiDict[i++]}: {move.ToString(Weapon.BaseDamage, Weapon.BaseDefence, (int)(NextEnemyMove.DamageModifier * Enemy.Damage))}");
            }

            e.AddField("Skills", options, true);
            e.AddField("Enemy Skills", string.Join("\n", Enemy.MoveList.Select(x => x.ToString(Enemy.Damage, Enemy.Defence, 0))), true);

            e.AddField("Log", $"```css\n{string.Join("\n\n", Log)}```");

            return e.Build();
        }

        private Embed EndEmbed(){
            EmbedBuilder e = new EmbedBuilder();

            e.WithAuthor(Program.Client.GetUser(User.Id).Username, Program.Client.GetUser(User.Id).GetAvatarUrl());
            e.WithThumbnailUrl(Enemy.ImageUrl);

            e.AddField("Stats", $"HP: {Health}/{User.CalcCurLevel() + Weapon.BaseDefence + 10}\n" + 
                                $"Rage: {Rage}!!!", true);

            e.AddField("Enemy Stats", $"HP: {Enemy.Health}\n" + 
                                      $"Rage: {Enemy.Rage}!!!", true);

            e.AddField("Log", $"```css\n{string.Join("\n\n", Log)}```");

            return e.Build();
        }
    }
}