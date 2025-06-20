namespace Core
{
    /// <summary>
    /// 通用网络接口
    /// </summary>
    public class NetChannel
    {
        public string RemoteAddress { get; set; }
        protected CancellationTokenSource cancel;

        public virtual Task StartAsync()
        {
            return Task.CompletedTask;
        }
        public virtual Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }
        public virtual void Write(Message message)
        {

        }

        public virtual void WriteError(string error)
        {
            
        }
    }
}
