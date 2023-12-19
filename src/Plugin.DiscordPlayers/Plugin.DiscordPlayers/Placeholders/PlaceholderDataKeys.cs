using Oxide.Ext.Discord.Libraries.Placeholders;

namespace DiscordPlayersPlugin.Placeholders
{
    public static class PlaceholderDataKeys
    {
        public static readonly PlaceholderDataKey CommandId = new PlaceholderDataKey("command.id");
        public static readonly PlaceholderDataKey CommandName = new PlaceholderDataKey("command.name");
        public static readonly PlaceholderDataKey PlayerIndex = new PlaceholderDataKey("player.index");
        public static readonly PlaceholderDataKey PlayerDuration = new PlaceholderDataKey("timespan");
        public static readonly PlaceholderDataKey MaxPage = new PlaceholderDataKey("page.max");
        public static readonly PlaceholderDataKey MessageState = new PlaceholderDataKey("message.state");
    }
}