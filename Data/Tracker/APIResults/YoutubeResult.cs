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

    public class ContentDetailsPlaylist
    {
        public int itemCount { get; set; }
    }

    public class Playlists
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
        public ContentDetailsPlaylist contentDetails { get; set; }
    }

    public class PlaylistCounts
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string nextPageToken { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<Playlists> items { get; set; }
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

    public class LiveItem
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public ResourceId id { get; set; }
        public LiveSnippet snippet { get; set; }
    }

    public class Live
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string regionCode { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<LiveItem> items { get; set; }
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

    public class LiveSnippet
    {
        public DateTime publishedAt { get; set; }
        public string channelId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Thumbnails thumbnails { get; set; }
        public string channelTitle { get; set; }
        public string liveBroadcastContent { get; set; }
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

    public class LiveStreamingDetails
    {
        public DateTime actualStartTime { get; set; }
        public DateTime scheduledStartTime { get; set; }
        public string concurrentViewers { get; set; }
        public string activeLiveChatId { get; set; }
    }

    public class LiveVideoSnippet
    {
        public DateTime publishedAt { get; set; }
        public string channelId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Thumbnails thumbnails { get; set; }
        public string channelTitle { get; set; }
        public List<string> tags { get; set; }
        public string categoryId { get; set; }
        public string liveBroadcastContent { get; set; }
        public Localized localized { get; set; }
        public string defaultAudioLanguage { get; set; }
    }

    public class LiveVideoItem
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
        public LiveVideoSnippet snippet { get; set; }
        public LiveStreamingDetails liveStreamingDetails { get; set; }
    }

    public class LiveVideo
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<LiveVideoItem> items { get; set; }
    }

    public class TextMessageDetails
    {
        public string messageText { get; set; }
    }

    public class Snippet
    {
        public string type { get; set; }
        public string liveChatId { get; set; }
        public string authorChannelId { get; set; }
        public DateTime publishedAt { get; set; }
        public bool hasDisplayContent { get; set; }
        public string displayMessage { get; set; }
        public TextMessageDetails textMessageDetails { get; set; }
    }

    public class AuthorDetails
    {
        public string channelId { get; set; }
        public string channelUrl { get; set; }
        public string displayName { get; set; }
        public string profileImageUrl { get; set; }
        public bool isVerified { get; set; }
        public bool isChatOwner { get; set; }
        public bool isChatSponsor { get; set; }
        public bool isChatModerator { get; set; }
    }

    public class Message
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
        public Snippet snippet { get; set; }
        public AuthorDetails authorDetails { get; set; }
    }

    public class ChatMessages
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string nextPageToken { get; set; }
        public int pollingIntervalMillis { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<Message> items { get; set; }
    }

    public class YoutubeNotification
    {
        public string Id { get; set; }
        public string VideoId { get; set; }
        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public Author Author { get; set; }
        public DateTimeOffset Published { get; set; }
        public DateTimeOffset Updated { get; set; }
        public bool IsNewVideo
        {
            get
            {
                return (Updated - Published).Days <= 3 && !default(DateTimeOffset).Equals(Published);
            }
        }
    }

    public class Author
    {
        public string Name { get; set; }
        public string Uri { get; set; }
    }

}