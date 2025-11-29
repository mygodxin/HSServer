


using Core;
using Core.Net;
using Core.Net.KCP;
using Core.Protocol;
using Core.Util;
using Luban;
using Share;
using System;
using System.Diagnostics;
using System.Threading;

namespace HSServer
{
    public static class Program
    {

        public static void Main(string[] args)
        {
            // 加载服务器配置
            ServerManager.Init();

            // 初始化日志
            Logger.Init();

            var startUp = new StartUp();
            startUp.Init();
        }
        private static CancellationTokenSource _cancel;
        private static async void TestWS()
        {
        }

        private static async void TestLogin()
        {
            var client = new NetCoreServer.HttpClient("127.0.0.1", 8081);
            client.ConnectAsync();
            while (true)
            {
                var input = Console.ReadLine();
                if (input != null)
                {
                    var login = new ReqLogin();
                    login.Account = input;
                    login.Password = "123456";
                    login.Platform = "taptap";

                    client.Send(MessageHandle.Write(login));
                }
            }
        }

        private static async void TestKCP()
        {
            var server = new KcpServer("127.0.0.1", 5000);
            await server.StartAsync();
            Logger.Info("服务器启动成功");
            var client = new KcpClient("127.0.0.1", 5000);
            await client.ConnectAsync();
            client.OnReceived = ((buffer) =>
            {
                var buf = new ByteBuf(buffer);
                int msgLen = buf.ReadInt();
                int msgID = buf.ReadInt();
                ReadOnlySpan<byte> data = buf.ReadBytes();
                var message = (ReqLogin)HSerializer.Deserialize<IMessage>(data);
                Logger.Info($"[Client Recive] " + message.Account);
            });
            var time = Stopwatch.GetTimestamp();
            Logger.Warn($"[kcp]开始发送:{time}");

            while (true)
            {
                var input = Console.ReadLine();
                if (input != null)
                {
                    var login = new ReqLogin();
                    login.Account = input;
                    login.Password = "123456";
                    login.Platform = "taptap";

                    //var bytes = HSerializer.Serialize(login);
                    //int len = 8 + bytes.Length;
                    //var msgID = HandleManager.Instance.GetID(login.GetType());

                    //var buf = new ByteBuffer();
                    //buf.WriteInt(len);
                    //buf.WriteInt(msgID);
                    //buf.WriteBytes(bytes);

                    client.Send(MessageHandle.Write(login));
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