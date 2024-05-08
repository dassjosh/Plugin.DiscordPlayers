using System.Collections.Generic;
using System.Linq;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Data;
using DiscordPlayersPlugin.Handlers;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;

namespace DiscordPlayersPlugin.Plugins;

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
            
        ApplicationCommandBuilder builder = new(command, "Shows players currently on the server", ApplicationCommandType.ChatInput);
        builder.AllowInDirectMessages(settings.AllowInDm);
        builder.AddDefaultPermissions(PermissionFlags.None);

        CommandCreate cmd = builder.Build();
        DiscordCommandLocalization loc = builder.BuildCommandLocalization();

        _commandCache[command] = settings;
            
        _localizations.RegisterCommandLocalizationAsync(this, settings.GetTemplateName(), loc, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0)).Then(_ =>
        {
            _localizations.ApplyCommandLocalizationsAsync(this, cmd, settings.GetTemplateName()).Then(() =>
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
                PrintWarning($"Failed to find channel ID: {config.ChannelId} in Guild: {created.Name}");
                continue;
            }

            _commandCache[config.GetTemplateName().Name] = config;
            PermanentMessageData existing = _pluginData.GetPermanentMessage(config);
            if (existing != null)
            {
                channel.GetMessage(Client, existing.MessageId)
                    .Then(message =>
                    {
                        _permanentHandler[message.Id] = new PermanentMessageHandler(Client, new MessageCache(config), config.UpdateRate, message);
                    })
                    .Catch<ResponseError>(error =>
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
        MessageCache cache = new(config);
        
        CreateMessage<MessageCreate>(cache, null, null, create =>
        {
            channel.CreateMessage(Client, create).Then(message =>
            {
                _pluginData.SetPermanentMessage(config, new PermanentMessageData
                {
                    MessageId = message.Id
                });
                _permanentHandler[message.Id] = new PermanentMessageHandler(Client, cache, config.UpdateRate, message);
                SaveData();
            });
        });
    }
        
    private void HandleApplicationCommand(DiscordInteraction interaction, InteractionDataParsed parsed)
    {
        MessageCache cache = GetCache(interaction);
        if (cache == null)
        {
            PrintError("Cache is null!!");
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