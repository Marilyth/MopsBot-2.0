using System.Threading.Tasks;
using Discord.Commands;
using Discord.Audio;

public class AudioModule : ModuleBase<ICommandContext>
{
    private readonly AudioService _service;

    public AudioModule(AudioService service)
    {
        _service = service;
    }
    
    [Command("join", RunMode = RunMode.Async)]
    public async Task JoinCmd()
    {
        await _service.JoinAudio(Context.Guild, (Context.User as Discord.IVoiceState).VoiceChannel);
    }
    
    [Command("leave", RunMode = RunMode.Async)]
    public async Task LeaveCmd()
    {
        await _service.LeaveAudio(Context.Guild);
    }
    
    [Command("play", RunMode = RunMode.Async)]
    public async Task PlayCmd([Remainder] string song)
    {
        await _service.SendAudioAsync(Context.Guild, Context.Channel, song);
    }
}
