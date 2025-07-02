


using Core;
using Core.Net;
using Core.Net.KCP;
using Core.Util;
using Google.Protobuf.WellKnownTypes;
using KcpTransport;
using LoginServer;
using Luban;
using MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Share;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

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
            var buffer = new ArraySegment<byte>(new byte[4096]); // 使用适当大小的缓冲区
            var login = new ReqLogin();
            login.Account = "123456";
            login.Password = "123456";
            login.Platform = "taptap";
            var time = Stopwatch.GetTimestamp();
            Logger.Warn($"[ws]开始发送:{time}");
            var times = 0;
            // 发送初始登录消息
            await client.SendAsync(
                MessageHandle.Write(login),
                WebSocketMessageType.Binary,
                true,
                _cancel.Token);

            while (client.State == WebSocketState.Open && !_cancel.IsCancellationRequested)
            {
                // 接收消息
                var receiveResult = await client.ReceiveAsync(buffer, _cancel.Token);

                if (receiveResult.Count > 0)
                {
                    // 处理接收到的消息
                    var messageData = new byte[receiveResult.Count];
                    Array.Copy(buffer.Array, buffer.Offset, messageData, 0, receiveResult.Count);

                    var msg = MessagePackSerializer.Deserialize<Message>(messageData);
                    times++;

                    // 每收到一条消息就发送响应（根据需求调整）
                    await client.SendAsync(MessageHandle.Write(login), WebSocketMessageType.Binary, true, _cancel.Token);

                    if (times >= 1000)
                    {
                        break;
                    }
                }
            }
            Logger.Warn($"[ws]结束发送:{Stopwatch.GetElapsedTime(time).TotalSeconds}");
        }

        private static async void TestKCP()
        {
            var server = new KcpServer("127.0.0.1", 5000);
            await server.StartAsync();
            Logger.Info("服务器启动成功");
            var client = new KCPSocket("127.0.0.1", 5000);
            var time = Stopwatch.GetTimestamp();
            Logger.Warn($"[kcp]开始发送:{time}");
            //var times = 0;
            client.OnDataReceived += (byte[] bytes) =>
            {
                var buf = new ByteBuffer(bytes);
                int msgLen = buf.ReadInt();
                int msgID = buf.ReadInt();
                ReadOnlyMemory<byte> data = buf.ReadBytes();
                var message = HSerializer.Deserialize<Message>(data);
                Logger.Info($"[Client Recive] " + message.ToString());
                //times++;
            };
            //var login = new ReqLogin();
            //login.Account = "123456";
            //login.Password = "123456";
            //login.Platform = "taptap";

            while (true)
            {
                var input = Console.ReadLine();
                if (input != null)
                {
                    var login = new ReqLogin();
                    login.Account = input;
                    login.Password = "123456";
                    login.Platform = "taptap";
                    client.Write(login);
                }
            }
            //while (true)
            //{
            //    //var str = Console.ReadLine();
            //    //if (str != null && str != "")HandshakeOkRequest-recive
            //    {
            //        client.SendReliableBuffer(MessageHandle.Write(login));
            //    }
            //    //if (times == 1000)
            //    //    break;
            //}
            Logger.Warn($"[kcp]结束发送:{Stopwatch.GetElapsedTime(time).TotalSeconds}");
        }
    }
}