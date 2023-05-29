using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace DiscordPlayersPlugin.Cache
{
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
}