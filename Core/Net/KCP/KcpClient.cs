using Core.Protocol;
using KcpTransport;
using System;
using System.Threading.Tasks;

namespace Core.Net.KCP
{
    public class KcpClient : NetClient
    {
        private KcpConnection _connect;
        private string _host;
        private int _port;

        // use by server
        public KcpClient(KcpConnection connect)
        {
            _connect = connect;
        }

        // use by client
        public KcpClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public Action<byte[]> OnReceived
        {
            set
            {
                _connect.OnReceived += value;
            }
            get
            {
                return _connect.OnReceived;
            }
        }

        public async Task ConnectAsync()
        {
            _connect = await KcpConnection.ConnectAsync(_host, _port);
        }

        public override void Send(IMessage message)
        {
            _connect.SendReliableBuffer(MessageHandle.Write(message));
        }

        public Task DisconnectAsync()
        {
            _connect?.Disconnect();
            return Task.CompletedTask;
        }
    }
}
