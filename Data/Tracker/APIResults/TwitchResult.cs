using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MopsBot.Data.Tracker.APIResults.Twitch
{
    public class TwitchResult
    {
        public Stream stream;
    }

    public class Host
    {
        public int host_id { get; set; }
        public int target_id { get; set; }
        public string host_login { get; set; }
        public string target_login { get; set; }
        public string host_display_name { get; set; }
        public string target_display_name { get; set; }

        public bool IsHosting() => target_id != 0;
    }

    public class HostObject
    {
        public List<Host> hosts { get; set; }
    }

    public class Stream
    {
        public ulong _id;
        public int video_height;
        public double average_fps;
        public double delay;
        public string created_at;
        public bool is_playlist;

        public string game;
        public int viewers;
        public Channel channel;
        public Preview preview;
    }

    public class Channel
    {
        public string game;
        public bool mature;
        public string status;
        public string broadcaster_language;
        public string display_name;
        public string language;
        public ulong _id;
        public string name;
        public string created_at;
        public string updated_at;
        public bool partner;
        public string logo;
        public string video_banner;
        public string profile_banner;
        public string profile_banner_background_color;
        public string url;
        public int views;
        public int followers;

    }
    public class Preview
    {
        public string small;
        public string medium;
        public string large;
        public string template;

    }
}
