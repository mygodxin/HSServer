using Core;
using MessagePack;

namespace Share
{
    /// <summary>
    /// 请求登陆
    /// </summary>
    [MessagePackObject(true)]
    public class C2S_Login : Message
    {
        public string Account;
        public string Password;
        public string ClientVersion;
    }

    /// <summary>
    /// 请求登陆回复
    /// </summary>
    [MessagePackObject(true)]
    public class S2C_Login : Message
    {
        public bool Success;
        public string Message;
        public string GameServerIp;
        public int GameServerPort;
        public string SessionToken;
    }
}
