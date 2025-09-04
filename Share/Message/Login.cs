using Core;
using MemoryPack;
using MessagePack;
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

    [MemoryPackable]
    public partial class GateForward
    {
        public int UserId { get; set; }
        public byte[] GameData { get; set; }
    }

    [MemoryPackable]
    public partial class UserLoginEvent
    {
        public int UserId { get; set; }
    }
}
