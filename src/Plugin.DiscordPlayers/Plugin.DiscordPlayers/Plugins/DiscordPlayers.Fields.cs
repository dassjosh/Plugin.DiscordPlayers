using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Data;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Oxide.Ext.Discord.Types;
using Oxide.Plugins;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        public DiscordClient Client { get; set; }
        public DiscordPluginPool Pool { get; set; }

        private PluginConfig _pluginConfig; //Plugin Config
        private PluginData _pluginData;
        
        private readonly DiscordAppCommand _appCommand = GetLibrary<DiscordAppCommand>();
        private readonly DiscordPlaceholders _placeholders = GetLibrary<DiscordPlaceholders>();
        private readonly DiscordMessageTemplates _templates = GetLibrary<DiscordMessageTemplates>();
        private readonly DiscordEmbedTemplates _embed = GetLibrary<DiscordEmbedTemplates>();
        private readonly DiscordEmbedFieldTemplates _field = GetLibrary<DiscordEmbedFieldTemplates>();
        private readonly DiscordCommandLocalizations _localizations = GetLibrary<DiscordCommandLocalizations>();

        private readonly BotConnection _discordSettings = new BotConnection();
        
        private readonly Hash<Snowflake, MessageCache> _messageCache = new Hash<Snowflake, MessageCache>();
        private readonly Hash<string, BaseMessageSettings> _commandCache = new Hash<string, BaseMessageSettings>();
        private readonly OnlinePlayerCache _playerCache = new OnlinePlayerCache();

        private const string BaseCommand = nameof(DiscordPlayers) + ".";
        private const string BackCommand = BaseCommand + "B";
        private const string RefreshCommand = BaseCommand + "R";
        private const string ForwardCommand = BaseCommand + "F";
        private const string ChangeSort = BaseCommand + "S";

        public static DiscordPlayers Instance;

        public PluginTimers Timer => timer;
    }
}