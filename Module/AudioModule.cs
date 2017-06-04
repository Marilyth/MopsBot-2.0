using System.Threading.Tasks;
using Discord.Commands;
using Discord.Audio;

public class AudioModule : ModuleBase<ICommandContext>
{
    // Scroll down further for the AudioService.
    // Like, way down.
    // Hit 'End' on your keyboard if you still can't find it.
    private readonly AudioService _service;

    public AudioModule(AudioService service)
    {
        _service = service;
    }

    // You *MUST* mark these commands with 'RunMode.Async'
    // otherwise the bot will not respond until the Task times out.
    [Command("join", RunMode = RunMode.Async)]
    public async Task JoinCmd()
    {
        await _service.JoinAudio(Context.Guild, (Context.User as Discord.IVoiceState).VoiceChannel);
    }

    // Remember to add preconditions to your commands,
    // this is merely the minimal amount necessary.
    // Adding more commands of your own is also encouraged.
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