using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Core.Net.UDP
{

    public class UdpSocket : ISocket
    {
        private Socket _socket;
        private IPEndPoint _remoteEndPoint;
        private bool _isDisposed;

        public bool IsConnected => _socket != null && !_isDisposed && (_remoteEndPoint != null || _socket.IsBound);

        // use by client
        public UdpSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        // use by server
        public UdpSocket(IPEndPoint remoteEndPoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _remoteEndPoint = remoteEndPoint;
        }

        public async Task ConnectAsync(string host, int port)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UdpSocket));

            var ipAddress = IPAddress.Parse(host);
            _remoteEndPoint = new IPEndPoint(ipAddress, port);

            // UDP 实际上不需要连接，这里只是设置默认远程端点
            await Task.CompletedTask;
        }

        public async Task DisconnectAsync()
        {
            if (_isDisposed)
                return;

            try
            {
                _socket?.Close();
            }
            finally
            {
                _isDisposed = true;
            }
            await Task.CompletedTask;
        }
        public int Send(byte[] data)
        {
            return _socket.SendTo(data, _remoteEndPoint);
        }
        public async Task<int> SendAsync(byte[] data)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UdpSocket));
            if (_remoteEndPoint == null)
                throw new InvalidOperationException("Not connected to a remote endpoint");

            return await SendToAsync(data, _remoteEndPoint);
        }

        public async Task<int> SendToAsync(byte[] data, IPEndPoint remoteEndPoint)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UdpSocket));

            var tcs = new TaskCompletionSource<int>();

            _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, remoteEndPoint, asyncResult =>
            {
                try
                {
                    int bytesSent = _socket.EndSendTo(asyncResult);
                    tcs.SetResult(bytesSent);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return await tcs.Task;
        }

        public async Task<(byte[], int)> ReceiveAsync()
        {
            return await ReceiveAsync(Timeout.Infinite);
        }

        public async Task<(byte[], int)> ReceiveAsync(int timeout)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UdpSocket));

            var tcs = new TaskCompletionSource<(byte[], int)>();
            var buffer = new byte[65507]; // UDP 最大数据包大小
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            _socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                ref remoteEP, asyncResult =>
                {
                    try
                    {
                        int bytesRead = _socket.EndReceiveFrom(asyncResult, ref remoteEP);
                        tcs.SetResult((buffer, bytesRead));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, null);

            if (timeout != Timeout.Infinite)
            {
                var delayTask = Task.Delay(timeout);
                var completedTask = await Task.WhenAny(tcs.Task, delayTask);

                if (completedTask == delayTask)
                {
                    throw new TimeoutException("Receive operation timed out");
                }
            }

            return await tcs.Task;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _socket?.Close();
                _isDisposed = true;
            }
        }
    }
}
