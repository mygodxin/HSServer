using Luban;
using System;

namespace Core.Protocol
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageHandle
    {
        public NetClient Channel;
        public IMessage Message;

        public virtual void Excute()
        {

        }

        public static byte[] Write(IMessage message)
        {
            var bytes = HSerializer.Serialize(message);
            int len = 8 + bytes.Length;
            var msgID = HandleManager.Instance.GetID(message.GetType());

            var buf = new ByteBuf();
            buf.WriteInt(len);
            buf.WriteInt(msgID);
            buf.WriteBytes(bytes);

            return buf.Bytes;
        }

        public static IMessage Read(ReadOnlySpan<byte> buffer, out int msgID)
        {
            var buf = new ByteBuf(buffer.ToArray());
            int msgLen = buf.ReadInt();
            msgID = buf.ReadInt();
            ReadOnlySpan<byte> bytes = buf.ReadBytes();
            return HSerializer.Deserialize<IMessage>(bytes);
        }
    }
}
