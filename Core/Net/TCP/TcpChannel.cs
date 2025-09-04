//using Core.Net;
//using System.Net;
//using System.Net.Sockets;

//namespace Core.Net.Tcp
//{
//    public class TcpChannel : NetChannel
//    {
//        private Socket _socket;
//        private const int BufferSize = 8192;

//        public bool IsConnected => _socket?.Connected ?? false;

//        public TcpChannel(Socket socket)
//        {
//            _socket = socket;
//        }

//        public TcpChannel(string host, int port)
//        {
//            RemoteAddress = new IPEndPoint(IPAddress.Parse(host), port);
//        }

//        public override async Task ConnectAsync()
//        {
//            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//            await _socket.ConnectAsync(RemoteAddress);
//        }

//        public override async Task DisconnectAsync()
//        {
//            await _socket.DisconnectAsync(false);
//        }

//        public override void Send(Message message)
//        {
//            var data = MessageHandle.Write(message);
//            if (data != null && data.Length > 0)
//            {
//                _socket.Send(data);
//            }
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