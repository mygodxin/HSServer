using Core;
using MemoryPack;
using MessagePack;
using Proto;
using Share.Message;

namespace Share
{
    /// <summary>
    /// 请求登陆
    /// </summary>
    [MemoryPackable]
    public partial class ReqLogin : ResLogin
    {
        public string Account;
        public string Password;
        public string Platform;
    }

    /// <summary>
    /// 请求登陆回复
    /// </summary>
    [MemoryPackable]
    public partial class ResLogin : IMessage
    {
        public string Token { get; set; }
        public string GateAddress { get; set; } // "IP:Port"
    }


    // 登录相关消息
    [MemoryPackable]
    public partial record LoginRequest(string Username, string Password) : IMessage;

    [MemoryPackable]
    public partial record LoginResponse(bool Success, string SessionToken, long UserId) : IMessage;

    // 玩家移动消息
    [MemoryPackable]
    public partial record PlayerMoveRequest(string SessionToken, float X, float Y, float Z) : IMessage;

    [MemoryPackable]
    public partial record PlayerMoveResponse(bool Success) : IMessage;

    // 会话验证消息
    [MemoryPackable]
    public partial record SessionValidationRequest(string SessionToken) : IMessage;

    [MemoryPackable]
    public partial record SessionValidationResponse(bool IsValid, long UserId) : IMessage;

    // 服务器注册消息
    [MemoryPackable]
    public partial record RegisterServer(string ServerType, ServerAddress Address) : IMessage;

    [MemoryPackable]
    public partial record ServerAddress(string Host, int Port) : IMessage;

    // 服务发现消息
    [MemoryPackable]
    public partial record GetServer(string ServerType) : IMessage;

    [MemoryPackable]
    public partial record ServerResponse(int Pid) : IMessage;

}
