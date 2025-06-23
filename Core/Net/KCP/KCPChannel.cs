using Core.Util;
using KcpTransport;
using MessagePack;
using Proto.Remote;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace Core.Net.KCP
{
    public class KCPChannel : NetChannel
    {
        private KcpConnection _connection;
        private KcpStream _stream;
        private ConcurrentQueue<byte[]> _messages = new ConcurrentQueue<byte[]>();
        private byte[] _buffer = new byte[4096];

        public KCPChannel(KcpConnection connection)
        {
            _connection = connection;
        }

        public override async Task DisconnectAsync()
        {
            _connection.Disconnect();
            await Task.CompletedTask;
        }

        public override void Write(Message message)
        {
            _messages.Append(MessageHandle.Write(message));
        }

        public override async Task StartAsync()
        {
            _stream = await _connection.OpenOutboundStreamAsync();
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    await Send();
                    await Recive();
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
                await _stream.WriteAsync(msg);
            }
        }

        private async Task Recive()
        {
            var result = await _stream.ReadAsync(_buffer, cancel.Token);
            if (result != null)
            {
                MessageHandle.Read(_buffer, this);
            }
        }
    }
}
