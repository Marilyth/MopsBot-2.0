using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MopsBot;

namespace MopsBot.Data.Interactive
{
    public class MopsPaginator
    {
        public List<Embed> pages;
        private int curPage;
        public IUserMessage message;

        private MopsPaginator() { }

        public static async Task CreatePagedMessage(ISocketMessageChannel channel, IEnumerable<Embed> pPages)
        {
            int pages = pPages.Count();
            pPages = pPages.Select((x, index) => x.ToEmbedBuilder().WithFooter(y => y.Text = $"Page: {index + 1} / {pages}").Build());

            var message = channel.SendMessageAsync(embed: pPages.First()).Result;

            var paginator = new MopsPaginator()
            {
                pages = pPages.ToList(),
                message = message
            };

            if(pPages.Count() > 1){
                await Program.ReactionHandler.AddHandler(message, new Emoji("◀"), paginator.PreviousPageAsync);
                await Program.ReactionHandler.AddHandler(message, new Emoji("◀"), paginator.PreviousPageAsync, true);
                await Program.ReactionHandler.AddHandler(message, new Emoji("▶"), paginator.NextPageAsync);
                await Program.ReactionHandler.AddHandler(message, new Emoji("▶"), paginator.NextPageAsync, true);
            }
        }

        public static async Task CreatePagedMessage(ulong channel, IEnumerable<Embed> pPages)
        {
            await CreatePagedMessage(Program.Client.GetChannel(channel) as ISocketMessageChannel, pPages);
        }

        public static async Task CreatePagedMessage(ISocketMessageChannel channel, IEnumerable<string> pPages)
        {
            var pages = new List<Embed>();
            foreach (string s in pPages)
            {
                pages.Add(new EmbedBuilder().WithDescription(new string(s.Take(Math.Min(2040, s.Length)).ToArray())).Build());
            }

            await CreatePagedMessage(channel, pages);
        }

        public static async Task CreatePagedMessage(ulong channel, IEnumerable<string> pPages)
        {
            await CreatePagedMessage(Program.Client.GetChannel(channel) as ISocketMessageChannel, pPages);
        }

        public async Task PreviousPageAsync(ReactionHandlerContext context)
        {
            if (curPage != 0)
            {
                await message.ModifyAsync(x => x.Embed = pages[--curPage]);
            }
        }

        public async Task NextPageAsync(ReactionHandlerContext context)
        {
            if (curPage != pages.Count - 1)
            {
                await message.ModifyAsync(x => x.Embed = pages[++curPage]);
            }
        }
    }
}