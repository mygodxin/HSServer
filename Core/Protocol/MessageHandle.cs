using Core.NetCore.Transports;
using Luban;
using System;

namespace Core.Protocol
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageHandle
    {
        public ClientTransport Transport;
        public Message Message;

        public virtual void Excute()
        {

        }

        public static byte[] Write(Message message)
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

        public static Message Read(ReadOnlySpan<byte> buffer, out int msgID)
        {
            var buf = new ByteBuf(buffer.ToArray());
            int msgLen = buf.ReadInt();
            msgID = buf.ReadInt();
            ReadOnlyMemory<byte> bytes = buf.ReadBytes();
            return HSerializer.Deserialize<Message>(bytes);
        }
    }
}
