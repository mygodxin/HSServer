using Core;
using NetCoreServer;
using Proto;

using Proto.Remote;
using Proto.Remote.GrpcNet;
using Share;
using System.Net;

namespace LoginServer
{
    /// <summary>
    /// 只负责账号验证
    /// </summary>
    public class LoginServer
    {
        private static SecureWebSocketServer _http;
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
            var server = new SecureWebSocketServer(
                IPAddress.Any, // 监听所有网络接口
                8080           // WebSocket端口
            );
            _http = server;
            // 启动服务器
            if (server.Start())
            {
                Console.WriteLine($"Server started on port {server.Port}");

                // 创建监控器
                var monitor = new SecurityMonitor(server);

                // 控制台命令处理
                Console.WriteLine("Enter 'stats' to view statistics, 'exit' to stop server");

                // 在后台线程中处理控制台输入
                var inputThread = new Thread(() =>
                {
                    string command;
                    while ((command = Console.ReadLine()) != "exit")
                    {
                        if (command == "stats")
                            monitor.PrintStats();
                        else if (command.StartsWith("blacklist "))
                        {
                            var ip = command.Substring(10);
                            server.IpBlacklist.AddToBlacklist(ip, true);
                            Console.WriteLine($"IP {ip} added to blacklist");
                        }
                        else if (command.StartsWith("unblacklist "))
                        {
                            var ip = command.Substring(12);
                            server.IpBlacklist.RemoveFromBlacklist(ip);
                            Console.WriteLine($"IP {ip} removed from blacklist");
                        }
                        else
                        {
                            Console.WriteLine("Unknown command");
                        }
                    }

                    // 停止服务器
                    Console.WriteLine("Stopping server...");
                    server.Stop();
                    Console.WriteLine("Server stopped");
                });

                inputThread.IsBackground = true;
                inputThread.Start();

                // 等待服务器停止
                while (server.IsStarted)
                {
                    Thread.Sleep(100);
                }
            }
            else
            {
                Console.WriteLine("Failed to start server!");
            }
        }

        public static async void StopAsync()
        {
            _http.Stop();
            await _system.ShutdownAsync();
        }

    }
}
