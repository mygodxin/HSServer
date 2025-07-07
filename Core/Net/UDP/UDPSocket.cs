//using Luban;
//using System.Buffers;
//using System.Collections.Concurrent;
//using System.Net;
//using System.Net.Sockets;

//public class UdpClient
//{
//    private Socket _socket;
//    private IPEndPoint _remoteEndPoint;
//    private SocketAsyncEventArgs _receiveArgs;
//    private SocketAsyncEventArgs _sendArgs;
//    private ByteBuf _receiveBuffer;
//    private ByteBuf _sendBuffer;

//    public UdpClient(string address, int port)
//    {
//        _remoteEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
//        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//        _socket.Bind(new IPEndPoint(IPAddress.Any, 0));

//        _receiveArgs = new SocketAsyncEventArgs();
//        _receiveArgs.Completed += OnEventCompleted;
//        _sendArgs = new SocketAsyncEventArgs();
//        _sendArgs.Completed += OnEventCompleted;
//    }

//    public void Connect()
//    {
//        _socket.Connect(_remoteEndPoint);
//    }

//    private void OnEventCompleted(object sender, SocketAsyncEventArgs e)
//    {
//        switch (e.LastOperation)
//        {
//            case SocketAsyncOperation.ReceiveFrom:
//                ProcessReceiveFrom(e);
//                break;
//            case SocketAsyncOperation.SendTo:
//                ProcessSendTo(e);
//                break;
//            default:
//                throw new ArgumentException("The last operation completed on the socket was not a receive or send");
//        }
//    }

//    private void StartReceivers()
//    {
//        for (int i = 0; i < _workerThreads; i++)
//        {
//            var args = _argsPool.Rent();
//            args.RemoteEndPoint = _serverEp;
//            if (!_socket.ReceiveAsync(args))
//                ProcessReceive(args); // 同步完成立即处理
//        }
//    }

//    private void ProcessReceiveFrom(SocketAsyncEventArgs e)
//    {
//        if (!IsConnected)
//            return;

//        // Disconnect on error
//        if (e.SocketError != SocketError.Success)
//        {
//            Disconnect();
//            return;
//        }

//        // Received some data from the server
//        long size = e.BytesTransferred;

//        // Update statistic
//        DatagramsReceived++;
//        BytesReceived += size;

//        // Call the datagram received handler
//        OnReceived(e.RemoteEndPoint, _receiveBuffer.Data, 0, size);

//        // If the receive buffer is full increase its size
//        if (_receiveBuffer.Capacity == size)
//        {
//            // Check the receive buffer limit
//            if (((2 * size) > OptionReceiveBufferLimit) && (OptionReceiveBufferLimit > 0))
//            {
//                SendError(SocketError.NoBufferSpaceAvailable);
//                Disconnect();
//                return;
//            }

//            _receiveBuffer.Reserve(2 * size);
//        }
//    }

//    public void Send(byte[] data)
//    {
//    }

//    private void ProcessSendTo(SocketAsyncEventArgs e)
//    {
//    }

//    public void Disconnect() => _socket.Close();
//}