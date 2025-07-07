using Core.Util;
using KcpTransport;
using MessagePack;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Net;

namespace Core.Net.KCP
{
    public class KCPSocket : NetChannel
    {
        private KcpConnection _connect;
        private bool _isRunning;
        private readonly ConcurrentQueue<byte[]> _receiveQueue = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<byte[]> _sendQueue = new ConcurrentQueue<byte[]>();

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<byte[]> OnDataReceived;
        public event Action<Exception> OnError;

        public KCPSocket(KcpConnection connect)
        {
            _connect = connect;
        }
        public KCPSocket()
        {
        }

        public void AddLister()
        {
            _connect.OnReceiveData += OnDataReceived;
        }

        public async ValueTask<KcpConnection> ConnectAsync(string host, int port)
        {
            _connect = await KcpConnection.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port));
            return _connect;
        }

        public override void Write(Message message)
        {
            var data = MessageHandle.Write(message);
            if (data != null && data.Length > 0)
            {
                _connect.SendReliableBuffer(data);
            }
        }

        public void Dispose()
        {
            _isRunning = false;

            _sendQueue?.Clear();
            _connect?.Dispose();
        }
    }
}
