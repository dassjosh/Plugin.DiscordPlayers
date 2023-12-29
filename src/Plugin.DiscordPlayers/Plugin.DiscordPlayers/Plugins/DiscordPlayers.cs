using Oxide.Ext.Discord.Interfaces;
using Oxide.Plugins;

namespace DiscordPlayersPlugin.Plugins
{
    [Info("Discord Players", "MJSU", "3.0.0")]
    [Description("Displays online players in discord")]
    public partial class DiscordPlayers : CovalencePlugin, IDiscordPlugin, IDiscordPool
    {
        
    }
}

