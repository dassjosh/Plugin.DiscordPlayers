using System.ComponentModel;
using DiscordPlayersPlugin.Cache;
using Newtonsoft.Json;

namespace DiscordPlayersPlugin.Configuration
{
    public abstract class BaseMessageSettings
    {
        [JsonProperty(PropertyName = "Display Admins In The Player List", Order = 1001)]
        public bool ShowAdmins { get; set; }
        
        [DefaultValue(25)]
        [JsonProperty(PropertyName = "Players Per Embed (0 - 25)", Order = 1002)]
        public int EmbedFieldLimit { get; set; }
        
        [DefaultValue(1)]
        [JsonProperty(PropertyName = "Embeds Per Message (1-10)", Order = 1003)]
        public int EmbedsPerMessage { get; set; }

        [JsonIgnore]
        public TemplateNameCache NameCache { get; private set; }
        
        [JsonIgnore]
        public int MaxPlayersPerPage { get; private set; }
        
        public abstract bool IsPermanent();
        public abstract string GetTemplateName();

        [JsonConstructor]
        public BaseMessageSettings() { }
        
        public BaseMessageSettings(BaseMessageSettings settings)
        {
            ShowAdmins = settings?.ShowAdmins ?? true;
            EmbedFieldLimit = settings?.EmbedFieldLimit ?? 25;
            EmbedsPerMessage = settings?.EmbedsPerMessage ?? 1;
        }

        public void Initialize()
        {
            NameCache = new TemplateNameCache(this);
            MaxPlayersPerPage = EmbedFieldLimit * EmbedsPerMessage;
        }
    }
}