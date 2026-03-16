using Core.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Core.Net
{
    /// <summary>
    /// TCP服务器
    /// </summary>
    public class TcpServerTransport : ServerTransport
    {
        /// <summary>
        /// 监听客户端连接对象
        /// </summary>
        private TcpListener _tcpListener;

        /// <summary>
        /// 地址
        /// </summary>
        private readonly string _address;
        /// <summary>
        /// 端口号
        /// </summary>
        private readonly ushort _port;
        /// <summary>
        /// 处理客户端连接后回调的线程集合
        /// </summary>
        private readonly List<Task> _handleClientTasks;
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

        public TcpServerTransport(string address, ushort port, int acceptBufferMaxLength = 1024)
        {
            this._address = address;
            this._port = port;
            this._acceptBufferMaxLength = acceptBufferMaxLength;
            this._acceptBuffer = new byte[this._acceptBufferMaxLength];

            _handleClientTasks = new List<Task>();
        }

        /// <summary>
        /// TCP服务器的地址信息
        /// </summary>
        /// <returns></returns>
        public override Uri Uri()
        {
            var builder = new UriBuilder();
            builder.Scheme = nameof(TcpServerTransport);
            builder.Host = System.Net.Dns.GetHostName();
            builder.Port = _port;
            return builder.Uri;
        }

        /// <summary>
        /// TCP服务器是否处于存活状态
        /// </summary>
        /// <returns></returns>
        public override bool Active() => _tcpListener.Server.IsBound;

        /// <summary>
        /// 开启服务器
        /// </summary>
        public override void Start()
        {
            var ipAddress = IPAddress.Parse(_address);
            _tcpListener = new TcpListener(ipAddress, _port);
            ListenForClientsAsync();
        }

        /// <summary>
        /// 断开服务器
        /// </summary>
        protected override void Disconnect()
        {
            base.Disconnect();
            _tcpListener.Stop();
        }

        /// <summary>
        /// 监听客户端连接
        /// </summary>
        private async void ListenForClientsAsync()
        {
            try
            {
                _tcpListener.Start();
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    Logger.Info($"AcceptTcpClientAsync -> RemoteEndPoint: {client.Client.RemoteEndPoint}");
                    _handleClientTasks.Add(Task.Run(() => HandleClientAsync(client)));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                if (_handleClientTasks.Count > 0)
                {
                    foreach (var task in _handleClientTasks) task.Dispose();
                    _handleClientTasks.Clear();
                }
                Disconnect();
            }
        }

        /// <summary>
        /// 处理客户端连接
        /// </summary>
        /// <param name="client"></param>
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                while (true)
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
                client.Close();
            }
        }

        /// <summary>
        /// 发送消息包
        /// </summary>
        /// <param name="packet">消息包</param>
        /// <param name="param">额外参数</param>
        public override void Send(Message packet, object param)
        {
            SendAsync(packet, (NetworkStream)param);
        }

        /// <summary>
        /// 异步发送消息包
        /// </summary>
        /// <param name="packet">消息包</param>
        /// <param name="stream">客户端连接流</param>
        private async void SendAsync(Message packet, NetworkStream stream)
        {
            try
            {
                var buffer = MessageHandle.Write(packet);
                await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));
                Logger.Info($"TcpSend -> MsgID:{packet.GetType().Name} dataSize:{buffer.Length}");
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
            }
        }

        /// <summary>
        /// 发送消息对象
        /// </summary>
        /// <param name="logicMsgId">消息ID</param>
        /// <param name="message">消息体</param>
        /// <param name="stream">客户端连接流</param>
        /// <typeparam name="T">消息类型</typeparam>
        public void SendMessage<T>(T message, NetworkStream stream) where T : Message
        {
            if (!Active()) return;
            Send(message, stream);
        }

        /// <summary>
        /// 断开TCP服务器
        /// </summary>
        public override void Shutdown() => Disconnect();
    }
}