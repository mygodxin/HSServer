using System;

namespace Hotfix.Login
{
    using Proto;
    using Share;
    // ClientActor.cs
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class ClientActor : IActor
    {
        private readonly TcpClient _client;
        private readonly PID _sessionManager;
        private readonly string _connectionId;
        private NetworkStream _networkStream;
        private string _currentToken;

        public ClientActor(TcpClient client, PID sessionManager, string connectionId)
        {
            _client = client;
            _sessionManager = sessionManager;
            _connectionId = connectionId;
            _networkStream = client.GetStream();
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case MessageReceived messageReceived:
                    await HandleMessageReceived(context, messageReceived.Data);
                    break;

                case SendMessage sendMessage:
                    await SendDataToClient(sendMessage.Data);
                    break;
            }
        }

        private async Task HandleMessageReceived(IContext context, string data)
        {
            try
            {
                // 尝试解析JSON消息
                using var jsonDoc = JsonDocument.Parse(data);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("type", out var typeProperty))
                {
                    var messageType = typeProperty.GetString();

                    switch (messageType)
                    {
                        case "login":
                            var loginRequest = JsonSerializer.Deserialize<LoginRequest>(data);
                            loginRequest.ConnectionId = _connectionId;
                            context.Send(_sessionManager, loginRequest);
                            break;

                        case "message":
                            var clientMessage = JsonSerializer.Deserialize<ClientMessage>(data);

                            // 验证token
                            if (!string.IsNullOrEmpty(clientMessage.Token) &&
                                clientMessage.Token == _currentToken)
                            {
                                var broadcastMessage = new BroadcastMessage
                                {
                                    Sender = "User", // 实际应该从会话中获取用户名
                                    Content = clientMessage.Content,
                                    Timestamp = DateTime.Now
                                };
                                context.Send(_sessionManager, broadcastMessage);
                            }
                            else
                            {
                                await SendDataToClient(JsonSerializer.Serialize(new
                                {
                                    type = "error",
                                    message = "Not authenticated"
                                }));
                            }
                            break;
                    }
                }
            }
            catch (JsonException)
            {
                await SendDataToClient(JsonSerializer.Serialize(new
                {
                    type = "error",
                    message = "Invalid message format"
                }));
            }
        }

        private async Task SendDataToClient(string data)
        {
            if (_client.Connected)
            {
                var bytes = Encoding.UTF8.GetBytes(data + "\n");
                await _networkStream.WriteAsync(bytes, 0, bytes.Length);
                await _networkStream.FlushAsync();
            }
        }
    }
}
