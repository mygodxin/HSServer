// TcpClientExample.cs
using Share;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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
        var loginRequest = new
        {
            type = "login",
            username = username,
            password = password
        };

        var json = JsonSerializer.Serialize(loginRequest);
        await SendAsync(json);
    }

    private async Task SendMessageAsync(string content)
    {
        var message = new
        {
            type = "message",
            content = content,
            token = "your-token-here" // 实际应该从登录响应中获取
        };

        var json = JsonSerializer.Serialize(message);
        await SendAsync(json);
    }

    private async Task SendAsync(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data + "\n");
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

                var message = Encoding.UTF8.GetString(dataBuffer, 0, bytesRead);
                Console.WriteLine($"Received: {message}");

                // 处理不同类型的消息
                using var jsonDoc = JsonDocument.Parse(message);
                if (jsonDoc.RootElement.TryGetProperty("type", out var typeProperty))
                {
                    var messageType = typeProperty.GetString();

                    if (messageType == "login_response")
                    {
                        // 处理登录响应
                        var response = JsonSerializer.Deserialize<LoginResponse>(message);
                        if (response.Success)
                        {
                            Console.WriteLine($"Login successful! Token: {response.Token}");
                        }
                        else
                        {
                            Console.WriteLine($"Login failed: {response.Message}");
                        }
                    }
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