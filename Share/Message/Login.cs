using Core;
using MemoryPack;
using System;

namespace Share
{
    // 客户端发送的登录请求
    [MemoryPackable]
    public partial class LoginRequest : IMessage
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConnectionId { get; set; }
    }

    // 服务器返回的登录响应
    [MemoryPackable]
    public partial class LoginResponse : IMessage
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
    }

    // 客户端发送的消息
    [MemoryPackable]
    public partial class ClientMessage : IMessage
    {
        public string Content { get; set; }
        public string Token { get; set; }
    }

    // 服务器广播的消息
    [MemoryPackable]
    public partial class BroadcastMessage : IMessage
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
