using System.Collections.Generic;
using DiscordPlayersPlugin.Configuration;
using Newtonsoft.Json;
using Oxide.Ext.Discord.Entities;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        protected override void LoadDefaultConfig() { }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.Formats = new FormatSettings(config.Formats);
            
            config.CommandMessages = config.CommandMessages ?? new List<CommandSettings>();
            if (config.CommandMessages.Count == 0)
            {
                config.CommandMessages.Add(new CommandSettings
                {
                    Command = "players",
                    ShowAdmins = true,
                    AllowInDm = true,
                    EmbedFieldLimit = 25,
                    EmbedsPerMessage = 1
                });

                config.CommandMessages.Add(new CommandSettings
                {
                    Command = "playersadmin",
                    ShowAdmins = true,
                    AllowInDm = true,
                    EmbedFieldLimit = 25,
                    EmbedsPerMessage = 1
                });
            }
            
            config.Permanent = config.Permanent ?? new List<PermanentMessageSettings>
            {
                new PermanentMessageSettings { Enabled = false, ChannelId = new Snowflake(123), UpdateRate = 1f }
            };

            for (int index = 0; index < config.CommandMessages.Count; index++)
            {
                CommandSettings settings = new CommandSettings(config.CommandMessages[index]);
                settings.Initialize();
                config.CommandMessages[index] = settings;
            }

            for (int index = 0; index < config.Permanent.Count; index++)
            {
                PermanentMessageSettings settings = new PermanentMessageSettings(config.Permanent[index]);
                settings.Initialize();
                config.Permanent[index] = settings;
            }

            return config;
        }
    }
}