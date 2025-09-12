using Core;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Login
{
    internal class LoginHttp:HttpServer
    {
        public LoginHttp(string address, int port) : base(address, port) { }

		protected override TcpSession CreateSession() { return new LoginSession(this); }

		protected override void OnError(SocketError error)
		{
			Logger.Error($"HTTP session caught an error: {error}");
		}
	}
}
