using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Data;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Entities;
using UnityEngine;

namespace DiscordPlayersPlugin.Plugins;

public partial class DiscordPlayers
{
    private void Init()
    {
        Instance = this;
        _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
        _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;
        _discordSettings.Intents = GatewayIntents.Guilds;

        _pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name) ?? new PluginData();
    }

    private void OnServerInitialized()
    {
        if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
        {
            PrintWarning("Please set the Discord Bot Token and reload the plugin");
            return;
        }
            
        _playerCache.Initialize(players.Connected);
            
        RegisterPlaceholders();
        RegisterTemplates();
            
        foreach (CommandSettings message in _pluginConfig.CommandMessages)
        {
            if (message.EmbedFieldLimit > 25)
            {
                PrintWarning($"Players For Embed cannot be greater than 25 for command {message.Command}");
            }
            else if (message.EmbedFieldLimit < 0)
            {
                PrintWarning($"Players For Embed cannot be less than 0 for command {message.Command}");
            }

            message.EmbedFieldLimit = Mathf.Clamp(message.EmbedFieldLimit, 0, 25);
        }

        Client.Connect(_discordSettings);
    }

    private void OnUserConnected(IPlayer player)
    {
        _playerCache.OnUserConnected(player);
    }

    private void OnUserDisconnected(IPlayer player)
    {
        _playerCache.OnUserDisconnected(player);
    }

    private void Unload()
    {
        SaveData();
        Instance = null;
    }
}