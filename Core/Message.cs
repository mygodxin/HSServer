using System;
using MessagePack;

namespace Core
{
    /// <summary>
    /// 消息基类，所有能收发的消息都应该继承该类
    /// </summary>
    [MessagePackObject(true)]
    public class Message
    {
        /// <summary>
        /// 全局唯一ID
        /// </summary>
        public int ID;
    }

    public class MessageError : Message
    {
        public string Error;
    }

    /// <summary>
    /// 消息标签
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageHandleAttribute : Attribute
    {
        public Type MessageType;

        public MessageHandleAttribute(Type type)
        {
            MessageType = type;
        }
    }
}