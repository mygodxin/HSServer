using System.Net;

namespace KcpTransport
{

    internal static class SocketAddressExtensions
    {
        internal static IPEndPoint ToIPEndPoint(this SocketAddress socketAddress)
        {
            IPEndPoint endpoint = new IPEndPoint(0, 0);
            return (IPEndPoint)endpoint.Create(socketAddress);
        }

        internal static SocketAddress Clone(this SocketAddress socketAddress)
        {
            return socketAddress.Clone();
        }
    }
}
