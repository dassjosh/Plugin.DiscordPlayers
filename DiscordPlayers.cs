using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Attributes.ApplicationCommands;
using Oxide.Ext.Discord.Builders.ApplicationCommands;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Applications;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Gatway.Events;
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
using Oxide.Ext.Discord.Interfaces.Callbacks.Async;
using Oxide.Ext.Discord.Interfaces.Entities.Messages;
using Oxide.Ext.Discord.Libraries.AppCommands;
using Oxide.Ext.Discord.Libraries.Langs;
using Oxide.Ext.Discord.Libraries.Placeholders;
using Oxide.Ext.Discord.Libraries.Templates;
using Oxide.Ext.Discord.Libraries.Templates.Commands;
using Oxide.Ext.Discord.Libraries.Templates.Messages;
using Oxide.Ext.Discord.Libraries.Templates.Messages.Bulk;
using Oxide.Ext.Discord.Libraries.Templates.Messages.Components;
using Oxide.Ext.Discord.Libraries.Templates.Messages.Embeds;
using Oxide.Ext.Discord.Libraries.Templates.Messages.Embeds.Fields;
using Oxide.Ext.Discord.Logging;
using Oxide.Ext.Discord.Pooling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

//DiscordPlayers created with PluginMerge v(1.0.5.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("Discord Players", "MJSU", "2.5.0")]
    [Description("Displays online players in discord")]
    public partial class DiscordPlayers : CovalencePlugin
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
                new PermanentMessageSettings { Enabled = false, ChannelId = new Snowflake(123), UpdateRate = 1f }
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
            DiscordApplication app = _client.Bot.Application;
            
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
                        _client.Bot.Application.GetGlobalCommand(_client, command.Value, oldCommand => oldCommand.Delete(_client, () =>
                        {
                            _pluginData.RegisteredCommands.Remove(command);
                            SaveData();
                        }), error =>
                        {
                            if (error.DiscordError?.Code == 10063)
                            {
                                _pluginData.RegisteredCommands.Remove(command);
                                SaveData();
                                error.SuppressErrorMessage();
                            }
                        });
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
            builder.AddDirectMessagePermission(settings.AllowInDm);
            builder.AddDefaultPermissions(PermissionFlags.None);
            
            CommandCreate cmd = builder.Build();
            DiscordCommandLocalization loc = builder.BuildCommandLocalization();
            
            _localizations.RegisterCommandLocalizationAsync(this, settings.NameCache.TemplateName, loc, new TemplateVersion(1, 0, 0)).OnSuccess(() =>
            {
                _localizations.ApplyCommandLocalizationsAsync(this, cmd, settings.NameCache.TemplateName).OnSuccess(() =>
                {
                    _client.Bot.Application.CreateGlobalCommand(_client, builder.Build(), appCommand =>
                    {
                        _pluginData.RegisteredCommands[command] = appCommand.Id;
                        SaveData();
                    });
                });
            });
            
            _appCommand.AddApplicationCommand(this, _client.Bot.Application, nameof(HandleApplicationCommand), command);
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
                    channel.GetChannelMessage(_client, existing.MessageId, message =>
                    {
                        _permanentState[message.Id] = new PermanentMessageHandler(new MessageCache(config), config.UpdateRate, message);
                    }, error =>
                    {
                        if (error.HttpStatusCode == 404)
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
                channel.CreateMessage(_client, create, message =>
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
                interaction.CreateInteractionResponse(_client, new InteractionResponse
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
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.UpdateMessage,
                    Data = create
                });
            });
        }
        #endregion

        #region Plugins\DiscordPlayers.Fields.cs
        // ReSharper disable once UnassignedField.Global
        // ReSharper disable once InconsistentNaming
        [DiscordClient] public DiscordClient _client;
        
        #pragma warning disable CS0649
        // ReSharper disable once InconsistentNaming
        [PluginReference] private Plugin Clans;
        #pragma warning restore CS0649
        
        private PluginConfig _pluginConfig; //Plugin Config
        private PluginData _pluginData;
        
        private readonly DiscordAppCommand _appCommand = GetLibrary<DiscordAppCommand>();
        private readonly DiscordPlaceholders _placeholders = GetLibrary<DiscordPlaceholders>();
        private readonly DiscordMessageTemplates _templates = GetLibrary<DiscordMessageTemplates>();
        private readonly DiscordEmbedTemplates _embed = GetLibrary<DiscordEmbedTemplates>();
        private readonly DiscordEmbedFieldTemplates _field = GetLibrary<DiscordEmbedFieldTemplates>();
        private readonly DiscordCommandLocalizations _localizations = GetLibrary<DiscordCommandLocalizations>();
        private readonly DiscordSettings _discordSettings = new DiscordSettings();
        
        private readonly Hash<string, DateTime> _onlineSince = new Hash<string, DateTime>();
        private readonly Hash<Snowflake, PermanentMessageHandler> _permanentState = new Hash<Snowflake, PermanentMessageHandler>();
        private readonly Hash<Snowflake, MessageCache> _messageCache = new Hash<Snowflake, MessageCache>();
        private readonly OnlinePlayerCache _playerCache = new OnlinePlayerCache();
        
        public const string BaseCommand = nameof(DiscordPlayers) + ".";
        public const string BackCommand = BaseCommand + "Back";
        public const string RefreshCommand = BaseCommand + "Refresh";
        public const string ForwardCommand = BaseCommand + "Forward";
        public const string ChangeSort = BaseCommand + "Sort";
        
        public readonly SortBy[] SortByList = (SortBy[])Enum.GetValues(typeof(SortBy));
        
        public static DiscordPlayers Instance;
        
        public PluginTimers Timer => timer;
        #endregion

        #region Plugins\DiscordPlayers.Helpers.cs
        private const string PluginIcon = "https://assets.umod.org/images/icons/plugin/61354f8bd5faf.png";
        
        public MessageCache GetCache(DiscordInteraction interaction)
        {
            DiscordMessage message = interaction.Message;
            string customId = interaction.Data.CustomId;
            
            MessageCache cache = _messageCache[message.Id];
            if (cache != null)
            {
                return cache;
            }
            
            string commandName = customId.Substring(customId.LastIndexOf(" ", StringComparison.Ordinal));
            CommandSettings command = _pluginConfig.CommandMessages.FirstOrDefault(c => c.Command == commandName);
            if (command != null)
            {
                cache = new MessageCache(command);
                _messageCache[message.Id] = cache;
                return cache;
            }
            
            interaction.CreateTemplateInteractionResponse(_client, this, InteractionResponseType.ChannelMessageWithSource, TemplateKeys.Errors.UnknownState, new InteractionCallbackData{Flags = MessageFlags.Ephemeral});
            return null;
        }
        
        public string GetClanTag(IPlayer player)
        {
            string clanTag = Clans?.Call<string>("GetClanOf", player);
            if (!string.IsNullOrEmpty(clanTag))
            {
                return $"[{clanTag}]";
            }
            
            return string.Empty;
        }
        
        public T NextEnum<T>(T src, T[] array) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");
            
            int index = Array.IndexOf(array, src) + 1;
            return array.Length == index ? array[0] : array[index];
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
                [LangKeys.OnlineTimeFormat] = "{1}h {2}m {3}s",
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
            cache.State.ClampPage(maxPage);
            
            PlaceholderData data = GetDefault(cache, maxPage + 1);
            data.ManualPool();
            
            CreateMessage(cache.Settings, data, interaction, create).OnSuccess(message =>
            {
                CreateEmbeds(cache.Settings, data, interaction, embedLimit).OnSuccess(embeds =>
                {
                    message.Embeds = embeds;
                    CreateFields(cache, data, interaction, onlineList).OnSuccess(fields =>
                    {
                        ProcessEmbeds(embeds, fields, cache.Settings.EmbedFieldLimit);
                        callback.Invoke(message);
                        data.Dispose();
                        DiscordPool.FreeList(ref onlineList);
                    });
                });
            });
        }
        
        public List<IPlayer> GetPlayerList(MessageCache cache)
        {
            int perPage = cache.Settings.MaxPlayersPerPage;
            return _playerCache.GetList(cache.State.Sort, cache.Settings.ShowAdmins).Skip(cache.State.Page * perPage).Take(perPage).ToPooledList();
        }
        
        public IDiscordAsyncCallback<T> CreateMessage<T>(BaseMessageSettings settings, PlaceholderData data, DiscordInteraction interaction, T message) where T : class, IDiscordMessageTemplate, new()
        {
            if (settings.IsPermanent())
            {
                return _templates.GetGlobalEntityAsync(this, settings.NameCache.TemplateName, data, message);
            }
            
            return _templates.GetLocalizedEntityAsync(this, settings.NameCache.TemplateName, interaction, data, message);
        }
        
        public IDiscordAsyncCallback<List<DiscordEmbed>> CreateEmbeds(BaseMessageSettings settings, PlaceholderData data, DiscordInteraction interaction, int embedLimit)
        {
            BulkTemplateRequest<DiscordEmbed> embedRequest = BulkTemplateRequest<DiscordEmbed>.Create();
            for (int i = 0; i < embedLimit; i++)
            {
                embedRequest.AddItem(settings.NameCache.GetEmbedName(i), data);
            }
            
            if (settings.IsPermanent())
            {
                _embed.GetGlobalBulkEntityAsync(this, embedRequest);
            }
            
            return _embed.GetLocalizedBulkEntityAsync(this, interaction, embedRequest);
        }
        
        public IDiscordAsyncCallback<List<EmbedField>> CreateFields(MessageCache cache, PlaceholderData data, DiscordInteraction interaction, List<IPlayer> onlineList)
        {
            int playerIndex = cache.Settings.MaxPlayersPerPage * cache.State.Page;
            BulkTemplateRequest<EmbedField> fieldRequest = BulkTemplateRequest<EmbedField>.Create();
            string template = cache.Settings.NameCache.TemplateName;
            for (int index = 0; index < onlineList.Count; index++)
            {
                fieldRequest.AddItem(template, CloneForPlayer(data, onlineList[index], playerIndex));
            }
            
            if (cache.Settings.IsPermanent())
            {
                _field.GetGlobalBulkEntityAsync(this, fieldRequest);
            }
            
            return _field.GetLocalizedBulkEntityAsync(this, interaction, fieldRequest);
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
        #endregion

        #region Plugins\DiscordPlayers.Placeholders.cs
        public void RegisterPlaceholders()
        {
            _placeholders.RegisterPlaceholder<IPlayer>(this, "discordplayers.player.duration", GetDuration);
            _placeholders.RegisterPlaceholder<IPlayer>(this, "discordplayers.player.clantag", GetClanTag);
            _placeholders.RegisterPlaceholder<int>(this, "discordplayers.player.index", PlaceholderKeys.Data.PlayerIndex, IntValue);
            _placeholders.RegisterPlaceholder<MessageState>(this, "discordplayers.state.page", GetPage);
            _placeholders.RegisterPlaceholder<MessageState>(this, "discordplayers.state.sort", GetSort);
            _placeholders.RegisterPlaceholder<MessageCache>(this, "discordplayers.command.id", GetCommand);
            _placeholders.RegisterPlaceholder<int>(this, "discordplayers.page.max", PlaceholderKeys.Data.MaxPage, IntValue);
        }
        
        public void GetDuration(StringBuilder builder, PlaceholderState state, IPlayer player)
        {
            TimeSpan duration = DateTime.UtcNow - _onlineSince[player.Id];
            PlaceholderFormatting.Replace(builder, state, Lang(LangKeys.OnlineTimeFormat, duration.Days, duration.Hours, duration.Minutes, duration.Seconds));
        }
        
        public void GetClanTag(StringBuilder builder, PlaceholderState state, IPlayer player) => PlaceholderFormatting.Replace(builder, state, GetClanTag(player));
        public void GetPage(StringBuilder builder, PlaceholderState state, MessageState embed) => PlaceholderFormatting.Replace(builder, state, embed.Page);
        public void IntValue(StringBuilder builder, PlaceholderState state, int value) => PlaceholderFormatting.Replace(builder, state, value);
        public void GetCommand(StringBuilder builder, PlaceholderState state, MessageCache cache) => PlaceholderFormatting.Replace(builder, state, cache.Settings.GetTemplateName());
        public void GetSort(StringBuilder builder, PlaceholderState state, MessageState embed)
        {
            DiscordInteraction interaction = state.Data.Get<DiscordInteraction>();
            string sort = interaction.GetLangMessage(this, embed.Sort == SortBy.Name ? LangKeys.SortByEnumName : LangKeys.SortByEnumTime);
            PlaceholderFormatting.Replace(builder, state, sort);
        }
        
        public PlaceholderData CloneForPlayer(PlaceholderData source, IPlayer player, int index)
        {
            DiscordUser user = player.GetDiscordUser();
            return source.Clone().AddPlayer(player).AddUser(user).Add(PlaceholderKeys.Data.PlayerIndex, index);
        }
        
        public PlaceholderData GetDefault(MessageCache cache, int maxPage)
        {
            return _placeholders.CreateData(this).Add(cache).Add(cache.State).Add(PlaceholderKeys.Data.MaxPage, maxPage);
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
            
            #if RUST
            foreach (Network.Connection connection in Network.Net.sv.connections)
            {
                _onlineSince[connection.ownerid.ToString()] = DateTime.UtcNow - TimeSpan.FromSeconds(connection.GetSecondsConnected());
            }
            #else
            foreach (IPlayer player in players.Connected)
            {
                _onlineSince[player.Id] = DateTime.UtcNow;
            }
            #endif
            
            _client.Connect(_discordSettings);
        }
        
        private void OnUserConnected(IPlayer player)
        {
            _onlineSince[player.Id] = DateTime.UtcNow;
        }
        
        private void OnUserDisconnected(IPlayer player)
        {
            _onlineSince.Remove(player.Id);
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
                TemplateNameCache cache = command.NameCache;
                
                DiscordMessageTemplate template = CreateBaseMessage();
                _templates.RegisterLocalizedTemplateAsync(this, cache.TemplateName, template, new TemplateVersion(1, 0, 0));
                
                DiscordEmbedFieldTemplate field = command.Command == "playersadmin" ? GetDefaultAdminFieldTemplate() : GetDefaultFieldTemplate();
                _field.RegisterLocalizedTemplateAsync(this, cache.TemplateName, field, new TemplateVersion(1, 0, 0));
                
                if (command.EmbedsPerMessage == 1)
                {
                    DiscordEmbedTemplate embed = GetDefaultEmbedTemplate();
                    _embed.RegisterLocalizedTemplateAsync(this, cache.GetFirstEmbedName(), embed, new TemplateVersion(1, 0, 0));
                }
                else if (command.EmbedsPerMessage >= 2)
                {
                    DiscordEmbedTemplate first = GetFirstEmbedTemplate();
                    _embed.RegisterLocalizedTemplateAsync(this, cache.GetFirstEmbedName(), first, new TemplateVersion(1, 0, 0));
                    
                    DiscordEmbedTemplate last = GetLastEmbedTemplate();
                    _embed.RegisterLocalizedTemplateAsync(this, cache.GetLastEmbedName(), last, new TemplateVersion(1, 0, 0));
                }
                
                if (command.EmbedsPerMessage >= 3)
                {
                    DiscordEmbedTemplate middle = GetMiddleEmbedTemplate();
                    _embed.RegisterLocalizedTemplateAsync(this, cache.GetMiddleEmbedName(), middle, new TemplateVersion(1, 0, 0));
                }
            }
            
            DiscordMessageTemplate unknownState = CreateTemplateEmbed("Error: Failed to find a state for this message. Please create a new message.", DiscordColor.Danger.ToHex(), new TemplateVersion(1, 0, 0));
            _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.UnknownState, unknownState, new TemplateVersion(1, 0, 0));
        }
        
        public DiscordMessageTemplate CreateBaseMessage()
        {
            return new DiscordMessageTemplate
            {
                Content = string.Empty,
                Components =
                {
                    new ButtonTemplate("Back", ButtonStyle.Primary, $"{BackCommand} {{discordplayers.command.id}}", "⬅"),
                    new ButtonTemplate("Page: {discordplayers.state.page}/{discordplayers.page.max}", ButtonStyle.Primary, "PAGE"),
                    new ButtonTemplate("Next", ButtonStyle.Primary, $"{ForwardCommand} {{discordplayers.command.id}}", "➡"),
                    new ButtonTemplate("Refresh", ButtonStyle.Primary, $"{RefreshCommand} {{discordplayers.command.id}}", "🔄"),
                    new ButtonTemplate("Sorted By: {discordplayers.state.sort}", ButtonStyle.Primary, $"{ChangeSort} {{discordplayers.command.id}}")
                },
                Version = new TemplateVersion(1, 0, 0)
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
                },
                Version = new TemplateVersion(1, 0, 0)
            };
        }
        
        public DiscordEmbedTemplate GetFirstEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Title = "{server.name}",
                Description = "{server.players}/{server.players.max} Online Players | {server.players.loading} Loading | {server.players.queued} Queued",
                Color = DiscordColor.Blurple.ToHex(),
                Version = new TemplateVersion(1, 0, 0)
            };
        }
        
        public DiscordEmbedTemplate GetMiddleEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Color = DiscordColor.Blurple.ToHex(),
                Version = new TemplateVersion(1, 0, 0)
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
                },
                Version = new TemplateVersion(1, 0, 0)
            };
        }
        
        public DiscordEmbedFieldTemplate GetDefaultFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate("{discordplayers.player.index} {player.name}", "**Online For:** {discordplayers.player.duration}") {Version = new TemplateVersion(1, 0, 0)};
        }
        
        public DiscordEmbedFieldTemplate GetDefaultAdminFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate("{discordplayers.player.index} {player.name}", "**Steam ID:**{player.id}\n**Online For:** {discordplayers.player.duration}\n**Ping:** {player.ping}ms\n**Country:** {player.address.data!country}") {Version = new TemplateVersion(1, 0, 0)};
        }
        
        public DiscordMessageTemplate CreateTemplateEmbed(string description, string color, TemplateVersion version)
        {
            return new DiscordMessageTemplate
            {
                Embeds = new List<MessageEmbedTemplate>
                {
                    new MessageEmbedTemplate
                    {
                        Description = $"[{{plugin.title}}] {description}",
                        Color = color
                    }
                },
                Version = version
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
                State = state ?? new MessageState();
            }
        }
        #endregion

        #region Cache\OnlinePlayerCache.cs
        public class OnlinePlayerCache
        {
            private readonly PlayerListCache _byNameCache = new PlayerListCache(SortByName);
            private readonly PlayerListCache _byOnlineTime = new PlayerListCache(SortByOnlineTime);
            
            private static int SortByName(List<IPlayer> list, IPlayer player)
            {
                int index = 0;
                
                for (; index < list.Count; index++)
                {
                    IPlayer sortedPlayer = list[index];
                    if (string.Compare(sortedPlayer.Name, player.Name, StringComparison.Ordinal) > 0)
                    {
                        break;
                    }
                }
                
                return index;
            }
            
            private static int SortByOnlineTime(List<IPlayer> list, IPlayer player)
            {
                return list.Count;
            }
            
            public void Initialize(IEnumerable<IPlayer> connected)
            {
                foreach (IPlayer player in connected)
                {
                    OnUserConnect(player);
                }
            }
            
            public void OnUserConnect(IPlayer player)
            {
                _byNameCache.Add(player);
                _byOnlineTime.Add(player);
            }
            
            public void OnUserDisconnected(IPlayer player)
            {
                _byNameCache.Remove(player);
                _byOnlineTime.Remove(player);
            }
            
            public List<IPlayer> GetList(SortBy sort, bool includeAdmin)
            {
                return sort == SortBy.Time ? _byOnlineTime.GetList(includeAdmin) : _byNameCache.GetList(includeAdmin);
            }
        }
        #endregion

        #region Cache\PlayerListCache.cs
        public class PlayerListCache
        {
            private readonly List<IPlayer> _allList = new List<IPlayer>();
            private readonly List<IPlayer> _nonAdminList = new List<IPlayer>();
            
            private readonly Func<List<IPlayer>, IPlayer, int> _sortFunc;
            
            public PlayerListCache(Func<List<IPlayer>, IPlayer, int> sortFunc)
            {
                _sortFunc = sortFunc;
            }
            
            public void Add(IPlayer player)
            {
                Remove(player);
                _allList.Insert(_sortFunc(_allList, player), player);
                if (!player.IsAdmin)
                {
                    _nonAdminList.Insert(_sortFunc(_nonAdminList, player), player);
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
                TemplateName = settings?.TemplateName ?? "Default";
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
        public enum SortBy
        {
            Name,
            Time
        }
        #endregion

        #region Handlers\PermanentMessageHandler.cs
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
                        if (error.HttpStatusCode == 404)
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
            public const string OnlineTimeFormat = nameof(OnlineTimeFormat);
        }
        #endregion

        #region Placeholders\PlaceholderKeys.cs
        public static class PlaceholderKeys
        {
            public static class Data
            {
                public const string PlayerIndex = "player.index";
                public const string MaxPage = "page.max";
                
            }
        }
        #endregion

        #region State\MessageState.cs
        public class MessageState
        {
            public int Page;
            public SortBy Sort;
            
            public void NextPage()
            {
                Page++;
            }
            
            public void PreviousPage()
            {
                Page--;
            }
            
            public void ClampPage(int maxPage)
            {
                Page = Page.Clamp(0, maxPage);
            }
            
            public void NextSort()
            {
                Sort = DiscordPlayers.Instance.NextEnum(Sort, DiscordPlayers.Instance.SortByList);
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
            }
        }
        #endregion

    }

}
