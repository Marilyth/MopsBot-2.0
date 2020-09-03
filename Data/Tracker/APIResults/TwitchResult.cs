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

    public partial class ChannelResult
    {
        [JsonProperty("stream")]
        public Stream Stream { get; set; }
    }

    public partial class Stream
    {
        [JsonProperty("_id")]
        public long Id { get; set; }

        [JsonProperty("game")]
        public string Game { get; set; }

        [JsonProperty("broadcast_platform")]
        public string BroadcastPlatform { get; set; }

        [JsonProperty("community_id")]
        public string CommunityId { get; set; }

        [JsonProperty("community_ids")]
        public object CommunityIds { get; set; }

        [JsonProperty("viewers")]
        public long Viewers { get; set; }

        [JsonProperty("video_height")]
        public long VideoHeight { get; set; }

        [JsonProperty("average_fps")]
        public long AverageFps { get; set; }

        [JsonProperty("delay")]
        public long Delay { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("is_playlist")]
        public bool IsPlaylist { get; set; }

        [JsonProperty("stream_type")]
        public string StreamType { get; set; }

        [JsonProperty("preview")]
        public Preview Preview { get; set; }

        [JsonProperty("channel")]
        public Channel Channel { get; set; }
    }

    public partial class Channel
    {
        [JsonProperty("mature")]
        public bool Mature { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("broadcaster_language")]
        public string BroadcasterLanguage { get; set; }

        [JsonProperty("broadcaster_software")]
        public string BroadcasterSoftware { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("game")]
        public string Game { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("_id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("partner")]
        public bool Partner { get; set; }

        [JsonProperty("logo")]
        public Uri Logo { get; set; }

        [JsonProperty("video_banner")]
        public Uri VideoBanner { get; set; }

        [JsonProperty("profile_banner")]
        public Uri ProfileBanner { get; set; }

        [JsonProperty("profile_banner_background_color")]
        public string ProfileBannerBackgroundColor { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("views")]
        public long Views { get; set; }

        [JsonProperty("followers")]
        public long Followers { get; set; }

        [JsonProperty("broadcaster_type")]
        public string BroadcasterType { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("private_video")]
        public bool PrivateVideo { get; set; }

        [JsonProperty("privacy_options_enabled")]
        public bool PrivacyOptionsEnabled { get; set; }
    }

    public partial class Preview
    {
        [JsonProperty("small")]
        public Uri Small { get; set; }

        [JsonProperty("medium")]
        public Uri Medium { get; set; }

        [JsonProperty("large")]
        public Uri Large { get; set; }

        [JsonProperty("template")]
        public string Template { get; set; }
    }

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
