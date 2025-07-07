//using Core.Net;
//using System.Net.Sockets;

//namespace Core.Net.Tcp
//{
//    public class TCPSocket : ISocket
//    {
//        private Socket _socket;
//        private const int BufferSize = 8192;

//        public bool IsConnected => _socket?.Connected ?? false;

//        public TCPSocket(Socket socket)
//        {
//            _socket = socket;
//        }

//        public TCPSocket(string host, int port)
//        {
//            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//        }

//        public async Task ConnectAsync(string host, int port)
//        {
//            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//            await _socket.ConnectAsync(host, port);
//        }

//        public async Task DisconnectAsync()
//        {
//            if (_socket != null && _socket.Connected)
//            {
//                await Task.Run(() => _socket.Shutdown(SocketShutdown.Both));
//                _socket.Close();
//            }
//        }

//        public async Task<int> SendAsync(byte[] data)
//        {
//            if (!IsConnected)
//                throw new InvalidOperationException("Socket is not connected");

//            return await _socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
//        }

//        public async Task<byte[]> ReceiveAsync()
//        {
//            return await ReceiveAsync(Timeout.Infinite);
//        }

//        public async Task<byte[]> ReceiveAsync(int timeout)
//        {
//            if (!IsConnected)
//                throw new InvalidOperationException("Socket is not connected");

//            var buffer = new byte[BufferSize];
//            var cts = new CancellationTokenSource();
//            if (timeout != Timeout.Infinite)
//                cts.CancelAfter(timeout);

//            var receiveTask = _socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
//            var completedTask = await Task.WhenAny(receiveTask, Task.Delay(timeout, cts.Token));

//            if (completedTask == receiveTask)
//            {
//                int bytesRead = await receiveTask;
//                if (bytesRead > 0)
//                {
//                    var result = new byte[bytesRead];
//                    Array.Copy(buffer, result, bytesRead);
//                    return result;
//                }
//                return Array.Empty<byte>(); // Connection closed
//            }
//            throw new TimeoutException("Receive operation timed out");
//        }
//    }
//}