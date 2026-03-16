using MessagePack;
using System;

namespace Share
{
    // 客户端发送的登录请求
    [MessagePackObject(true)]
    public partial class LoginRequest : Core.Message
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConnectionId { get; set; }
    }

    // 服务器返回的登录响应
    [MessagePackObject(true)]
    public partial class LoginResponse : Core.Message
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
    }

    // 客户端发送的消息
    [MessagePackObject(true)]
    public partial class ClientMessage : Core.Message
    {
        public string Content { get; set; }
        public string Token { get; set; }
    }

    // 服务器广播的消息
    [MessagePackObject(true)]
    public partial class BroadcastMessage : Core.Message
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
