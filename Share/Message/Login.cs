using Core;
using MessagePack;

namespace Share
{
    /// <summary>
    /// 请求登陆
    /// </summary>
    [MessagePackObject(true)]
    public class C2SLogin : Message
    {
        /// <summary>
        /// 账号
        /// </summary>
        public string Account;
        /// <summary>
        /// 密码
        /// </summary>
        public string Password;
    }

    /// <summary>
    /// 请求登陆回复
    /// </summary>
    [MessagePackObject(true)]
    public class S2CLogin : Message
    {

    }
}
