using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Enums;
using DiscordPlayersPlugin.Lang;
using DiscordPlayersPlugin.Placeholders;
using DiscordPlayersPlugin.State;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Libraries.Placeholders;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        public void RegisterPlaceholders()
        {
            _placeholders.RegisterPlaceholder<int>(this, "discordplayers.player.index", PlaceholderKeys.PlayerIndex);
            _placeholders.RegisterPlaceholder<MessageState, int>(this, "discordplayers.state.page", GetPage);
            _placeholders.RegisterPlaceholder<MessageState, string>(this, "discordplayers.state.sort", GetSort);
            _placeholders.RegisterPlaceholder<string>(this, "discordplayers.command.id", PlaceholderKeys.CommandId);
            _placeholders.RegisterPlaceholder<string>(this, "discordplayers.command.name", PlaceholderKeys.CommandName);
            _placeholders.RegisterPlaceholder<int>(this, "discordplayers.page.max", PlaceholderKeys.MaxPage);
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
    }
}