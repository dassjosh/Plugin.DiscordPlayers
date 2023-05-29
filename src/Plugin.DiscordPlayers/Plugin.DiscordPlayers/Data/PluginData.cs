using DiscordPlayersPlugin.Configuration;
using Oxide.Ext.Discord.Entities;
using Oxide.Plugins;

namespace DiscordPlayersPlugin.Data
{
    public class PluginData
    {
        public Hash<string, PermanentMessageData> PermanentMessageIds = new Hash<string, PermanentMessageData>();
        public Hash<string, Snowflake> RegisteredCommands = new Hash<string, Snowflake>();

        public PermanentMessageData GetPermanentMessage(PermanentMessageSettings config)
        {
            return PermanentMessageIds[config.TemplateName];
        }

        public void SetPermanentMessage(PermanentMessageSettings config, PermanentMessageData data)
        {
            PermanentMessageIds[config.TemplateName] = data;
        }
    }
}