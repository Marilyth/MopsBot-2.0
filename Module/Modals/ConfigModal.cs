using Discord.Interactions;
using Discord;
using MopsBot.Data.Tracker;

namespace MopsBot.Module.Modals
{
    public class ModalBuilders{
        public class ConfigModal : IModal{
            public string Title {get; set;}
            [InputLabel("New config")]
            [ModalTextInput("new_config", style: TextInputStyle.Paragraph)]
            public string NewConfig {get; set;}
            [InputLabel("name")]
            public string Name {get; set;}
        }

        public static Modal GetConfigModal(string title, string trackerName, string initialConfig, BaseTracker.TrackerType trackerType){
            var builder = new ModalBuilder(title, $"config_{trackerType}");
            builder.AddTextInput("Name", "name", value: trackerName);
            builder.AddTextInput("New config", "new_config", TextInputStyle.Paragraph, "The new config to be used", required: true, value: initialConfig);
            return builder.Build();
        }
    }
}
