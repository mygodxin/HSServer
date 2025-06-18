using KcpTransport;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace Core
{
    public class KCPChannel : NetChannel
    {
        private KcpConnection _connection;
        private KcpStream _stream;
        private ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();
        private Action<NetChannel, Message> _onMessage;

        public KCPChannel(KcpConnection connection, Action<NetChannel, Message> onMessage)
        {
            _connection = connection;
            _onMessage = onMessage;
        }

        public override async Task DisconnectAsync()
        {
            _connection.Disconnect();
            await Task.CompletedTask;
        }

        public override void Write(Message message)
        {
            _messages.Append(message);
        }

        public override async Task StartAsync()
        {
            _stream = await _connection.OpenOutboundStreamAsync();
            Send();
            Recive();
        }

        private async void Send()
        {
            var stream = _stream;
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    if (_messages.TryDequeue(out var message))
                    {
                        var msg = MessagePackSerializer.Serialize(message);
                        await stream.WriteAsync(msg);
                    }
                }
            }
            catch (KcpDisconnectedException)
            {
                Console.WriteLine($"Disconnected, Id:{_connection.ConnectionId}");
            }
        }

        private async void Recive()
        {
            var stream = _stream;
            try
            {
                var buffer = new byte[1024];
                while (!cancel.IsCancellationRequested)
                {
                    // Wait incoming data
                    var len = await stream.ReadAsync(buffer, cancel.Token);

                    var msg = MessagePackSerializer.Deserialize<Message>(buffer);

                    _onMessage?.Invoke(this, msg);
                }
            }
            catch (KcpDisconnectedException)
            {
                Console.WriteLine($"Disconnected, Id:{_connection.ConnectionId}");
            }
        }
    }
}
