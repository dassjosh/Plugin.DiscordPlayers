using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Ext.Discord.Logging;

namespace DiscordPlayersPlugin.Configuration
{
    public class PluginConfig
    {
        [DefaultValue("")]
        [JsonProperty(PropertyName = "Discord Bot Token")]
        public string DiscordApiKey { get; set; }

        [JsonProperty(PropertyName = "Command Messages")]
        public List<CommandSettings> CommandMessages { get; set; }
        
        [JsonProperty(PropertyName = "Permanent Messages")]
        public List<PermanentMessageSettings> Permanent { get; set; }
        
        [JsonProperty(PropertyName = "Format Settings")]
        public FormatSettings Formats { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DiscordLogLevel.Info)]
        [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
        public DiscordLogLevel ExtensionDebugging { get; set; }
    }
}