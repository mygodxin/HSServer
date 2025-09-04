//using Core;
//using Luban;
//using Microsoft.AspNetCore.DataProtection;
//using Microsoft.Extensions.Options;
//using System.Buffers;
//using System.Collections.Concurrent;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;

//namespace Core.Net.UDP
//{

//    public class UdpChannel : NetChannel
//    {
//        private Socket _socket;
//        public MsgChannel<byte[]> ReceiveChannel = new MsgChannel<byte[]>();
//        public MsgChannel<byte[]> _sendQueue = new MsgChannel<byte[]>();

//        public UdpChannel(Socket socket)
//        {
//            _socket = socket;
//        }

//        public UdpChannel(string address, int port)
//        {
//            RemoteAddress = new IPEndPoint(IPAddress.Parse(address), port);
//            StartReceiveData();
//        }

//        public override async Task ConnectAsync()
//        {
//            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//            await _socket.ConnectAsync(RemoteAddress);
//        }

//        public override async Task DisconnectAsync()
//        {
//            await _socket.DisconnectAsync(true);
//        }


//        public override async Task SendAsync(Message message)
//        {
//            var data = MessageHandle.Write(message);
//            if (data != null && data.Length > 0)
//            {
//                await _socket.SendAsync(data);
//            }
//        }

//        private async void StartReceiveData()
//        {
//            var socketBuffer = new byte[MAX_MESSAGE_LEN];
//            while (true)
//            {
//                var received = await _socket.ReceiveAsync(socketBuffer, SocketFlags.None);
//                if (received > 0)
//                    ReceiveChannel.Write(socketBuffer.AsSpan(0, received).ToArray());
//            }
//        }
//    }
//}