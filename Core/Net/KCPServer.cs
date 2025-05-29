using KcpTransport;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
    public class KcpServer : IDisposable
    {
        private KcpListener _transport;
        private readonly CancellationTokenSource _cts = new();
        private readonly IPEndPoint _localEndPoint;

        // 事件：客户端连接/断开/接收消息
        public event Action<uint> OnClientConnected;
        public event Action<uint> OnClientDisconnected;
        public event Action<uint, ReadOnlyMemory<byte>> OnMessageReceived;

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

                    ConsumeClient(result);
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

        static async void ConsumeClient(KcpConnection connection)
        {
            using (connection)
            using (var stream = await connection.OpenOutboundStreamAsync())
            {
                try
                {
                    var buffer = new byte[1024];
                    while (true)
                    {
                        // Wait incoming data
                        var len = await stream.ReadAsync(buffer);

                        var str = Encoding.UTF8.GetString(buffer, 0, len);
                        Console.WriteLine("Server Request  Received: " + str);

                        // Send to Client(KCP, Reliable)
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(str));

                        // Send to Client(Unreliable)
                        //await stream.WriteUnreliableAsync(Encoding.UTF8.GetBytes(str));
                    }
                }
                catch (KcpDisconnectedException)
                {
                    // when client has been disconnected, ReadAsync will throw KcpDisconnectedException
                    Console.WriteLine($"Disconnected, Id:{connection.ConnectionId}");
                }
            }
        }

        public ValueTask SendAsync(uint connectionId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            return default;
        }

        public void DisconnectClient(uint connectionId)
        {
            //_transport.(connectionId);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _transport.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}