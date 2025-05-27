namespace Core
{
    /// <summary>
    /// 通用网络接口
    /// </summary>
    public interface ISocket
    {
        public event Action<Guid> OnClientConnected;
        public event Action<Guid> OnClientDisconnected;
        public event Action<Guid, string> OnMessageReceived;

        Task StartAsync();
        Task DisconnectAsync(Guid clientId);
        Task SendAsync(Guid clientId, string message);
    }
}
