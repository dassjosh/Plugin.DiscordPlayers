using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.State;

namespace DiscordPlayersPlugin.Cache
{
    public class MessageCache
    {
        public readonly BaseMessageSettings Settings;
        public readonly MessageState State;

        public MessageCache(BaseMessageSettings settings, MessageState state = null)
        {
            Settings = settings;
            State = state ?? MessageState.CreateNew(Settings.GetTemplateName());
        }
    }
}