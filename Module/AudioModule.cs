// using System.Threading.Tasks;
// using Discord.Commands;
// using Discord.Audio;
// using Discord;

// namespace MopsBot.Module
// {
// public class AudioModule : ModuleBase<ICommandContext>
// {

//     private readonly AudioService _service;

//     public AudioModule(AudioService service)
//     {
//         _service = service;
//     }

//     [Command("join", RunMode = RunMode.Async)]
//     [Summary("Joins the Voice Channel you are in currently.")]
//     [RequireBotPermission(GuildPermission.Speak)]
//     [RequireBotPermission(GuildPermission.Connect)]
//     public async Task JoinCmd()
//     {
//         await ReplyAsync("Discord.Net seems to have broken the voice functionality for now.\n"+
//                          "Sorry for the inconvenience. It will be back once it's fixed!");
//         //await _service.JoinAudio(Context.Guild, (Context.User as Discord.IVoiceState).VoiceChannel);
//     }

//     [Command("leave", RunMode = RunMode.Async)]
//     [Summary("Leaves the AudioChannel.")]
//     public async Task LeaveCmd()
//     {
//         await _service.LeaveAudio(Context.Guild);
//     }

//     [Command("skip", RunMode = RunMode.Async)]
//     [Summary("Skips the song currently at #1")]
//     [RequireBotPermission(ChannelPermission.SendMessages)]
//     public async Task SkipCmd()
//     {
//         await _service.LeaveAudio(Context.Guild);
//         StaticBase.playlist.RemoveAt(0);
//         await _service.JoinAudio(Context.Guild, (Context.User as Discord.IVoiceState).VoiceChannel);
//         await _service.SendAudioAsync(Context.Guild, Context.Channel, StaticBase.playlist[0]);
//     }

//     [Command("queue", RunMode = RunMode.Async)]
//     [Summary("Returns the 5 closest entries in the queue.")]
//     [RequireBotPermission(ChannelPermission.SendMessages)]
//     public async Task QueueCmd()
//     {
//         string output = "";
//         int i = 1;

//         foreach(string s in StaticBase.playlist){
//             output += $"#{i}: **{AudioService.VideoTitle(s)}**\n";
//             i++;

//             if(i > 5)
//                 break;
//         }

//         await ReplyAsync(output);
//     }
    
//     [Command("add", RunMode = RunMode.Async)]
//     [Summary("Appends an entry into the Queue.")]
//     [RequireBotPermission(ChannelPermission.SendMessages)]
//     public async Task AddCmd([Remainder] string song)
//     {
//         StaticBase.playlist.Add(song);
//         if(StaticBase.playlist.Count == 1)
//             await _service.SendAudioAsync(Context.Guild, Context.Channel, song);
//         else
//             await ReplyAsync("Added your request to the Queue (#" + StaticBase.playlist.Count + ")");
//     }
// }
// }