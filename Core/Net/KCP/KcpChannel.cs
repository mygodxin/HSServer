using KcpTransport;
using System.Net;

namespace Core.Net.KCP
{
    public class KcpChannel : NetChannel
    {
        private KcpConnection _connect;

        // use by server
        public KcpChannel(KcpConnection connect)
        {
            _connect = connect;
        }

        // use by client
        public KcpChannel(string host, int port)
        {
            RemoteAddress = new IPEndPoint(IPAddress.Parse(host), port);
        }

        public void SetListener(Action<byte[]> onMessage)
        {
            _connect.OnMessage += onMessage;
        }

        public override async Task ConnectAsync()
        {
            _connect = await KcpConnection.ConnectAsync(RemoteAddress);
        }

        public override void Send(Message message)
        {
            var data = MessageHandle.Write(message);
            if (data != null && data.Length > 0)
            {
                _connect.SendReliableBuffer(data);
            }
        }

        public override void SendError(string error)
        {
            var message = new MessageError();
            message.Error = error;
            var data = MessageHandle.Write(message);
            _connect.SendReliableBuffer(data);
        }

        public override Task DisconnectAsync()
        {
            _connect?.Disconnect();
            return Task.CompletedTask;
        }
    }
}
