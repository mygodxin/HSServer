using Core.Protocol;
using kcp2k;
using System;
using System.Threading.Tasks;

namespace Core.NetCore.Transports
{
    public class KcpClientTransport : ClientTransport
    {
        private KcpClient _client;
        private int _port;
        /// <summary>
        /// KCP客户端配置
        /// </summary>
        private readonly KcpConfig _config;
        /// <summary>
        /// 需要发送的消息包队列
        /// </summary>
        private readonly RingBuffer<Message> _packetInfos;
        /// <summary>
        /// 连接成功时回调
        /// </summary>
        public Action OnConnected;
        /// <summary>
        /// 收到服务器数据时回调
        /// </summary>
        public Action<ArraySegment<byte>, KcpChannel> OnDataReceived;
        /// <summary>
        /// 断开连接时回调
        /// </summary>
        public Action OnDisconnected;
        /// <summary>
        /// 发生错误时回调
        /// </summary>
        public Action<ErrorCode, string> OnError;

        public KcpClientTransport(KcpConfig config)
        {
            _packetInfos = new RingBuffer<Message>(1024);
            _config = config;

            _client = new KcpClient(
                () => OnConnected?.Invoke(),
                (data, channel) => OnDataReceived?.Invoke(data, channel),
                () => OnDisconnected?.Invoke(),
                (errorCode, error) => OnError?.Invoke(errorCode, error),
                this._config
                );
        }

        public override bool IsConnected() => _client.connected;

        public override void Connect(string address, ushort port)
        {
            _client.Connect(address, port);
        }

        public override void Update()
        {
            Task.Run(async () =>
            {
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    UpdatePacketInfosSent();
                    _client.Tick();
                    await Task.Delay(TimeSpan.FromMilliseconds(_config.Interval), TokenSource.Token);
                }
            }, TokenSource.Token);
        }

        /// <summary>
        /// 处理消息发送队列
        /// </summary>
        private void UpdatePacketInfosSent()
        {
            if (!_packetInfos.TryDequeue(out var message))
                return;
            try
            {
                var buffer = MessageHandle.Write(message);
                _client.Send(new ArraySegment<byte>(buffer), KcpChannel.Unreliable);
                Logger.Info($"KcpSend -> MsgID:{message.GetType().Name}");
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
                Disconnect();
            }
            finally
            {

            }
        }

        protected override void Disconnect()
        {
            base.Disconnect();
            _client.Disconnect();
        }

        public override void Send(Message message)
        {
            try
            {
                _packetInfos.Enqueue(message);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
