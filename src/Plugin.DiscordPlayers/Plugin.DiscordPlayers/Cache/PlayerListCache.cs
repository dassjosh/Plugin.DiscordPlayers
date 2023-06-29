using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace DiscordPlayersPlugin.Cache
{
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
}