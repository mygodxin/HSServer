using Core;
using Core.Protocol;
using Share;

namespace Hotfix.Login
{
    [MessageType(typeof(ClientMessage))]
    public class ClientMessageHandle : MessageHandle
    {
        public override void Excute()
        {
            var reqLogin = Message as ClientMessage;
            var account = reqLogin.Content;
            var channel = Channel;
            if (string.IsNullOrEmpty(account))
            {
                //channel.SendError("账号不能为空");
                return;
            }
            Logger.Info($"[server] ClientMessage={account}");
            reqLogin.Content = reqLogin.Content + "1";
            channel.Send(reqLogin);
        }
        public void OnLogin()
        {

        }
    }
}
