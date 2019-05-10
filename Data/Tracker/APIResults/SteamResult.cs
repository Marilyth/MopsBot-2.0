using System.Collections.Generic;

namespace MopsBot.Data.Tracker.APIResults.Steam
{
    public class PlayerSummary
    {
        public string steamid { get; set; }
        public int communityvisibilitystate { get; set; }
        public int profilestate { get; set; }
        public string personaname { get; set; }
        public int lastlogoff { get; set; }
        public string profileurl { get; set; }
        public string avatar { get; set; }
        public string avatarmedium { get; set; }
        public string avatarfull { get; set; }
        public int personastate { get; set; }
        public string realname { get; set; }
        public string primaryclanid { get; set; }
        public int timecreated { get; set; }
        public int personastateflags { get; set; }
        public string gameextrainfo { get; set; }
        public string gameid { get; set; }
    }

    public class UserSummaryResponse
    {
        public List<PlayerSummary> players { get; set; }
    }

    public class UserSummary
    {
        public UserSummaryResponse response { get; set; }
    }

    public class VanityResponse
    {
        public string steamid { get; set; }
        public int success { get; set; }
    }

    public class Vanity
    {
        public VanityResponse response { get; set; }
    }

    public class RecentlyPlayedGame
    {
        public int appid { get; set; }
        public string name { get; set; }
        public int playtime_2weeks { get; set; }
        public int playtime_forever { get; set; }
        public string img_icon_url { get; set; }
        public string img_logo_url { get; set; }
    }

    public class RecentlyPlayedResponse
    {
        public int total_count { get; set; }
        public List<RecentlyPlayedGame> games { get; set; }
    }

    public class RecentlyPlayed
    {
        public RecentlyPlayedResponse response { get; set; }
    }

    public class StatsAchievement
    {
        public string apiname { get; set; }
        public int achieved { get; set; }
        public int unlocktime { get; set; }
    }

    public class PlayerstatsResponse
    {
        public string steamID { get; set; }
        public string gameName { get; set; }
        public List<StatsAchievement> achievements { get; set; }
        public bool success { get; set; }
    }

    public class PlayerStats
    {
        public PlayerstatsResponse playerstats { get; set; }
    }

    public class PercentageAchievement
    {
        public string name { get; set; }
        public double percent { get; set; }
    }

    public class Achievementpercentages
    {
        public List<PercentageAchievement> achievements { get; set; }
    }

    public class AchievementPercentage
    {
        public Achievementpercentages achievementpercentages { get; set; }
    }

    public class GameAchievement
    {
        public string name { get; set; }
        public int defaultvalue { get; set; }
        public string displayName { get; set; }
        public int hidden { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string icongray { get; set; }
    }

    public class GameStat
    {
        public string name { get; set; }
        public int defaultvalue { get; set; }
        public string displayName { get; set; }
    }

    public class AvailableGameStats
    {
        public List<GameAchievement> achievements { get; set; }
        public List<GameStat> stats { get; set; }
    }

    public class Game
    {
        public string gameName { get; set; }
        public string gameVersion { get; set; }
        public AvailableGameStats availableGameStats { get; set; }
    }

    public class GameStats
    {
        public Game game { get; set; }
    }

    public class Achievement{
        public string name { get; set; }
        public int defaultvalue { get; set; }
        public string displayName { get; set; }
        public int hidden { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string icongray { get; set; }
        public double percent { get; set; }
        public string apiname { get; set; }
        public int achieved { get; set; }
        public int unlocktime { get; set; }
    }
}