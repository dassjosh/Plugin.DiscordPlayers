using System.Collections.Generic;
using System.Linq;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Data;
using DiscordPlayersPlugin.Handlers;
using DiscordPlayersPlugin.Templates;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Attributes.ApplicationCommands;
using Oxide.Ext.Discord.Builders.ApplicationCommands;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Api;
using Oxide.Ext.Discord.Entities.Applications;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Gateway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Interactions.ApplicationCommands;
using Oxide.Ext.Discord.Entities.Interactions.Response;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Permissions;
using Oxide.Ext.Discord.Libraries.Templates;
using Oxide.Ext.Discord.Libraries.Templates.Commands;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        [HookMethod(DiscordExtHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            DiscordApplication app = Client.Bot.Application;
            
            foreach (CommandSettings command in _pluginConfig.CommandMessages)
            {
                CreateApplicationCommand(command);
            }

            foreach (KeyValuePair<string, Snowflake> command in _pluginData.RegisteredCommands.ToList())
            {
                if (_pluginConfig.CommandMessages.All(c => c.Command != command.Key))
                {
                    if (command.Value.IsValid())
                    {
                        app.GetGlobalCommand(Client, command.Value).Then(oldCommand => oldCommand.Delete(Client).Then(() =>
                        {
                            _pluginData.RegisteredCommands.Remove(command);
                            SaveData();
                        }).Catch<ResponseError>(error =>
                        {
                            if (error.DiscordError?.Code == 10063)
                            {
                                _pluginData.RegisteredCommands.Remove(command);
                                SaveData();
                                error.SuppressErrorMessage();
                            }
                        }));
                    }
                }
            }

            Puts($"{Title} Ready");
        }

        public void CreateApplicationCommand(CommandSettings settings)
        {
            string command = settings.Command;
            if (string.IsNullOrEmpty(command))
            {
                return;
            }
            
            ApplicationCommandBuilder builder = new ApplicationCommandBuilder(command, "Shows players currently on the server", ApplicationCommandType.ChatInput);
            builder.AllowInDirectMessages(settings.AllowInDm);
            builder.AddDefaultPermissions(PermissionFlags.None);

            CommandCreate cmd = builder.Build();
            DiscordCommandLocalization loc = builder.BuildCommandLocalization();

            _localizations.RegisterCommandLocalizationAsync(this, settings.NameCache.TemplateName, loc, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0)).Then(() =>
            {
                _localizations.ApplyCommandLocalizationsAsync(this, cmd, settings.NameCache.TemplateName).Then(() =>
                {
                    Client.Bot.Application.CreateGlobalCommand(Client, builder.Build()).Then(appCommand =>
                    {
                        _pluginData.RegisteredCommands[command] = appCommand.Id;
                        SaveData();
                    });
                });
            });
            
            _appCommand.AddApplicationCommand(this, Client.Bot.Application.Id, HandleApplicationCommand, command);
        }

        [HookMethod(DiscordExtHooks.OnDiscordGuildCreated)]
        private void OnDiscordGuildCreated(DiscordGuild created)
        {
            foreach (PermanentMessageSettings config in _pluginConfig.Permanent)
            {
                if (!config.Enabled || !config.ChannelId.IsValid())
                {
                    continue;
                }

                DiscordChannel channel = created.GetChannel(config.ChannelId);
                if (channel == null)
                {
                    continue;
                }

                PermanentMessageData existing = _pluginData.GetPermanentMessage(config);
                if (existing != null)
                {
                    channel.GetMessage(Client, existing.MessageId).Then(message =>
                    {
                        _permanentState[message.Id] = new PermanentMessageHandler(new MessageCache(config), config.UpdateRate, message);
                    }).Catch<ResponseError>(error =>
                    {
                        if (error.HttpStatusCode == DiscordHttpStatusCode.NotFound)
                        {
                            CreatePermanentMessage(config, channel);
                            error.SuppressErrorMessage();
                        }
                    });
                }
                else
                {
                    CreatePermanentMessage(config, channel);
                }
            }
        }

        private void CreatePermanentMessage(PermanentMessageSettings config, DiscordChannel channel)
        {
            MessageCache cache = new MessageCache(config);

            CreateMessage<MessageCreate>(cache, null, null, create =>
            {
                channel.CreateMessage(Client, create).Then(message =>
                {
                    _pluginData.SetPermanentMessage(config, new PermanentMessageData
                    {
                        MessageId = message.Id
                    });
                    SaveData();
                    _permanentState[message.Id] = new PermanentMessageHandler(cache, config.UpdateRate, message);
                });
            });
        }
        
        private void HandleApplicationCommand(DiscordInteraction interaction, InteractionDataParsed parsed)
        {
            MessageCache cache = GetCache(interaction);
            if (cache == null)
            {
                return;
            }
            
            CreateMessage<InteractionCallbackData>(cache, interaction, null, create =>
            {
                interaction.CreateResponse(Client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = create
                });
            });
        }

        [DiscordMessageComponentCommand(BackCommand)]
        private void HandleBackCommand(DiscordInteraction interaction)
        {
            MessageCache cache = GetCache(interaction);
            if (cache == null)
            {
                return;
            }
            
            cache.State.PreviousPage();
            HandleUpdate(interaction, cache);
        }
        
        [DiscordMessageComponentCommand(RefreshCommand)]
        private void HandleRefreshCommand(DiscordInteraction interaction)
        {
            MessageCache cache = GetCache(interaction);
            if (cache == null)
            {
                return;
            }
            
            HandleUpdate(interaction, cache);
        }
        
        [DiscordMessageComponentCommand(ForwardCommand)]
        private void HandleForwardCommand(DiscordInteraction interaction)
        {
            MessageCache cache = GetCache(interaction);
            if (cache == null)
            {
                return;
            }
            
            cache.State.NextPage();
            HandleUpdate(interaction, cache);
        }
        
        [DiscordMessageComponentCommand(ChangeSort)]
        private void HandleChangeSortCommand(DiscordInteraction interaction)
        {
            MessageCache cache = GetCache(interaction);
            if (cache == null)
            {
                return;
            }
            
            cache.State.NextSort();
            HandleUpdate(interaction, cache);
        }

        private void HandleUpdate(DiscordInteraction interaction, MessageCache cache)
        {
            CreateMessage<InteractionCallbackData>(cache, interaction, null, create =>
            {
                interaction.CreateResponse(Client, new InteractionResponse
                {
                    Type = InteractionResponseType.UpdateMessage,
                    Data = create
                });
            });
        }
    }
}