using MessagePack;
using System;

namespace Core
{
    /// <summary>
    /// 消息基类，所有能收发的消息都应该继承该类
    /// </summary>
    [MessagePackObject(true)]
    public class Message
    {
        ///// <summary>
        ///// 全局唯一ID
        ///// </summary>
        //public int ID => GetType().FullName.GetHashCode();
    }

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