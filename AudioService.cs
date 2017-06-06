using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using Discord;
using Discord.Audio;

namespace MopsBot
{
public class AudioService
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
    private List<string> urlQueue = new List<string>();
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

    public async Task LeaveAudio(IGuild guild)
    {
        IAudioClient client;
        if (ConnectedChannels.TryRemove(guild.Id, out client))
        {
            await client.StopAsync();
            DeleteFiles();
        }
    }
    
    public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string url)
    {
        IAudioClient client;
        if (ConnectedChannels.TryGetValue(guild.Id, out client))
        {
            await channel.SendMessageAsync($"Now downloading **{VideoTitle(url)}**\nPlease wait.");

            var output = CreateStream(url).StandardOutput.BaseStream;
            var stream = client.CreatePCMStream(AudioApplication.Music);

            await channel.SendMessageAsync($"Now playing **{VideoTitle(url)}**");

            await output.CopyToAsync(stream);
            await stream.FlushAsync().ConfigureAwait(false);

            StaticBase.playlist.RemoveAt(0);
            DeleteFiles();

            if(StaticBase.playlist.Count > 0)
                await SendAudioAsync(guild, channel, StaticBase.playlist[0]);
        }
    }

    private Process CreateStream(string url)
    {
        DownloadURL(url);

        var dir = new DirectoryInfo("data//");
        var file = dir.GetFiles().Where(x => x.Extension.ToLower().Equals(".mp3")).First();
        
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{file.FullName}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
    }

    
    private void DownloadURL(string url)
    {
        var prc = new Process();
        prc.StartInfo.FileName = "youtube-dl";
        prc.StartInfo.Arguments = $"--extract-audio --audio-format mp3 -o \"data//%(title)s.%(ext)s\" \"{(url.Contains("://") ? url : $"ytsearch:{url}")}\"";
        prc.StartInfo.UseShellExecute = false;
        prc.StartInfo.RedirectStandardOutput = true;

        prc.Start();
        
        prc.WaitForExit();
    }
    

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

    private void DeleteFiles()
    {
        var dir = new DirectoryInfo("data//");
        var files = dir.GetFiles().Where(x => x.Extension.ToLower().Equals(".mp3"));
        foreach(var f in files)
            f.Delete();
    }
}
}
