using Core;
using KcpTransport;
using SharpCompress.Compressors.Xz;
using System.IO;
using System.Net;
using System.Net.Sockets;
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

        TestKCP();
        while (true)
        {

        }
    }

    private static async void TestKCP()
    {
        var client = await KcpConnection.ConnectAsync("127.0.0.1", 3000);
        var stream = await client.OpenOutboundStreamAsync();
        while (true)
        {
            var str = Console.ReadLine();
            if (str != null && str != "")
            {
                Console.WriteLine($"[Client]{str}");
                stream.WriteAsync(Encoding.UTF8.GetBytes(str));
            }
        }

    }
}