using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Plugins;
using Oxide.Ext.Discord.Entities.Api;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Plugins;

namespace DiscordPlayersPlugin.Handlers
{
    public class PermanentMessageHandler
    {
        private readonly MessageCache _cache;
        private readonly DiscordMessage _message;
        private readonly Timer _timer;

        public PermanentMessageHandler(MessageCache cache, float updateRate, DiscordMessage message)
        {
            _cache = cache;
            _message = message;
            _timer = DiscordPlayers.Instance.Timer.Every(updateRate, SendUpdate);
            SendUpdate();
        }

        private void SendUpdate()
        {
            DiscordPlayers.Instance.CreateMessage(_cache, null, _message, message =>
            {
                _message.EditMessage(DiscordPlayers.Instance._client, null, error =>
                {
                    if (error.HttpStatusCode == DiscordHttpStatusCode.NotFound)
                    {
                        _timer?.Destroy();
                    }
                });
            });
        }
    }
}