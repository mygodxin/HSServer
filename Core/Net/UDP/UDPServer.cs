using Core.Net.KCP;
using Core.Net.Tcp;
using KcpTransport.LowLevel;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Core.Net.UDP
{
    public class UdpSocketServer : ISocketServer
    {
        private Socket _udpSocket;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private ConcurrentDictionary<IPEndPoint, DateTime> _activeClients = new ConcurrentDictionary<IPEndPoint, DateTime>();

        public bool IsRunning => _isRunning;
        public IPEndPoint ServerEndPoint { get; private set; }
        public Action<IPEndPoint, byte[], int> OnReceivedData;

        public async Task StartAsync(string host, int port)
        {
            if (_isRunning)
                throw new InvalidOperationException("Server is already running");

            var ipAddress = IPAddress.Parse(host);
            ServerEndPoint = new IPEndPoint(ipAddress, port);
            _cts = new CancellationTokenSource();

            // 创建UDP Socket
            _udpSocket = new Socket(ipAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            // 设置Socket选项
            _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpSocket.ReceiveTimeout = 0; // 非阻塞模式
            _udpSocket.SendTimeout = 5000;

            // 绑定到本地端点
            _udpSocket.Bind(ServerEndPoint);

            _isRunning = true;

            // 开始接收消息
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            _cts?.Cancel();

            try
            {
                if (_udpSocket != null)
                {
                    _udpSocket.Close();
                    _udpSocket.Dispose();
                }
            }
            finally
            {
                _isRunning = false;
                _udpSocket = null;
                _activeClients.Clear();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[(int)KcpMethods.IKCP_MTU_DEF]; // UDP最大数据包大小
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 异步接收数据
                    var receiveTask = Task.Factory.FromAsync(
                        (callback, state) => _udpSocket.BeginReceiveFrom(
                            buffer, 0, buffer.Length, SocketFlags.None,
                            ref remoteEP, callback, state),
                        asyncResult => _udpSocket.EndReceiveFrom(asyncResult, ref remoteEP),
                        null);

                    var timeoutTask = Task.Delay(Timeout.Infinite, cancellationToken);
                    var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                    if (completedTask == receiveTask)
                    {
                        int bytesReceived = await receiveTask;
                        var clientEP = (IPEndPoint)remoteEP;

                        // 更新活跃客户端列表
                        _activeClients[clientEP] = DateTime.UtcNow;

                        // 处理接收到的数据
                        if (bytesReceived > 0)
                        {
                            OnDataReceived(clientEP, buffer, bytesReceived);
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Socket已关闭
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    // 操作被取消
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UDP receive error: {ex.Message}");
                    await Task.Delay(1000); // 防止错误循环
                }
            }
        }

        protected virtual void OnDataReceived(IPEndPoint clientEP, byte[] data, int len)
        {
            // 可由子类重写处理具体数据
            //Console.WriteLine($"Received {data.Length} bytes from {clientEP}");
            OnReceivedData?.Invoke(clientEP, data, len);
        }

        // 向特定客户端发送消息
        public async Task SendToAsync(IPEndPoint clientEP, byte[] data)
        {
            if (!_isRunning)
                throw new InvalidOperationException("Server is not running");

            try
            {
                int bytesSent = await Task.Factory.FromAsync(
                    (callback, state) => _udpSocket.BeginSendTo(
                        data, 0, data.Length, SocketFlags.None,
                        clientEP, callback, state),
                    _udpSocket.EndSendTo,
                    null);

                Console.WriteLine($"Sent {bytesSent} bytes to {clientEP}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to send to {clientEP}: {ex.SocketErrorCode}");
            }
        }

        // 广播消息给所有已知客户端
        public async Task BroadcastAsync(byte[] data)
        {
            foreach (var clientEP in _activeClients.Keys.ToArray())
            {
                await SendToAsync(clientEP, data);
            }
        }
    }
}
