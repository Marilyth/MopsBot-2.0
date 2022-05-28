using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MopsBot.Data.Tracker.APIResults.Twitch
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class TwitchStreamInfo
    {
        public string id { get; set; }
        public string user_id { get; set; }
        public string user_login { get; set; }
        public string user_name { get; set; }
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public int viewer_count { get; set; }
        public DateTime started_at { get; set; }
        public string language { get; set; }
        public string thumbnail_url { get; set; }
        public List<string> tag_ids { get; set; }
        public bool is_mature { get; set; }
    }

    public class Pagination
    {
        public string cursor { get; set; }
    }

    public class TwitchStreamResult
    {
        public List<TwitchStreamInfo> data { get; set; }
        public Pagination pagination { get; set; }
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
