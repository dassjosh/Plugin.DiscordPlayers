using System;
using System.Collections.Generic;
using System.Linq;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Interfaces.Entities.Templates;
using Oxide.Ext.Discord.Libraries.Placeholders;
using Oxide.Ext.Discord.Libraries.Pooling;
using Oxide.Ext.Discord.Libraries.Templates.Messages.Bulk;
using Oxide.Ext.Discord.Promise;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        public void CreateMessage<T>(MessageCache cache, DiscordInteraction interaction, T create, Action<T> callback) where T : class, IDiscordMessageTemplate, new()
        {
            List<IPlayer> onlineList = GetPlayerList(cache);
            int embedLimit = (onlineList.Count - 1) / cache.Settings.EmbedFieldLimit;
            embedLimit = embedLimit.Clamp(1, 10);

            int maxPage = (onlineList.Count - 1) / cache.Settings.MaxPlayersPerPage;
            cache.State.ClampPage(maxPage);
            
            PlaceholderData data = GetDefault(cache, interaction, maxPage + 1);
            data.ManualPool();

            T message = CreateMessage(cache.Settings, data, interaction, create);
            CreateEmbeds(cache.Settings, data, interaction, embedLimit).Then(embeds =>
            {
                message.Embeds = embeds;
                CreateFields(cache, data, interaction, onlineList).Then(fields =>
                {
                    ProcessEmbeds(embeds, fields, cache.Settings.EmbedFieldLimit);
                    callback.Invoke(message);
                    data.Dispose();
                    _pool.FreeList(onlineList);
                });
            });
        }

        public List<IPlayer> GetPlayerList(MessageCache cache)
        {
            int perPage = cache.Settings.MaxPlayersPerPage;
            return _playerCache.GetList(cache.State.Sort, cache.Settings.ShowAdmins).Skip(cache.State.Page * perPage).Take(perPage).ToPooledList(_pool);
        }

        public T CreateMessage<T>(BaseMessageSettings settings, PlaceholderData data, DiscordInteraction interaction, T message) where T : class, IDiscordMessageTemplate, new()
        {
            if (settings.IsPermanent())
            {
                return _templates.GetGlobalEntity(this, settings.NameCache.TemplateName, data, message);
            }

            return _templates.GetLocalizedEntity(this, settings.NameCache.TemplateName, interaction, data, message);
        }

        public IDiscordPromise<List<DiscordEmbed>> CreateEmbeds(BaseMessageSettings settings, PlaceholderData data, DiscordInteraction interaction, int embedLimit)
        {
            BulkTemplateRequest embedRequest = BulkTemplateRequest.Create(this);
            for (int i = 0; i < embedLimit; i++)
            {
                embedRequest.AddItem(settings.NameCache.GetEmbedName(i), data);
            }
            
            if (settings.IsPermanent())
            {
                _embed.GetGlobalBulkEntityAsync(this, embedRequest);
            }

            return _embed.GetLocalizedBulkEntityAsync(this, embedRequest);
        }

        public IDiscordPromise<List<EmbedField>> CreateFields(MessageCache cache, PlaceholderData data, DiscordInteraction interaction, List<IPlayer> onlineList)
        {
            int playerIndex = cache.Settings.MaxPlayersPerPage * cache.State.Page;
            BulkTemplateRequest fieldRequest = BulkTemplateRequest.Create(this);
            string template = cache.Settings.NameCache.TemplateName;
            for (int index = 0; index < onlineList.Count; index++)
            {
                fieldRequest.AddItem(template, CloneForPlayer(data, onlineList[index], playerIndex));
            }

            if (cache.Settings.IsPermanent())
            {
                _field.GetGlobalBulkEntityAsync(this, fieldRequest);
            }

            return _field.GetLocalizedBulkEntityAsync(this, fieldRequest);
        }
        
        public void ProcessEmbeds(List<DiscordEmbed> embeds, List<EmbedField> fields, int fieldLimit)
        {
            int embedIndex = 0;
            DiscordEmbed embed = null;
            for (int i = 0; i < fields.Count; i++)
            {
                if (i % fieldLimit == 0)
                {
                    embed = embeds[embedIndex];
                    if (embed.Fields == null)
                    {
                        embed.Fields = new List<EmbedField>();
                    }
                    embedIndex += 1;
                }
                
                embed.Fields.Add(fields[i]);
            }
        }
    }
}