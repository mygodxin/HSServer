using Google.Protobuf.WellKnownTypes;
using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace KcpTransport
{
    public static class TimeMeasurement
    {
        public static TimeSpan GetElapsedTime(long startTimestamp)
        {
            long elapsed = Stopwatch.GetTimestamp() - startTimestamp;
            return TimeSpan.FromTicks(elapsed * TimeSpan.TicksPerSecond / Stopwatch.Frequency);
        }

        public static TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
        {
            long elapsed = endTimestamp - startTimestamp;
            return TimeSpan.FromTicks(elapsed * TimeSpan.TicksPerSecond / Stopwatch.Frequency);
        }
    }

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

            var elapsed = TimeMeasurement.GetElapsedTime(timestamp);
            if (elapsed < timeout)
            {
                return true;
            }

            return false;
        }

        static uint GenerateCore(ReadOnlySpan<byte> hashKey, SocketAddress remoteAddress, long timestamp)
        {
            // 1. 使用Span避免不必要的数组拷贝
            Span<byte> source = stackalloc byte[remoteAddress.Size + sizeof(long)];

            // 2. 直接拷贝SocketAddress内容
            for (int i = 0; i < remoteAddress.Size; i++)
            {
                source[i] = remoteAddress[i];
            }

            // 3. 添加时间戳(确保字节序正确)
            if (!BitConverter.TryWriteBytes(source.Slice(remoteAddress.Size), timestamp))
            {
                throw new InvalidOperationException("Failed to write timestamp bytes");
            }

            // 4. 计算HMAC-SHA256
            using (var hmac = new HMACSHA256(hashKey.ToArray())) // 注意: ToArray()在这里是必要的
            {
                byte[] hash = hmac.ComputeHash(source.ToArray()); // 注意: Span需要转换为Array

                // 5. 取前4字节作为uint(明确指定字节序)
                return BitConverter.ToUInt32(hash, 0);
            }
            //Span<byte> source = stackalloc byte[remoteAddress.Size + 8];

            //remoteAddress.Buffer.Span.CopyTo(source);
            //MemoryMarshal.Write(source.Slice(remoteAddress.Size), timestamp);

            //Span<byte> dest = stackalloc byte[HMACSHA256.HashSizeInBytes];
            //HMACSHA256.TryHashData(hashKey, source, dest, out _);

            //return MemoryMarshal.Read<uint>(dest);
        }

        internal static SocketAddress Clone(this SocketAddress socketAddress)
        {
            var clone = new SocketAddress(socketAddress.Family, socketAddress.Size);
            socketAddress.Buffer.CopyTo(clone.Buffer);
            return clone;
        }
    }
}