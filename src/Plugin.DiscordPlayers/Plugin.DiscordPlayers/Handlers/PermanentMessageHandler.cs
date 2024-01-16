using System;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Plugins;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Entities;
using Oxide.Plugins;

namespace DiscordPlayersPlugin.Handlers;

public class PermanentMessageHandler
{
    private readonly DiscordClient _client;
    private readonly MessageCache _cache;
    private readonly DiscordMessage _message;
    private readonly MessageUpdate _update = new();
    private readonly Timer _timer;
    private DateTime _lastUpdate;

    public PermanentMessageHandler(DiscordClient client, MessageCache cache, float updateRate, DiscordMessage message)
    {
        _client = client;
        _cache = cache;
        _message = message;
        _timer = DiscordPlayers.Instance.Timer.Every(updateRate * 60f, SendUpdate);
        SendUpdate();
    }

    private void SendUpdate()
    {
        if (_lastUpdate + TimeSpan.FromSeconds(5) > DateTime.UtcNow)
        {
            return;
        }
            
        _lastUpdate = DateTime.UtcNow;
            
        DiscordPlayers.Instance.CreateMessage(_cache, null, _update, message =>
        {
            _message.Edit(_client, message).Catch<ResponseError>(error =>
            {
                if (error.HttpStatusCode == DiscordHttpStatusCode.NotFound)
                {
                    _timer?.Destroy();
                }
            });
        });
    }
}