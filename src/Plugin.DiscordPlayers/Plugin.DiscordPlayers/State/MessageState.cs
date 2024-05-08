using System;
using System.Buffers;
using System.IO;
using DiscordPlayersPlugin.Enums;
using DiscordPlayersPlugin.Plugins;
using Oxide.Ext.Discord.Cache;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Libraries;
using ProtoBuf;

namespace DiscordPlayersPlugin.State;

[ProtoContract]
public class MessageState
{
    [ProtoMember(1)]
    public short Page;
        
    [ProtoMember(2)]
    public SortBy Sort;
        
    [ProtoMember(3)]
    public string Command;

    private MessageState() { }

    public static MessageState CreateNew(TemplateKey command)
    {
        return new MessageState
        {
            Command = command.Name
        };
    }
        
    public static MessageState Create(ReadOnlySpan<char> base64)
    {
        try
        {
            Span<byte> buffer = stackalloc byte[64];
            Convert.TryFromBase64Chars(base64, buffer, out int written);
            MemoryStream stream = DiscordPlayers.Instance.Pool.GetMemoryStream();
            stream.Write(buffer[..written]);
            stream.Flush();
            stream.Position = 0;
            MessageState state = Serializer.Deserialize<MessageState>(stream);
            DiscordPlayers.Instance.Pool.FreeMemoryStream(stream);
            return state;
        }
        catch (Exception ex)
        {
            DiscordPlayers.Instance.PrintError($"An error occured parsing state. State: {base64.ToString()}. Exception:\n{ex}");
            return null;
        }
    }

    public string CreateBase64String()
    {
        MemoryStream stream = DiscordPlayers.Instance.Pool.GetMemoryStream();
        Serializer.Serialize(stream, this);
        stream.TryGetBuffer(out ArraySegment<byte> buffer);
        string base64 = Convert.ToBase64String(buffer.AsSpan());
        DiscordPlayers.Instance.Pool.FreeMemoryStream(stream);
        return base64;
    }

    public void NextPage() => Page++;

    public void PreviousPage() => Page--;

    public void ClampPage(short maxPage) => Page = Page.Clamp((short)0, maxPage);

    public void NextSort() => Sort = EnumCache<SortBy>.Instance.Next(Sort);

    public override string ToString()
    {
        return $"{{ Command = '{Command}' Sort = {Sort.ToString()} Page = {Page} }}";
    }
}