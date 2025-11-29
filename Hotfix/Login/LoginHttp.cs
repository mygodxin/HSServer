using Core;
using NetCoreServer;
using System.Net.Sockets;

namespace Hotfix.Login
{
    internal class LoginHttp : HttpServer
    {
        public LoginHttp(string address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new LoginSession(this); }

        protected override void OnError(SocketError error)
        {
            Logger.Error($"HTTP session caught an error: {error}");
        }
    }
}
