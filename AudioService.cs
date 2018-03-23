using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using Discord;
using Discord.Audio;
using Newtonsoft.Json;

namespace MopsBot
{
public class AudioService
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
    private List<string> urlQueue = new List<string>();

    /// <summary>
    /// Joins an Audio Channel
    /// </summary>
    /// <param name="guild">The Guild the channel is located in</param>
    /// <param name="target">The channel to join</param>
    /// <returns>An async task that can be awaited</returns>
    public async Task JoinAudio(IGuild guild, IVoiceChannel target)
    {
        IAudioClient client;
        if (ConnectedChannels.TryGetValue(guild.Id, out client))
        {
            return;
        }
        if (target.Guild.Id != guild.Id)
        {
            return;
        }

        var audioClient = await target.ConnectAsync();

        if (ConnectedChannels.TryAdd(guild.Id, audioClient))
        {
            //await Log(LogSeverity.Info, $"Connected to voice on {guild.Name}.");
}
    }

    /// <summary>
    /// Leaves an Audio Channel
    /// </summary>
    /// <param name="guild">The Guild to leave the Channel in</param>
    /// <returns>An async task that can be awaited</returns>
    public async Task LeaveAudio(IGuild guild)
    {
        IAudioClient client;
        if (ConnectedChannels.TryRemove(guild.Id, out client))
        {
            await client.StopAsync();
            DeleteFiles();
        }
    }
    
    /// <summary>
    /// Main job is to send an Audio Stream to the Voice Channel
    /// </summary>
    /// <param name="guild">The Guild whose Voice Channel it should send the Stream into</param>
    /// <param name="channel">The Channel in which to notify when the song changes etc</param>
    /// <param name="url">The url or song name to stream</param>
    /// <returns>An async task that can be awaited</returns>
    public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string url)
    {
        IAudioClient client;
        if (ConnectedChannels.TryGetValue(guild.Id, out client))
        {
            if(url.ToLower().Contains("playlist"))
                await channel.SendMessageAsync("Processing Playlist");
            else
                await channel.SendMessageAsync($"Now downloading **{VideoTitle(url)}**\nPlease wait.");
            
            var result = CreateStream(url);

            if(result != null){
                var output = result.StandardOutput.BaseStream;
                var stream = client.CreatePCMStream(AudioApplication.Music);

                await channel.SendMessageAsync($"Now playing **{VideoTitle(url)}**");

                await output.CopyToAsync(stream);
                await stream.FlushAsync().ConfigureAwait(false);
            }

            StaticBase.playlist.RemoveAt(0);
            DeleteFiles();

            if(StaticBase.playlist.Count > 0)
                await SendAudioAsync(guild, channel, StaticBase.playlist[0]);
        }
    }

    /// <summary>
    /// Creates an audio stream to send
    /// </summary>
    /// <param name="url">The url or song name</param>
    /// <returns>A Process which fetches the audio stream</returns>
    private Process CreateStream(string url)
    {
        if(url.ToLower().Contains("playlist")){
            dynamic entries = JsonConvert.DeserializeObject(playlistURLs(url));

            foreach(dynamic entry in entries["entries"]){
                try{
                    StaticBase.playlist.Add($"https://www.youtube.com/watch?v={entry["id"]}");
                }
                catch(Exception e){
                    
                }
            }
            return null;
        }

        else 
            DownloadURL(url);

        var dir = new DirectoryInfo("mopsdata//");
        var file = dir.GetFiles().Where(x => x.Extension.ToLower().Equals(".mp3")).First();
        
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{file.FullName}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
    }

    /// <summary>
    /// Downloads the url as .mp3 to stream it
    /// </summary>
    /// <param name="url">The url or song name</param>
    private void DownloadURL(string url)
    {
        var prc = new Process();
        prc.StartInfo.FileName = "youtube-dl";
        prc.StartInfo.Arguments = $"--extract-audio --audio-format mp3 -o \"mopsdata//%(title)s.%(ext)s\" \"{(url.Contains("://") ? url : $"ytsearch:{url}")}\"";
        prc.StartInfo.UseShellExecute = false;
        prc.StartInfo.RedirectStandardOutput = true;

        prc.Start();
        
        prc.WaitForExit();
    }

    /// <summary>
    /// Fetches all youtube-links corresponding to a playlist
    /// </summary>
    /// <param name="url">The playlist url</param>
    /// <returns>A string representing all youtube links of the playlist</returns>
    private string playlistURLs(string url)
    {
        var prc = new Process();
        prc.StartInfo.FileName = "youtube-dl";
        prc.StartInfo.Arguments = $"-J -i --flat-playlist --playlist-random \"{url}\"";
        prc.StartInfo.UseShellExecute = false;
        prc.StartInfo.RedirectStandardOutput = true;

        prc.Start();
        
        return prc.StandardOutput.ReadToEndAsync().Result.Replace("\n", "");
    }
    
    /// <summary>
    /// Fetches the title of the specified Video/Song/Stream
    /// </summary>
    /// <param name="url">The url or song name</param>
    /// <returns></returns>
    public static string VideoTitle(string url)
    {
        var prc = new Process();
        prc.StartInfo.FileName = "youtube-dl";
        prc.StartInfo.Arguments = $"-e \"{(url.Contains("://") ? url : $"ytsearch:{url}")}\"";
        prc.StartInfo.UseShellExecute = false;
        prc.StartInfo.RedirectStandardOutput = true;

        prc.Start();
        
        return prc.StandardOutput.ReadToEndAsync().Result.Replace("\n", "");
    }

    /// <summary>
    /// Deletes all .mp3 files after streaming them
    /// </summary>
    private void DeleteFiles()
    {
        var dir = new DirectoryInfo("mopsdata//");
        var files = dir.GetFiles().Where(x => x.Extension.ToLower().Equals(".mp3"));
        foreach(var f in files)
            f.Delete();
    }
}
}
