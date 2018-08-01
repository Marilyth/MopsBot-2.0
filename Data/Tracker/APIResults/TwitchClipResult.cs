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

    public class Clip
    {
        public string slug { get; set; }
        public string tracking_id { get; set; }
        public string url { get; set; }
        public string embed_url { get; set; }
        public string embed_html { get; set; }
        public Broadcaster broadcaster { get; set; }
        public Curator curator { get; set; }
        public Vod vod { get; set; }
        public string broadcast_id { get; set; }
        public string game { get; set; }
        public string language { get; set; }
        public string title { get; set; }
        public int views { get; set; }
        public double duration { get; set; }
        public DateTime created_at { get; set; }
        public Thumbnails thumbnails { get; set; }
    }

    public class TwitchClipResult
    {
        public List<Clip> clips { get; set; }
        public string _cursor { get; set; }
    }
}