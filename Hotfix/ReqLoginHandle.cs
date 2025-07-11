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
                channel.SendError("账号不能为空");
                return;
            }
            //Logger.Info($"[server] account={account}");
            //var session = new session();
            //session.Account = account;
            //session.LoginTime = DateTime.UtcNow;
            //session.Channel = channel;
            reqLogin.Account = reqLogin.Account + "1";
            channel.Send(reqLogin);
        }
        public void OnLogin()
        {

        }
    }
}
