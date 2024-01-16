using System;
using System.IO;
using DiscordPlayersPlugin.Enums;
using DiscordPlayersPlugin.Plugins;
using Oxide.Ext.Discord.Cache;
using Oxide.Ext.Discord.Extensions;
using ProtoBuf;

namespace DiscordPlayersPlugin.State
{
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

        public static MessageState CreateNew(string command)
        {
            return new MessageState
            {
                Command = command
            };
        }
        
        public static MessageState Create(string base64)
        {
            try
            {
                byte[] data = Convert.FromBase64String(base64);
                MemoryStream stream = DiscordPlayers.Instance.Pool.GetMemoryStream();
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                MessageState state = Serializer.Deserialize<MessageState>(stream);
                DiscordPlayers.Instance.Pool.FreeMemoryStream(stream);
                return state;
            }
            catch (Exception ex)
            {
                DiscordPlayers.Instance.PrintError($"An error occured parsing state. State: {base64}. Exception:\n{ex}");
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
}