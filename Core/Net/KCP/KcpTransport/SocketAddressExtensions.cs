using System.Net;
using System.Net.Sockets;

namespace KcpTransport
{

    internal static class SocketAddressExtensions
    {
        // 将 IPEndPoint 转换为 SocketAddress
        public static SocketAddress ToSocketAddress(this IPEndPoint endPoint)
        {
            return endPoint.Serialize();
        }

        // 将 SocketAddress 转换回 IPEndPoint
        public static IPEndPoint ToIPEndPoint(this SocketAddress socketAddress)
        {
            if (socketAddress == null)
                throw new ArgumentNullException(nameof(socketAddress));

            if (socketAddress.Family == AddressFamily.InterNetwork) // IPv4
            {
                if (socketAddress.Size < 16) // IPv4 SocketAddress 的最小大小
                    throw new ArgumentException("Invalid IPv4 SocketAddress size.");

                // 读取端口号 (大端序)
                int port = (socketAddress[2] << 8) | socketAddress[3];

                // 读取IP地址 (4字节)
                byte[] addressBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    addressBytes[i] = socketAddress[4 + i];
                }

                return new IPEndPoint(new IPAddress(addressBytes), port);
            }
            else if (socketAddress.Family == AddressFamily.InterNetworkV6) // IPv6
            {
                if (socketAddress.Size < 28) // IPv6 SocketAddress 的最小大小
                    throw new ArgumentException("Invalid IPv6 SocketAddress size.");

                // 读取端口号 (大端序)
                int port = (socketAddress[2] << 8) | socketAddress[3];

                // 读取IP地址 (16字节)
                byte[] addressBytes = new byte[16];
                for (int i = 0; i < 16; i++)
                {
                    addressBytes[i] = socketAddress[8 + i];
                }

                return new IPEndPoint(new IPAddress(addressBytes), port);
            }
            else
            {
                throw new ArgumentException("Only IPv4 and IPv6 addresses are supported.");
            }
        }
    }
}
