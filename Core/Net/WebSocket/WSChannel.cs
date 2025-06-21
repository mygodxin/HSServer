using MessagePack;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace Core.Net.WS
{
    public class WSChannel : NetChannel
    {
        private WebSocket _connection;
        private ConcurrentQueue<byte[]> _messages = new ConcurrentQueue<byte[]>();
        private byte[] _buffer = new byte[4096];

        public WSChannel(WebSocket connection)
        {
            _connection = connection;
        }

        public override async Task DisconnectAsync()
        {
            _connection.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, default);
            await Task.CompletedTask;
        }

        public override void Write(Message message)
        {
            _messages.Append(MessageHandle.Write(message));
        }

        public override async Task StartAsync()
        {
            try
            {
                var buffer = new byte[4096];
                while (!cancel.IsCancellationRequested)
                {
                    Send();
                    Recive();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }

        private async void Send()
        {
            if (_messages.TryDequeue(out var message))
            {
                await _connection.SendAsync(message, WebSocketMessageType.Binary, true, cancel.Token);
            }
        }

        private async void Recive()
        {
            var result = await _connection.ReceiveAsync(_buffer, cancel.Token);
            if (result != null)
            {
                MessageHandle.Read(_buffer, this);
            }
        }
    }
}
