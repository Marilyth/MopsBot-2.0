namespace MopsBot.Data.Tracker.APIResults.Chess
{
    public class Blitz
    {
        public int games { get; set; }
        public int rating { get; set; }
        public int rd { get; set; }
        public int prog { get; set; }
        public bool prov { get; set; }
    }

    public class Bullet
    {
        public int games { get; set; }
        public int rating { get; set; }
        public int rd { get; set; }
        public int prog { get; set; }
        public bool prov { get; set; }
    }

    public class Correspondence
    {
        public int games { get; set; }
        public int rating { get; set; }
        public int rd { get; set; }
        public int prog { get; set; }
        public bool prov { get; set; }
    }

    public class Classical
    {
        public int games { get; set; }
        public int rating { get; set; }
        public int rd { get; set; }
        public int prog { get; set; }
        public bool prov { get; set; }
    }

    public class Rapid
    {
        public int games { get; set; }
        public int rating { get; set; }
        public int rd { get; set; }
        public int prog { get; set; }
        public bool prov { get; set; }
    }

    public class Perfs
    {
        public Blitz blitz { get; set; }
        public Bullet bullet { get; set; }
        public Correspondence correspondence { get; set; }
        public Classical classical { get; set; }
        public Rapid rapid { get; set; }
    }

    public class PlayTime
    {
        public int total { get; set; }
        public int tv { get; set; }
    }

    public class Count
    {
        public int all { get; set; }
        public int rated { get; set; }
        public int ai { get; set; }
        public int draw { get; set; }
        public int drawH { get; set; }
        public int loss { get; set; }
        public int lossH { get; set; }
        public int win { get; set; }
        public int winH { get; set; }
        public int bookmark { get; set; }
        public int playing { get; set; }
        public int import { get; set; }
        public int me { get; set; }
    }

    public class LichessUser
    {
        public string id { get; set; }
        public string username { get; set; }
        public bool online { get; set; }
        public Perfs perfs { get; set; }
        public long createdAt { get; set; }
        public long seenAt { get; set; }
        public PlayTime playTime { get; set; }
        public string url { get; set; }
        public int nbFollowing { get; set; }
        public int nbFollowers { get; set; }
        public Count count { get; set; }
        public bool followable { get; set; }
        public bool following { get; set; }
        public bool blocking { get; set; }
        public bool followsYou { get; set; }
    }
}