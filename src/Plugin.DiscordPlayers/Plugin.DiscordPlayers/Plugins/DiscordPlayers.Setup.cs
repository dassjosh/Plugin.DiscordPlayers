using System;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Data;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Libraries.Pooling;
using UnityEngine;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        private void Init()
        {
            Instance = this;
            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;

            _pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name) ?? new PluginData();
            _pool = GetLibrary<DiscordPool>().GetOrCreate(this);
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

                if (message.EmbedsPerMessage > 10)
                {
                    PrintWarning($"Embeds Per Message cannot be greater than 10 for command {message.Command}");
                }
                else if (message.EmbedsPerMessage < 1)
                {
                    PrintWarning($"Embeds Per Message cannot be less than 1 for command {message.Command}");
                }

                message.EmbedFieldLimit = Mathf.Clamp(message.EmbedFieldLimit, 0, 25);
                message.EmbedsPerMessage = Mathf.Clamp(message.EmbedsPerMessage, 1, 10);
            }

#if RUST
            foreach (Network.Connection connection in Network.Net.sv.connections)
            {
                _onlineSince[connection.ownerid.ToString()] = DateTime.UtcNow - TimeSpan.FromSeconds(connection.GetSecondsConnected());
            }
#else
            foreach (IPlayer player in players.Connected)
            {
                _onlineSince[player.Id] = DateTime.UtcNow;
            }
#endif

            _client.Connect(_discordSettings);
        }

        private void OnUserConnected(IPlayer player)
        {
            _onlineSince[player.Id] = DateTime.UtcNow;
        }

        private void OnUserDisconnected(IPlayer player)
        {
            _onlineSince.Remove(player.Id);
        }

        private void Unload()
        {
            SaveData();
            Instance = null;
        }
    }
}