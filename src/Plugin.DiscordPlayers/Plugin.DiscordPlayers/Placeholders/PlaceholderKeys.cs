using DiscordPlayersPlugin.Plugins;
using Oxide.Ext.Discord.Libraries;

namespace DiscordPlayersPlugin.Placeholders;

public class PlaceholderKeys
{
    public static readonly PlaceholderKey PlayerIndex = new(nameof(DiscordPlayers), "player.index");
    public static readonly PlaceholderKey Page = new(nameof(DiscordPlayers), "state.page");
    public static readonly PlaceholderKey SortState = new(nameof(DiscordPlayers), "state.sort");
    public static readonly PlaceholderKey CommandId = new(nameof(DiscordPlayers), "command.id");
    public static readonly PlaceholderKey CommandName = new(nameof(DiscordPlayers), "command.name");
    public static readonly PlaceholderKey MaxPage = new(nameof(DiscordPlayers), "page.max");
}