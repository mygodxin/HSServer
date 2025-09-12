using Core;
using Core.Protocol;
using NetCoreServer;
using Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Login
{
    public class LoginHandleData
    {
        public string Url;
        public ReqLogin ReqLogin;
    }

    internal class LoginSession: HttpSession
	{
        public LoginSession(HttpServer httpServer) : base(httpServer) { }

        protected override void OnReceivedRequest(HttpRequest request)
        {
            if (request.Method != "POST") return;

            var url = request.Url;
            var datas = request.BodyBytes;

            var reqLogin = (ReqLogin)HSerializer.Deserialize<IMessage>(datas);
            if(reqLogin == null) return;

            var data = new LoginHandleData();
            data.Url = url;
            data.ReqLogin = reqLogin;

			LoginServer.LoginServer.OnReceive(data);
        }

        protected override void OnReceivedRequestError(HttpRequest request, string error)
        {
            Logger.Error($"LoginSessionError:{error}");
        }

        protected override void OnError(SocketError error)
        {
			Logger.Error($"LoginSessionError:{error}");
		}
    }
}
