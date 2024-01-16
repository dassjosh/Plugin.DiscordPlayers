using System;
using System.Collections.Generic;
using System.Linq;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;

namespace DiscordPlayersPlugin.Plugins;

public partial class DiscordPlayers
{
    public void CreateMessage<T>(MessageCache cache, DiscordInteraction interaction, T create, Action<T> callback) where T : class, IDiscordMessageTemplate, new()
    {
        List<IPlayer> allList = GetPlayerList(cache);
        int perPage = cache.Settings.EmbedFieldLimit;
        List<IPlayer> pageList = allList.Skip(cache.State.Page * perPage).Take(perPage).ToPooledList(Pool);

        int maxPage = (allList.Count - 1) / cache.Settings.EmbedFieldLimit;
        cache.State.ClampPage((short)maxPage);
            
        PlaceholderData data = GetDefault(cache, interaction, maxPage + 1);
        data.ManualPool();

        T message = CreateMessage(cache.Settings, data, interaction, create);
        message.AllowedMentions = AllowedMentions.None;
        SetButtonState(message, BackCommand, cache.State.Page > 0);
        SetButtonState(message, ForwardCommand, cache.State.Page < maxPage);

        DiscordEmbed embed = CreateEmbeds(cache.Settings, data, interaction);
            
        message.Embeds = new List<DiscordEmbed>{embed};
        CreateFields(cache, data, interaction, pageList).Then(fields =>
        {
            ProcessEmbeds(embed, fields);
        }).Finally(() =>
        {
            callback.Invoke(message);
            data.Dispose();
            Pool.FreeList(pageList);
        });
    }

    public List<IPlayer> GetPlayerList(MessageCache cache)
    {
        return _playerCache.GetList(cache.State.Sort, cache.Settings.ShowAdmins);
    }

    public T CreateMessage<T>(BaseMessageSettings settings, PlaceholderData data, DiscordInteraction interaction, T message) where T : class, IDiscordMessageTemplate, new()
    {
        if (settings.IsPermanent())
        {
            return _templates.GetGlobalTemplate(this, settings.GetTemplateName()).ToMessage(data, message);
        }

        return _templates.GetLocalizedTemplate(this, settings.GetTemplateName(), interaction).ToMessage(data, message);
    }

    public void SetButtonState(IDiscordMessageTemplate message, string command, bool enabled)
    {
        for (int index = 0; index < message.Components.Count; index++)
        {
            ActionRowComponent row = message.Components[index];
            for (int i = 0; i < row.Components.Count; i++)
            {
                BaseComponent component = row.Components[i];
                if (component is ButtonComponent)
                {
                    ButtonComponent button = (ButtonComponent)component;
                    if (button.CustomId.StartsWith(command))
                    {
                        button.Disabled = !enabled;
                        return;
                    }
                }
            }
        }
    }

    public DiscordEmbed CreateEmbeds(BaseMessageSettings settings, PlaceholderData data, DiscordInteraction interaction)
    {
        string name = settings.GetTemplateName();
        return settings.IsPermanent() ? _embed.GetGlobalTemplate(this, name).ToEntity(data) : _embed.GetLocalizedTemplate(this, name, interaction).ToEntity(data);
    }

    public IPromise<List<EmbedField>> CreateFields(MessageCache cache, PlaceholderData data, DiscordInteraction interaction, List<IPlayer> onlineList)
    {
        DiscordEmbedFieldTemplate template;
        if (cache.Settings.IsPermanent())
        {
            template = _field.GetGlobalTemplate(this, cache.Settings.GetTemplateName());
        }
        else
        {
            template = _field.GetLocalizedTemplate(this, cache.Settings.GetTemplateName(), interaction);
        }
            
        List<PlaceholderData> placeholders = new();

        for (int index = 0; index < onlineList.Count; index++)
        {
            PlaceholderData playerData = CloneForPlayer(data, onlineList[index], cache.State.Page * cache.Settings.EmbedFieldLimit + index + 1);
            placeholders.Add(playerData);
        }
            
        return template.ToEntityBulk(placeholders);
    }
        
    public void ProcessEmbeds(DiscordEmbed embed, List<EmbedField> fields)
    {
        embed.Fields ??= new List<EmbedField>();
        embed.Fields.AddRange(fields);
    }
}