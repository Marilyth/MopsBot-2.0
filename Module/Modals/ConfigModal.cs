using Discord.Interactions;
using Discord;
using MopsBot.Data.Tracker;

namespace MopsBot.Module.Modals
{
    public class ModalBuilders{
        /// <summary>
        /// Generate a modal in which the user can modify the tracker config.
        /// </summary>
        /// <param name="initialConfig"></param>
        /// <returns></returns>
        public static Modal GetConfigModal(string initialConfig){
            var builder = new ModalBuilder().WithTitle("Change config").WithCustomId("config");
            builder.AddTextInput("New config", "new_config", TextInputStyle.Paragraph, "The new config to be used", required: true, value: initialConfig);
            return builder.Build();
        }

        /// <summary>
        /// Generate a model in which the user can specify JSON paths.
        /// </summary>
        /// <param name="initialNotification"></param>
        /// <returns></returns>
        public static Modal GetJsonModal(){
            var builder = new ModalBuilder().WithTitle("Change notification").WithCustomId("paths");
            builder.AddTextInput("Paths", "paths", TextInputStyle.Paragraph, "The json paths to be used, e.g.\nalways:player->name->as:Name\ngraph:player->level->as:Level", required: true);
            return builder.Build();
        }

        public static Modal GetNotificationModal(string initialNotification){
            var builder = new ModalBuilder().WithTitle("Change notification").WithCustomId("notification");
            builder.AddTextInput("New notification", "new_notification", TextInputStyle.Paragraph, "The new notification to be used", required: true, value: initialNotification);
            return builder.Build();
        }
    }
}
