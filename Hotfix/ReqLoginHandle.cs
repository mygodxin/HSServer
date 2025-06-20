using Core;
using LoginServer;
using Share;
using System;

namespace Hotfix
{
    [MessageType(typeof(ReqLogin))]
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
            //var session = new session();
            //session.Account = account;
            //session.LoginTime = DateTime.UtcNow;
            //session.Channel = channel;
        }
        public void OnLogin()
        {

        }
    }
}
