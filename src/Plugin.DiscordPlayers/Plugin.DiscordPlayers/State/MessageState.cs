using DiscordPlayersPlugin.Enums;
using DiscordPlayersPlugin.Plugins;
using Oxide.Ext.Discord.Extensions;

namespace DiscordPlayersPlugin.State
{
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
}