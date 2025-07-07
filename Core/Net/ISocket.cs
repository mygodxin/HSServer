using System;
using System.Net;


namespace Core.Net
{
    public interface IClient
    {
        void OnConnected();
        void OnDisconnected();
        void OnReceived(byte[] data);
    }
}
