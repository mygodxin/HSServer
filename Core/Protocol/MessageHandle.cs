using Core.Net;

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

            using var buf = new ByteBuffer();
            buf.WriteInt(len);
            buf.WriteInt(msgID);
            buf.WriteBytes(bytes);

            return buf.Bytes;
        }

        public static void Read(ReadOnlySpan<byte> buffer, NetClient channel)
        {
            using var buf = new ByteBuffer(buffer);
            int msgLen = buf.ReadInt();
            int msgID = buf.ReadInt();
            ReadOnlySpan<byte> bytes = buf.ReadBytes();
            var message = HSerializer.Deserialize<IMessage>(bytes);
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
