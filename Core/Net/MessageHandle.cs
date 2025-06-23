using MessagePack;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

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
            int offset = 0;
            span.WriteInt32(len, ref offset);
            span.WriteInt32(msgID, ref offset);
            span.WriteBytes(bytes, ref offset);
            Logger.Warn($"[write] {msgID}");
            return span.ToArray();
        }

        public static void Read(ReadOnlySpan<byte> buffer, NetChannel channel)
        {
            var offset = 0;
            int msgLen = buffer.ReadInt32(ref offset);
            int msgID = buffer.ReadInt32(ref offset);
            ReadOnlySpan<byte> bytes = buffer.ReadBytes(msgLen - 8, ref offset);
            var message = MessagePackSerializer.Deserialize<Message>(bytes.ToArray());
            Logger.Warn($"[read] {msgID}");
            var handle = HandleManager.Instance.GetMessageHandle(msgID);
            if (handle != null)
            {
                //Logger.Info($"{message as Reqlogin}");
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
