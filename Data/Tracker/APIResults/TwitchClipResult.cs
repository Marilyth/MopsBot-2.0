using System;
using System.Collections.Generic;

namespace MopsBot.Data.Tracker.APIResults.TwitchClip
{
    public class Broadcaster
    {
        public string id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string channel_url { get; set; }
        public string logo { get; set; }
    }

    public class Curator
    {
        public string id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string channel_url { get; set; }
        public string logo { get; set; }
    }

    public class Vod
    {
        public string id { get; set; }
        public string url { get; set; }
        public int offset { get; set; }
        public string preview_image_url { get; set; }
    }

    public class Thumbnails
    {
        public string medium { get; set; }
        public string small { get; set; }
        public string tiny { get; set; }
    }

    public class TwitchClipInfo
    {
        public string id { get; set; }
        public string url { get; set; }
        public string embed_url { get; set; }
        public string broadcaster_id { get; set; }
        public string broadcaster_name { get; set; }
        public string creator_id { get; set; }
        public string creator_name { get; set; }
        public string video_id { get; set; }
        public string game_id { get; set; }
        public string language { get; set; }
        public string title { get; set; }
        public int view_count { get; set; }
        public DateTime created_at { get; set; }
        public string thumbnail_url { get; set; }
        public double duration { get; set; }
    }

    public class Pagination
    {
        public string cursor { get; set; }
    }

    public class TwitchClipResult
    {
        public List<TwitchClipInfo> data { get; set; }
        public Pagination pagination { get; set; }
    }

    public class GameInfo
    {
        public string box_art_url { get; set; }
        public string id { get; set; }
        public string name { get; set; }
    }

    public class TwitchGameResult
    {
        public List<GameInfo> data { get; set; }
        public Pagination pagination { get; set; }
    }
}