using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MopsBot.Module.Preconditions;

namespace MopsBot.Module{
    [Hide]
    public class Testing : ModuleBase{

        [Command("colour")]
        [RequireBotManage]
        public async Task cTest([Remainder]string name){
            var colour = MopsBot.Data.DatePlot.StringToColour(name);
            await ReplyAsync($"{colour.R}, {colour.G}, {colour.B}");
        }
    }
}
