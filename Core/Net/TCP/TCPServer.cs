//using Core.Net.Tcp;
//using System;
//using System.Collections.Concurrent;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;

//namespace Core.Net.TCP
//{
//    public class TcpSocketServer : ISocketServer
//    {
//        private Socket _serverSocket;
//        private CancellationTokenSource _cts;
//        private bool _isRunning;

//        public bool IsRunning => _isRunning;
//        public IPEndPoint ServerEndPoint { get; private set; }

//        public async Task StartAsync(string host, int port)
//        {
//            if (_isRunning)
//                throw new InvalidOperationException("Server is already running");

//            var ipAddress = IPAddress.Parse(host);
//            ServerEndPoint = new IPEndPoint(ipAddress, port);
//            _cts = new CancellationTokenSource();

//            // 创建原生Socket
//            _serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

//            // 设置Socket选项
//            _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//            _serverSocket.ReceiveTimeout = 5000;
//            _serverSocket.SendTimeout = 5000;

//            // 绑定并监听
//            _serverSocket.Bind(ServerEndPoint);
//            _serverSocket.Listen(backlog: 100); // 设置挂起连接队列的最大长度

//            _isRunning = true;

//            // 不需要在这里开始接受连接，AcceptClientAsync会处理
//        }

//        public async Task StopAsync()
//        {
//            if (!_isRunning)
//                return;

//            _cts?.Cancel();

//            try
//            {
//                // 优雅关闭
//                if (_serverSocket != null)
//                {
//                    _serverSocket.Shutdown(SocketShutdown.Both);
//                    _serverSocket.Close();
//                    _serverSocket.Dispose();
//                }
//            }
//            finally
//            {
//                _isRunning = false;
//                _serverSocket = null;
//            }
//        }

//        public async Task<NetChannel> AcceptClientAsync()
//        {
//            if (!_isRunning || _serverSocket == null)
//                throw new InvalidOperationException("Server is not running");

//            try
//            {
//                // 使用Task.Factory.FromAsync实现真正的异步Accept
//                var acceptTask = Task.Factory.FromAsync(
//                    _serverSocket.BeginAccept,
//                    _serverSocket.EndAccept,
//                    null);

//                // 添加取消支持
//                var completedTask = await Task.WhenAny(
//                    acceptTask,
//                    Task.Delay(Timeout.Infinite, _cts.Token)
//                );

//                if (completedTask == acceptTask)
//                {
//                    Socket clientSocket = await acceptTask;
//                    return new TcpChannel(clientSocket);
//                }

//                throw new OperationCanceledException("Server accept operation was cancelled");
//            }
//            catch (ObjectDisposedException)
//            {
//                throw new OperationCanceledException("Server was stopped");
//            }
//            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
//            {
//                throw new OperationCanceledException("Server accept operation was aborted");
//            }
//        }
//    }

//}
