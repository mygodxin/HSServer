using Core;
using Hotfix.Login;
using NetCoreServer;
using Proto;

using Proto.Remote;
using Proto.Remote.GrpcNet;
using System;

namespace LoginServer
{
    /// <summary>
    /// 只负责账号验证
    /// </summary>
    public class LoginServer
    {
        private static HttpServer _http;
        private static int _port = 8080;
        private static ActorSystem _system;

        public static async void StartAsync()
        {
            var remoteConfig = GrpcNetRemoteConfig
                .BindTo("127.0.0.1", _port)
                .WithRemoteKind("login_server", Props.FromProducer(() => new LoginHandler()))
                .WithSerializer(10, 10, new MsgPackSerializer());
            var system = new ActorSystem()
                .WithRemote(remoteConfig);
            await system
               .Remote()
               .StartAsync();
            _system = system;

            Console.WriteLine("Starting secure WebSocket server...");

            // 创建安全WebSocket服务器
            var server = new LoginHttp(
                "127.0.0.1",
                8081
            );
            server.Start();
            _http = server;
        }

        public static async void StopAsync()
        {
            _http.Stop();
            await _system.ShutdownAsync();
        }

        public static void OnReceive(LoginHandleData data)
        {
            _system.Root.Send(_system.Root.Self, data);
        }
    }
}
