using MessagePack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;

namespace Core.Net.WS
{
    public class WSChannel : NetChannel
    {
        private WebSocket _connection;
        private ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();
        private Action<NetChannel, Message> _onMessage;

        public WSChannel(WebSocket connection, Action<NetChannel, Message> onMessage)
        {
            _connection = connection;
            _onMessage = onMessage;
        }

        public override async Task DisconnectAsync()
        {
            _connection.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, default);
            await Task.CompletedTask;
        }

        public override void Write(Message message)
        {
            _messages.Append(message);
        }

        public override async Task StartAsync()
        {
            Send();
            Recive();
        }

        private async void Send()
        {
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    if (_messages.TryDequeue(out var message))
                    {
                        var msg = MessagePackSerializer.Serialize(message);
                        await _connection.SendAsync(msg, WebSocketMessageType.Binary, true, cancel.Token);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Disconnected, Id:{_connection.State}");
            }
        }

        private async void Recive()
        {
            try
            {
                var buffer = new byte[1024];
                while (!cancel.IsCancellationRequested)
                {
                    // Wait incoming data
                    var len = await _connection.ReceiveAsync(buffer, cancel.Token);

                    var msg = MessagePackSerializer.Deserialize<Message>(buffer);

                    _onMessage?.Invoke(this, msg);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Disconnected, Id:{_connection.State}");
            }
        }
    }
}
