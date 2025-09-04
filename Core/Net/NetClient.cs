using Core.Net;
using System.Net;

namespace Core
{
    /// <summary>
    /// 连接基类
    /// </summary>
    public class NetClient
    {
        public IPEndPoint RemoteAddress { get; set; }
        protected readonly int MAX_MESSAGE_LEN = 1400;
        public Action<byte[]> OnMessage;

        public virtual Task ConnectAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void Send(byte[] data)
        {
        }
    }
}
