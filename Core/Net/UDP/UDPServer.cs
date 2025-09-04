//using Core.Net.KCP;
//using KcpTransport.LowLevel;
//using System;
//using System.Collections.Concurrent;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;

//namespace Core.Net.UDP
//{
//    using System;
//    using System.Collections.Concurrent;
//    using System.Net;
//    using System.Net.Sockets;
//    using System.Threading;
//    using System.Threading.Tasks;

//    public class UdpServer
//    {
//        private Socket _socket;
//        private ConcurrentQueue<(EndPoint, byte[])> _receiveQueue = new();
//        private ConcurrentQueue<(EndPoint, byte[])> _sendQueue = new();
//        private ConcurrentDictionary<EndPoint, DateTime> _clients = new();
//        private CancellationTokenSource _cts;
//        private ReaderWriterLockSlim _clientLock = new(); // 读写锁优化连接字典
//        private int _workerThreads; // 可配置的线程数（8/16）

//        public event Action<EndPoint> OnConnected;
//        public event Action<EndPoint> OnDisconnected;
//        public event Action<EndPoint, byte[]> OnReceived;

//        public UdpServer(int port, int workerThreads = 8)
//        {
//            _workerThreads = workerThreads;
//            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
//            _cts = new CancellationTokenSource();
//        }

//        public void Start()
//        {
//            // 1个接收线程 + N个工作线程 + 1个发送线程 + 1个心跳线程
//            Task.Run(ReceiveLoop, _cts.Token);
//            for (int i = 0; i < _workerThreads; i++) // 启动工作线程池
//            {
//                Task.Run(ProcessLoop, _cts.Token);
//            }
//            Task.Run(SendLoop, _cts.Token);
//            Task.Run(HeartbeatCheck, _cts.Token);
//        }

//        private void ReceiveLoop()
//        {
//            byte[] buffer = new byte[8192]; // 扩大缓冲区
//            EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

//            while (!_cts.IsCancellationRequested)
//            {
//                try
//                {
//                    int bytesRead = _socket.ReceiveFrom(buffer, ref remoteEp);
//                    byte[] data = new byte[bytesRead];
//                    Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);
//                    _receiveQueue.Enqueue((remoteEp, data)); // 入队待处理
//                }
//                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
//                {
//                    break; // 安全退出
//                }
//            }
//        }

//        private void ProcessLoop()
//        {
//            while (!_cts.IsCancellationRequested)
//            {
//                if (_receiveQueue.TryDequeue(out var packet))
//                {
//                    var (remoteEp, data) = packet;
//                    bool isNew = false;

//                    // 读写锁保护连接字典[9,10](@ref)
//                    _clientLock.EnterUpgradeableReadLock();
//                    try
//                    {
//                        if (!_clients.ContainsKey(remoteEp))
//                        {
//                            _clientLock.EnterWriteLock();
//                            _clients[remoteEp] = DateTime.Now;
//                            isNew = true;
//                        }
//                        else
//                        {
//                            _clients[remoteEp] = DateTime.Now; // 更新活跃时间
//                        }
//                    }
//                    finally
//                    {
//                        if (_clientLock.IsWriteLockHeld) _clientLock.ExitWriteLock();
//                        _clientLock.ExitUpgradeableReadLock();
//                    }

//                    if (isNew) OnConnected?.Invoke(remoteEp);
//                    OnReceived?.Invoke(remoteEp, data);
//                }
//                else
//                {
//                    Thread.Sleep(1); // 避免CPU空转
//                }
//            }
//        }

//        private void SendLoop()
//        {
//            while (!_cts.IsCancellationRequested)
//            {
//                if (_sendQueue.TryDequeue(out var packet))
//                {
//                    var (remoteEp, data) = packet;
//                    _socket.SendTo(data, remoteEp);
//                }
//                Thread.Sleep(1);
//            }
//        }

//        private void HeartbeatCheck()
//        {
//            while (!_cts.IsCancellationRequested)
//            {
//                foreach (var client in _clients)
//                {
//                    if ((DateTime.Now - client.Value).TotalSeconds > 30)
//                    {
//                        OnDisconnected?.Invoke(client.Key);
//                        _clients.TryRemove(client.Key, out _);
//                    }
//                }
//                Thread.Sleep(5000); // 每5秒检测一次
//            }
//        }

//        public void Send(EndPoint endPoint, byte[] data) =>
//            _sendQueue.Enqueue((endPoint, data));

//        public void Stop() => _cts?.Cancel();
//    }
//}
