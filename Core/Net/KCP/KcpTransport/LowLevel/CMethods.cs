#pragma warning disable CS8500
#pragma warning disable CS8981

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IINT32 = System.UInt32;
using IUINT16 = System.UInt16;
using IUINT32 = System.UInt32;
using IUINT8 = System.Byte;
using size_t = System.IntPtr;  // 使用 IntPtr 替代 nint 以提高兼容性

#if UNITY_2021_2_OR_NEWER
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace KcpTransport.LowLevel
{
    internal static unsafe class CMethods
    {
        public static void memcpy(void* dest, void* src, int n)
        {
            Buffer.MemoryCopy(src, dest, n, n);
        }

        public static void* malloc(size_t size)
        {
#if UNITY_2021_2_OR_NEWER
            // Unity 2021.2+ 高性能方案
            return UnsafeUtility.Malloc((int)size, 4, Unity.Collections.Allocator.Persistent);
#else
            // 兼容性方案
            return (void*)Marshal.AllocHGlobal((IntPtr)size);
#endif
        }

        public static void free(void* ptr)
        {
#if UNITY_2021_2_OR_NEWER
            // Unity 2021.2+ 高性能方案
            UnsafeUtility.Free(ptr, Unity.Collections.Allocator.Persistent);
#else
            // 兼容性方案
            Marshal.FreeHGlobal((IntPtr)ptr);
#endif
        }

        [Conditional("DEBUG")]
        public static void assert<T>(T _)
        {
        }

        [Conditional("DEBUG")]
        public static void assert(IKCPCB* _)
        {
        }

        [Conditional("DEBUG")]
        public static void assert(IKCPSEG* _)
        {
        }

        public static void abort()
        {
            //Environment.Exit(0);
        }
    }
}