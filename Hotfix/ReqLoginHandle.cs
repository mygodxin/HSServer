using System;
using Core;
using LoginServer;
using Share;

namespace Hotfix
{
    [MessageHandle(typeof(ReqLogin))]
    public class ReqLoginHandle : MessageHandle
    {
        public override void Excute()
        {
            var reqLogin = Message as ReqLogin;
            var account = reqLogin.Account;
            var channel = Channel;
            if (string.IsNullOrEmpty(account))
            {
                channel.WriteError("账号不能为空");
                return;
            }
            var session = new Session();
            session.Account = account;
            session.LoginTime = DateTime.UtcNow;
            session.Channel = channel;
        }
        public void OnLogin()
        {

        }
    }
}
