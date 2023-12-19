using DiscordPlayersPlugin.Plugins;
using Oxide.Ext.Discord.Libraries.Placeholders;

namespace DiscordPlayersPlugin.Placeholders
{
    public class PlaceholderKeys
    {
        public static readonly PlaceholderKey PlayerIndex = new PlaceholderKey(nameof(DiscordPlayers), "player.index");
        public static readonly PlaceholderKey Page = new PlaceholderKey(nameof(DiscordPlayers), "state.page");
        public static readonly PlaceholderKey SortState = new PlaceholderKey(nameof(DiscordPlayers), "state.sort");
        public static readonly PlaceholderKey CommandId = new PlaceholderKey(nameof(DiscordPlayers), "command.id");
        public static readonly PlaceholderKey CommandName = new PlaceholderKey(nameof(DiscordPlayers), "command.name");
        public static readonly PlaceholderKey MaxPage = new PlaceholderKey(nameof(DiscordPlayers), "page.max");
    }
}