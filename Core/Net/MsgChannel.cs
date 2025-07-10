using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Net
{
    public class MsgChannel<T>
    {
        private readonly ConcurrentQueue<T> _dataQueue = new ConcurrentQueue<T>();
        private readonly ConcurrentQueue<TaskCompletionSource<T>> _waitingReceivers = new ConcurrentQueue<TaskCompletionSource<T>>();
        private readonly object _lock = new object();
        private bool _isClosed;

        // 发送数据（生产者）
        public void Write(T data)
        {
            lock (_lock)
            {
                if (_isClosed) throw new InvalidOperationException("Channel is closed");

                // 优先唤醒等待的接收者
                if (_waitingReceivers.TryDequeue(out var tcs))
                {
                    if (tcs.TrySetResult(data)) return;
                }
                // 无等待者时，数据入队
                _dataQueue.Enqueue(data);
            }
        }

        // 异步接收数据（消费者）
        public Task<T> ReadAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                // 1. 队列有数据时直接返回
                if (_dataQueue.TryDequeue(out T data))
                    return Task.FromResult(data);

                // 2. Channel已关闭且无数据
                if (_isClosed)
                    return Task.FromException<T>(new ChannelClosedException());

                // 3. 创建等待源并加入队列
                var tcs = new TaskCompletionSource<T>();
                ct.Register(() => tcs.TrySetCanceled()); // 支持取消
                _waitingReceivers.Enqueue(tcs);
                return tcs.Task;
            }
        }

        // 关闭Channel
        public void Close()
        {
            lock (_lock)
            {
                _isClosed = true;
                // 唤醒所有等待者并通知关闭
                while (_waitingReceivers.TryDequeue(out var tcs))
                    tcs.TrySetException(new ChannelClosedException());
            }
        }
    }

    public class ChannelClosedException : Exception { }
}
