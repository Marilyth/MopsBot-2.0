using Discord.Interactions;
using Discord;
using MopsBot.Data.Tracker;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MopsBot.Module
{
    /// <summary>
    /// Autocompletes the name of trackers of the current module.
    /// </summary>
    public class TrackerAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var name = autocompleteInteraction.Data.Current.Value.ToString();
            var module = autocompleteInteraction.Data.CommandName;
            var worked = Enum.TryParse<BaseTracker.TrackerType>(module, true, out BaseTracker.TrackerType type);

            if(!BaseTracker.CapSensitive.Any(x => x == type))
                name = name.ToLower();
            if(type == BaseTracker.TrackerType.JSON)
                name = name.Replace(";", "\n");

            var results = StaticBase.Trackers[type].GetGuildTrackers(context.Guild.Id).Where(x => x.Name.Replace(";", "\n").StartsWith(name)).OrderBy(x => x.Name.Length).Select(x => {
                string name = x.Name.Replace("\n", ";");
                name = name.Substring(0, Math.Min(100, name.Length));
                return new AutocompleteResult(name, name);
            });

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }
}