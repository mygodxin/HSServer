using KcpTransport;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Net.KCP
{
    public class KcpServer : IDisposable
    {
        private KcpListener _transport;
        private readonly CancellationTokenSource _cts = new();
        private readonly IPEndPoint _localEndPoint;

        public KcpServer(IPEndPoint localEndPoint)
        {
            _localEndPoint = localEndPoint;
        }

        public async Task StartAsync()
        {
            _transport = await KcpListener.ListenAsync(_localEndPoint, _cts.Token);

            // 启动接收循环
            _ = Task.Run(ReceiveLoopAsync);
        }

        private async Task ReceiveLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var result = await _transport.AcceptConnectionAsync(_cts.Token);
                    if (result.ConnectionId == 0) continue; // 无效连接

                    var conn = new KCPChannel(result, OnMessage);
                    await conn.StartAsync();
                    await conn.DisconnectAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Receive error: {ex}");
                }
            }
        }

        public void OnMessage(NetChannel channel, Message message)
        {
            var handle = HotfixManager.Instance.GetMessageHandle(message.ID);
            handle.Channel = channel;
            handle.Message = message;
            handle.Excute();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _transport.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}