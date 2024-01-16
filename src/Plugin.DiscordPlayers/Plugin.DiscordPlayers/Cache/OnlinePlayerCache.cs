using System;
using System.Collections.Generic;
using DiscordPlayersPlugin.Enums;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;

namespace DiscordPlayersPlugin.Cache
{
    public class OnlinePlayerCache
    {
        private readonly PlayerListCache _byNameCache = new PlayerListCache(new NameComparer());
        private readonly PlayerListCache _byOnlineTime;
        private readonly Hash<string, DateTime> _onlineSince = new Hash<string, DateTime>();

        public OnlinePlayerCache()
        {
            _byOnlineTime = new PlayerListCache(new OnlineSinceComparer(_onlineSince));
        }

        public void Initialize(IEnumerable<IPlayer> connected)
        {
#if RUST
            foreach (Network.Connection connection in Network.Net.sv.connections)
            {
                _onlineSince[connection.ownerid.ToString()] = DateTime.UtcNow - TimeSpan.FromSeconds(connection.GetSecondsConnected());
            }
#endif
            
            foreach (IPlayer player in connected)
            {
                OnUserConnected(player);
            }
        }

        public TimeSpan GetOnlineDuration(IPlayer player)
        {
            return DateTime.UtcNow - _onlineSince[player.Id];
        }

        public List<IPlayer> GetList(SortBy sort, bool includeAdmin)
        {
            List<IPlayer> list = sort == SortBy.Time ? _byOnlineTime.GetList(includeAdmin) : _byNameCache.GetList(includeAdmin);
            //return Enumerable.Range(0, 100).Select(i => list[0]).ToList();
            return list;
        }
        
        public void OnUserConnected(IPlayer player)
        {
            _onlineSince.TryAdd(player.Id, DateTime.UtcNow);
            _byNameCache.Add(player);
            _byOnlineTime.Add(player);
        }

        public void OnUserDisconnected(IPlayer player)
        {
            _onlineSince.Remove(player.Id);
            _byNameCache.Remove(player);
            _byOnlineTime.Remove(player);
        }
        
        class NameComparer : Comparer<IPlayer>
        {
            public override int Compare(IPlayer x, IPlayer y)
            {
                return string.Compare(x?.Name, y?.Name, StringComparison.Ordinal);
            }
        }
        
        class OnlineSinceComparer : Comparer<IPlayer>
        {
            private readonly Hash<string, DateTime> _onlineSince;

            public OnlineSinceComparer(Hash<string, DateTime> onlineSince)
            {
                _onlineSince = onlineSince;
            }

            public override int Compare(IPlayer x, IPlayer y)
            {
                return _onlineSince[x.Id].CompareTo(_onlineSince[y.Id]);
            }
        }
    }
}