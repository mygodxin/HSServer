using Core;
using Core.Net;
using System;
using System.IO;
using System.Net.Sockets;

namespace Hotfix
{
    /// <summary>
    /// 网络管理器
    /// </summary>
    public class NetworkManager : Singleton<NetworkManager>
    {
        /// <summary>
        /// KCP服务器对象
        /// </summary>
        private KcpServerTransport _kcpServerTransport;
        /// <summary>
        /// TCP服务器对象
        /// </summary>
        private TcpServerTransport _tcpServerTransport;
        /// <summary>
        /// TCP服务器以及KCP服务器的数据流对象
        /// </summary>
        private MemoryStream _memoryStream;

        /// <summary>
        /// KCP服务器是否处于存活状态
        /// </summary>
        public bool KcpActive => _kcpServerTransport.Active();
        /// <summary>
        /// TCP服务器是否处于存活状态
        /// </summary>
        public bool TcpActive => _tcpServerTransport.Active();

        /// <summary>
        /// KCP服务器的地址信息
        /// </summary>
        public Uri KcpUri => _kcpServerTransport.Uri();
        /// <summary>
        /// TCP服务器的地址信息
        /// </summary>
        public Uri TcpUri => _tcpServerTransport.Uri();

        /// <summary>
        /// 服务器初始化
        /// </summary>
        public void Init(params object[] objs)
        {
            _memoryStream = new MemoryStream();
            _kcpServerTransport = new KcpServerTransport(KcpUtil.DefaultConfig, NetSetting.KcpPort)
            {
                OnConnected = OnKcpConnected,
                OnDataReceived = OnKcpDataReceived,
                OnDisconnected = OnKcpDisconnected,
                OnError = OnKcpError
            };
            _tcpServerTransport = new TcpServerTransport(NetSetting.NetAddress, NetSetting.TcpPort)
            {
                OnDataReceived = OnTcpDataReceived
            };
        }

        /// <summary>
        /// 客户端KCP连接回调
        /// </summary>
        /// <param name="connectionId">客户端KCP连接ID</param>
        private void OnKcpConnected(int connectionId)
        {
            Logger.Info($"OnKcpConnected connectionId: {connectionId}");
        }

        /// <summary>
        /// 收到客户端KCP消息时的回调处理函数
        /// </summary>
        /// <param name="connectionId">客户端KCP连接ID</param>
        /// <param name="data">字节数据数组</param>
        /// <param name="channel">KCP消息类型</param>
        private void OnKcpDataReceived(int connectionId, ArraySegment<byte> data, kcp2k.KcpChannel channel)
        {
            if (!KcpActive)
            {
                Logger.Info($"Kcp Not Active!");
                return;
            }
            Logger.Info($"OnKcpDataReceived connectionId: {connectionId} data.len: {data.Count} channel: {channel}");
            if (data.Array == null)
            {
                Logger.Info($"OnKcpDataReceived data.Array == null");
                return;
            }
        }

        /// <summary>
        /// KCP服务器发送客户端断开连接时回调
        /// </summary>
        /// <param name="connectionId">客户端KCP连接ID</param>
        private void OnKcpDisconnected(int connectionId)
        {
            Logger.Info($"OnKcpDisconnected connectionId: {connectionId}");
        }

        /// <summary>
        /// KCP服务器发送错误时回调
        /// </summary>
        /// <param name="connectionId">客户端KCP连接ID</param>
        /// <param name="errorCode">错误码</param>
        /// <param name="error">错误原因</param>
        private void OnKcpError(int connectionId, kcp2k.ErrorCode errorCode, string error)
        {
            Logger.Info($"OnKcpError connectionId: {connectionId} errorCode: {errorCode} error: {error}");
        }

        /// <summary>
        /// 开启KCP服务器
        /// </summary>
        public void KcpStart()
        {
            _kcpServerTransport.Start();
        }

        /// <summary>
        /// KCP服务器轮询
        /// </summary>
        public void KcpUpdate()
        {
            _kcpServerTransport.Update();
        }

        /// <summary>
        /// 关闭KCP服务器
        /// </summary>
        public void KcpShutdown()
        {
            _kcpServerTransport.Shutdown();
        }

        /// <summary>
        /// 收到客户端TCP消息时的回调处理函数
        /// </summary>
        /// <param name="data">字节数据数组</param>
        /// <param name="read">已读</param>
        /// <param name="stream"></param>
        private void OnTcpDataReceived(byte[] data, int read, NetworkStream stream)
        {
            if (!TcpActive)
            {
                Logger.Info($"Tcp Not Active!");
                return;
            }
        }

        /// <summary>
        /// 开启TCP服务器
        /// </summary>
        public void TcpStart()
        {
            _tcpServerTransport.Start();
        }

        /// <summary>
        /// 关闭TCP服务器
        /// </summary>
        public void TcpShutdown()
        {
            _tcpServerTransport.Shutdown();
        }
    }
}