//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Core.Net
//{
//    using System;
//    using System.Collections.Concurrent;
//    using System.Diagnostics;
//    using System.Net;
//    using System.Net.WebSockets;
//    using System.Threading;
//    using System.Threading.Tasks;


//    public class WebSocketServer : ISocketServer
//    {
//        private HttpListener _httpListener;
//        private Thread _listenerThread;
//        private bool _isRunning;
//        private readonly ConcurrentDictionary<IPEndPoint, WSocket> _clients = new ConcurrentDictionary<IPEndPoint, WSocket>();
//        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

//        public bool IsRunning => _isRunning;
//        public IPEndPoint ServerEndPoint { get; }

//        public event Action<ISocket> OnClientConnected;
//        public event Action<ISocket> OnClientDisconnected;
//        public event Action<Exception> OnError;

//        public WebSocketServer(IPEndPoint endPoint)
//        {
//            ServerEndPoint = endPoint;
//        }

//        public void Start()
//        {
//            if (_isRunning) return;

//            try
//            {

//                _httpListener = new HttpListener();
//                string prefix = $"http://{ServerEndPoint.Address}:{ServerEndPoint.Port}/";
//                _httpListener.Prefixes.Add(prefix);
//                //Debug.Log($"WebSocket服务器正在启动，监听: {prefix}");

//                _isRunning = true;
//                _listenerThread = new Thread(ListenForClients) { IsBackground = true };
//                _listenerThread.Start();
//            }
//            catch (Exception ex)
//            {
//                //Debug.LogError($"WebSocket服务器启动失败: {ex}");
//                OnError?.Invoke(ex);
//                Stop();
//            }
//        }

//        private async void ListenForClients()
//        {
//            _httpListener.Start();
//            //Debug.Log("WebSocket服务器已启动");

//            while (_isRunning)
//            {
//                try
//                {
//                    // 异步获取客户端连接
//                    HttpListenerContext context = await _httpListener.GetContextAsync();
//                    if (context.Request.IsWebSocketRequest)
//                    {
//                        ProcessWebSocketClient(context);
//                    }
//                    else
//                    {
//                        context.Response.StatusCode = 400;
//                        context.Response.Close();
//                        //Debug.Log("拒绝了非WebSocket请求");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    if (_isRunning)
//                    {
//                        //Debug.LogError($"监听客户端时出错: {ex}");
//                        OnError?.Invoke(ex);
//                    }
//                }
//            }
//        }

//        private async void ProcessWebSocketClient(HttpListenerContext context)
//        {
//            WebSocket webSocket = null;

//            try
//            {
//                // 接受WebSocket连接
//                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
//                webSocket = wsContext.WebSocket;

//                // 创建客户端包装器
//                var client = new WSocket(webSocket);

//                client.OnDisconnected += () =>
//                {
//                    _clients.TryRemove(webSocket, out _);
//                    OnClientDisconnected?.Invoke(client);
//                    //Debug.Log($"客户端断开: {client.RemoteEndPoint}");
//                };

//                client.OnError += ex =>
//                {
//                    //Debug.LogError($"客户端错误: {ex}");
//                    OnError?.Invoke(ex);
//                };

//                // 添加到客户端列表
//                _clients.TryAdd(client, true);
//                OnClientConnected?.Invoke(client);

//                //Debug.Log($"新客户端连接: {client.RemoteEndPoint}");

//                // 开始接收消息
//                await client.StartReceiving();
//            }
//            catch (Exception ex)
//            {
//                //Debug.LogError($"处理客户端连接时出错: {ex}");
//                OnError?.Invoke(ex);
//                webSocket?.Dispose();
//            }
//        }

//        public void Stop()
//        {
//            if (!_isRunning) return;

//            //Debug.Log("正在停止WebSocket服务器...");
//            _isRunning = false;
//            _cancellationTokenSource.Cancel();

//            try
//            {
//                _httpListener?.Stop();
//                _listenerThread?.Join(1000); // 等待监听线程结束

//                // 断开所有客户端
//                foreach (var client in _clients.Keys)
//                {
//                    client.Dispose();
//                }
//                _clients.Clear();

//                //Debug.Log("WebSocket服务器已停止");
//            }
//            catch (Exception ex)
//            {
//                //Debug.LogError($"停止服务器时出错: {ex}");
//            }
//            finally
//            {
//                _httpListener = null;
//                _listenerThread = null;
//            }
//        }

//        public void Update()
//        {
//            // 更新所有客户端
//            foreach (var client in _clients.Keys)
//            {
//                client.Update();
//            }
//        }

//        public void Dispose()
//        {
//            Stop();
//            _cancellationTokenSource.Dispose();
//        }
//    }

//    // WebSocket客户端包装类
//    public class WebSocketClient : ISocket
//    {
//        private WebSocket _webSocket;
//        private readonly CancellationTokenSource _cts;
//        private readonly ConcurrentQueue<byte[]> _receiveQueue = new ConcurrentQueue<byte[]>();
//        private bool _isConnected;

//        public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;
//        public IPEndPoint LocalEndPoint => null;
//        public IPEndPoint RemoteEndPoint { get; } = new IPEndPoint(IPAddress.None, 0);
//        public int Available => _receiveQueue.Count;

//        public event Action OnConnected;
//        public event Action OnDisconnected;
//        public event Action<byte[]> OnDataReceived;
//        public event Action<Exception> OnError;

//        public WebSocketClient(WebSocket webSocket, CancellationToken cancellationToken)
//        {
//            _webSocket = webSocket;
//            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//            RemoteEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0); // 实际地址在连接时设置
//        }

//        public async Task StartReceiving()
//        {
//            _isConnected = true;
//            OnConnected?.Invoke();

//            byte[] buffer = new byte[4096];

//            try
//            {
//                while (_isConnected && _webSocket.State == WebSocketState.Open)
//                {
//                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(
//                        new ArraySegment<byte>(buffer), _cts.Token);

//                    if (result.MessageType == WebSocketMessageType.Close)
//                    {
//                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
//                            "Client closed", CancellationToken.None);
//                        break;
//                    }

//                    byte[] data = new byte[result.Count];
//                    Buffer.BlockCopy(buffer, 0, data, 0, result.Count);
//                    _receiveQueue.Enqueue(data);
//                }
//            }
//            catch (Exception ex)
//            {
//                if (_isConnected)
//                {
//                    //Debug.LogError($"接收数据时出错: {ex}");
//                    OnError?.Invoke(ex);
//                }
//            }
//            finally
//            {
//                Disconnect();
//            }
//        }

//        public void Send(byte[] data)
//        {
//            if (!IsConnected || data == null || data.Length == 0) return;

//            try
//            {
//                _ = _webSocket.SendAsync(new ArraySegment<byte>(data),
//                    WebSocketMessageType.Binary, true, _cts.Token);
//            }
//            catch (Exception ex)
//            {
//                //Debug.LogError($"发送数据时出错: {ex}");
//                OnError?.Invoke(ex);
//                Disconnect();
//            }
//        }

//        public byte[] Receive()
//        {
//            _receiveQueue.TryDequeue(out byte[] data);
//            return data;
//        }

//        public void Update()
//        {
//            while (_receiveQueue.TryDequeue(out byte[] data))
//            {
//                OnDataReceived?.Invoke(data);
//            }
//        }

//        private void Disconnect()
//        {
//            if (!_isConnected) return;

//            _isConnected = false;

//            try
//            {
//                _webSocket?.Dispose();
//            }
//            catch { /* 忽略关闭错误 */ }

//            OnDisconnected?.Invoke();
//        }

//        public void Dispose()
//        {
//            Disconnect();
//            _cts.Cancel();
//        }
//    }
//}
