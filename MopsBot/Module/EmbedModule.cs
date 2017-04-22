using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace MopsBot.Module{
    public class EmbedModule:ModuleBase{
        [Command("test")]
        public async Task test(){
            EmbedBuilder builder = new EmbedBuilder();
            builder.Description = "test";
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
            fieldBuilder.Name="test2";
            fieldBuilder.IsInline=false;
            fieldBuilder.Value="testvalue";
            builder.Title = "test3";
            builder.Color = new Color(34, 65, 65);
            builder.AddField(fieldBuilder);
            builder.Url="https://google.com";
            builder.ImageUrl="https://storage.googleapis.com/gweb-uniblog-publish-prod/static/blog/images/google-200x200.7714256da16f.png";
            EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder();
            authorBuilder.Name="test4";
            authorBuilder.IconUrl="https://storage.googleapis.com/gweb-uniblog-publish-prod/static/blog/images/google-200x200.7714256da16f.png";
            authorBuilder.Url="https://google.de";
            builder.Author=authorBuilder;
            EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
            footerBuilder.IconUrl="https://storage.googleapis.com/gweb-uniblog-publish-prod/static/blog/images/google-200x200.7714256da16f.png";
            footerBuilder.Text="test5";
            builder.Footer=footerBuilder;
            fieldBuilder = new EmbedFieldBuilder();
            fieldBuilder.Name="test2";
            fieldBuilder.IsInline=true;
            fieldBuilder.Value="testvalue";
            builder.Title = "test3";
            builder.Color = new Color(34, 65, 65);
            builder.AddField(fieldBuilder);
            await Context.Channel.SendMessageAsync("",false,builder.Build());
        }


    }
}