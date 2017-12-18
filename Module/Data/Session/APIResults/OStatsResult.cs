using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MopsBot.Module.Data.Session.APIResults
{
    class OStatsResult
    {
        public OStatsResult _request;
        public GameModes eu;
        public GameModes us;
        public GameModes kr;
    }

    class StatsWrapper
    {
        public Stats quickplay;
        public Stats competitive;
    }

    class GameModes
    {
        public StatsWrapper stats;
    }

    class Stats
    {
        public OverallStats overall_stats;
        public TotalStats game_stats;
        public AverageStats rolling_average_stats;
        public Boolean competitive;
    }

    class OverallStats
    {
        public double win_rate;
        public int level;
        public int prestige;
        public string avatar;
        public int wins;
        public int games;
        public int comprank;
        public int losses;
    }

    class TotalStats
    {
        public double objective_kills;
        public double games_won;
        public double kpd;
        public double objective_kills_most_in_game;
        public double time_spent_on_fire_most_in_game;
        public double healing_done;
        public double defensive_assists;
        public double offensive_assists;
        public double final_blows_most_in_game;
        public double objective_time;
        public double melee_final_blows;
        public double medals;
        public double cards;
        public double multikill_best;
        public double multikills;
        public double defensive_assists_most_in_game;
        public double offensive_assists_most_in_game;
        public double melee_final_blow_most_in_game;
        public double damage_done;
        public double medals_silver;
        public double medals_gold;
        public double healing_done_most_in_game;
        public double environmental_kills;
        public double medals_bronze;
        public double solo_kills;
        public double time_spent_on_fire;
        public double eliminations_most_in_game;
        public double final_blows;
        public double time_played;
        public double environmental_deaths;
        public double solo_kills_most_in_game;
        public double damage_done_most_in_game;
        public double games_played;
        public double eliminations;
        public double objective_time_most_in_game;
        public double deaths;
    }

    class AverageStats
    {
        public double healing_done_avg;
        public double eliminations_avg;
        public double melee_final_blows_avg;
        public double final_blows_avg;
        public double defensive_assists_avg;
        public double damage_done_avg;
        public double deaths_avg;
        public double objective_time_avg;
        public double offensive_assists_avg;
        public double solo_kills_avg;
        public double time_spent_on_fire_avg;
        public double objective_kills_avg;
    }
}
