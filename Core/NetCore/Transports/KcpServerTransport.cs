using Core.Protocol;
using kcp2k;
using System;
using System.Threading.Tasks;

namespace Core.Net
{

    /// <summary>
    /// KCP消息包
    /// </summary>
    public struct PacketInfo
    {
        /// <summary>
        /// 客户端KCP连接ID
        /// </summary>
        public int ConnectionId;

        /// <summary>
        /// 消息包
        /// </summary>
        public Message Message;
    }

    /// <summary>
    /// KCP服务器
    /// </summary>
    public class KcpServerTransport : ServerTransport
    {
        /// <summary>
        /// KCP服务器对象
        /// </summary>
        private readonly KcpServer _server;
        /// <summary>
        /// 端口号
        /// </summary>
        private readonly ushort _port;
        /// <summary>
        /// KCP服务器配置
        /// </summary>
        private readonly KcpConfig _config;
        /// <summary>
        /// 需要发送的消息包队列
        /// </summary>
        private readonly RingBuffer<PacketInfo> _packetInfos;
        /// <summary>
        /// 已连接的客户端数量
        /// </summary>
        public int ConnectionCount => _server.connections.Count;
        /// <summary>
        /// 有玩家连接时回调
        /// </summary>
        public Action<int> OnConnected;
        /// <summary>
        /// 收到玩家数据时回调
        /// </summary>
        public Action<int, ArraySegment<byte>, KcpChannel> OnDataReceived;
        /// <summary>
        /// 有玩家断开连接时回调
        /// </summary>
        public Action<int> OnDisconnected;
        /// <summary>
        /// 服务器发生错误时回调
        /// </summary>
        public Action<int, ErrorCode, string> OnError;

        public KcpServerTransport(KcpConfig config, ushort port)
        {
            _packetInfos = new RingBuffer<PacketInfo>(1024);

            this._port = port;
            this._config = config;
            _server = new KcpServer(
                (connectionId) => OnConnected?.Invoke(connectionId),
                (connectionId, data, channel) => OnDataReceived?.Invoke(connectionId, data, channel),
                (connectionId) => OnDisconnected?.Invoke(connectionId),
                (connectionId, errorCode, error) => OnError?.Invoke(connectionId, errorCode, error),
                this._config
            );
        }

        /// <summary>
        /// KCP服务器地址信息
        /// </summary>
        /// <returns></returns>
        public override Uri Uri()
        {
            var builder = new UriBuilder();
            builder.Scheme = nameof(KcpServerTransport);
            builder.Host = System.Net.Dns.GetHostName();
            builder.Port = _port;
            return builder.Uri;
        }

        /// <summary>
        /// KCP服务器是否处于存活状态
        /// </summary>
        /// <returns></returns>
        public override bool Active() => _server.IsActive();

        /// <summary>
        /// 开启KCP服务器
        /// </summary>
        public override void Start() => _server.Start(_port);

        /// <summary>
        /// 断开服务器
        /// </summary>
        protected override void Disconnect()
        {
            base.Disconnect();
            _server.Stop();
        }

        /// <summary>
        /// KCP服务器轮询
        /// </summary>
        public override void Update()
        {
            Task.Run(async () =>
            {
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    UpdatePacketInfosSent();
                    _server.Tick();
                    await Task.Delay(TimeSpan.FromMilliseconds(_config.Interval), TokenSource.Token);
                }
            }, TokenSource.Token);
        }

        /// <summary>
        /// 处理消息发送队列
        /// </summary>
        private void UpdatePacketInfosSent()
        {
            if (!_packetInfos.TryDequeue(out var packetInfo))
                return;
            try
            {
                var buffer = MessageHandle.Write(packetInfo.Message);
                _server.Send(packetInfo.ConnectionId, new ArraySegment<byte>(buffer), KcpChannel.Unreliable);
                Logger.Info($"KcpSend -> connectionId:{packetInfo.ConnectionId} MsgID:{packetInfo.Message.GetType().Name}");
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
                Shutdown();
            }
            finally
            {

            }
        }

        /// <summary>
        /// 发送消息包
        /// </summary>
        /// <param name="message">消息包</param>
        /// <param name="param">额外参数</param>
        public override void Send(Message message, object param)
        {
            try
            {
                _packetInfos.Enqueue(new PacketInfo() { ConnectionId = (int)param, Message = message });
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 发送消息对象
        /// </summary>
        /// <param name="battleMsgId">消息ID</param>
        /// <param name="message">消息对象</param>
        /// <param name="connectionId">客户端KCP连接ID</param>
        /// <typeparam name="T">消息类型</typeparam>
        public void SendMessage<T>(Message message, int connectionId) where T : Message
        {
            if (!Active()) return;
            Send(message, connectionId);
        }

        /// <summary>
        /// 断开某一个客户端的KCP连接
        /// </summary>
        /// <param name="connectionId">客户端KCP连接ID</param>
        public override void Disconnect(int connectionId) => _server.Disconnect(connectionId);

        /// <summary>
        /// 获取某一个客户端的地址信息
        /// </summary>
        /// <param name="connectionId">客户端KCP连接ID</param>
        /// <returns>地址信息</returns>
        public override string GetClientAddress(int connectionId)
        {
            var endPoint = _server.GetClientEndPoint(connectionId);
            return endPoint.Address.IsIPv4MappedToIPv6 ? endPoint.Address.MapToIPv4().ToString() : endPoint.Address.ToString();
        }

        /// <summary>
        /// 关闭KCP服务器
        /// </summary>
        public override void Shutdown() => Disconnect();
    }
}