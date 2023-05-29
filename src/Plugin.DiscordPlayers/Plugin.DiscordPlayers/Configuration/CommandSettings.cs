using Newtonsoft.Json;

namespace DiscordPlayersPlugin.Configuration
{
    public class CommandSettings : BaseMessageSettings
    {
        [JsonProperty(PropertyName = "Command Name (Must Be Unique)")]
        public string Command { get; set; }
        
        [JsonProperty(PropertyName = "Allow Command In Direct Messages")]
        public bool AllowInDm { get; set; }

        [JsonConstructor]
        public CommandSettings() { }
        
        public CommandSettings(CommandSettings settings) : base(settings)
        {
            Command = settings?.Command ?? "players";
            AllowInDm = settings?.AllowInDm ?? true;
        }

        public override bool IsPermanent() => false;
        public override string GetTemplateName() => Command;
    }
}