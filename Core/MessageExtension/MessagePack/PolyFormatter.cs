using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.MessageExtension.PolymorphicMessagePack
{
    public class PolyFormatter<T> : IMessagePackFormatter<T>
    {
        private object lockObj = new object();

        public PolyFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var actualtype = value.GetType();

            if (!PolyTypeMapper.TryGet(actualtype, out var typeId))
                throw new MessagePackSerializationException($"Type '{actualtype.FullName}' is not registered in {nameof(PolyTypeMapper)}");


            writer.WriteArrayHeader(2);
            writer.WriteInt32(typeId);

            //Bottleneck
            (options.Resolver as PolyResolver).InnerSerialize(actualtype, ref writer, value, options);
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return default;

            options.Security.DepthStep(ref reader);

            try
            {
                Type type = null;
                switch (reader.NextMessagePackType)
                {
                    //如果是数组，说明里面存了类型id
                    case MessagePackType.Array:
                        var count = reader.ReadArrayHeader();
                        if (count != 2)
                            throw new MessagePackSerializationException("Invalid polymorphic array count");
                        switch (reader.NextMessagePackType)
                        {
                            case MessagePackType.Integer:
                                var typeId = reader.ReadInt32();
                                if (!PolyTypeMapper.TryGet(typeId, out type))
                                    throw new MessagePackSerializationException($"Cannot find Type Id: {typeId} registered in {nameof(PolyTypeMapper)}");
                                break;
                            case MessagePackType.String:
                                var typeStr = reader.ReadString();
                                if (!PolyTypeMapper.TryGet(typeStr, typeof(T), out type))
                                    throw new MessagePackSerializationException($"Cannot find Type Id: {typeStr} registered in {nameof(PolyTypeMapper)}");
                                break;
                        }
                        break;
                    default:
                        type = typeof(T);
                        break;
                }
                return (T)(options.Resolver as PolyResolver).InnerDeserialize(type, ref reader, options);
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

}