using MemoryPack;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static byte[] Serialize<T>(T value, MemoryPackSerializerOptions? options = null)
        {
            return MemoryPackSerializer.Serialize(value, options);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static T Deserialize<T>(ReadOnlySpan<byte> buffer, MemoryPackSerializerOptions? options = null)
        {
            return MemoryPackSerializer.Deserialize<T>(buffer, options);
        }
    }
}
