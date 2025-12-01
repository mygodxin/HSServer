namespace Hotfix.Login
{
    using Core;
    using Core.Protocol;
    using Proto;
    using Share;
    // ClientActor.cs
    using System.Net.Sockets;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class ClientActor : NetClient, IActor
    {
        private readonly TcpClient _client;
        private readonly PID _sessionManager;
        private readonly string _connectionId;
        private NetworkStream _networkStream;
        private string _currentToken;

        public ClientActor(TcpClient client, PID sessionManager, string connectionId)
        {
            _client = client;
            _sessionManager = sessionManager;
            _connectionId = connectionId;
            _networkStream = client.GetStream();
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case MessageReceived messageReceived:
                    await HandleMessageReceived(context, messageReceived.Data);
                    break;

                case SendMessage sendMessage:
                    Send(sendMessage.Data);
                    break;
            }
        }

        private async Task HandleMessageReceived(IContext context, byte[] data)
        {
            try
            {
                var message = MessageHandle.Read(data, out var msgID);
                switch (message)
                {
                    case LoginRequest loginRequest:
                        loginRequest.ConnectionId = _connectionId;
                        context.Send(_sessionManager, loginRequest);
                        break;
                    default:
                        var handle = HandleManager.Instance.GetMessageHandle(msgID);
                        if (handle != null)
                        {
                            handle.Channel = this;
                            handle.Message = message;
                            handle.Excute();
                        }
                        else
                        {
                            Logger.Error("recive error msg");
                        }
                        break;
                }
            }
            catch (JsonException)
            {
            }
        }

        public override async void Send(IMessage data)
        {
            if (_client.Connected)
            {
                var bytes = MessageHandle.Write(data);
                await _networkStream.WriteAsync(bytes, 0, bytes.Length);
                await _networkStream.FlushAsync();
            }
        }
    }
}
