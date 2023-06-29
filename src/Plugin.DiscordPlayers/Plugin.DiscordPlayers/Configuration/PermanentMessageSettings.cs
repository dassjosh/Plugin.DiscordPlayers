using Newtonsoft.Json;
using Oxide.Ext.Discord.Entities;

namespace DiscordPlayersPlugin.Configuration
{
    public class PermanentMessageSettings : BaseMessageSettings
    {
        [JsonProperty(PropertyName = "Enabled")]
        public bool Enabled { get; set; }
        
        [JsonProperty(PropertyName = "Template Name (Must Be Unique)")]
        public string TemplateName { get; set; }
            
        [JsonProperty(PropertyName = "Permanent Message Channel ID")]
        public Snowflake ChannelId { get; set; }

        [JsonProperty(PropertyName = "Update Rate (Minutes)")]
        public float UpdateRate { get; set; }

        [JsonConstructor]
        public PermanentMessageSettings() { }
        
        public PermanentMessageSettings(PermanentMessageSettings settings) : base(settings)
        {
            Enabled = settings?.Enabled ?? false;
            TemplateName = settings?.TemplateName ?? "Permanent";
            ChannelId = settings?.ChannelId ?? default(Snowflake);
            UpdateRate = settings?.UpdateRate ?? 1f;
        }
        
        public override bool IsPermanent() => true;
        public override string GetTemplateName() => TemplateName;
    }
}