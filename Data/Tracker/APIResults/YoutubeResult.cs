using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MopsBot.Data.Tracker.APIResults.Youtube
{
    public class PageInfo
    {
        public int totalResults { get; set; }
        public int resultsPerPage { get; set; }
    }

    public class RelatedPlaylists
    {
        public string likes { get; set; }
        public string favorites { get; set; }
        public string uploads { get; set; }
        public string watchHistory { get; set; }
        public string watchLater { get; set; }
    }

    public class ContentDetails
    {
        public RelatedPlaylists relatedPlaylists { get; set; }
    }

    public class ChannelItem
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
        public ContentDetails contentDetails { get; set; }
        public ChannelSnippet snippet { get; set; }
    }

    public class Channel
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<ChannelItem> items { get; set; }
    }

    public class Thumbnail
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Thumbnails
    {
        public Thumbnail @default { get; set; }
        public Thumbnail medium { get; set; }
        public Thumbnail high { get; set; }
        public Thumbnail standard { get; set; }
        public Thumbnail maxres { get; set; }
    }

    public class ResourceId
    {
        public string kind { get; set; }
        public string videoId { get; set; }
    }

    public class ChannelSnippet
    {
        public string title { get; set; }
        public string description { get; set; }
        public string customUrl { get; set; }
        public DateTime publishedAt { get; set; }
        public Thumbnails thumbnails { get; set; }
        public Localized localized { get; set; }
    }

    public class Localized
    {
        public string title { get; set; }
        public string description { get; set; }
    }

    public class VideoSnippet
    {
        public DateTime publishedAt { get; set; }
        public string channelId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Thumbnails thumbnails { get; set; }
        public string channelTitle { get; set; }
        public string playlistId { get; set; }
        public int position { get; set; }
        public ResourceId resourceId { get; set; }
    }

    public class Video
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
        public VideoSnippet snippet { get; set; }
    }

    public class Playlist
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string nextPageToken { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<Video> items { get; set; }
    }
}