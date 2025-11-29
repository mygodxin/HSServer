using Google.Protobuf;
using MemoryPack;
using Proto.Remote;
using System;

namespace Core
{
    /// <summary>
    /// Proto.Actor扩充MessagePack序列化
    /// </summary>
    public class MsgPackSerializer : ISerializer
    {
        private readonly MemoryPackSerializerOptions _options;

        public MsgPackSerializer()
        {
            //_options = MemoryPackSerializerOptions.
            //    .WithResolver(CompositeResolver.Create(
            //        NativeDateTimeResolver.Instance,
            //        StandardResolver.Instance
            //    ))
            //    .WithCompression(MessagePackCompression.Lz4BlockArray);
        }

        public object Deserialize(ByteString bytes, string typeName)
        {
            var type = Type.GetType(typeName);
            return MemoryPackSerializer.Deserialize(type, bytes.ToByteArray(), _options);
        }

        public ByteString Serialize(object obj)
        {
            return ByteString.CopyFrom(MemoryPackSerializer.Serialize(obj.GetType(), obj, _options));
        }

        public string GetTypeName(object obj)
        {
            return obj.GetType().AssemblyQualifiedName;
        }

        public bool CanSerialize(object obj)
        {
            return obj is IMessage;
        }
    }
}
