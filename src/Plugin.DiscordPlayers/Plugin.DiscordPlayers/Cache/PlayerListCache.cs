using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace DiscordPlayersPlugin.Cache;

public class PlayerListCache
{
    private readonly List<IPlayer> _allList = new();
    private readonly List<IPlayer> _nonAdminList = new();

    private readonly IComparer<IPlayer> _comparer;

    public PlayerListCache(IComparer<IPlayer> comparer)
    {
        _comparer = comparer;
    }

    public void Add(IPlayer player)
    {
        Remove(player);
        Insert(_allList, player);
        Insert(_nonAdminList, player);
    }

    public void Insert(List<IPlayer> list, IPlayer player)
    {
        int index = list.BinarySearch(player, _comparer);
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
}