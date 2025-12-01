using Core;
using Core.Protocol;
using Share;

namespace Hotfix.Login
{
    [MessageType(typeof(LoginRequest))]
    public class ReqLoginHandle : MessageHandle
    {
        public override void Excute()
        {
            var reqLogin = Message as LoginRequest;
            var account = reqLogin.Username;
            var channel = Channel;
            if (string.IsNullOrEmpty(account))
            {
                //channel.SendError("账号不能为空");
                return;
            }
            Logger.Info($"[server] account={account}");
            //var session = new session();
            //session.Account = account;
            //session.LoginTime = DateTime.UtcNow;
            //session.Channel = channel;
            reqLogin.Username = reqLogin.Username + "1";
            channel.Send(Write(reqLogin));
        }
        public void OnLogin()
        {

        }
    }
}
