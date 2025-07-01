using System;
using System.Net;


namespace Core.Net
{
    public interface ISocket
    {
        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();
        Task<int> SendAsync(byte[] data);
        //Task<(byte[], int)> ReceiveAsync();
        //Task<(byte[], int)> ReceiveAsync(int timeout);
        bool IsConnected { get; }
    }
}
