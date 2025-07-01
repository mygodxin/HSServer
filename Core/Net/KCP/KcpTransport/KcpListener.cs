using Core;
//using Luban;
using Core.Net;
using Core.Net.UDP;
using KcpTransport.LowLevel;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KcpTransport
{

    public abstract class KcpOptions
    {
        public bool EnableNoDelay { get; set; } = true;
        public int IntervalMilliseconds { get; set; } = 10; // ikcp_nodelay min is 10.
        public int Resend { get; set; } = 2;
        public bool EnableFlowControl { get; set; } = false;
        public (int SendWindow, int ReceiveWindow) WindowSize { get; set; } = ((int)KcpMethods.IKCP_WND_SND, (int)KcpMethods.IKCP_WND_RCV);
        public int MaximumTransmissionUnit { get; set; } = (int)KcpMethods.IKCP_MTU_DEF;
        // public int MinimumRetransmissionTimeout { get; set; } this value is changed in ikcp_nodelay(and use there default) so no configurable.
    }

    public enum ListenerSocketType
    {
        Receive,
        Send
    }

    public delegate ReadOnlySpan<byte> HashFunc();

    public sealed class KcpListener : IDisposable, IAsyncDisposable
    {
        static readonly byte[] DefaultRandomHashKey = new byte[32];

        public IPEndPoint ListenEndPoint { get; set; }
        public TimeSpan UpdatePeriod { get; set; } = TimeSpan.FromMilliseconds(5);
        public int EventLoopCount { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);
        public bool ConfigureAwait { get; set; } = false;
        public TimeSpan KeepAliveDelay { get; set; } = TimeSpan.FromSeconds(2000000000d);
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public HashFunc Handshake32bitHashKeyGenerator { get; set; } = KeyGenerator;

        static ReadOnlySpan<byte> KeyGenerator() => DefaultRandomHashKey;

        UdpSocketServer socket;
        Random random = new Random();
        ConcurrentQueue<KcpConnection> acceptQueue;
        bool isDisposed;
        ConcurrentDictionary<uint, KcpConnection> connections = new ConcurrentDictionary<uint, KcpConnection>();

        Task[] socketEventLoopTasks;
        Thread updateConnectionsWorkerThread;

        public static ValueTask<KcpListener> ListenAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            return ListenAsync(new IPEndPoint(IPAddress.Parse(host), port), cancellationToken);
        }

        public static ValueTask<KcpListener> ListenAsync(IPEndPoint listenEndPoint, CancellationToken cancellationToken = default)
        {
            return new ValueTask<KcpListener>(new KcpListener(listenEndPoint));
        }
        KcpListener(IPEndPoint ipEndpoint)
        {
            var socket = new UdpSocketServer();
            socket.StartAsync(ipEndpoint.Address.ToString(), ipEndpoint.Port);
            socket.OnReceivedData += OnReceivedData;
            this.socket = socket;
            this.acceptQueue = new ConcurrentQueue<KcpConnection>();

            updateConnectionsWorkerThread = new Thread(RunUpdateKcpConnectionLoop)
            {
                Name = $"{nameof(KcpListener)}",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
            };
            updateConnectionsWorkerThread.Start();
        }

        public KcpConnection AcceptConnection()
        {
            if (isDisposed)
                throw new System.ObjectDisposedException(nameof(KcpConnection));

            // 先检查队列中是否已有可用连接
            if (acceptQueue.TryDequeue(out var connection))
            {
                return connection;
            }
            return null;
        }

        private void OnReceivedData(IPEndPoint RemoteEndPoint, byte[] e, int len)
        {

            try
            {
                var socketBuffer = new ByteBuffer(e);

                var receivedAddress = RemoteEndPoint;

                var received = len;

                var conversationId = socketBuffer.ReadUint();
                var packetType = (PacketType)conversationId;
                switch (packetType)
                {
                    case PacketType.HandshakeInitialRequest:
                    ISSUE_CONVERSATION_ID:
                        {
                            conversationId = (uint)random.Next(100, int.MaxValue); // 0~99 is reserved
                            if (connections.ContainsKey(conversationId))
                            {
                                goto ISSUE_CONVERSATION_ID;
                            }

                            // send tentative id and cookie to avoid syn-flood(don't allocate memory in this phase)
                            var (cookie, timestamp) = SynCookie.Generate(DefaultRandomHashKey, receivedAddress);
                            Logger.Error($"[send] conversationId={conversationId}, cookie={cookie}, timestamp={timestamp}");
                            SendHandshakeInitialResponse(socket, receivedAddress, conversationId, cookie, timestamp);
                        }
                        break;
                    case PacketType.HandshakeOkRequest:
                        {
                            conversationId = socketBuffer.ReadUint();//MemoryMarshal.Read<uint>(socketBuffer.AsSpan(4, received - 4));
                            var cookie = socketBuffer.ReadUint();//MemoryMarshal.Read<uint>(socketBuffer.AsSpan(8, received - 8));
                            var timestamp = socketBuffer.ReadLong();//MemoryMarshal.Read<long>(socketBuffer.AsSpan(12, received - 12));
                            Logger.Error($"[receive] conversationId={conversationId}, cookie={cookie}, timestamp={timestamp}");
                            if (!SynCookie.Validate(DefaultRandomHashKey, HandshakeTimeout, cookie, receivedAddress, timestamp))
                            {
                                SendHandshakeNgResponse(socket, receivedAddress);
                                break;
                            }

                            if (!connections.TryAdd(conversationId, null))
                            {
                                // can't added, client should retry
                                SendHandshakeNgResponse(socket, receivedAddress);
                                break;
                            }

                            // create new connection
                            var kcpConnection = new KcpConnection(conversationId, receivedAddress);
                            connections[conversationId] = kcpConnection;
                            acceptQueue.Enqueue(kcpConnection);
                            SendHandshakeOkResponse(socket, receivedAddress);

                        }
                        break;
                    case PacketType.Ping:
                        {
                            conversationId = socketBuffer.ReadUint();
                            if (!connections.TryGetValue(conversationId, out var kcpConnection))
                            {
                                // may incoming old packet, TODO: log it.
                                return;
                            }

                            kcpConnection.PingReceived();
                        }
                        break;
                    case PacketType.Pong:
                        {
                            conversationId = socketBuffer.ReadUint();
                            if (!connections.TryGetValue(conversationId, out var kcpConnection))
                            {
                                // may incoming old packet, TODO: log it.
                                return;
                            }

                            kcpConnection.PongReceived();
                        }
                        break;
                    case PacketType.Disconnect:
                        {
                            conversationId = socketBuffer.ReadUint();
                            if (!connections.TryRemove(conversationId, out var kcpConnection))
                            {
                                // may incoming old packet, TODO: log it.
                                return;
                            }

                            kcpConnection.Disconnect();
                            kcpConnection.Dispose();
                        }
                        break;
                    case PacketType.Unreliable:
                        {
                            conversationId = socketBuffer.ReadUint();
                            if (!connections.TryGetValue(conversationId, out var kcpConnection))
                            {
                                // may incoming old packet, TODO: log it.
                                return;
                            }


                            lock (kcpConnection.SyncRoot)
                            {
                                kcpConnection.InputReceivedUnreliableBuffer(socketBuffer.ReadBytes());
                            }
                        }
                        break;
                    default:
                        {
                            Logger.Error($"[收到数据] conversationId={conversationId}");
                            // Reliable
                            if (conversationId < 100)
                            {
                                // may incoming invalid packet, TODO: log it.
                                return;
                            }
                            if (!connections.TryGetValue(conversationId, out var kcpConnection))
                            {
                                // may incoming old packet, TODO: log it.
                                return;
                            }

                            lock (kcpConnection.SyncRoot)
                            {
                                unsafe
                                {
                                    //var socketBufferPointer = (byte*)Unsafe.AsPointer(ref e);

                                    var socketBufferPointer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(e));

                                    //var buffer = new ByteBuffer(e);
                                    //var id = buffer.ReadUint();
                                    //Logger.Error($"[收到数据1] id={id}");
                                    if (!kcpConnection.InputReceivedKcpBuffer(socketBufferPointer, received)) return;
                                }

                                kcpConnection.ConsumeKcpFragments(receivedAddress);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        static void SendHandshakeInitialResponse(UdpSocketServer socket, IPEndPoint clientAddress, uint conversationId, uint cookie, long timestamp)
        {
            var data = new ByteBuffer(20); // type(4) + conv(4) + cookie(4) + timestamp(8)
            data.WriteUint((uint)PacketType.HandshakeInitialResponse);
            data.WriteUint(conversationId);
            data.WriteUint(cookie);
            data.WriteLong(timestamp);

            socket.SendToAsync(clientAddress, data.ToArray());
        }

        static void SendHandshakeOkResponse(UdpSocketServer socket, IPEndPoint clientAddress)
        {
            var data = new ByteBuffer(4);
            data.WriteUint((uint)PacketType.HandshakeOkResponse);
            socket.SendToAsync(clientAddress, data.ToArray());
        }

        static void SendHandshakeNgResponse(UdpSocketServer socket, IPEndPoint clientAddress)
        {
            var data = new ByteBuffer(4);
            data.WriteUint((uint)PacketType.HandshakeNgResponse);
            socket.SendToAsync(clientAddress, data.ToArray());
        }

        void RunUpdateKcpConnectionLoop(object state)
        {
            // NOTE: should use ikcp_check? https://github.com/skywind3000/kcp/wiki/EN_KCP-Best-Practice#advance-update

            // All Windows(.NET) Timer and Sleep is low-resolution(min is 16ms).
            // We use custom high-resolution timer instead.

            var period = UpdatePeriod;
            var timeout = ConnectionTimeout;
            var waitTime = (int)period.TotalMilliseconds;

            var removeConnection = new List<uint>();
            while (true)
            {
                Thread.Sleep(waitTime);

                var currentTimestamp = Stopwatch.GetTimestamp();
                foreach (var kvp in connections)
                {
                    var connection = kvp.Value;
                    if (connection != null) // at handshake, connection is not created yet so sometimes null...
                    {
                        if (connection.IsAlive(currentTimestamp, timeout))
                        {
                            connection.TrySendPing(currentTimestamp);
                            connection.UpdateKcp();
                        }
                        else
                        {
                            removeConnection.Add(kvp.Key);
                        }
                    }
                }

                if (removeConnection.Count > 0)
                {
                    foreach (var id in removeConnection)
                    {
                        if (connections.TryRemove(id, out var conn))
                        {
                            conn.Disconnect();
                            conn.Dispose();
                        }
                    }

                    removeConnection.Clear();
                }
            }
        }

        public unsafe void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (isDisposed) return;
            isDisposed = true;

            acceptQueue = null;

            // wait loop complete
            try
            {
                var curr = SynchronizationContext.Current;
                await Task.WhenAll(socketEventLoopTasks);
            }
            catch { }

            // before socket dispose, send disocnnected event to all connections
            foreach (var connection in connections)
            {
                var conn = connection.Value;
                if (conn != null)
                {
                    conn.Disconnect();
                    conn.Dispose();
                }
            }
            connections.Clear();
            socket.StopAsync();
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
