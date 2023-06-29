using System;
using System.Collections.Generic;
using System.Linq;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Interfaces.Entities.Messages;
using Oxide.Ext.Discord.Interfaces.Promises;
using Oxide.Ext.Discord.Libraries.Placeholders;
using Oxide.Ext.Discord.Libraries.Templates.Embeds;

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
            cache.State.ClampPage((short)maxPage);
            
            PlaceholderData data = GetDefault(cache, interaction, maxPage + 1);
            data.ManualPool();

            T message = CreateMessage(cache.Settings, data, interaction, create);
            SetButtonState(message, BackCommand, cache.State.Page > 0);
            SetButtonState(message, ForwardCommand, cache.State.Page < maxPage);

            List<DiscordEmbed> embeds = CreateEmbeds(cache.Settings, data, interaction, embedLimit);
            
            message.Embeds = embeds;
            CreateFields(cache, data, interaction, onlineList).Then(fields =>
            {
                ProcessEmbeds(embeds, fields, cache.Settings.EmbedFieldLimit);
                callback.Invoke(message);
                data.Dispose();
                Pool.FreeList(onlineList);
            }).Catch(ex =>
            {
                PrintError(ex.ToString());
            });
        }

        public List<IPlayer> GetPlayerList(MessageCache cache)
        {
            int perPage = cache.Settings.MaxPlayersPerPage;
            return _playerCache.GetList(cache.State.Sort, cache.Settings.ShowAdmins).Skip(cache.State.Page * perPage).Take(perPage).ToPooledList(Pool);
        }

        public T CreateMessage<T>(BaseMessageSettings settings, PlaceholderData data, DiscordInteraction interaction, T message) where T : class, IDiscordMessageTemplate, new()
        {
            if (settings.IsPermanent())
            {
                return _templates.GetGlobalTemplate(this, settings.NameCache.TemplateName).ToMessage(data, message);
            }

            return _templates.GetLocalizedTemplate(this, settings.NameCache.TemplateName, interaction).ToMessage(data, message);
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

        public List<DiscordEmbed> CreateEmbeds(BaseMessageSettings settings, PlaceholderData data, DiscordInteraction interaction, int embedLimit)
        {
            List<DiscordEmbed> embeds = new List<DiscordEmbed>();
            for (int i = 0; i < embedLimit; i++)
            {
                string name = settings.NameCache.GetEmbedName(i);
                DiscordEmbed embed;
                if (settings.IsPermanent())
                {
                    embed = _embed.GetGlobalTemplate(this, name).ToEntity(data);
                }
                else
                {
                    embed = _embed.GetLocalizedTemplate(this, name, interaction).ToEntity(data);
                }
                
                embeds.Add(embed);
            }

            return embeds;
        }

        public IPromise<List<EmbedField>> CreateFields(MessageCache cache, PlaceholderData data, DiscordInteraction interaction, List<IPlayer> onlineList)
        {
            DiscordEmbedFieldTemplate template;
            if (cache.Settings.IsPermanent())
            {
                template = _field.GetGlobalTemplate(this, cache.Settings.NameCache.TemplateName);
            }
            else
            {
                template = _field.GetLocalizedTemplate(this, cache.Settings.NameCache.TemplateName, interaction);
            }
            
            List<PlaceholderData> placeholders = new List<PlaceholderData>();

            for (int index = 0; index < onlineList.Count; index++)
            {
                placeholders.Add(CloneForPlayer(data, onlineList[index], index + 1));
            }
            
            //Puts($"{placeholders.Count}");
            return template.ToEntityBulk(placeholders);
        }
        
        public void ProcessEmbeds(List<DiscordEmbed> embeds, List<EmbedField> fields, int fieldLimit)
        {
            int embedIndex = 0;
            DiscordEmbed embed = null;
            Puts($"{fields.Count}");
            for (int i = 0; i < fields.Count; i++)
            { 
                if (i % fieldLimit == 0)
                {
                    embed = embeds[embedIndex];
                    if (embed.Fields == null)
                    {
                        embed.Fields = new List<EmbedField>();
                    }

                    embedIndex++;
                }
                
                embed.Fields.Add(fields[i]);
            }
        }
    }
}