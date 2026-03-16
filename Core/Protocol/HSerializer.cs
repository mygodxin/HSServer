using MessagePack;
using System;

namespace Core.Protocol
{
    /// <summary>
    /// 序列化实现
    /// </summary>
    public class HSerializer
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static byte[] Serialize<T>(T value, MessagePackSerializerOptions? options = null)
        {
            return MessagePackSerializer.Serialize(value, options);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static T Deserialize<T>(ReadOnlyMemory<byte> buffer, MessagePackSerializerOptions? options = null)
        {
            return MessagePackSerializer.Deserialize<T>(buffer, options);
        }
    }
}
