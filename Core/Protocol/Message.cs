using MessagePack;
using System;

namespace Core
{
    [MessagePackObject(true)]
    public class Message
    {

    }

    [MessagePackObject(true)]
    public class MessageError : Message
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