using Core.Util;
using KcpTransport;
using MessagePack;
using Proto.Remote;
using System;
using System.Collections.Concurrent;

namespace Core.Net.KCP
{
    public class KCPChannel : NetChannel
    {
        private KcpConnection _connection;
        private ConcurrentQueue<byte[]> _messages = new ConcurrentQueue<byte[]>();

        public KCPChannel(KcpConnection connection)
        {
            _connection = connection;
            _connection.OnRecive += OnRecive;
        }

        public override async Task DisconnectAsync()
        {
            _connection.Disconnect();
            await Task.CompletedTask;
        }

        public override void Write(Message message)
        {
            _messages.Enqueue(MessageHandle.Write(message));
        }

        public override async Task StartAsync()
        {
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    await Send();
                }
            }
            catch (KcpDisconnectedException)
            {
                Console.WriteLine($"Disconnected, Id:{_connection.ConnectionId}");
            }
        }

        private async Task Send()
        {
            if (_messages.TryDequeue(out var message))
            {
                var msg = HSerializer.Serialize(message);
                _connection.SendReliableBuffer(msg);
            }
        }

        private void OnRecive(byte[] bytes)
        {
            MessageHandle.Read(bytes, this);
        }
    }
}
