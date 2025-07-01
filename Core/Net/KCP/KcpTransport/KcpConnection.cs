#pragma warning disable CS8500

using Core;
//using Luban;
using Core.Net;
using Core.Net.UDP;
using KcpTransport.LowLevel;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static KcpTransport.LowLevel.KcpMethods;

namespace KcpTransport
{
    // KcpConnections is both used for server and client
    public class KcpConnection : IDisposable
    {
        public TimeSpan UpdatePeriod { get; set; } = TimeSpan.FromMilliseconds(5);
        public bool ConfigureAwait { get; set; } = false;
        public TimeSpan KeepAliveDelay { get; set; } = TimeSpan.FromSeconds(20);
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public bool EnableNoDelay { get; set; } = true;
        public int IntervalMilliseconds { get; set; } = 10; // ikcp_nodelay min is 10.
        public int Resend { get; set; } = 2;
        public bool EnableFlowControl { get; set; } = false;
        public (int SendWindow, int ReceiveWindow) WindowSize { get; set; } = ((int)KcpMethods.IKCP_WND_SND, (int)KcpMethods.IKCP_WND_RCV);
        public int MaximumTransmissionUnit { get; set; } = (int)KcpMethods.IKCP_MTU_DEF;
        // public int MinimumRetransmissionTimeout { get; set; } this value is changed in ikcp_nodelay(and use there default) so no configurable.
        /// <summary>
        /// 回调
        /// </summary>
        public Action<byte[]> OnRecive;
        private static SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
        private static SocketAsyncEventArgs reciveArgs = new SocketAsyncEventArgs();

        static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(5);

        unsafe IKCPCB* kcp;
        uint conversationId;
        IPEndPoint remoteAddress;
        UdpSocket socket;
        Task receiveEventLoopTask; // only used for client
        Thread updateKcpWorkerThread; // only used for client
        //ValueTask<bool> lastFlushResult = default;

        readonly long startingTimestamp = Stopwatch.GetTimestamp();
        readonly TimeSpan keepAliveDelay;
        CancellationTokenSource connectionCancellationTokenSource = new CancellationTokenSource();
        readonly object gate = new object();
        long lastReceivedTimestamp;
        long lastPingSent;
        bool isDisposed;

        public uint ConnectionId => conversationId;
        internal object SyncRoot => gate;
        private ByteBuffer sendBuffer;
        private ByteBuffer reciveBuffer;

        private readonly int MaxSize = 1024;

        // create by User(from KcpConnection.ConnectAsync), for client connection
        unsafe KcpConnection(UdpSocket socket, uint conversationId)
        {
            this.conversationId = conversationId;
            this.keepAliveDelay = KeepAliveDelay;
            this.kcp = ikcp_create(conversationId, GCHandle.ToIntPtr(GCHandle.Alloc(this)).ToPointer());
            this.kcp->output = &KcpOutputCallback;
            ConfigKcpWorkMode(EnableNoDelay, IntervalMilliseconds, Resend, EnableFlowControl);
            ConfigKcpWindowSize(WindowSize.SendWindow, WindowSize.ReceiveWindow);
            ConfigKcpMaximumTransmissionUnit(MaximumTransmissionUnit);

            sendBuffer = new ByteBuffer(MaximumTransmissionUnit);
            reciveBuffer = new ByteBuffer(MaximumTransmissionUnit);
            this.socket = socket;
            this.lastReceivedTimestamp = startingTimestamp;

            this.receiveEventLoopTask = StartSocketEventLoopAsync();


            UpdateKcp(); // initial set kcp timestamp
            updateKcpWorkerThread = new Thread(RunUpdateKcpLoop)
            {
                Name = $"{nameof(KcpConnection)}-{conversationId}",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
            };
            updateKcpWorkerThread.Start();
        }

        // create from Listerner for server connection
        internal unsafe KcpConnection(uint conversationId, IPEndPoint remoteAddress)
        {
            this.conversationId = conversationId;
            this.keepAliveDelay = KeepAliveDelay;
            this.kcp = ikcp_create(conversationId, GCHandle.ToIntPtr(GCHandle.Alloc(this)).ToPointer());
            this.kcp->output = &KcpOutputCallback;
            ConfigKcpWorkMode(EnableNoDelay, IntervalMilliseconds, Resend, EnableFlowControl);
            ConfigKcpWindowSize(WindowSize.SendWindow, WindowSize.ReceiveWindow);
            ConfigKcpMaximumTransmissionUnit(MaximumTransmissionUnit);

            this.remoteAddress = remoteAddress;

            sendBuffer = new ByteBuffer(MaximumTransmissionUnit);
            reciveBuffer = new ByteBuffer(MaximumTransmissionUnit);
            // bind same port and connect client IP, this socket is used only for Send

            this.socket = new UdpSocket(remoteAddress);

            this.lastReceivedTimestamp = startingTimestamp;

            UpdateKcp(); // initial set kcp timestamp
                         // StartUpdateKcpLoopAsync(); server operation, Update will be called from KcpListener so no need update self.
        }

        public static ValueTask<KcpConnection> ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            return ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port), cancellationToken);
        }

        public static async ValueTask<KcpConnection> ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken cancellationToken = default)
        {
            var socket = new UdpSocket(remoteEndPoint);
            await socket.ConnectAsync(remoteEndPoint.Address.ToString(), remoteEndPoint.Port);

            var buffer = new byte[4];
            var initID = (uint)PacketType.HandshakeInitialRequest;
            MemoryMarshal.Write(buffer, ref initID);
            await socket.SendAsync(buffer);

            var receiveInit = await socket.ReceiveAsync();
            if (receiveInit.Item2 != 20) throw new Exception();

            var receiveBytes = new ByteBuffer(receiveInit.Item1);
            var msgID = receiveBytes.ReadUint();
            var conversationId = receiveBytes.ReadUint();
            var cookie = receiveBytes.ReadUint();
            var timesamp = receiveBytes.ReadLong();
            var sendBytes = new ByteBuffer(20);
            sendBytes.WriteUint((uint)PacketType.HandshakeOkRequest);
            sendBytes.WriteUint(conversationId);
            sendBytes.WriteUint(cookie);
            sendBytes.WriteLong(timesamp);
            await socket.SendAsync(sendBytes.ToArray());

            var receiveOK = await socket.ReceiveAsync();
            if (receiveOK.Item2 != 4) throw new Exception();
            var responseCode = (PacketType)MemoryMarshal.Read<uint>(receiveOK.Item1);

            if (responseCode != PacketType.HandshakeOkResponse) throw new Exception();

            var connection = new KcpConnection(socket, conversationId);
            return connection;
        }

        async Task StartSocketEventLoopAsync()
        {
            while (true)
            {
                if (isDisposed) return;

                // Socket is datagram so received contains full block
                var socketBuffer = await socket.ReceiveAsync();
                var buffer = new ByteBuffer(socketBuffer.Item1);
                var conversationId = buffer.ReadUint();
                var packetType = (PacketType)conversationId;
                switch (packetType)
                {
                    case PacketType.Unreliable:
                        {
                            conversationId = buffer.ReadUint();
                            InputReceivedUnreliableBuffer(buffer.ReadBytes());
                        }
                        break;
                    case PacketType.Ping:
                        PingReceived();
                        break;
                    case PacketType.Pong:
                        PongReceived();
                        break;
                    case PacketType.Disconnect:
                        Disconnect();
                        Dispose();
                        break;
                    default:
                        {
                            // Reliable
                            if (conversationId < 100)
                            {
                                // may incoming invalid packet, TODO: log it.
                                continue;
                            }

                            unsafe
                            {
                                //var buf = buffer.ToArray();
                                var socketBufferPointer = (byte*)Unsafe.AsPointer(ref socketBuffer.Item1);
                                if (!InputReceivedKcpBuffer(socketBufferPointer, socketBuffer.Item2)) continue;
                            }

                            ConsumeKcpFragments(null);
                        }
                        break;
                }
            }
        }

        // same of KcpListener.RunUpdateKcpConnectionLoop
        void RunUpdateKcpLoop(object state)
        {
            var cancellationToken = connectionCancellationTokenSource.Token;
            var period = UpdatePeriod;
            var timeout = ConnectionTimeout;
            var waitTime = (int)period.TotalMilliseconds;

            while (true)
            {
                Thread.Sleep(waitTime);

                if (cancellationToken.IsCancellationRequested) break;

                var currentTimestamp = Stopwatch.GetTimestamp();
                if (IsAlive(currentTimestamp, timeout))
                {
                    TrySendPing(currentTimestamp);
                    UpdateKcp();
                }
                else
                {
                    // TODO: Disconnect.
                }
            }
        }

        internal unsafe bool InputReceivedKcpBuffer(byte* buffer, int length)
        {
            lastReceivedTimestamp = Stopwatch.GetTimestamp();
            lock (gate)
            {
                if (isDisposed) return false;

                var inputResult = ikcp_input(kcp, buffer, length);

                if (inputResult == 0)
                {
                    return true;
                }
                else
                {
                    // TODO: log
                    return false;
                }
            }
        }

        internal unsafe void ConsumeKcpFragments(IPEndPoint remoteAddress)
        {
            lock (gate)
            {
                if (isDisposed) return;

                var size = ikcp_peeksize(kcp);
                if (size > 0)
                {
                    if (remoteAddress != null && !remoteAddress.Equals(this.remoteAddress))
                    {
                        // TODO: shutdown existing socket and create new one?
                    }

                    reciveBuffer.Clear();
                    var buffer = reciveBuffer.ToArray();

                    fixed (byte* p = buffer)
                    {
                        var len = ikcp_recv(kcp, p, buffer.Length);
                        if (len > 0)
                        {
                            OnRecive(buffer);
                        }
                    }
                }
            }
        }

        internal unsafe void InputReceivedUnreliableBuffer(ReadOnlySpan<byte> span)
        {
            lastReceivedTimestamp = Stopwatch.GetTimestamp();
            OnRecive(span.ToArray());
        }

        // KcpStream.Write operations send buffer to socket.

        public unsafe void SendReliableBuffer(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* p = buffer)
            {
                lock (gate)
                {
                    if (isDisposed) return;

                    ikcp_send(kcp, p, buffer.Length);
                }
            }
        }

        public unsafe void SendUnreliableBuffer(ReadOnlySpan<byte> buffer)
        {
            sendBuffer.Clear();
            sendBuffer.WriteUint((uint)PacketType.Unreliable);
            sendBuffer.WriteUint(kcp->conv);
            sendBuffer.WriteBytes(buffer.ToArray());

            lock (gate)
            {
                if (isDisposed) return;

                socket.SendAsync(sendBuffer.ReadBytes());
            }
        }

        internal unsafe void KcpFlush()
        {
            lock (gate)
            {
                if (isDisposed) return;

                ikcp_flush(kcp);
            }
        }

        internal unsafe void UpdateKcp()
        {
            var elapsed = TimeMeasurement.GetElapsedTime(startingTimestamp);
            var currentTimestampMilliseconds = (uint)elapsed.TotalMilliseconds;
            lock (gate)
            {
                if (isDisposed) return;

                ikcp_update(kcp, currentTimestampMilliseconds);
            }
        }

        internal unsafe bool IsAlive(long currentTimestamp, TimeSpan timeout)
        {
            if (isDisposed) return false;

            // ikcp.c
            // if (segment->xmit >= kcp->dead_link) kcp->state = unchecked((IUINT32)(-1));
            if (kcp->state == unchecked((uint)(-1)))
            {
                return false;
            }

            var elapsed = TimeMeasurement.GetElapsedTime(lastReceivedTimestamp, currentTimestamp);
            if (elapsed < timeout)
            {
                return true;
            }

            return false;
        }

        internal unsafe void TrySendPing(long currentTimestamp)
        {
            if (isDisposed) return;

            var elapsed = TimeMeasurement.GetElapsedTime(lastReceivedTimestamp, currentTimestamp);
            if (elapsed > keepAliveDelay)
            {
                // send ping per 5 seconds.
                var ping = TimeMeasurement.GetElapsedTime(lastPingSent, currentTimestamp);
                if (ping > PingInterval)
                {
                    lastPingSent = currentTimestamp;

                    sendBuffer.Clear();
                    sendBuffer.WriteUint((uint)PacketType.Ping);
                    sendBuffer.WriteUint(ConnectionId);

                    lock (gate)
                    {
                        if (isDisposed) return;

                        socket.SendAsync(sendBuffer.ToArray());
                    }
                }
            }
        }

        internal unsafe void PingReceived()
        {
            lastReceivedTimestamp = Stopwatch.GetTimestamp();

            sendBuffer.Clear();
            sendBuffer.WriteUint((uint)PacketType.Pong);
            sendBuffer.WriteUint(conversationId);

            lock (gate)
            {
                if (isDisposed) return;

                socket.SendAsync(sendBuffer.ToArray());
            }
        }

        internal unsafe void PongReceived()
        {
            lastReceivedTimestamp = Stopwatch.GetTimestamp();
        }

        // https://github.com/skywind3000/kcp/wiki/EN_KCP-Basic-Usage#config-kcp

        unsafe void ConfigKcpWorkMode(bool enableNoDelay, int intervalMilliseconds, int resend, bool enableFlowControl)
        {
            // int ikcp_nodelay(ikcpcb *kcp, int nodelay, int interval, int resend, int nc)
            // nodelay: Whether enable nodelay mode. 0: Off; 1: On.
            // interval: The internal interval in milliseconds, such as 10ms or 30ms.
            // resend: Whether enable fast retransmit mode. 0: Off; 2: Retransmit when missed in 2 ACK.
            // nc: Whether disable the flow control. 0: Enable. 1: Disable.

            // For normal mode, like TCP: ikcp_nodelay(kcp, 0, 40, 0, 0);
            // For high efficient transport: ikcp_nodelay(kcp, 1, 10, 2, 1);

            lock (gate)
            {
                if (isDisposed) return;

                ikcp_nodelay(kcp, enableNoDelay ? 1 : 0, intervalMilliseconds, resend, enableFlowControl ? 0 : 1);
            }
        }

        unsafe void ConfigKcpWindowSize(int sendWindow, int receiveWindow)
        {
            // int ikcp_wndsize(ikcpcb* kcp, int sndwnd, int rcvwnd);
            // Setup the max send or receive window size in packets,
            // default to 32 packets.Similar to TCP SO_SNDBUF and SO_RECVBUF, but they are in bytes, while ikcp_wndsize in packets.
            lock (gate)
            {
                if (isDisposed) return;

                ikcp_wndsize(kcp, sendWindow, receiveWindow);
            }
        }

        unsafe void ConfigKcpMaximumTransmissionUnit(int mtu)
        {
            // The MTU(Maximum Transmission Unit) default to 1400 bytes, which can be set by ikcp_setmtu.
            // Notice that KCP never probe the MTU, user must tell KCP the right MTU to use if need.
            lock (gate)
            {
                if (isDisposed) return;

                if (kcp->mtu != mtu)
                {
                    ikcp_setmtu(kcp, mtu);
                }
            }
        }

        unsafe void ConfigKcpMinimumRetransmissionTimeout(int minimumRto)
        {
            // Both TCP and KCP use minimum RTO, for example, when calculated RTO is 40ms but default minimum RTO is 100ms,
            // then KCP never detect the dropped packet util after 100ms.The default value of minimum RTO is 100ms for normal mode,
            // while 30ms for high efficient transport.
            // User can setup minimum RTO by: kcp->rx_minrto = 10;
            lock (gate)
            {
                if (isDisposed) return;

                kcp->rx_minrto = minimumRto;
            }
        }

        public unsafe void Disconnect()
        {
            if (isDisposed) return;

            // Send disconnect message
            sendBuffer.Clear();
            sendBuffer.WriteUint((uint)PacketType.Disconnect);
            sendBuffer.WriteUint(conversationId);
            lock (gate)
            {
                socket.SendAsync(sendBuffer.ToArray());
            }
        }

        public unsafe void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        unsafe void Dispose(bool disposing)
        {
            Disconnect();

            lock (gate)
            {
                if (isDisposed) return;
                isDisposed = true;

                if (disposing)
                {
                    connectionCancellationTokenSource.Cancel();
                    connectionCancellationTokenSource.Dispose();

                    GCHandle.FromIntPtr((IntPtr)kcp->user).Free();
                    ikcp_release(kcp);
                    kcp = null;

                    socket.Dispose();
                    socket = null;

                    sendBuffer.Clear();
                    reciveBuffer.Clear();
                }
                else
                {
                    // only cleanup unmanaged resource
                    GCHandle.FromIntPtr((IntPtr)kcp->user).Free();
                    ikcp_release(kcp);
                }
            }
        }

        ~KcpConnection()
        {
            Dispose(false);
        }


        static unsafe int KcpOutputCallback(byte* buf, int len, IKCPCB* kcp, void* user)
        {
            var self = (KcpConnection)GCHandle.FromIntPtr((IntPtr)user).Target;
            var buffer = new Span<byte>(buf, len);

            var sent = self.socket.Send(buffer.ToArray());
            Console.WriteLine($"[KcpOutputCallback] len={len}, self={self.ConnectionId}");
            return sent;
        }

        //static unsafe void KcpWriteLog(string msg, IKCPCB* kcp, object user)
        //{
        //    Console.WriteLine(msg);
        //}
    }
}
