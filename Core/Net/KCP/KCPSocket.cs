using Core.Util;
using KcpTransport;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Core.Net.KCP
{
    public class KCPSocket
    {
        private KcpConnection _connect;
        private bool _isRunning;
        private readonly ConcurrentQueue<byte[]> _receiveQueue = new ConcurrentQueue<byte[]>();
        private readonly BlockingCollection<byte[]> _sendQueue = new BlockingCollection<byte[]>();

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<byte[]> OnDataReceived;
        public event Action<Exception> OnError;

        /// <summary>
        /// use by server
        /// </summary>
        public KCPSocket(KcpConnection socket)
        {
            try
            {
                _connect = socket;

                _isRunning = true;
                Thread sendThread = new Thread(SendLoop) { IsBackground = true };
                sendThread.Start();

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
        public KCPSocket(IPEndPoint remoteEndPoint)
        {
            try
            {
                var task = KcpConnection.ConnectAsync(remoteEndPoint, default);
                task.AsTask().Wait();
                _connect = task.Result;

                _isRunning = true;
                _connect.OnRecive += ReceiveLoop;
                Thread sendThread = new Thread(SendLoop) { IsBackground = true };
                sendThread.Start();

                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                Dispose();
            }
        }

        /// <summary>
        /// 作为服务器接受连接 (在Accept的Socket上使用)
        /// </summary>
        public void Start(KcpConnection acceptedSocket)
        {
            try
            {
                _connect = acceptedSocket;
                _connect.OnRecive += ReceiveLoop;
                _isRunning = true;
                Thread sendThread = new Thread(SendLoop) { IsBackground = true };
                sendThread.Start();

                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                Dispose();
            }
        }

        private void ReceiveLoop(byte[] bytes)
        {
            byte[] buffer = new byte[4096];

            while (_isRunning)
            {
                try
                {
                    int bytesRead = bytes.Length;
                    if (bytesRead > 0)
                    {
                        _receiveQueue.Enqueue(bytes);
                    }
                    else
                    {
                        // 连接已关闭
                        _isRunning = false;
                        OnDisconnected?.Invoke();
                    }
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

        private void SendLoop()
        {
            while (_isRunning)
            {
                try
                {
                    byte[] data = _sendQueue.Take(); // 阻塞直到有数据
                    _connect.SendReliableBuffer(data);
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

        public void Send(byte[] data)
        {
            if (data != null && data.Length > 0)
            {
                _sendQueue.Add(data);
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
                OnDataReceived?.Invoke(data);
            }
        }

        public void Dispose()
        {
            _isRunning = false;

            try
            {
                _sendQueue?.CompleteAdding();
                _connect?.Dispose();
            }
            catch { /* 忽略关闭时的错误 */ }

            _sendQueue?.Dispose();
            _connect?.Dispose();
        }
    }
}
