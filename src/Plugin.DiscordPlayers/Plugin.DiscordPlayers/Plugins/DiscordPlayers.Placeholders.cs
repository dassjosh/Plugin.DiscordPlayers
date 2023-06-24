using System;
using System.Text;
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
using Oxide.Ext.Discord.Libraries.Placeholders.Default;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        public void RegisterPlaceholders()
        {
            TimeSpanPlaceholders.RegisterPlaceholders(this, "discordplayers.duration", PlaceholderKeys.Data.PlayerDuration);
            _placeholders.RegisterPlaceholder<IPlayer, string>(this, "discordplayers.player.clantag", GetClanTag);
            _placeholders.RegisterPlaceholder<int>(this, "discordplayers.player.index", PlaceholderKeys.Data.PlayerIndex);
            _placeholders.RegisterPlaceholder<MessageState, int>(this, "discordplayers.state.page", GetPage);
            _placeholders.RegisterPlaceholder<MessageState, string>(this, "discordplayers.state.sort", GetSort);
            _placeholders.RegisterPlaceholder<MessageCache, string>(this, "discordplayers.command.id", GetCommand);
            _placeholders.RegisterPlaceholder<int>(this, "discordplayers.page.max", PlaceholderKeys.Data.MaxPage);
        }
        
        public int GetPage(MessageState embed) => embed.Page;
        public string GetCommand(MessageCache cache) => cache.Settings.GetTemplateName();
        public string GetSort(PlaceholderState state, MessageState embed)
        {
            DiscordInteraction interaction = state.Data.Get<DiscordInteraction>();
            string key = embed.Sort == SortBy.Name ? LangKeys.SortByEnumName : LangKeys.SortByEnumTime;
            string sort = interaction != null ? interaction.GetLangMessage(this, key) : Lang(key);
            return sort;
        }

        public PlaceholderData CloneForPlayer(PlaceholderData source, IPlayer player, int index)
        {
            DiscordUser user = player.GetDiscordUser();
            return source.Clone().AddPlayer(player).AddUser(user).Add(PlaceholderKeys.Data.PlayerIndex, index).Add(PlaceholderKeys.Data.PlayerDuration, DateTime.UtcNow - _onlineSince[player.Id]);
        }
        
        public PlaceholderData GetDefault(MessageCache cache, DiscordInteraction interaction, int maxPage)
        {
            return _placeholders.CreateData(this).Add(nameof(MessageCache), cache).Add(nameof(MessageState), cache.State).Add(PlaceholderKeys.Data.MaxPage, maxPage).AddInteraction(interaction);
        }
    }
}