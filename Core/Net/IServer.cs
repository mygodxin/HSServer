using System;
using System.Net;

namespace Core.Net
{
    public interface ISocketServer
    {
        bool IsRunning { get; }
        IPEndPoint ServerEndPoint { get; }
        Task StartAsync(string host, int port);
        Task StopAsync();
    }
}
