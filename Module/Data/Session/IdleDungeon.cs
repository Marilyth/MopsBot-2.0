using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace MopsBot.Module.Data.Session
{
    class IdleDungeon
    {
        public Discord.IUserMessage updateMessage;
        public Individual.Dungeon dungeon;
        public Random ran;
        public string username, log;
        public int vitality, attack, length;
        public Individual.User player;
        public System.Diagnostics.Stopwatch time;
        public System.Threading.Timer timer;
        public IdleDungeon(Discord.IUserMessage pUpdateMessage, Individual.User pUser, int pLength)
        {
            username = Program.client.GetUser(pUser.ID).Username;
            log = $"00:00 {username} has entered the dungeon.";
            player = pUser;
            ran = new Random();
            dungeon = new Individual.Dungeon(20, 20);
            updateMessage = pUpdateMessage;
            timer = new System.Threading.Timer(eventHappened, new System.Threading.AutoResetEvent(false), ran.Next(10000, 60000), 100000);

            foreach(Individual.Items item in player.equipment){
                attack += item.attack;
                vitality += item.vitality + 7;
            }

            length = pLength;

            time = new System.Diagnostics.Stopwatch();
            time.Start();
            modifyMessage();
        }

        public void eventHappened(object obj)
        {
            int minute = (int)(time.ElapsedMilliseconds / 60000);
            int seconds = (int)(time.ElapsedMilliseconds % 60000) / 1000;

            
            string tempLog = $"\n\n{(minute.ToString().Length < 2 ? "0" + minute.ToString() : minute.ToString())}:{(seconds.ToString().Length < 2 ? "0" + seconds.ToString() : seconds.ToString())} ";

            if(minute >= length || vitality <= 1){
                tempLog += $"{username} left the dungeon.";
                log += tempLog;
                modifyMessage();
                timer.Dispose();
                time.Stop();
                return;
            }

            int Event = ran.Next(1, 21);

            if(Event <= 2){
                if(dungeon.treasureCount > 0){
                    int gold = ran.Next(length/3, length*2);
                    tempLog += $"{username} has found a treasure!\n     +{gold} gold.";
                    StaticBase.people.addStat(player.ID, gold, "score");
                }
                dungeon.treasureCount--;
            }

            else if(Event <= 5){
                if(dungeon.enemyCount > 0){
                    Individual.Enemy curEnemy = new Individual.Enemy(Individual.Enemy.enemies[ran.Next(0,4)]);
                    tempLog += $"{username} encounters a {curEnemy.name}!";
                    while(curEnemy.curHP > 0){
                        vitality -= curEnemy.dmg;
                        tempLog += $"\n      {username} got hit for {curEnemy.dmg} damage.";
                        curEnemy.curHP -= attack;
                        tempLog += $"\n      {curEnemy.name} got hit for {attack} damage. -> {curEnemy.curHP}/{curEnemy.HP}";
                    }
                }
                dungeon.enemyCount--;
            }

            else if(Event <= 6){
                vitality -= 1;
                tempLog += $"{username} ran against a wall.\n     -1HP";
            }
            else if (Event <= 20){
                tempLog = "";
            }

            log += tempLog;

            modifyMessage();
            timer.Change(ran.Next(10000, 60000), 100000);
        }

        public void modifyMessage()
        {
            string status = "";

            for(int i = 1; i < time.ElapsedMilliseconds/60000; i++)
            {
                status += "-";
            }
            status += "⚔";
            while(status.Count() < length)
            {
                status += "-";
            }

            updateMessage.ModifyAsync(x => x.Content = "```\n" + log + "\n\n\n" + status + $"\nHitpoints: {vitality}```");
        }
    }
}