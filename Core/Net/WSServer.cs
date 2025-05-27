using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace Core
{
    public class WSServer : IDisposable, ISocket
    {
        private readonly HttpListener _httpListener;
        private readonly ConcurrentDictionary<Guid, ClientInfo> _connectedClients = new();
        private readonly CancellationTokenSource _cts = new();
        private const int InactiveTimeoutSeconds = 20; // 20秒无活动断开连接
        private const int CheckInterval = 5000; // 5秒

        public event Action<Guid> OnClientConnected;
        public event Action<Guid> OnClientDisconnected;
        public event Action<Guid, string> OnMessageReceived;

        public WSServer(string listenerPrefix)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(listenerPrefix);
        }

        public async Task StartAsync()
        {
            _httpListener.Start();
            Logger.Info($"服务器已启动，监听 {string.Join(", ", _httpListener.Prefixes)}");

            // 启动定时检查闲置连接的任务
            _ = Task.Run(CheckInactiveConnectionsAsync);

            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var context = await _httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex) when (!_cts.IsCancellationRequested)
            {
                Logger.Error($"服务器错误: {ex.Message}");
            }
        }

        private async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            System.Net.WebSockets.WebSocket webSocket = null;
            Guid clientId = Guid.NewGuid();

            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                webSocket = webSocketContext.WebSocket;

                var clientInfo = new ClientInfo(webSocket);
                _connectedClients.TryAdd(clientId, clientInfo);

                OnClientConnected?.Invoke(clientId);
                Logger.Info($"客户端 {clientId} 已连接，当前连接数: {_connectedClients.Count}");

                await ReceiveMessagesAsync(clientId, clientInfo);
            }
            catch (Exception ex)
            {
                Logger.Error($"客户端 {clientId} 连接错误: {ex.Message}");
                await DisconnectAsync(clientId);
            }
        }

        private async Task ReceiveMessagesAsync(Guid clientId, ClientInfo clientInfo)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (clientInfo.WebSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var result = await clientInfo.WebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync(clientId);
                        return;
                    }

                    // 更新最后活动时间
                    clientInfo.UpdateLastActivityTime();

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessageReceived?.Invoke(clientId, message);
                    Logger.Info($"收到来自 {clientId} 的消息: {message}");
                }
            }
            catch (WebSocketException ex)
            {
                Logger.Error($"客户端 {clientId} 接收错误: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                // 服务器正在关闭，忽略
            }
            finally
            {
                await DisconnectAsync(clientId);
            }
        }

        /// <summary>
        /// 定时检查闲置连接（每5秒检查一次）
        /// </summary>
        private async Task CheckInactiveConnectionsAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(CheckInterval, _cts.Token); // 每5秒检查一次

                foreach (var client in _connectedClients)
                {
                    if (client.Value.IsInactive(InactiveTimeoutSeconds))
                    {
                        Console.WriteLine($"客户端 {client.Key} 因闲置超时将被断开");
                        await DisconnectAsync(client.Key);
                    }
                }
            }
        }

        public async Task DisconnectAsync(Guid clientId)
        {
            if (_connectedClients.TryRemove(clientId, out var clientInfo))
            {
                try
                {
                    if (clientInfo.WebSocket.State == WebSocketState.Open)
                    {
                        await clientInfo.WebSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "连接超时",
                            _cts.Token);
                    }
                    clientInfo.WebSocket.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭客户端 {clientId} 连接时出错: {ex.Message}");
                }

                OnClientDisconnected?.Invoke(clientId);
                Console.WriteLine($"客户端 {clientId} 已断开，当前连接数: {_connectedClients.Count}");
            }
        }

        public async Task SendAsync(Guid clientId, string message)
        {
            if (_connectedClients.TryGetValue(clientId, out var clientInfo) &&
                clientInfo.WebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await clientInfo.WebSocket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        _cts.Token);

                    // 发送消息也算活动，更新最后活动时间
                    clientInfo.UpdateLastActivityTime();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"向客户端 {clientId} 发送消息失败: {ex.Message}");
                    await DisconnectAsync(clientId);
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _httpListener?.Close();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 客户端连接信息（包含最后活动时间）
        /// </summary>
        private class ClientInfo
        {
            public System.Net.WebSockets.WebSocket WebSocket { get; }
            private DateTime _lastActivityTime;

            public ClientInfo(System.Net.WebSockets.WebSocket webSocket)
            {
                WebSocket = webSocket;
                _lastActivityTime = DateTime.UtcNow;
            }

            public void UpdateLastActivityTime() => _lastActivityTime = DateTime.UtcNow;

            public bool IsInactive(int timeoutSeconds) =>
                (DateTime.UtcNow - _lastActivityTime).TotalSeconds > timeoutSeconds;
        }
    }
}