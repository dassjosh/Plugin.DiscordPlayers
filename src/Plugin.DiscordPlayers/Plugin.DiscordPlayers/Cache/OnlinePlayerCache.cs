using System;
using System.Collections.Generic;
using DiscordPlayersPlugin.Enums;
using Oxide.Core.Libraries.Covalence;

namespace DiscordPlayersPlugin.Cache
{
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
}