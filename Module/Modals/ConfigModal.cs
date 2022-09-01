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
    }
}