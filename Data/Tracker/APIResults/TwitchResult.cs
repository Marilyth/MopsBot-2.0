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
    public class Commenter
    {
        public string display_name { get; set; }
        public string _id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string bio { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string logo { get; set; }
    }

    public class Emoticon
    {
        public string emoticon_id { get; set; }
        public string emoticon_set_id { get; set; }
    }

    public class Fragment
    {
        public string text { get; set; }
        public Emoticon emoticon { get; set; }
    }

    public class UserBadge
    {
        public string _id { get; set; }
        public string version { get; set; }
    }

    public class UserNoticeParams
    { }

    public class Emoticon2
    {
        public string _id { get; set; }
        public int begin { get; set; }
        public int end { get; set; }
    }

    public class Message
    {
        public string body { get; set; }
        public List<Fragment> fragments { get; set; }
        public bool is_action { get; set; }
        public List<UserBadge> user_badges { get; set; }
        public string user_color { get; set; }
        public UserNoticeParams user_notice_params { get; set; }
        public List<Emoticon2> emoticons { get; set; }
    }

    public class Comment
    {
        public string _id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string channel_id { get; set; }
        public string content_type { get; set; }
        public string content_id { get; set; }
        public double content_offset_seconds { get; set; }
        public Commenter commenter { get; set; }
        public string source { get; set; }
        public string state { get; set; }
        public Message message { get; set; }
        public bool more_replies { get; set; }
    }

    public class RootChatObject
    {
        public List<Comment> comments { get; set; }
        public string _prev { get; set; }
        public string _next { get; set; }
    }
}
