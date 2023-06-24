using System;
using System.Linq;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Templates;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
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
            
            interaction.CreateTemplateResponse(Client, InteractionResponseType.ChannelMessageWithSource, TemplateKeys.Errors.UnknownState, new InteractionCallbackData{Flags = MessageFlags.Ephemeral});
            return null;
        }
        
        public string GetClanTag(IPlayer player)
        {
            string clanTag = Clans?.Call<string>("GetClanOf", player);
            return !string.IsNullOrEmpty(clanTag) ? string.Format(_pluginConfig.Formats.ClanTagFormat, clanTag) : string.Empty;
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
    }
}