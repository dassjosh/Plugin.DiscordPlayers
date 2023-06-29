using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Attributes.ApplicationCommands;
using Oxide.Ext.Discord.Attributes.Pooling;
using Oxide.Ext.Discord.Builders.ApplicationCommands;
using Oxide.Ext.Discord.Cache;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Api;
using Oxide.Ext.Discord.Entities.Applications;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Gateway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Interactions.ApplicationCommands;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Entities.Interactions.Response;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Oxide.Ext.Discord.Entities.Permissions;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Interfaces.Entities.Messages;
using Oxide.Ext.Discord.Interfaces.Promises;
using Oxide.Ext.Discord.Libraries.AppCommands;
using Oxide.Ext.Discord.Libraries.Placeholders;
using Oxide.Ext.Discord.Libraries.Placeholders.Default;
using Oxide.Ext.Discord.Libraries.Templates;
using Oxide.Ext.Discord.Libraries.Templates.Commands;
using Oxide.Ext.Discord.Libraries.Templates.Components;
using Oxide.Ext.Discord.Libraries.Templates.Embeds;
using Oxide.Ext.Discord.Libraries.Templates.Messages;
using Oxide.Ext.Discord.Logging;
using Oxide.Ext.Discord.Pooling;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEngine;

//DiscordPlayers created with PluginMerge v(1.0.5.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("Discord Players", "MJSU", "3.0.0")]
    [Description("Displays online players in discord")]
    public partial class DiscordPlayers : CovalencePlugin, IDiscordPlugin
    {
        #region Plugins\DiscordPlayers.Config.cs
        protected override void LoadDefaultConfig() { }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }
        
        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.CommandMessages = config.CommandMessages ?? new List<CommandSettings>();
            if (config.CommandMessages.Count == 0)
            {
                config.CommandMessages.Add(new CommandSettings
                {
                    Command = "players",
                    ShowAdmins = true,
                    AllowInDm = true,
                    EmbedFieldLimit = 25,
                    EmbedsPerMessage = 1
                });
                
                config.CommandMessages.Add(new CommandSettings
                {
                    Command = "playersadmin",
                    ShowAdmins = true,
                    AllowInDm = true,
                    EmbedFieldLimit = 25,
                    EmbedsPerMessage = 1
                });
            }
            
            config.Permanent = config.Permanent ?? new List<PermanentMessageSettings>
            {
                new PermanentMessageSettings
                {
                    Enabled = false,
                    ChannelId = new Snowflake(0),
                    UpdateRate = 1f,
                    EmbedFieldLimit = 25,
                    EmbedsPerMessage = 1
                }
            };
            
            for (int index = 0; index < config.CommandMessages.Count; index++)
            {
                CommandSettings settings = new CommandSettings(config.CommandMessages[index]);
                settings.Initialize();
                config.CommandMessages[index] = settings;
            }
            
            for (int index = 0; index < config.Permanent.Count; index++)
            {
                PermanentMessageSettings settings = new PermanentMessageSettings(config.Permanent[index]);
                settings.Initialize();
                config.Permanent[index] = settings;
            }
            
            return config;
        }
        #endregion

        #region Plugins\DiscordPlayers.DiscordHooks.cs
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
                        _permanentState[message.Id] = new PermanentMessageHandler(Client, new MessageCache(config), config.UpdateRate, message);
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
                    _permanentState[message.Id] = new PermanentMessageHandler(Client, cache, config.UpdateRate, message);
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
                Puts("A");
                interaction.CreateResponse(Client, new InteractionResponse
                {
                    Type = InteractionResponseType.UpdateMessage,
                    Data = create
                });
            });
        }
        #endregion

        #region Plugins\DiscordPlayers.Fields.cs
        public DiscordClient Client { get; set; }
        
        #pragma warning disable CS0649
        // ReSharper disable once InconsistentNaming
        [PluginReference] private Plugin Clans;
        #pragma warning restore CS0649
        
        private PluginConfig _pluginConfig; //Plugin Config
        private PluginData _pluginData;
        
        [DiscordPool]
        public DiscordPluginPool Pool;
        private readonly DiscordAppCommand _appCommand = GetLibrary<DiscordAppCommand>();
        private readonly DiscordPlaceholders _placeholders = GetLibrary<DiscordPlaceholders>();
        private readonly DiscordMessageTemplates _templates = GetLibrary<DiscordMessageTemplates>();
        private readonly DiscordEmbedTemplates _embed = GetLibrary<DiscordEmbedTemplates>();
        private readonly DiscordEmbedFieldTemplates _field = GetLibrary<DiscordEmbedFieldTemplates>();
        private readonly DiscordCommandLocalizations _localizations = GetLibrary<DiscordCommandLocalizations>();
        
        private readonly BotConnection _discordSettings = new BotConnection();
        
        private readonly Hash<Snowflake, PermanentMessageHandler> _permanentState = new Hash<Snowflake, PermanentMessageHandler>();
        private readonly Hash<Snowflake, MessageCache> _messageCache = new Hash<Snowflake, MessageCache>();
        private readonly OnlinePlayerCache _playerCache = new OnlinePlayerCache();
        
        private const string BaseCommand = nameof(DiscordPlayers) + ".";
        private const string BackCommand = BaseCommand + "B";
        private const string RefreshCommand = BaseCommand + "R";
        private const string ForwardCommand = BaseCommand + "F";
        private const string ChangeSort = BaseCommand + "S";
        
        public static DiscordPlayers Instance;
        
        public PluginTimers Timer => timer;
        #endregion

        #region Plugins\DiscordPlayers.Helpers.cs
        private const string PluginIcon = "https://assets.umod.org/images/icons/plugin/61354f8bd5faf.png";
        
        public MessageCache GetCache(DiscordInteraction interaction)
        {
            DiscordMessage message = interaction.Message;
            CommandSettings command;
            if (message == null)
            {
                InteractionDataParsed args = interaction.Parsed;
                command = _pluginConfig.CommandMessages.FirstOrDefault(c => c.Command == args.Command);
                return command != null ? new MessageCache(command) : null;
            }
            string customId = interaction.Data.CustomId;
            
            MessageCache cache = _messageCache[message.Id];
            if (cache != null)
            {
                return cache;
            }
            
            Puts(customId);
            string base64 = customId.Substring(customId.LastIndexOf(" ", StringComparison.Ordinal) + 1);
            Puts($"{base64}");
            MessageState state = MessageState.Create(base64);
            if (state == null)
            {
                SendResponse(interaction, TemplateKeys.Errors.UnknownState, GetDefault(interaction));
                return null;
            }
            
            Puts(state.ToString());
            command = _pluginConfig.CommandMessages.FirstOrDefault(c => c.Command == state.Command);
            if (command == null)
            {
                SendResponse(interaction, TemplateKeys.Errors.UnknownCommand, GetDefault(interaction).Add(PlaceholderKeys.CommandName, state.Command));
                return null;
            }
            
            cache = new MessageCache(command, state);
            _messageCache[message.Id] = cache;
            return cache;
        }
        
        public void SendResponse(DiscordInteraction interaction, string templateName, PlaceholderData data, MessageFlags flags = MessageFlags.Ephemeral)
        {
            interaction.CreateTemplateResponse(Client, InteractionResponseType.ChannelMessageWithSource, templateName, new InteractionCallbackData { Flags = flags }, data);
        }
        
        public string Lang(string key)
        {
            return lang.GetMessage(key, this);
        }
        
        public string Lang(string key, params object[] args)
        {
            try
            {
                return string.Format(Lang(key), args);
            }
            catch(Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }
        
        private void SaveData()
        {
            if (_pluginData != null)
            {
                Interface.Oxide.DataFileSystem.WriteObject(Name, _pluginData);
            }
        }
        
        public new void PrintError(string format, params object[] args) => base.PrintError(format, args);
        #endregion

        #region Plugins\DiscordPlayers.Lang.cs
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.SortByEnumName] = "Name",
                [LangKeys.SortByEnumTime] = "Time",
            }, this);
        }
        #endregion

        #region Plugins\DiscordPlayers.MessageHandling.cs
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
            
            Puts($"{placeholders.Count}");
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
        #endregion

        #region Plugins\DiscordPlayers.Placeholders.cs
        public void RegisterPlaceholders()
        {
            TimeSpanPlaceholders.RegisterPlaceholders(this, "discordplayers.duration", PlaceholderKeys.PlayerDuration);
            _placeholders.RegisterPlaceholder<int>(this, "discordplayers.player.index", PlaceholderKeys.PlayerIndex);
            _placeholders.RegisterPlaceholder<MessageState, int>(this, "discordplayers.state.page", GetPage);
            _placeholders.RegisterPlaceholder<MessageState, string>(this, "discordplayers.state.sort", GetSort);
            _placeholders.RegisterPlaceholder<string>(this, "discordplayers.command.id", PlaceholderKeys.CommandId);
            _placeholders.RegisterPlaceholder<string>(this, "discordplayers.command.name", PlaceholderKeys.CommandName);
            _placeholders.RegisterPlaceholder<int>(this, "discordplayers.page.max", PlaceholderKeys.MaxPage);
        }
        
        public int GetPage(MessageState embed) => embed.Page;
        public string GetSort(PlaceholderState state, MessageState embed)
        {
            DiscordInteraction interaction = state.Data.Get<DiscordInteraction>();
            string key = embed.Sort == SortBy.Name ? LangKeys.SortByEnumName : LangKeys.SortByEnumTime;
            return interaction != null ? interaction.GetLangMessage(this, key) : Lang(key);
        }
        
        public PlaceholderData CloneForPlayer(PlaceholderData source, IPlayer player, int index)
        {
            DiscordUser user = player.GetDiscordUser();
            return source.Clone()
            .AddPlayer(player)
            .AddUser(user)
            .Add(PlaceholderKeys.PlayerIndex, index)
            .Add(PlaceholderKeys.PlayerDuration, _playerCache.GetOnlineDuration(player));
        }
        
        public PlaceholderData GetDefault(DiscordInteraction interaction)
        {
            return _placeholders.CreateData(this).AddInteraction(interaction);
        }
        
        public PlaceholderData GetDefault(MessageCache cache, DiscordInteraction interaction)
        {
            return GetDefault(interaction)
            .Add(nameof(MessageCache), cache)
            .Add(nameof(MessageState), cache.State);
        }
        
        public PlaceholderData GetDefault(MessageCache cache, DiscordInteraction interaction, int maxPage)
        {
            return GetDefault(cache, interaction)
            .Add(PlaceholderKeys.MaxPage, maxPage)
            .Add(PlaceholderKeys.CommandId, cache.State.CreateBase64String());
        }
        #endregion

        #region Plugins\DiscordPlayers.Setup.cs
        private void Init()
        {
            Instance = this;
            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;
            
            _pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name) ?? new PluginData();
        }
        
        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
            {
                PrintWarning("Please set the Discord Bot Token and reload the plugin");
                return;
            }
            
            _playerCache.Initialize(players.Connected);
            
            RegisterPlaceholders();
            RegisterTemplates();
            
            foreach (CommandSettings message in _pluginConfig.CommandMessages)
            {
                if (message.EmbedFieldLimit > 25)
                {
                    PrintWarning($"Players For Embed cannot be greater than 25 for command {message.Command}");
                }
                else if (message.EmbedFieldLimit < 0)
                {
                    PrintWarning($"Players For Embed cannot be less than 0 for command {message.Command}");
                }
                
                if (message.EmbedsPerMessage > 10)
                {
                    PrintWarning($"Embeds Per Message cannot be greater than 10 for command {message.Command}");
                }
                else if (message.EmbedsPerMessage < 1)
                {
                    PrintWarning($"Embeds Per Message cannot be less than 1 for command {message.Command}");
                }
                
                message.EmbedFieldLimit = Mathf.Clamp(message.EmbedFieldLimit, 0, 25);
                message.EmbedsPerMessage = Mathf.Clamp(message.EmbedsPerMessage, 1, 10);
            }
            
            Client.Connect(_discordSettings);
        }
        
        private void OnUserConnected(IPlayer player)
        {
            _playerCache.OnUserConnected(player);
        }
        
        private void OnUserDisconnected(IPlayer player)
        {
            _playerCache.OnUserDisconnected(player);
        }
        
        private void Unload()
        {
            SaveData();
            Instance = null;
        }
        #endregion

        #region Plugins\DiscordPlayers.Templates.cs
        public void RegisterTemplates()
        {
            foreach (CommandSettings command in _pluginConfig.CommandMessages)
            {
                DiscordEmbedFieldTemplate embed = command.Command == "playersadmin" ? GetDefaultAdminFieldTemplate() : GetDefaultFieldTemplate();
                CreateCommandTemplates(command, embed, false);
            }
            
            foreach (PermanentMessageSettings permanent in _pluginConfig.Permanent)
            {
                CreateCommandTemplates(permanent, GetDefaultFieldTemplate(), true);
            }
            
            DiscordMessageTemplate unknownState = CreateTemplateEmbed("Error: Failed to find a state for this message. Please create a new message.", DiscordColor.Danger);
            _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.UnknownState, unknownState, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            
            DiscordMessageTemplate unknownCommand = CreateTemplateEmbed("Error: Command not found '{discordplayers.command.name}'. Please create a new message", DiscordColor.Danger);
            _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.UnknownCommand, unknownCommand, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        }
        
        private void CreateCommandTemplates(BaseMessageSettings command, DiscordEmbedFieldTemplate @default, bool isGlobal)
        {
            TemplateNameCache cache = command.NameCache;
            
            DiscordMessageTemplate template = CreateBaseMessage();
            RegisterTemplate(_templates, cache.TemplateName, template, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            RegisterTemplate(_field, cache.TemplateName, @default, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            
            if (command.EmbedsPerMessage == 1)
            {
                DiscordEmbedTemplate embed = GetDefaultEmbedTemplate();
                RegisterTemplate(_embed, cache.GetFirstEmbedName(), embed, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            }
            else if (command.EmbedsPerMessage >= 2)
            {
                DiscordEmbedTemplate first = GetFirstEmbedTemplate();
                RegisterTemplate(_embed, cache.GetFirstEmbedName(), first, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                
                DiscordEmbedTemplate last = GetLastEmbedTemplate();
                RegisterTemplate(_embed, cache.GetLastEmbedName(), last, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            }
            
            if (command.EmbedsPerMessage >= 3)
            {
                DiscordEmbedTemplate middle = GetMiddleEmbedTemplate();
                RegisterTemplate(_embed, cache.GetMiddleEmbedName(), middle, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            }
        }
        
        public void RegisterTemplate<TTemplate>(BaseMessageTemplateLibrary<TTemplate> library, string name, TTemplate template, bool isGlobal, TemplateVersion version, TemplateVersion minVersion) where TTemplate : class, new()
        {
            if (isGlobal)
            {
                library.RegisterGlobalTemplateAsync(this, name, template, version, minVersion);
            }
            else
            {
                library.RegisterLocalizedTemplateAsync(this, name, template, version, minVersion);
            }
        }
        
        public DiscordMessageTemplate CreateBaseMessage()
        {
            return new DiscordMessageTemplate
            {
                Content = string.Empty,
                Components =
                {
                    new ButtonTemplate("Back", ButtonStyle.Primary, $"{BackCommand} {{discordplayers.command.id}}", "⬅"),
                    new ButtonTemplate("Page: {discordplayers.state.page}/{discordplayers.page.max}", ButtonStyle.Primary, "PAGE", false),
                    new ButtonTemplate("Next", ButtonStyle.Primary, $"{ForwardCommand} {{discordplayers.command.id}}", "➡"),
                    new ButtonTemplate("Refresh", ButtonStyle.Primary, $"{RefreshCommand} {{discordplayers.command.id}}", "🔄"),
                    new ButtonTemplate("Sorted By: {discordplayers.state.sort}", ButtonStyle.Primary, $"{ChangeSort} {{discordplayers.command.id}}")
                }
            };
        }
        
        public DiscordEmbedTemplate GetDefaultEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Title = "{server.name}",
                Description = "{server.players}/{server.players.max} Online Players | {server.players.loading} Loading | {server.players.queued} Queued",
                Color = DiscordColor.Blurple.ToHex(),
                TimeStamp = true,
                Footer =
                {
                    Enabled = true,
                    Text = "{plugin.title} V{plugin.version} by {plugin.author}",
                    IconUrl = PluginIcon
                }
            };
        }
        
        public DiscordEmbedTemplate GetFirstEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Title = "{server.name}",
                Description = "{server.players}/{server.players.max} Online Players | {server.players.loading} Loading | {server.players.queued} Queued",
                Color = DiscordColor.Blurple.ToHex()
            };
        }
        
        public DiscordEmbedTemplate GetMiddleEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Color = DiscordColor.Blurple.ToHex()
            };
        }
        
        public DiscordEmbedTemplate GetLastEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Color = DiscordColor.Blurple.ToHex(),
                TimeStamp = true,
                Footer =
                {
                    Enabled = true,
                    Text = "{plugin.title} V{plugin.version} by {plugin.author}",
                    IconUrl = PluginIcon
                }
            };
        }
        
        public DiscordEmbedFieldTemplate GetDefaultFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate("{discordplayers.player.index} [{player.clan.tag}] {player.name}", "**Online For:** {discordplayers.duration.hours}h {discordplayers.duration.minutes}m {discordplayers.duration.seconds}s");
        }
        
        public DiscordEmbedFieldTemplate GetDefaultAdminFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate("{discordplayers.player.index} [{player.clan.tag}] {player.name}", "**Steam ID:**{player.id}\n**Online For:** {discordplayers.duration.hours}h {discordplayers.duration.minutes}m {discordplayers.duration.seconds}s\n**Ping:** {player.ping}ms\n**Country:** {player.address.data!country}");
        }
        
        public DiscordMessageTemplate CreateTemplateEmbed(string description, DiscordColor color)
        {
            return new DiscordMessageTemplate
            {
                Embeds = new List<DiscordEmbedTemplate>
                {
                    new DiscordEmbedTemplate
                    {
                        Description = $"[{{plugin.title}}] {description}",
                        Color = color.ToHex()
                    }
                }
            };
        }
        #endregion

        #region Cache\MessageCache.cs
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
        #endregion

        #region Cache\OnlinePlayerCache.cs
        public class OnlinePlayerCache
        {
            private readonly PlayerListCache _byNameCache = new PlayerListCache((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
            private readonly PlayerListCache _byOnlineTime;
            private readonly Hash<string, DateTime> _onlineSince = new Hash<string, DateTime>();
            
            public OnlinePlayerCache()
            {
                _byOnlineTime = new PlayerListCache((left, right) => _onlineSince[left.Id].CompareTo(_onlineSince[right.Id]));
            }
            
            public void Initialize(IEnumerable<IPlayer> connected)
            {
                foreach (IPlayer player in connected)
                {
                    OnUserConnected(player);
                }
                
                #if RUST
                foreach (Network.Connection connection in Network.Net.sv.connections)
                {
                    _onlineSince[connection.ownerid.ToString()] = DateTime.UtcNow - TimeSpan.FromSeconds(connection.GetSecondsConnected());
                }
                #endif
            }
            
            public TimeSpan GetOnlineDuration(IPlayer player)
            {
                return DateTime.UtcNow - _onlineSince[player.Id];
            }
            
            public List<IPlayer> GetList(SortBy sort, bool includeAdmin)
            {
                var list = sort == SortBy.Time ? _byOnlineTime.GetList(includeAdmin) : _byNameCache.GetList(includeAdmin);
                return Enumerable.Range(0, 100).Select(i => list[0]).ToList();
                //return list;
            }
            
            public void OnUserConnected(IPlayer player)
            {
                _onlineSince[player.Id] = DateTime.UtcNow;
                _byNameCache.Add(player);
                _byOnlineTime.Add(player);
            }
            
            public void OnUserDisconnected(IPlayer player)
            {
                _onlineSince.Remove(player.Id);
                _byNameCache.Remove(player);
                _byOnlineTime.Remove(player);
            }
        }
        #endregion

        #region Cache\PlayerListCache.cs
        public class PlayerListCache
        {
            private readonly List<IPlayer> _allList = new List<IPlayer>();
            private readonly List<IPlayer> _nonAdminList = new List<IPlayer>();
            
            private readonly Func<IPlayer, IPlayer, int> _compareTo;
            
            public PlayerListCache(Func<IPlayer, IPlayer, int> compareTo)
            {
                _compareTo = compareTo;
            }
            
            public void Add(IPlayer player)
            {
                Remove(player);
                Insert(_allList, player);
                Insert(_nonAdminList, player);
            }
            
            public void Insert(List<IPlayer> list, IPlayer player)
            {
                int index = IndexOf(list, player);
                if (index < 0)
                {
                    list.Insert(~index, player);
                }
                else
                {
                    list[index] = player;
                }
            }
            
            public void Remove(IPlayer player)
            {
                _allList.Remove(player);
                _nonAdminList.Remove(player);
            }
            
            public List<IPlayer> GetList(bool includeAdmin)
            {
                return includeAdmin ? _allList : _nonAdminList;
            }
            
            private int IndexOf(List<IPlayer> players, IPlayer player)
            {
                int min = 0;
                int max = players.Count - 1;
                while (min <= max)
                {
                    int mid = min + (max - min) / 2;
                    IPlayer midPlayer = players[mid];
                    int compare = _compareTo(player, midPlayer);
                    
                    if (compare < 0)
                    {
                        max = mid - 1;
                    }
                    else if (compare > 0)
                    {
                        min = mid + 1;
                    }
                    else
                    {
                        return mid;
                    }
                }
                
                return ~min;
            }
        }
        #endregion

        #region Cache\TemplateNameCache.cs
        public class TemplateNameCache
        {
            public readonly string TemplateName;
            private readonly List<string> _embedNames;
            
            public TemplateNameCache(BaseMessageSettings settings)
            {
                _embedNames = new List<string>(settings.EmbedsPerMessage);
                string name = settings.GetTemplateName();
                TemplateName = char.ToUpper(name[0]) + name.Substring(1);
                SetEmbedNames(settings.EmbedsPerMessage);
            }
            
            private void SetEmbedNames(int embedLimit)
            {
                if (embedLimit == 1)
                {
                    _embedNames.Add(TemplateName);
                    return;
                }
                
                _embedNames.Add($"{TemplateName}.{{First}}");
                
                string middle = $"{TemplateName}.{{Middle}}";
                for (int i = 1; i < embedLimit - 1; i++)
                {
                    _embedNames.Add(middle);
                }
                
                _embedNames.Add($"{TemplateName}.{{Last}}");
            }
            
            public string GetEmbedName(int index) => _embedNames[index];
            public string GetFirstEmbedName() => GetEmbedName(0);
            public string GetLastEmbedName() => GetEmbedName(_embedNames.Count - 1);
            public string GetMiddleEmbedName() => _embedNames[1];
        }
        #endregion

        #region Configuration\BaseMessageSettings.cs
        public abstract class BaseMessageSettings
        {
            [JsonProperty(PropertyName = "Display Admins In The Player List", Order = 1001)]
            public bool ShowAdmins { get; set; }
            
            [DefaultValue(25)]
            [JsonProperty(PropertyName = "Players Per Embed (0 - 25)", Order = 1002)]
            public int EmbedFieldLimit { get; set; }
            
            [DefaultValue(1)]
            [JsonProperty(PropertyName = "Embeds Per Message (1-10)", Order = 1003)]
            public int EmbedsPerMessage { get; set; }
            
            [JsonIgnore]
            public TemplateNameCache NameCache { get; private set; }
            
            [JsonIgnore]
            public int MaxPlayersPerPage { get; private set; }
            
            public abstract bool IsPermanent();
            public abstract string GetTemplateName();
            
            [JsonConstructor]
            public BaseMessageSettings() { }
            
            public BaseMessageSettings(BaseMessageSettings settings)
            {
                ShowAdmins = settings?.ShowAdmins ?? true;
                EmbedFieldLimit = settings?.EmbedFieldLimit ?? 25;
                EmbedsPerMessage = settings?.EmbedsPerMessage ?? 1;
            }
            
            public void Initialize()
            {
                NameCache = new TemplateNameCache(this);
                MaxPlayersPerPage = EmbedFieldLimit * EmbedsPerMessage;
            }
        }
        #endregion

        #region Configuration\CommandSettings.cs
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
        #endregion

        #region Configuration\PermanentMessageSettings.cs
        public class PermanentMessageSettings : BaseMessageSettings
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled { get; set; }
            
            [JsonProperty(PropertyName = "Template Name (Must Be Unique)")]
            public string TemplateName { get; set; }
            
            [JsonProperty(PropertyName = "Permanent Message Channel ID")]
            public Snowflake ChannelId { get; set; }
            
            [JsonProperty(PropertyName = "Update Rate (Minutes)")]
            public float UpdateRate { get; set; }
            
            [JsonConstructor]
            public PermanentMessageSettings() { }
            
            public PermanentMessageSettings(PermanentMessageSettings settings) : base(settings)
            {
                Enabled = settings?.Enabled ?? false;
                TemplateName = settings?.TemplateName ?? "Permanent";
                ChannelId = settings?.ChannelId ?? default(Snowflake);
                UpdateRate = settings?.UpdateRate ?? 1f;
            }
            
            public override bool IsPermanent() => true;
            public override string GetTemplateName() => TemplateName;
        }
        #endregion

        #region Configuration\PluginConfig.cs
        public class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [JsonProperty(PropertyName = "Command Messages")]
            public List<CommandSettings> CommandMessages { get; set; }
            
            [JsonProperty(PropertyName = "Permanent Messages")]
            public List<PermanentMessageSettings> Permanent { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(DiscordLogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel ExtensionDebugging { get; set; }
        }
        #endregion

        #region Data\PermanentMessageData.cs
        public class PermanentMessageData
        {
            public Snowflake MessageId { get; set; }
        }
        #endregion

        #region Data\PluginData.cs
        public class PluginData
        {
            public Hash<string, PermanentMessageData> PermanentMessageIds = new Hash<string, PermanentMessageData>();
            public Hash<string, Snowflake> RegisteredCommands = new Hash<string, Snowflake>();
            
            public PermanentMessageData GetPermanentMessage(PermanentMessageSettings config)
            {
                return PermanentMessageIds[config.TemplateName];
            }
            
            public void SetPermanentMessage(PermanentMessageSettings config, PermanentMessageData data)
            {
                PermanentMessageIds[config.TemplateName] = data;
            }
        }
        #endregion

        #region Enums\SortBy.cs
        public enum SortBy : byte
        {
            Name,
            Time
        }
        #endregion

        #region Handlers\PermanentMessageHandler.cs
        public class PermanentMessageHandler
        {
            private readonly DiscordClient _client;
            private readonly MessageCache _cache;
            private readonly DiscordMessage _message;
            private readonly MessageUpdate _update = new MessageUpdate();
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
        #endregion

        #region Lang\LangKeys.cs
        public static class LangKeys
        {
            public const string SortByEnumName = nameof(SortByEnumName);
            public const string SortByEnumTime = nameof(SortByEnumTime);
        }
        #endregion

        #region Placeholders\PlaceholderKeys.cs
        public static class PlaceholderKeys
        {
            public const string CommandId = "command.id";
            public const string CommandName = "command.name";
            public const string PlayerIndex = "player.index";
            public const string PlayerDuration = "player.duration";
            public const string MaxPage = "page.max";
        }
        #endregion

        #region State\MessageState.cs
        [ProtoContract]
        public class MessageState
        {
            [ProtoMember(1)]
            public short Page;
            
            [ProtoMember(2)]
            public SortBy Sort;
            
            [ProtoMember(3)]
            public string Command;
            
            private MessageState() { }
            
            public static MessageState CreateNew(string command)
            {
                return new MessageState
                {
                    Command = command
                };
            }
            
            public static MessageState Create(string base64)
            {
                try
                {
                    byte[] data = Convert.FromBase64String(base64);
                    MemoryStream stream = DiscordPlayers.Instance.Pool.GetMemoryStream();
                    stream.Write(data, 0, data.Length);
                    stream.Position = 0;
                    MessageState state = Serializer.Deserialize<MessageState>(stream);
                    DiscordPlayers.Instance.Pool.FreeMemoryStream(stream);
                    return state;
                }
                catch (Exception ex)
                {
                    DiscordPlayers.Instance.PrintError($"An error occured parsing state. State: {base64}. Exception:\n{ex}");
                    return null;
                }
            }
            
            public string CreateBase64String()
            {
                MemoryStream stream = DiscordPlayers.Instance.Pool.GetMemoryStream();
                Serializer.Serialize(stream, this);
                string base64 = Convert.ToBase64String(stream.ToArray());
                DiscordPlayers.Instance.Pool.FreeMemoryStream(stream);
                return base64;
            }
            
            public void NextPage() => Page++;
            
            public void PreviousPage() => Page--;
            
            public void ClampPage(short maxPage) => Page = Page.Clamp((short)0, maxPage);
            
            public void NextSort() => Sort = EnumCache<SortBy>.Instance.Next(Sort);
            
            public override string ToString()
            {
                return $"{{ Command = '{Command}' Sort = {Sort.ToString()} Page = {Page} }}";
            }
        }
        #endregion

        #region Templates\TemplateKeys.cs
        public static class TemplateKeys
        {
            public static class Errors
            {
                private const string Base = nameof(Errors) + ".";
                
                public const string UnknownState = Base + nameof(UnknownState);
                public const string UnknownCommand = Base + nameof(UnknownCommand);
            }
        }
        #endregion

    }

}
