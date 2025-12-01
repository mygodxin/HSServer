// TcpClientExample.cs
using Core;
using Core.Protocol;
using Luban;
using Share;
using System.Net.Sockets;

public class TcpClientExample
{
    private TcpClient _client;
    private NetworkStream _stream;

    public async Task ConnectAsync(string server = "127.0.0.1", int port = 8080)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(server, port);
        _stream = _client.GetStream();

        Console.WriteLine("Connected to server");

        // 启动接收消息的任务
        _ = ReceiveMessagesAsync();

        // 发送登录请求
        await LoginAsync("admin", "123456");

        // 发送测试消息
        await SendMessageAsync("Hello, World!");

        // 保持连接
        Console.ReadLine();
    }

    private async Task LoginAsync(string username, string password)
    {
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };
        await SendAsync(loginRequest);
    }

    private async Task SendMessageAsync(string message)
    {
        var messageData = new ClientMessage
        {
            Content = message
        };
        await SendAsync(messageData);
    }

    private async Task SendAsync(IMessage data)
    {
        var bytes = MessageHandle.Write(data);
        await _stream.WriteAsync(bytes, 0, bytes.Length);
        await _stream.FlushAsync();
    }

    private async Task ReceiveMessagesAsync()
    {
        var dataBuffer = new byte[1024];

        while (_client.Connected)
        {
            try
            {
                var bytesRead = await _stream.ReadAsync(dataBuffer, 0, dataBuffer.Length);
                if (bytesRead == 0) break;
                var buffer = dataBuffer.AsSpan(0, bytesRead);
                var buf = new ByteBuf(buffer.ToArray());
                int msgLen = buf.ReadInt();
                int msgID = buf.ReadInt();
                ReadOnlySpan<byte> bytes = buf.ReadBytes();
                var message = HSerializer.Deserialize<IMessage>(bytes);
                Console.WriteLine($"Received: {message}");

                switch (message)
                {
                    case LoginResponse loginResponse:
                        Console.WriteLine($"Login response: {loginResponse.Success} - {loginResponse.Message}");
                        break;
                    case ClientMessage clientMessage:
                        Console.WriteLine($"Received message: {clientMessage.Content}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                break;
            }
        }
    }
}

// 客户端主程序
class ClientProgram
{
    static async Task Main(string[] args)
    {
        var client = new TcpClientExample();
        await client.ConnectAsync();
    }
}