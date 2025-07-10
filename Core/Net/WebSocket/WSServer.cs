using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;

namespace Core.Net
{


    public class WSServer : ISocketServer
    {
        private HttpListener _httpListener;
        private Thread _listenerThread;
        private bool _isRunning;
        private readonly ConcurrentDictionary<IPEndPoint, WSocket> _clients = new ConcurrentDictionary<IPEndPoint, WSocket>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public bool IsRunning => _isRunning;
        public IPEndPoint ServerEndPoint { get; }

        bool ISocketServer.IsRunning => throw new NotImplementedException();

        IPEndPoint ISocketServer.ServerEndPoint => throw new NotImplementedException();

        public WSServer(IPEndPoint endPoint)
        {
            ServerEndPoint = endPoint;
        }

        public Task StartAsync(string host, int port)
        {
            try
            {

                _httpListener = new HttpListener();
                string prefix = $"http://{ServerEndPoint.Address}:{ServerEndPoint.Port}/";
                _httpListener.Prefixes.Add(prefix);
                //Debug.Log($"WebSocket服务器正在启动，监听: {prefix}");
                _httpListener.Start();

                _isRunning = true;
                _listenerThread = new Thread(ListenForClients) { IsBackground = true };
                _listenerThread.Start();
            }
            catch (Exception ex)
            {
                //Debug.LogError($"WebSocket服务器启动失败: {ex}");
            }
            return Task.CompletedTask;
        }

        private async void ListenForClients()
        {
            //Debug.Log("WebSocket服务器已启动");

            while (_isRunning)
            {
                try
                {
                    // 异步获取客户端连接
                    HttpListenerContext context = await _httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketClient(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        //Debug.Log("拒绝了非WebSocket请求");
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        //Debug.LogError($"监听客户端时出错: {ex}");
                    }
                }
            }
        }

        private async void ProcessWebSocketClient(HttpListenerContext context)
        {
            WebSocket webSocket = null;

            try
            {
                // 接受WebSocket连接
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                webSocket = wsContext.WebSocket;

                // 创建客户端包装器
                var client = new WSocket(webSocket);

                client.OnDisconnected += () =>
                {
                    _clients.TryRemove(client.RemoteAddress, out _);
                    //Debug.Log($"客户端断开: {client.RemoteEndPoint}");
                };

                client.OnError += ex =>
                {
                };

                // 添加到客户端列表
                _clients.TryAdd(client.RemoteAddress, client);

                //Debug.Log($"新客户端连接: {client.RemoteEndPoint}");

                // 开始接收消息
                await client.StartReceiving();
            }
            catch (Exception ex)
            {
                //Debug.LogError($"处理客户端连接时出错: {ex}");
                webSocket?.Dispose();
            }
        }

        public Task StopAsync()
        {
            //Debug.Log("正在停止WebSocket服务器...");
            _isRunning = false;
            _cancellationTokenSource.Cancel();

            try
            {
                _httpListener?.Stop();
                _listenerThread?.Join(1000); // 等待监听线程结束

                // 断开所有客户端
                foreach (var client in _clients.Values)
                {
                    client.Dispose();
                }
                _clients.Clear();

                //Debug.Log("WebSocket服务器已停止");
            }
            catch (Exception ex)
            {
                //Debug.LogError($"停止服务器时出错: {ex}");
            }
            finally
            {
                _httpListener = null;
                _listenerThread = null;
            }
            return Task.CompletedTask;
        }
    }
}
