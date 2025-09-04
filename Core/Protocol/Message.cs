using MemoryPack;
using MessagePack;
using System;

namespace Core
{
    [MemoryPackable(GenerateType.NoGenerate)]
    public partial interface IMessage
    {

    }

    [MemoryPackable]
    public partial class MessageError : IMessage
    {
        public string Error;
    }

    /// <summary>
    /// 消息标签
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageTypeAttribute : Attribute
    {
        public Type MessageType;

        public MessageTypeAttribute(Type type)
        {
            MessageType = type;
        }
    }
}