//using System.Collections.Concurrent;
//using System.Threading;
//using System.Threading.Channels;

//namespace Core
//{
//    /// <summary>
//    /// 参考Proto.Actor实现基于channel的简单Actor模型
//    /// </summary>
//    public interface IActor
//    {
//        Task ReceiveAsync(IContext context);
//    }

//    public interface IContext
//    {
//        // 基础属性
//        PID Self { get; }
//        PID Sender { get; }
//        object Message { get; set; }
//        IActor Actor { get; }
//        CancellationToken CancellationToken { get; }
//        TimeSpan ReceiveTimeout { get; set; }

//        // 消息传递
//        void Send(PID target, object message);
//        void Request(PID target, object message, PID? sender);
//        Task<T> RequestAsync<T>(PID target, object message, TimeSpan? timeout = null);
//        void Respond(object message);
//        void Respond(object message, MessageHeader header);
//        void CancelReceiveTimeout();

//        // 生命周期管理
//        PID Spawn(Props props);
//        void Stop(PID pid);
//    }

//    public readonly struct PID
//    {
//        public string Id { get; }
//        public static readonly PID None = new("none");

//        public PID(string id) => Id = id;
//        public bool IsValid => Id != "none";
//        public override string ToString() => $"pid:{Id}";
//    }

//    public sealed class Props
//    {
//        public Func<IActor> Producer { get; init; } = null!;
//        public TimeSpan ReceiveTimeout { get; init; } = Timeout.InfiniteTimeSpan;
//        public static Props FromProducer(Func<IActor> producer) => new() { Producer = producer };
//    }

//    public sealed class MessageHeader
//    {
//        // TODO头信息实现
//    }

//    public sealed class MessageEnvelope
//    {
//        public object Message { get; }
//        public MessageHeader Header { get; }

//        public MessageEnvelope(object message, MessageHeader header)
//        {
//            Message = message;
//            Header = header;
//        }
//    }

//    public sealed class ActorSystem : IContext, IDisposable
//    {
//        #region 私有实现
//        private sealed class ActorProcess
//        {
//            private readonly Channel<(object Message, PID Sender)> _mailbox =
//                Channel.CreateUnbounded<(object, PID)>();
//            private readonly CancellationTokenSource _cts = new();
//            private Task? _task;
//            private Timer? _receiveTimeoutTimer;

//            public void StartAsync(Props props, PID self, ActorSystem system)
//            {
//                var actor = props.Producer();
//                _task = Task.Run(async () =>
//                {
//                    while (await _mailbox.Reader.WaitToReadAsync(_cts.Token))
//                    {
//                        var (message, sender) = await _mailbox.Reader.ReadAsync(_cts.Token);
//                        var ctx = new ActorContext(self, sender, system, actor, props.ReceiveTimeout, _cts.Token);
//                        ctx.Message = message;

//                        ResetReceiveTimeout(props.ReceiveTimeout);
//                        await actor.ReceiveAsync(ctx);
//                    }
//                }, _cts.Token);
//            }

//            private void ResetReceiveTimeout(TimeSpan timeout)
//            {
//                _receiveTimeoutTimer?.Dispose();
//                if (timeout != Timeout.InfiniteTimeSpan)
//                {
//                    _receiveTimeoutTimer = new Timer(_ =>
//                    {
//                        _cts.Cancel();
//                    }, null, timeout, Timeout.InfiniteTimeSpan);
//                }
//            }

//            public void Send(object message, PID sender) =>
//                _mailbox.Writer.TryWrite((message, sender));

//            public void Stop()
//            {
//                _receiveTimeoutTimer?.Dispose();
//                _cts.Cancel();
//            }
//        }

//        private sealed class ActorContext : IContext
//        {
//            private readonly ActorSystem _system;
//            private readonly CancellationTokenSource _receiveTimeoutCts;
//            private readonly Timer? _receiveTimeoutTimer;

//            public PID Self { get; }
//            public PID Sender { get; }
//            public object Message { get; set; } = null!;
//            public IActor Actor { get; }
//            public CancellationToken CancellationToken { get; }
//            public TimeSpan ReceiveTimeout { get; set; }

//            public ActorContext(PID self, PID sender, ActorSystem system, IActor actor, TimeSpan receiveTimeout, CancellationToken parentCt)
//            {
//                Self = self;
//                Sender = sender;
//                _system = system;
//                Actor = actor;
//                ReceiveTimeout = receiveTimeout;

//                _receiveTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);
//                CancellationToken = _receiveTimeoutCts.Token;

//                if (receiveTimeout != Timeout.InfiniteTimeSpan)
//                {
//                    _receiveTimeoutTimer = new Timer(_ =>
//                    {
//                        _receiveTimeoutCts.Cancel();
//                    }, null, receiveTimeout, Timeout.InfiniteTimeSpan);
//                }
//            }

//            public void Send(PID target, object message) =>
//                _system.Send(target, message, Self);

//            public void Request(PID target, object message, PID? sender) =>
//                _system.Request(target, message, sender ?? Self);

//            public Task<T> RequestAsync<T>(PID target, object message, TimeSpan? timeout = null) =>
//                _system.RequestAsync<T>(target, message, Self, timeout);

//            public void Respond(object message)
//            {
//                if (!Sender.IsValid)
//                    throw new InvalidOperationException("No sender to respond to");
//                _system.Send(Sender, message, Self);
//            }

//            public void Respond(object message, MessageHeader header) =>
//                Respond(new MessageEnvelope(message, header));

//            public void CancelReceiveTimeout()
//            {
//                _receiveTimeoutTimer?.Dispose();
//                if (!_receiveTimeoutCts.IsCancellationRequested)
//                    _receiveTimeoutCts.Cancel();
//            }

//            public PID Spawn(Props props) => _system.Spawn(props);

//            public void Stop(PID pid) => _system.Stop(pid);
//        }
//        #endregion

//        #region 系统实现
//        private readonly ConcurrentDictionary<PID, ActorProcess> _processes = new();
//        private readonly CancellationTokenSource _systemCts = new();

//        // IContext实现（系统作为特殊上下文）
//        public PID Self => new("system");
//        public PID Sender => PID.None;
//        public object Message { get; set; } = null!;
//        public IActor Actor => null!;
//        public CancellationToken CancellationToken => _systemCts.Token;
//        public TimeSpan ReceiveTimeout { get; set; } = Timeout.InfiniteTimeSpan;

//        public void Send(PID target, object message)
//        {
//            if (_processes.TryGetValue(target, out var process))
//                process.Send(message, Self);
//        }

//        internal void Send(PID target, object message, PID sender)
//        {
//            if (_processes.TryGetValue(target, out var process))
//                process.Send(message, sender);
//        }

//        public void Request(PID target, object message, PID? sender)
//        {
//            if (_processes.TryGetValue(target, out var process))
//                process.Send(message, sender ?? Self);
//        }

//        public async Task<T> RequestAsync<T>(PID target, object message, TimeSpan? timeout = null)
//        {
//            return await RequestAsync<T>(target, message, Self, timeout);
//        }

//        internal async Task<T> RequestAsync<T>(PID target, object message, PID sender, TimeSpan? timeout = null)
//        {
//            var tcs = new TaskCompletionSource<T>();
//            var tempPid = Spawn(Props.FromProducer(() => new ResponseActor<T>(tcs)));

//            try
//            {
//                Send(target, message, tempPid);
//                using var cts = timeout.HasValue
//                    ? new CancellationTokenSource(timeout.Value)
//                    : null;

//                return await tcs.Task.WaitAsync(cts?.Token ?? CancellationToken.None);
//            }
//            finally
//            {
//                Stop(tempPid);
//            }
//        }

//        public void Stop(PID pid)
//        {
//            if (_processes.TryRemove(pid, out var process))
//                process.Stop();
//        }

//        public void Dispose()
//        {
//            _systemCts.Cancel();
//            foreach (var process in _processes.Values)
//                process.Stop();
//            _processes.Clear();
//        }

//        // IContext显式实现
//        void IContext.Respond(object message) => throw new NotSupportedException("System context cannot respond");
//        void IContext.Respond(object message, MessageHeader header) => throw new NotSupportedException();
//        void IContext.CancelReceiveTimeout() { } // 系统上下文无超时
//        #endregion

//        #region 内部Actor
//        private sealed class ResponseActor<T> : IActor
//        {
//            private readonly TaskCompletionSource<T> _tcs;
//            public ResponseActor(TaskCompletionSource<T> tcs) => _tcs = tcs;

//            public Task ReceiveAsync(IContext context)
//            {
//                switch (context.Message)
//                {
//                    case T response: _tcs.TrySetResult(response); break;
//                    case Exception ex: _tcs.TrySetException(ex); break;
//                    case MessageEnvelope envelope when envelope.Message is T response:
//                        _tcs.TrySetResult(response);
//                        break;
//                }
//                return Task.CompletedTask;
//            }
//        }
//        #endregion

//        public PID Spawn(Props props)
//        {
//            var pid = new PID($"actor-{Guid.NewGuid()}");
//            var process = new ActorProcess();
//            process.StartAsync(props, pid, this);
//            _processes.TryAdd(pid, process);
//            return pid;
//        }

//        public IContext Root => this;
//    }
//}