using Oxide.Ext.Discord.Libraries;

namespace DiscordPlayersPlugin.Templates;

public static class TemplateKeys
{
    public static class Errors
    {
        private const string Base = nameof(Errors) + ".";

        public static readonly TemplateKey UnknownState = new(Base + nameof(UnknownState));
        public static readonly TemplateKey UnknownCommand = new(Base + nameof(UnknownCommand));
    }
}