


using Core;
using Core.Net.KCP;
using Core.Util;
using KcpTransport;
using LoginServer;
using Luban;
using MessagePack;
using Microsoft.Extensions.Hosting;
using Share;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace HSServer
{
    public static class Program
    {

        public static void Main(string[] args)
        {
            // 初始化日志
            Logger.Init();

            // 初始化代码加载
            var controller = new CodeLoader();
            controller.Init();

            // 登录服务启动
            LoginServer.LoginServer.StartAsync();

            // 网关服务启动
            //GateServer.GateServer.StartAsync();

            // 游戏服务启动
            TestKCP();
            //TestWS();
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "reload")
                {
                    controller.Reload();
                }
                else if (input == "exit")
                {
                    LoginServer.LoginServer.StopAsync();
                    Environment.Exit(0);
                }
            }
        }
        private static CancellationTokenSource _cancel;
        private static async void TestWS()
        {
            _cancel = new CancellationTokenSource();
            var client = new ClientWebSocket();
            Console.WriteLine($"[Client] 开始连接");
            await client.ConnectAsync(new Uri("ws://localhost:3000/ws"), _cancel.Token);
            Console.WriteLine($"[Client] 连接成功");
            var data = new byte[4096];
            while (true)
            {
                var str = Console.ReadLine();
                if (str != null && str != "")
                {
                    Console.WriteLine($"[Client]{str}");
                    var login = new ReqLogin();
                    login.Account = str;
                    login.Password = "123456";
                    login.Platform = "taptap";

                    var d = HSerializer.Serialize<ReqLogin>(login);
                    var c = HSerializer.Deserialize<Message>(d);
                    var p = c as ReqLogin;
                    Logger.Info(p.ToString());
                    await client.SendAsync(MessageHandle.Write(login), WebSocketMessageType.Binary, true, _cancel.Token);
                }
                //if (client.State == WebSocketState.Open)
                //{
                //    var result = await client.ReceiveAsync(data, _cancel.Token);
                //    if (result != null)
                //    {
                //        var msg = MessagePackSerializer.Deserialize<Message>(data);
                //        Console.WriteLine(msg);
                //    }
                //}

            }
        }

        private static async void TestKCP()
        {
            var server = new KcpServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000));
            await server.StartAsync();
            Logger.Info("服务器启动成功");
            var client = await KcpConnection.ConnectAsync("127.0.0.1", 5000);
            client.OnRecive = (byte[] bytes) =>
            {
                var msg = MessagePackSerializer.Deserialize<Message>(bytes);
                Console.WriteLine(msg);
                Logger.Info($"[Client Recive] ");
            };
            while (true)
            {
                var str = Console.ReadLine();
                if (str != null && str != "")
                {
                    Console.WriteLine($"[Client]{str}");
                    client.SendReliableBuffer(Encoding.UTF8.GetBytes(str));
                }
            }
        }
    }
}