using Oxide.Ext.Discord.Libraries;

namespace DiscordPlayersPlugin.Placeholders;

public static class PlaceholderDataKeys
{
    public static readonly PlaceholderDataKey CommandId = new("command.id");
    public static readonly PlaceholderDataKey CommandName = new("command.name");
    public static readonly PlaceholderDataKey PlayerIndex = new("player.index");
    public static readonly PlaceholderDataKey PlayerDuration = new("timespan");
    public static readonly PlaceholderDataKey MaxPage = new("page.max");
    public static readonly PlaceholderDataKey MessageState = new("message.state");
}