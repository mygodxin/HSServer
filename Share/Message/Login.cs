using Core;
using MessagePack;

namespace Share
{
    /// <summary>
    /// 请求登陆
    /// </summary>
    [MessagePackObject(true)]
    [Union(0, typeof(ReqLogin))]
    public class ReqLogin : Message
    {
        public string Account;
        public string Password;
        public string Platform;
    }

    /// <summary>
    /// 请求登陆回复
    /// </summary>
    [MessagePackObject(true)]
    public class ResLogin : Message
    {
        public string Token { get; set; }
        public string GateAddress { get; set; } // "IP:Port"
    }

    [MessagePackObject(true)]
    public class GateForward
    {
        public int UserId { get; set; }
        public byte[] GameData { get; set; }
    }

    [MessagePackObject(true)]
    public class UserLoginEvent
    {
        public int UserId { get; set; }
    }
}
