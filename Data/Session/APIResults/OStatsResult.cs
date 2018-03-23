using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MopsBot.Data.Session.APIResults
{
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
            return eu != null ? eu : kr != null ? kr : us;
        }
    }
}
