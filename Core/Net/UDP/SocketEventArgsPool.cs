//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Core.Net.UDP
//{
//    using System.Collections.Concurrent;
//    using System.Net.Sockets;

//    public class SocketEventArgsPool
//    {
//        private ConcurrentStack<SocketAsyncEventArgs> _pool = new();

//        // 初始化时预创建对象
//        public void Init(int poolSize, int bufferSize)
//        {
//            for (int i = 0; i < poolSize; i++)
//            {
//                var args = new SocketAsyncEventArgs();
//                args.SetBuffer(new byte[bufferSize], 0, bufferSize);
//                args.Completed += OnOperationCompleted;
//                _pool.Push(args);
//            }
//        }

//        // 租用对象
//        public SocketAsyncEventArgs Rent()
//        {
//            return _pool.TryPop(out var args) ? args : null;
//        }

//        // 归还对象
//        public void Return(SocketAsyncEventArgs args)
//        {
//            _pool.Push(args);
//        }

//        private void OnOperationCompleted(object sender, SocketAsyncEventArgs e)
//        {
//            if (e.LastOperation == SocketAsyncOperation.ReceiveFrom)
//                ProcessReceive(e);
//            else if (e.LastOperation == SocketAsyncOperation.SendTo)
//                ProcessSend(e);
//        }
//    }
//}
