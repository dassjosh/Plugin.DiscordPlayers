using System;
using System.Linq;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Placeholders;
using DiscordPlayersPlugin.State;
using DiscordPlayersPlugin.Templates;
using Oxide.Core;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Interactions.Response;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Libraries.Placeholders;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
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
    }
}