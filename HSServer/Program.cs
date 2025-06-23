


using Core;
using LoginServer;
using Luban;
using MessagePack;
using Share;
using System.Net.WebSockets;

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
            GateServer.GateServer.StartAsync();

            // 游戏服务启动

            TestWS();
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

                    var d = MessagePackSerializer.Serialize<ReqLogin>(login);
                    var c = MessagePackSerializer.Deserialize<Message>(d);
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
    }
}