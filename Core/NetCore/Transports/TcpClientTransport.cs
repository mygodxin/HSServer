using System;
using System.Net.Sockets;

namespace Core.NetCore.Transports
{
    public class TcpClientTransport : ClientTransport
    {
        /// <summary>
        /// 监听客户端连接对象
        /// </summary>
        private TcpClient _client;

        /// <summary>
        /// 地址
        /// </summary>
        private string _address;
        /// <summary>
        /// 端口号
        /// </summary>
        private ushort _port;

        /// <summary>
        /// 最大接受数据字节长度
        /// </summary>
        private readonly int _acceptBufferMaxLength;
        /// <summary>
        /// 用于接受数据的字节数组
        /// </summary>
        private readonly byte[] _acceptBuffer;

        /// <summary>
        /// 收到消息后回调
        /// </summary>
        public Action<byte[], int, NetworkStream> OnDataReceived;

        public TcpClientTransport()
        {
            _client = new TcpClient();
        }

        public override void Connect(string address, ushort port)
        {
            _address = address;
            _port = port;
            _client.Connect(address, port);
            ListenerForServerAsync();
        }

        protected override void Disconnect()
        {
            base.Disconnect();
            _client.Close();
        }

        private async void ListenerForServerAsync()
        {
            try
            {
                var stream = _client.GetStream();
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(_acceptBuffer!, 0, _acceptBufferMaxLength);
                    if (read == 0) break;
                    OnDataReceived?.Invoke(_acceptBuffer, read, stream);
                }
            }
            catch (Exception ex)
            {
                Logger.Info($"{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _client.Close();
            }
        }

        public override async void Update()
        {
        }

        public override void Send(Message message)
        {
            base.Send(message);
        }
    }
}
