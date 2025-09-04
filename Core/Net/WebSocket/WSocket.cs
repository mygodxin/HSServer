using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;

namespace Core.Net
{
    public class WSocket : NetClient
    {
        private WebSocket _webSocket;
        private readonly ConcurrentQueue<byte[]> _receiveQueue = new ConcurrentQueue<byte[]>();
        private bool _isRunning;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;
        public int Available => _receiveQueue.Count;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<byte[]> OnReceived;
        public event Action<Exception> OnError;

        public WSocket(WebSocket webSocket)
        {
            try
            {
                _isRunning = true;
                _webSocket = webSocket;

                // 启动接收循环
                Thread receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();

                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                Dispose();
            }
        }

        /// <summary>
        /// use by client
        /// </summary>
        public WSocket(IPEndPoint remoteEndPoint)
        {
            if (IsConnected || _isRunning) return;

            try
            {
                _isRunning = true;
                _webSocket = new ClientWebSocket();

                // 获取IP地址和端口号，并将其转换为URI格式的字符串
                String uriString = "http://" + remoteEndPoint.Address + ":" + remoteEndPoint.Port;

                // 开始连接
                ((ClientWebSocket)_webSocket).ConnectAsync(new Uri(uriString), default);

                // 启动接收循环
                Thread receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();

                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                Dispose();
            }
        }

        private async void ReceiveLoop()
        {
            byte[] buffer = new byte[4096];

            while (_isRunning && IsConnected)
            {
                try
                {
                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        default);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            "Closed by server",
                            default);
                        break;
                    }

                    byte[] data = new byte[result.Count];
                    Buffer.BlockCopy(buffer, 0, data, 0, result.Count);
                    _receiveQueue.Enqueue(data);
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        OnError?.Invoke(ex);
                        _isRunning = false;
                        OnDisconnected?.Invoke();
                    }
                    break;
                }
            }
        }

        public async void Send(byte[] data)
        {
            if (IsConnected && data != null && data.Length > 0)
            {
                try
                {
                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(data),
                        WebSocketMessageType.Binary,
                        true,
                        default);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                    Dispose();
                }
            }
        }

        public byte[] Receive()
        {
            if (_receiveQueue.TryDequeue(out byte[] data))
            {
                return data;
            }
            return null;
        }

        public void Update()
        {
            // 在主线程处理接收到的数据
            while (_receiveQueue.TryDequeue(out byte[] data))
            {
                //OnDataReceived?.Invoke(data);
            }
        }

        public async void Dispose()
        {
            _isRunning = false;

            try
            {
                if (_webSocket != null)
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            "Client closed",
                            CancellationToken.None);
                    }

                    _webSocket.Dispose();
                    _webSocket = null;
                }

                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                // TODO ($"WebSocket关闭错误: {ex.Message}");
            }
            finally
            {
            }
        }
    }
}
