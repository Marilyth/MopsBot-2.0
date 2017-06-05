using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System;
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
            //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
        }
    }
    
    public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string url)
    {
        IAudioClient client;
        if (ConnectedChannels.TryGetValue(guild.Id, out client))
        {
            await channel.SendMessageAsync($"Now playing **{VideoTitle(url)}**");

            var output = CreateStream(url).StandardOutput.BaseStream;
            var stream = client.CreatePCMStream(AudioApplication.Music);
            await output.CopyToAsync(stream);
            await stream.FlushAsync().ConfigureAwait(false);

            StaticBase.playlist.RemoveAt(0);

            if(StaticBase.playlist.Count > 0)
                await SendAudioAsync(guild, channel, StaticBase.playlist[0]);
        }
    }

    private Process CreateStream(string url)
    {
        string streamUrl = StreamURL(url);
        
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i {streamUrl} -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
    }

    private string StreamURL(string url)
    {
        var prc = new Process();
        prc.StartInfo.FileName = "youtube-dl";
        prc.StartInfo.Arguments = $"-g \"ytsearch:{url}\"";
        prc.StartInfo.UseShellExecute = false;
        prc.StartInfo.RedirectStandardOutput = true;

        prc.Start();
        
        string output = prc.StandardOutput.ReadToEndAsync().Result.Replace("\n", "");
        string[] outputArray = output.Split(':');
        return outputArray[outputArray.Length-2].Contains("https") ? "https:" + outputArray[outputArray.Length-1] : "http:" + outputArray[outputArray.Length-1];
    }

    public static string VideoTitle(string url)
    {
        var prc = new Process();
        prc.StartInfo.FileName = "youtube-dl";
        prc.StartInfo.Arguments = $"-e --get-duration \"ytsearch:{url}\"";
        prc.StartInfo.UseShellExecute = false;
        prc.StartInfo.RedirectStandardOutput = true;

        prc.Start();
        
        return prc.StandardOutput.ReadToEndAsync().Result.Replace("\n", " | ");
    }
}
}