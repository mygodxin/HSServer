using Core;
using Core.Util;
using KcpTransport;
using MessagePack;
using Share;
using SharpCompress.Compressors.Xz;
using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

public class Program
{
    static void Main(string[] args)
    {
        //var client = new TcpClient();
        //client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 3000);
        //var stream = client.GetStream();
        //while (true)
        //{
        //    var str = Console.ReadLine();
        //    if (str != null && str != "")
        //    {
        //        Console.WriteLine($"[Client]{str}");
        //        stream.WriteAsync(Encoding.UTF8.GetBytes(str));
        //    }
        //}
        //stream.Close();
        //client.Close();

        //TestKCP();
        //TestWS();
        while (true)
        {

        }
    }

    private static async void TestKCP()
    {
        var client = await KcpConnection.ConnectAsync("127.0.0.1", 3000);
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
            //if (client.State == WebSocketState.Open)
            //{
            //    var result = await client.ReceiveAsync(data, _cancel.Token);
            //    if (result != null)
            //    {
            //        var msg = MessagePackSerializer.Deserialize<Message>(data);
            //        Console.WriteLine(msg);
            //    }
            //}

            var str = Console.ReadLine();
            if (str != null && str != "")
            {
                Console.WriteLine($"[Client]{str}");
                var login = new ReqLogin();
                login.Account = str;
                login.Password = "123456";
                login.Platform = "taptap";

                var bytes = HSerializer.Serialize(login);


                await client.SendAsync(bytes, WebSocketMessageType.Binary, true, _cancel.Token);
            }
        }
    }
}