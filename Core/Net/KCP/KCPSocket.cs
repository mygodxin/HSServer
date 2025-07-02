using Core.Util;
using KcpTransport;
using MessagePack;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        /// <summary>
        /// use by server
        /// </summary>
        public KCPSocket(KcpConnection socket)
        {
            try
            {
                _connect = socket;
                _connect.OnRecive += ReceiveLoop;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                Dispose();
            }
        }

        /// <summary>
        /// use by client
        /// </summary>
        public KCPSocket(string host, int port)
        {
            try
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
                var task = KcpConnection.ConnectAsync(remoteEndPoint, default);
                task.AsTask().Wait();
                _connect = task.Result;
                _connect.OnRecive += Receive;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                Dispose();
            }
        }
        private void Receive(byte[] data)
        {
            OnDataReceived?.Invoke(data);
        }

        private void ReceiveLoop(byte[] bytes)
        {
            MessageHandle.Read(bytes, this);
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
