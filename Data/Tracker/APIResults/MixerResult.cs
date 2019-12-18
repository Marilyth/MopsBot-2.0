using System.Collections.Generic;
using System;

namespace MopsBot.Data.Tracker.APIResults.Mixer
{
    public class Meta
    {
        public List<int> size { get; set; }
    }

    public class Badge
    {
        public DateTime createdAt { get; set; }
        public int id { get; set; }
        public Meta meta { get; set; }
        public int relid { get; set; }
        public string remotePath { get; set; }
        public string store { get; set; }
        public string type { get; set; }
        public DateTime updatedAt { get; set; }
        public string url { get; set; }
    }

    public class Type
    {
        public object availableAt { get; set; }
        public string backgroundUrl { get; set; }
        public string coverUrl { get; set; }
        public object description { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int online { get; set; }
        public string parent { get; set; }
        public string source { get; set; }
        public int viewersCurrent { get; set; }
    }

    public class Meta2
    {
        public List<int> size { get; set; }
    }

    public class Thumbnail
    {
        public DateTime createdAt { get; set; }
        public int id { get; set; }
        public Meta2 meta { get; set; }
        public int relid { get; set; }
        public string remotePath { get; set; }
        public string store { get; set; }
        public string type { get; set; }
        public DateTime updatedAt { get; set; }
        public string url { get; set; }
    }

    public class Group
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class Social
    {
        public string instagram { get; set; }
        public string twitter { get; set; }
        public List<object> verified { get; set; }
        public string youtube { get; set; }
    }

    public class User
    {
        public string avatarUrl { get; set; }
        public string bio { get; set; }
        public DateTime createdAt { get; set; }
        public object deletedAt { get; set; }
        public int experience { get; set; }
        public List<Group> groups { get; set; }
        public int id { get; set; }
        public int level { get; set; }
        public object primaryTeam { get; set; }
        public Social social { get; set; }
        public int sparks { get; set; }
        public DateTime updatedAt { get; set; }
        public string username { get; set; }
        public bool verified { get; set; }
    }

    public class __invalid_type__1
    {
        public int minLevel { get; set; }
        public int maxLevel { get; set; }
    }

    public class __invalid_type__2
    {
        public int minLevel { get; set; }
        public int maxLevel { get; set; }
    }

    public class ChannelCatbotAscensionranges
    {
        public __invalid_type__1 __invalid_name__1 { get; set; }
        public __invalid_type__2 __invalid_name__2 { get; set; }
    }

    public class MixerResult
    {
        public int id { get; set; }
        public string token { get; set; }
        public int userId { get; set; }
        public object costreamId { get; set; }
        public bool featured { get; set; }
        public int featureLevel { get; set; }
        public int ftl { get; set; }
        public bool hasTranscodes { get; set; }
        public bool hasVod { get; set; }
        public object hosteeId { get; set; }
        public bool interactive { get; set; }
        public object interactiveGameId { get; set; }
        public int numFollowers { get; set; }
        public bool online { get; set; }
        public bool partnered { get; set; }
        public string sellerId { get; set; }
        public object transcodingProfileId { get; set; }
        public int viewersCurrent { get; set; }
        public string audience { get; set; }
        public int badgeId { get; set; }
        public string bannerUrl { get; set; }
        public object coverId { get; set; }
        public DateTime createdAt { get; set; }
        public object deletedAt { get; set; }
        public string description { get; set; }
        public string languageId { get; set; }
        public string name { get; set; }
        public bool suspended { get; set; }
        public int thumbnailId { get; set; }
        public int typeId { get; set; }
        public DateTime updatedAt { get; set; }
        public int viewersTotal { get; set; }
        public bool vodsEnabled { get; set; }
        public Badge badge { get; set; }
        public object cover { get; set; }
        public Type type { get; set; }
        public Thumbnail thumbnail { get; set; }
        public User user { get; set; }
    }
}