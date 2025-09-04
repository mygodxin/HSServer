using System;
using System.Net;


namespace Core.Net
{
    public interface ISocketClient
    {
        void OnConnected();
        void OnDisconnected();
        void OnReceived(byte[] data);
    }
}
