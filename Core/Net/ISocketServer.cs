using System.Net;
using System.Threading.Tasks;

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
