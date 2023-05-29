using System.Collections.Generic;
using DiscordPlayersPlugin.Lang;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.SortByEnumName] = "Name",
                [LangKeys.SortByEnumTime] = "Time",
            }, this);
        }
    }
}