using Core;
using MessagePack;

namespace Share
{
    /// <summary>
    /// 请求登陆
    /// </summary>
    [MessagePackObject(true)]
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
        public bool Success;
        public string Message;
        public string GameServerIp;
        public int GameServerPort;
        public string SessionToken;
    }
}
