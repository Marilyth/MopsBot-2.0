using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace MopsBot.Data.Updater
{
    public class IdleDungeon
    {
        public Discord.IUserMessage updateMessage;
        public Random ran;
        public string username, log;
        public int vitality, attack, length, gold;
        public ulong player;
        public System.Diagnostics.Stopwatch time;
        public System.Threading.Timer timer;
        public IdleDungeon(Discord.IUserMessage pUpdateMessage, ulong ID, int pLength)
        {
            username = Program.Client.GetUser(ID).Username;
            log = $"00:00 {username} has entered the dungeon.";
            StaticBase.people.Users[ID].getEquipment(ID);
            player = ID;
            ran = new Random();
            updateMessage = pUpdateMessage;
            timer = new System.Threading.Timer(eventHappened, new System.Threading.AutoResetEvent(false), ran.Next(10000, 60000), 100000);

            vitality = 7;   
            foreach(Individual.Items item in StaticBase.people.Users[ID].equipment){
                attack += item.attack;
                vitality += item.vitality;
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

            if(minute >= length || vitality <= 0){
                if(vitality <= 0)
                    gold = 0;
                StaticBase.people.AddStat(player, gold, "score");
                tempLog += $"{username} left the dungeon.";
                log += tempLog;
                modifyMessage();
                timer.Dispose();
                time.Stop();
                return;
            }

            int Event = ran.Next(1, 21);

            if(Event <= 2){
                    int treasureValue = ran.Next((minute+1)/3, (minute+1)*2);
                    gold += treasureValue;
                    tempLog += $"{username} has found a treasure!\n     +{treasureValue} gold.";
            }

            else if(Event <= 5){
                    Individual.Enemy curEnemy = new Individual.Enemy(new AllEnemies().getEnemy(minute));
                    tempLog += $"{username} encounters a {curEnemy.name}!";
                    while(curEnemy.curHP > 0)
                    {
                        curEnemy.curHP -= attack;
                        tempLog += $"\n      {curEnemy.name} got hit for {attack} damage. -> {curEnemy.curHP}/{curEnemy.HP}";
                        
                        if(curEnemy.curHP > 0)
                        {
                            vitality -= curEnemy.dmg;
                            tempLog += $"\n      {username} got hit for {curEnemy.dmg} damage.";
                        }
                    }
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

            for(int i = 1; i < (time.ElapsedMilliseconds/60000.0)/(length/10.0); i++)
            {
                status += "-";
            }
            status += "⚔";
            while(status.Count() < 10)
            {
                status += "-";
            }

            updateMessage.ModifyAsync(x => x.Content = "```\n" + log + "\n\n\n" + status + $"\nHitpoints: {vitality}\nGold obtained: {gold}```");
        }
    }
}
