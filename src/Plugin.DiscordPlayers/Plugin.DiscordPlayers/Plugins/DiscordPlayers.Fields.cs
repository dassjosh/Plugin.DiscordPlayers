using System;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Data;
using DiscordPlayersPlugin.Enums;
using DiscordPlayersPlugin.Handlers;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries.AppCommands;
using Oxide.Ext.Discord.Libraries.Placeholders;
using Oxide.Ext.Discord.Libraries.Templates.Commands;
using Oxide.Ext.Discord.Libraries.Templates.Embeds;
using Oxide.Ext.Discord.Libraries.Templates.Messages;
using Oxide.Ext.Discord.Pooling;
using Oxide.Plugins;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        public DiscordClient Client { get; set; }
        
#pragma warning disable CS0649
        // ReSharper disable once InconsistentNaming
        [PluginReference] private Plugin Clans;
#pragma warning restore CS0649

        private PluginConfig _pluginConfig; //Plugin Config
        private PluginData _pluginData;
        
        private readonly DiscordAppCommand _appCommand = GetLibrary<DiscordAppCommand>();
        private readonly DiscordPlaceholders _placeholders = GetLibrary<DiscordPlaceholders>();
        private readonly DiscordMessageTemplates _templates = GetLibrary<DiscordMessageTemplates>();
        private readonly DiscordEmbedTemplates _embed = GetLibrary<DiscordEmbedTemplates>();
        private readonly DiscordEmbedFieldTemplates _field = GetLibrary<DiscordEmbedFieldTemplates>();
        private readonly DiscordCommandLocalizations _localizations = GetLibrary<DiscordCommandLocalizations>();
        private DiscordPluginPool _pool;
        private readonly BotConnection _discordSettings = new BotConnection();

        private readonly Hash<string, DateTime> _onlineSince = new Hash<string, DateTime>();
        private readonly Hash<Snowflake, PermanentMessageHandler> _permanentState = new Hash<Snowflake, PermanentMessageHandler>();
        private readonly Hash<Snowflake, MessageCache> _messageCache = new Hash<Snowflake, MessageCache>();
        private readonly OnlinePlayerCache _playerCache = new OnlinePlayerCache();

        private const string BaseCommand = nameof(DiscordPlayers) + ".";
        private const string BackCommand = BaseCommand + "Back";
        private const string RefreshCommand = BaseCommand + "Refresh";
        private const string ForwardCommand = BaseCommand + "Forward";
        private const string ChangeSort = BaseCommand + "Sort";

        public readonly SortBy[] SortByList = (SortBy[])Enum.GetValues(typeof(SortBy));

        public static DiscordPlayers Instance;

        public PluginTimers Timer => timer;
    }
}