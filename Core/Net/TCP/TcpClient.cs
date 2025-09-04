using System.Net;
using System.Net.Sockets;

namespace Core.Net.Tcp
{
    public sealed class TcpClient : NetClient
    {
        private Socket _socket;
        private bool _isDisposed;
        private Task? receiveEventLoopTask; // only used for client

        public IPEndPoint RemoteEndPoint;
        public int MAX_DATA_LEN = 1024;

        // use by server
        public TcpClient(Socket socket)
        {
            _socket = socket;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _socket.NoDelay = true;
        }

        // use by client
        public TcpClient(IPEndPoint remoteEndPoint)
        {
            RemoteEndPoint = remoteEndPoint;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = true;
        }

        public override async Task ConnectAsync()
        {
            await _socket.ConnectAsync(RemoteEndPoint);
            receiveEventLoopTask = StartSocketEventLoopAsync();
        }
        async Task StartSocketEventLoopAsync()
        {
            var socketBuffer = new byte[MAX_DATA_LEN];

            while (true)
            {
                if (_isDisposed) return;

                var receiveLen = await _socket.ReceiveAsync(socketBuffer, SocketFlags.None);
                if (receiveLen > 0)
                {
                    OnMessage?.Invoke(socketBuffer.AsSpan(0, receiveLen).ToArray());
                }
            }
        }

        public override void Send(byte[] data)
        {
            _socket.Send(data);
        }

        public override async Task DisconnectAsync()
        {
            await _socket.DisconnectAsync(false);
        }
    }
}