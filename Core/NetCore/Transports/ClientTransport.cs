using System.Threading;

namespace Core.NetCore.Transports
{
    public class ClientTransport
    {
        /// <summary>
        /// 取消Token
        /// </summary>
        protected readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        /// <summary>
        /// 客户端是否处于存活状态
        /// </summary>
        /// <returns></returns>
        public virtual bool IsConnected() { return false; }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public virtual void Connect(string address, ushort port) { }

        /// <summary>
        /// 客户端轮询
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// 发送消息包
        /// </summary>
        /// <param name="message">消息包</param>
        /// <param name="param">额外参数</param>
        public virtual void Send(Message message) { }

        /// <summary>
        /// 断开服务器
        /// </summary>
        protected virtual void Disconnect()
        {
            TokenSource.Cancel();
            TokenSource.Dispose();
        }
    }
}
