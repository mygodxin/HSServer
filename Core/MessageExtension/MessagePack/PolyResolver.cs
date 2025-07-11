using FormatterExtension;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Core.MessageExtension.PolymorphicMessagePack
{
    public sealed class PolyResolver : IFormatterResolver
    {
        public static PolyResolver Instance { get; private set; } = new PolyResolver();

        static IFormatterResolver InnerResolver;
        static List<IFormatterResolver> innerResolver = new()
        {
               FormatterExtensionResolver.Instance,
               BuiltinResolver.Instance,
               StandardResolver.Instance,
               ContractlessStandardResolver.Instance
        };

        //先调用此函数注册需要的resolver，然后再调用init，比如客户端需要注册proto和配置表的resolver
        public static void AddInnerResolver(IFormatterResolver resolver, int index = 0)
        {
            if (innerResolver.IndexOf(resolver) < 0)
            {
                innerResolver.Insert(index, resolver);
            }
        }

        public void Init()
        {
            PolyTypeMapper.RegisterCore();
            StaticCompositeResolver.Instance.Register(innerResolver.ToArray());
            InnerResolver = StaticCompositeResolver.Instance;
            MessagePackSerializer.DefaultOptions = new MessagePackSerializerOptions(Instance).WithCompression(MessagePackCompression.Lz4Block);
        }

        private readonly ConcurrentDictionary<Type, PolyDelegate> _innerFormatterCache = new ConcurrentDictionary<Type, PolyDelegate>();

        private PolyResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (PolyTypeMapper.Contains(typeof(T)))
            {
                return FormatterCache<T>.Formatter;
            }

            return InnerResolver.GetFormatter<T>();
        }

        public void RemoveFormatterDelegateCache(Type type)
        {
            _innerFormatterCache.Remove(type, out _);
        }

        //Bottleneck
        public void InnerSerialize(Type type, ref MessagePackWriter writer, object value, MessagePackSerializerOptions options)
        {
            GetDelegate(type).Serialize(ref writer, value, options);
        }

        //Bottleneck
        public object InnerDeserialize(Type type, ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return GetDelegate(type).Deserialize(ref reader, options);
        }

        private PolyDelegate GetDelegate(Type type)
        {
            if (!_innerFormatterCache.TryGetValue(type, out var ploymorphicDeletegate))
            {
                var constructedType = typeof(PolyDelegate<>).MakeGenericType(type);

                ploymorphicDeletegate = (PolyDelegate)Activator.CreateInstance(constructedType, InnerResolver);

                _innerFormatterCache.TryAdd(type, ploymorphicDeletegate);
            }

            return ploymorphicDeletegate;
        }

        private static class FormatterCache<T>
        {
            public static IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = new PolyFormatter<T>();
            }
        }
    }
}