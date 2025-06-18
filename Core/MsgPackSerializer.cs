using Google.Protobuf;
using MessagePack;
using MessagePack.Resolvers;
using Proto.Remote;

namespace Core
{
    /// <summary>
    /// Proto.Actor扩充MessagePack序列化
    /// </summary>
    public class MsgPackSerializer : ISerializer
    {
        private readonly MessagePackSerializerOptions _options;

        public MsgPackSerializer()
        {
            _options = MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(
                    NativeDateTimeResolver.Instance,
                    StandardResolver.Instance
                ))
                .WithCompression(MessagePackCompression.Lz4BlockArray);
        }

        public object Deserialize(ByteString bytes, string typeName)
        {
            var type = Type.GetType(typeName);
            return MessagePackSerializer.Deserialize(type, bytes.ToByteArray(), _options);
        }

        public ByteString Serialize(object obj)
        {
            return ByteString.CopyFrom(MessagePackSerializer.Serialize(obj.GetType(), obj, _options));
        }

        public string GetTypeName(object obj)
        {
            return obj.GetType().AssemblyQualifiedName;
        }

        public bool CanSerialize(object obj)
        {
            return obj is Message;
        }
    }
}
