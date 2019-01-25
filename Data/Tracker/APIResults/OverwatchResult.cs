using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MopsBot.Data.Tracker.APIResults.Overwatch
{
    public class GeneralStats
    {
        public double offensive_assists_most_in_game { get; set; }
        public double weapon_accuracy_best_in_game { get; set; }
        public double hero_damage_done { get; set; }
        public double medals { get; set; }
        public double kill_streak_best { get; set; }
        public double weapon_accuracy { get; set; }
        public double eliminations { get; set; }
        public double games_lost { get; set; }
        public double all_damage_done { get; set; }
        public double time_spent_on_fire { get; set; }
        public double barrier_damage_done { get; set; }
        public double objective_kills { get; set; }
        public double games_played { get; set; }
        public double hero_damage_done_most_in_game { get; set; }
        public double all_damage_done_most_in_game { get; set; }
        public double eliminations_most_in_game { get; set; }
        public double objective_kills_most_in_game { get; set; }
        public double medals_silver { get; set; }
        public double games_won { get; set; }
        public double final_blows_most_in_game { get; set; }
        public double melee_final_blows { get; set; }
        public double multikill_best { get; set; }
        public double deaths { get; set; }
        public double objective_time_most_in_game { get; set; }
        public double medals_bronze { get; set; }
        public double barrier_damage_done_most_in_game { get; set; }
        public double solo_kills_most_in_game { get; set; }
        public double final_blows { get; set; }
        public double melee_final_blows_most_in_game { get; set; }
        public double solo_kills { get; set; }
        public double all_damage_done_most_in_life { get; set; }
        public double multikill { get; set; }
        public double medals_gold { get; set; }
        public double hero_damage_done_most_in_life { get; set; }
        public double eliminations_per_life { get; set; }
        public double offensive_assists { get; set; }
        public double objective_time { get; set; }
        public double win_percentage { get; set; }
        public double time_spent_on_fire_most_in_game { get; set; }
        public double time_played { get; set; }
        public double eliminations_most_in_life { get; set; }
    }

    public class Hero
    {
        public  GeneralStats general_stats { get; set; }
        public  RollingAverageStats average_stats { get; set; }
    }

    public class HeroStats
    {
        public Hero sombra { get; set; }
        public Hero mei { get; set; }
        public Hero genji { get; set; }
        public Hero ana { get; set; }
        public Hero hanzo { get; set; }
        public Hero tracer { get; set; }
        public Hero torbjorn { get; set; }
        public Hero bastion { get; set; }
        public Hero winston { get; set; }
        public Hero mercy { get; set; }
        public Hero reaper { get; set; }
        public Hero junkrat { get; set; }
        public Hero roadhog { get; set; }
        public Hero soldier76 { get; set; }
        public Hero widowmaker { get; set; }
        public Hero pharah { get; set; }
        public Hero lucio { get; set; }
        public Hero dva { get; set; }
        public Hero zarya { get; set; }
        public Hero doomfist { get; set; }
        public Hero reinhardt { get; set; }
        public Hero symmetra { get; set; }
        public Hero zenyatta { get; set; }
        public Hero mccree { get; set; }
        public Hero orisa { get; set; }
        public Hero moira { get; set; }
        public Hero brigitte { get; set; }
        public Hero wrecking_ball { get; set; }
        public Hero ashe {get; set;}


        public Dictionary<string, Hero> heroesToDict(){
            return new Dictionary<string, Hero>
            {
                {"McCree", mccree},
                {"Doomfist", doomfist},
                {"Genji", genji},
                {"Pharah", pharah},
                {"Reaper", reaper},
                {"Soldier 76", soldier76},
                {"Sombra", sombra},
                {"Tracer", tracer},
                {"Bastion", bastion},
                {"Hanzo", hanzo},
                {"Junkrat", junkrat},
                {"Mei", mei},
                {"Torbjörn", torbjorn},
                {"Widowmaker", widowmaker},
                {"D.Va", dva},
                {"Orisa", orisa},
                {"Reinhardt", reinhardt},
                {"Roadhog", roadhog},
                {"Winston", winston},
                {"Zarya", zarya},
                {"Ana", ana},
                {"Lucio", lucio},
                {"Mercy", mercy},
                {"Moira", moira},
                {"Symmetra", symmetra},
                {"Zenyatta", zenyatta},
                {"Brigitte", brigitte},
                {"Wrecking-Ball", wrecking_ball},
                {"Ashe", ashe}
            };
        }
    }

    public class StatsHeroes
    {
        public HeroStats competitive { get; set; }
        public HeroStats quickplay { get; set; }
    }

    public class GamePlaytime
    {
        public double sombra { get; set; }
        public double mei { get; set; }
        public double ana { get; set; }
        public double hanzo { get; set; }
        public double tracer { get; set; }
        public double doomfist { get; set; }
        public double torbjorn { get; set; }
        public double bastion { get; set; }
        public double winston { get; set; }
        public double mercy { get; set; }
        public double lucio { get; set; }
        public double orisa { get; set; }
        public double junkrat { get; set; }
        public double roadhog { get; set; }
        public double soldier76 { get; set; }
        public double widowmaker { get; set; }
        public double pharah { get; set; }
        public double reaper { get; set; }
        public double dva { get; set; }
        public double zarya { get; set; }
        public double symmetra { get; set; }
        public double reinhardt { get; set; }
        public double zenyatta { get; set; }
        public double moira { get; set; }
        public double genji { get; set; }
        public double mccree { get; set; }
        public double brigitte {get; set; }
        public double wrecking_ball {get; set; }
        public double ashe {get; set;}

        public Dictionary<string, double> heroesToDict(){
            return new Dictionary<string, double>
            {
                {"McCree", mccree},
                {"Doomfist", doomfist},
                {"Genji", genji},
                {"Pharah", pharah},
                {"Reaper", reaper},
                {"Soldier-76", soldier76},
                {"Sombra", sombra},
                {"Tracer", tracer},
                {"Bastion", bastion},
                {"Hanzo", hanzo},
                {"Junkrat", junkrat},
                {"Mei", mei},
                {"Torbjorn", torbjorn},
                {"Widowmaker", widowmaker},
                {"DVa", dva},
                {"Orisa", orisa},
                {"Reinhardt", reinhardt},
                {"Roadhog", roadhog},
                {"Winston", winston},
                {"Zarya", zarya},
                {"Ana", ana},
                {"Lucio", lucio},
                {"Mercy", mercy},
                {"Moira", moira},
                {"Symmetra", symmetra},
                {"Zenyatta", zenyatta},
                {"Brigitte", brigitte},
                {"Wrecking-Ball", wrecking_ball},
                {"Ashe", ashe}
            };
        }
    }

    public class Playtime
    {
        public GamePlaytime competitive { get; set; }
        public GamePlaytime quickplay { get; set; }
        public Dictionary<string, double> merge(){
            var mergedDictionary = quickplay.heroesToDict();
            
            if(competitive != null){
            var toMerge = competitive.heroesToDict();

            foreach(string key in toMerge.Keys)
                mergedDictionary[key] += toMerge[key];
            }

            return mergedDictionary;           
        }
    }

    public class Heroes
    {
        public StatsHeroes stats { get; set; }
        public Playtime playtime { get; set; }
    }
    public class Request
    {
        public int api_ver { get; set; }
        public string route { get; set; }
    }

    public class GameStats
    {
        public double hero_damage_done { get; set; }
        public double medals { get; set; }
        public double kill_streak_best { get; set; }
        public double solo_kills { get; set; }
        public double eliminations { get; set; }
        public double games_lost { get; set; }
        public double healing_done { get; set; }
        public double eliminations_most_in_game { get; set; }
        public double barrier_damage_done { get; set; }
        public double objective_kills { get; set; }
        public double games_played { get; set; }
        public double hero_damage_done_most_in_game { get; set; }
        public double all_damage_done { get; set; }
        public double all_damage_done_most_in_game { get; set; }
        public double solo_kills_most_in_game { get; set; }
        public double medals_silver { get; set; }
        public double defensive_assists_most_in_game { get; set; }
        public double offensive_assists_most_in_game { get; set; }
        public double deaths { get; set; }
        public double objective_time_most_in_game { get; set; }
        public double turret_destroyed_most_in_game { get; set; }
        public double medals_bronze { get; set; }
        public double barrier_damage_done_most_in_game { get; set; }
        public double objective_kills_most_in_game { get; set; }
        public double final_blows { get; set; }
        public double healing_done_most_in_game { get; set; }
        public double medals_gold { get; set; }
        public double turret_destroyed { get; set; }
        public double offensive_assists { get; set; }
        public double objective_time { get; set; }
        public double kpd { get; set; }
        public double final_blows_most_in_game { get; set; }
        public double time_played { get; set; }
        public double defensive_assists { get; set; }
    }

    public class OverallStats
    {
        public int games { get; set; }
        public string tier { get; set; }
        public string rank_image { get; set; }
        public string tier_image {get; set;}
        public int prestige { get; set; }
        public int wins { get; set; }
        public string avatar { get; set; }
        public int comprank { get; set; }
        public int losses { get; set; }
        public int ties { get; set; }
        public double win_rate { get; set; }
        public int level { get; set; }
        public string endorsement_shotcaller { get; set; }
        public string endorsement_sportsmanship { get; set; }
        public string endorsement_teammate { get; set; }
        public int endorsement_level { get; set; }

        public async static Task<int> GetLevelAsync(string name){
            var query = await MopsBot.Module.Information.GetURLAsync($"https://playoverwatch.com/en-us/search/account-by-name/{name.Split("-")[0]}?rand={StaticBase.ran.Next(999999999)}");

            var tmpResult = JsonConvert.DeserializeObject<dynamic>(query);
            foreach(var cur in tmpResult){
                string playerName = cur["urlName"].ToString();
                if(playerName == name){
                    return cur["level"];
                };
            }

            return 0;
        }

    }

    public class AverageStats
    {
    }

    public class RollingAverageStats
    {
        public double barrier_damage_done { get; set; }
        public double objective_kills { get; set; }
        public double deaths { get; set; }
        public double hero_damage_done { get; set; }
        public double all_damage_done { get; set; }
        public double objective_time { get; set; }
        public double final_blows { get; set; }
        public double eliminations { get; set; }
        public double solo_kills { get; set; }
        public double healing_done { get; set; }
    }

    public class Competitive
    {
        public GameStats game_stats { get; set; }
        public OverallStats overall_stats { get; set; }
        public AverageStats average_stats { get; set; }
        public bool competitive { get; set; }
        public RollingAverageStats rolling_average_stats { get; set; }
    }

    public class Quickplay
    {
        public GameStats game_stats { get; set; }
        public OverallStats overall_stats { get; set; }
        public AverageStats average_stats { get; set; }
        public bool competitive { get; set; }
        public RollingAverageStats rolling_average_stats { get; set; }
    }

    public class Stats
    {
        public Competitive competitive { get; set; }
        public Quickplay quickplay { get; set; }
    }

    public class Location
    {
        public Stats stats { get; set; }
        public Heroes heroes {get; set;}
    }

    public class OStatsResult
    {
        public Request _request { get; set; }
        public Location eu { get; set; }
        public object any { get; set; }
        public Location us { get; set; }
        public Location kr { get; set; }

        public Location getNotNull(){
            return eu ?? us ?? kr;
        }
    }
}
