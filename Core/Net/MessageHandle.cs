using Core.Util;
using Luban;

namespace Core
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageHandle
    {
        public NetChannel Channel;
        public Message Message;

        public virtual void Excute()
        {

        }

        public static byte[] Write(Message message)
        {
            var bytes = HSerializer.Serialize(message);
            int len = 8 + bytes.Length;
            var msgID = HandleManager.Instance.GetID(message.GetType());
            Span<byte> span = stackalloc byte[len];

            var buf = new ByteBuf();
            buf.WriteInt(len);
            buf.WriteInt(msgID);
            buf.WriteBytes(bytes);

            return buf.Bytes;
        }

        public static void Read(byte[] buffer, NetChannel channel)
        {
            var buf = new ByteBuf(buffer);
            int msgLen = buf.ReadInt();
            int msgID = buf.ReadInt();
            ReadOnlyMemory<byte> bytes = buf.ReadBytes();
            var message = HSerializer.Deserialize<Message>(bytes);
            var handle = HandleManager.Instance.GetMessageHandle(msgID);
            if (handle != null)
            {
                handle.Channel = channel;
                handle.Message = message;
                handle.Excute();
            }
            else
            {
                Logger.Error("recive error msg");
            }
        }
    }
}
