using System;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Enums;
using DiscordPlayersPlugin.Lang;
using DiscordPlayersPlugin.Placeholders;
using DiscordPlayersPlugin.State;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Libraries;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        public void RegisterPlaceholders()
        {
            _placeholders.RegisterPlaceholder<int>(this,PlaceholderKeys.PlayerIndex, PlaceholderDataKeys.PlayerIndex);
            _placeholders.RegisterPlaceholder<MessageState, int>(this, PlaceholderKeys.Page, PlaceholderDataKeys.MessageState, GetPage);
            _placeholders.RegisterPlaceholder<MessageState, string>(this, PlaceholderKeys.SortState, GetSort);
            _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.CommandId, PlaceholderDataKeys.CommandId);
            _placeholders.RegisterPlaceholder<string>(this,PlaceholderKeys.CommandName, PlaceholderDataKeys.CommandName);
            _placeholders.RegisterPlaceholder<int>(this, PlaceholderKeys.MaxPage, PlaceholderDataKeys.MaxPage);
        }
        
        public int GetPage(MessageState embed) => embed.Page + 1;
        public string GetSort(PlaceholderState state, MessageState embed)
        {
            DiscordInteraction interaction = state.Data.Get<DiscordInteraction>();
            string key = embed.Sort == SortBy.Name ? LangKeys.SortByEnumName : LangKeys.SortByEnumTime;
            return interaction != null ? interaction.GetLangMessage(this, key) : Lang(key);
        }

        public PlaceholderData CloneForPlayer(PlaceholderData source, IPlayer player, int index)
        {
            DiscordUser user = player.GetDiscordUser();
            var onlineDuration = _playerCache.GetOnlineDuration(player);
            return source.Clone()
                         .RemoveUser()
                         .AddUser(user)
                         .AddPlayer(player)
                         .Add(PlaceholderDataKeys.PlayerIndex, index)
                         .Add(PlaceholderDataKeys.PlayerDuration, onlineDuration)
                         .AddTimestamp(DateTimeOffset.UtcNow - onlineDuration);
        }

        public PlaceholderData GetDefault(DiscordInteraction interaction)
        {
            return _placeholders.CreateData(this).AddInteraction(interaction);
        }
        
        public PlaceholderData GetDefault(MessageCache cache, DiscordInteraction interaction)
        {
            return GetDefault(interaction)
                .Add(PlaceholderDataKeys.MessageState, cache.State);
        }
        
        public PlaceholderData GetDefault(MessageCache cache, DiscordInteraction interaction, int maxPage)
        {
            return GetDefault(cache, interaction)
                   .Add(PlaceholderDataKeys.MaxPage, maxPage)
                   .Add(PlaceholderDataKeys.CommandId, cache.State.CreateBase64String());
        }
    }
}