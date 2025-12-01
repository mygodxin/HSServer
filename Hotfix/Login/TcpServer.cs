using System;

namespace Hotfix.Login
{
    using Proto;
    // TcpServer.cs
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;


    // 连接管理消息
    public partial class ClientConnected
    {
        public string ConnectionId { get; set; }
        public PID ClientActor { get; set; }
    }

    public partial class ClientDisconnected
    {
        public string ConnectionId { get; set; }
    }

    public class TcpServer
    {
        private readonly ActorSystem _actorSystem;
        private TcpListener _listener;
        private PID _sessionManager;

        public TcpServer(ActorSystem actorSystem)
        {
            _actorSystem = actorSystem;
            _sessionManager = _actorSystem.Root.Spawn(
                Props.FromProducer(() => new SessionManagerActor())
            );
        }

        public async Task StartAsync(string ip = "127.0.0.1", int port = 8080)
        {
            _listener = new TcpListener(IPAddress.Parse(ip), port);
            _listener.Start();

            Console.WriteLine($"TCP Server started on {ip}:{port}");

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var connectionId = Guid.NewGuid().ToString();
            var networkStream = client.GetStream();
            var buffer = new byte[1024];

            // 创建客户端Actor
            var clientActorProps = Props.FromProducer(() => new ClientActor(client, _sessionManager, connectionId));
            var clientActor = _actorSystem.Root.Spawn(clientActorProps);

            // 通知会话管理器有新连接
            _actorSystem.Root.Send(_sessionManager, new ClientConnected
            {
                ConnectionId = connectionId,
                ClientActor = clientActor
            });

            try
            {
                while (client.Connected)
                {
                    var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var messageData = buffer.AsSpan(0, bytesRead).ToArray();
                    Console.WriteLine($"Received from {connectionId}: {messageData}");

                    // 将消息发送给客户端Actor处理
                    _actorSystem.Root.Send(clientActor, new MessageReceived { Data = messageData });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client {connectionId}: {ex.Message}");
            }
            finally
            {
                // 通知会话管理器连接断开
                _actorSystem.Root.Send(_sessionManager, new ClientDisconnected
                {
                    ConnectionId = connectionId
                });

                client.Close();
                _actorSystem.Root.Stop(clientActor);
                Console.WriteLine($"Client {connectionId} disconnected");
            }
        }
    }

    // 接收消息的辅助类
    public class MessageReceived
    {
        public byte[] Data { get; set; }
    }
}
