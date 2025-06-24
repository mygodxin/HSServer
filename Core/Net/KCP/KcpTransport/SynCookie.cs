using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;

namespace KcpTransport
{

    internal static class SynCookie
    {
        public static (uint Cookie, long Timestamp) Generate(ReadOnlySpan<byte> hashKey, SocketAddress remoteAddress)
        {
            var timestamp = Stopwatch.GetTimestamp();
            var hash = GenerateCore(hashKey, remoteAddress, timestamp);

            return (hash, timestamp);
        }

        public static bool Validate(ReadOnlySpan<byte> hashKey, TimeSpan timeout, uint cookie, SocketAddress remoteAddress, long timestamp)
        {
            var cookie2 = GenerateCore(hashKey, remoteAddress, timestamp);
            if (cookie != cookie2)
            {
                return false;
            }

            var elapsed = TimeSpan.FromTicks(timestamp);
            if (elapsed < timeout)
            {
                return true;
            }

            return false;
        }

        static uint GenerateCore(ReadOnlySpan<byte> hashKey, SocketAddress remoteAddress, long timestamp)
        {
            // 1. 将 SocketAddress 转换为字节数组
            byte[] addressBytes = new byte[remoteAddress.Size];
            for (int i = 0; i < remoteAddress.Size; i++)
            {
                addressBytes[i] = remoteAddress[i]; // 直接索引访问
            }

            // 2. 合并地址数据和时间戳
            byte[] source = new byte[addressBytes.Length + 8];
            Buffer.BlockCopy(addressBytes, 0, source, 0, addressBytes.Length);
            byte[] timestampBytes = BitConverter.GetBytes(timestamp);
            Buffer.BlockCopy(timestampBytes, 0, source, addressBytes.Length, 8);

            // 3. 计算 HMAC-SHA256
            using (var hmac = new HMACSHA256(hashKey.ToArray()))
            {
                byte[] hash = hmac.ComputeHash(source);

                // 4. 取前 4 字节作为 uint（小端序）
                return BitConverter.ToUInt32(hash, 0);
            }
        }
    }
}