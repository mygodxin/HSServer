using System.Net;

namespace Core
{
    /// <summary>
    /// 通用网络接口
    /// </summary>
    public class NetChannel
    {
        public IPEndPoint RemoteAddress { get; set; }
        protected readonly int MAX_MESSAGE_LEN = 1400;

        public virtual Task ConnectAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task SendAsync(Message message)
        {
            return Task.CompletedTask;
        }

        public virtual Task SendErrorAsync(string error)
        {
            return Task.CompletedTask;
        }
    }
}
