using System.ComponentModel;
using Newtonsoft.Json;
using Oxide.Ext.Discord.Libraries;

namespace DiscordPlayersPlugin.Configuration;

public abstract class BaseMessageSettings
{
    [JsonProperty(PropertyName = "Display Admins In The Player List", Order = 1001)]
    public bool ShowAdmins { get; set; }
        
    [DefaultValue(25)]
    [JsonProperty(PropertyName = "Players Per Embed (0 - 25)", Order = 1002)]
    public int EmbedFieldLimit { get; set; }

    public abstract bool IsPermanent();
    public abstract TemplateKey GetTemplateName();

    [JsonConstructor]
    public BaseMessageSettings() { }
        
    public BaseMessageSettings(BaseMessageSettings settings)
    {
        ShowAdmins = settings?.ShowAdmins ?? true;
        EmbedFieldLimit = settings?.EmbedFieldLimit ?? 25; 
    }
}