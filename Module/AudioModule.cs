using System.Threading.Tasks;
using Discord.Commands;
using Discord.Audio;

namespace MopsBot.Module
{
public class AudioModule : ModuleBase<ICommandContext>
{

    private readonly AudioService _service;

    public AudioModule(AudioService service)
    {
        _service = service;
    }

    [Command("join", RunMode = RunMode.Async)]
    [Summary("Joins the Voice Channel you are in currently.")]
    public async Task JoinCmd()
    {
        await _service.JoinAudio(Context.Guild, (Context.User as Discord.IVoiceState).VoiceChannel);
    }

    [Command("leave", RunMode = RunMode.Async)]
    [Summary("Leaves the AudioChannel.")]
    public async Task LeaveCmd()
    {
        await _service.LeaveAudio(Context.Guild);
    }

    [Command("queue", RunMode = RunMode.Async)]
    [Summary("Returns the 5 closest entries in the queue.")]
    public async Task QueueCmd()
    {
        string output = "";
        int i = 1;

        foreach(string s in StaticBase.playlist){
            output += $"#{i}: **{AudioService.VideoTitle(s)}**\n";
            i++;

            if(i > 5)
                break;
        }

        await ReplyAsync(output);
    }
    
    [Command("add", RunMode = RunMode.Async)]
    [Summary("Appends an entry into the Queue.")]
    public async Task AddCmd([Remainder] string song)
    {
        StaticBase.playlist.Add(song);
        if(StaticBase.playlist.Count == 1)
            await _service.SendAudioAsync(Context.Guild, Context.Channel, song);
        else
            await ReplyAsync("Added your request to the Queue (#" + StaticBase.playlist.Count + ")");
    }
}
}
