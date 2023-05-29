using Newtonsoft.Json;

namespace DiscordPlayersPlugin.Configuration
{
    public class FormatSettings
    {
        [JsonProperty("Clan Tag Format")]
        public string ClanTagFormat { get; set; }

        [JsonConstructor]
        public FormatSettings() {}

        public FormatSettings(FormatSettings settings)
        {
            ClanTagFormat = settings?.ClanTagFormat ?? "[{0}] ";
        }
    }
}