using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Core.Net
{
    public unsafe class ByteBuffer : IDisposable
    {
        private byte[] _buffer;
        private int _position;
        private int _length;
        private int _capacity;
        private bool _isReadMode;
        private bool _disposed;

        // ===== 构造函数优化 =====
        public ByteBuffer(int len = 64)
        {
            _capacity = Math.Max(64, len);
            _buffer = ArrayPool<byte>.Shared.Rent(_capacity); // 内存池复用 [5](@ref)
            _isReadMode = false;
        }

        public ByteBuffer(ReadOnlySpan<byte> data) : this(data, 0, data.Length) { }

        public ByteBuffer(ReadOnlySpan<byte> data, int offset, int count)
        {
            if (offset < 0 || count < 0 || offset + count > data.Length)
                throw new ArgumentOutOfRangeException();

            _capacity = count;
            _buffer = ArrayPool<byte>.Shared.Rent(count);

            // 使用CopyTo,避免类型转换,底层使用memmove命令
            data.Slice(offset, count).CopyTo(_buffer.AsSpan(0, count));

            _length = count;
            _isReadMode = true;
        }

        // ===== 核心优化技术 =====
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int required)
        {
            if (_position + required <= _capacity) return;

            // 指数扩容策略 [6](@ref)
            int newCapacity = Math.Max(_capacity * 2, _position + required);
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
            ArrayPool<byte>.Shared.Return(_buffer); // 归还旧内存池
            _buffer = newBuffer;
            _capacity = newCapacity;
        }

        // ===== 写入方法优化 =====
        public void WriteInt(int value)
        {
            if (_isReadMode) throw new InvalidOperationException("Buffer in read mode");
            EnsureCapacity(4);
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position), value);
            _position += 4;
            if (_position > _length) _length = _position;
        }

        public void WriteUint(uint value)
        {
            if (_isReadMode) throw new InvalidOperationException("Buffer in read mode");
            EnsureCapacity(4);
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_position), value);
            _position += 4;
            if (_position > _length) _length = _position;
        }

        public void WriteLong(long value)
        {
            if (_isReadMode) throw new InvalidOperationException("Buffer in read mode");
            EnsureCapacity(8);
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_position), value);
            _position += 8;
            if (_position > _length) _length = _position;
        }

        public void WriteFloat(float value)
        {
            if (_isReadMode) throw new InvalidOperationException("Buffer in read mode");
            EnsureCapacity(4);
            BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_position), value);
            _position += 4;
            if (_position > _length) _length = _position;
        }

        public void WriteDouble(double value)
        {
            if (_isReadMode) throw new InvalidOperationException("Buffer in read mode");
            EnsureCapacity(8);
            BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_position), value);
            _position += 8;
            if (_position > _length) _length = _position;
        }

        public void WriteString(string value)
        {
            if (_isReadMode) throw new InvalidOperationException("Buffer in read mode");

            int maxLen = Encoding.UTF8.GetMaxByteCount(value.Length);
            EnsureCapacity(maxLen + 4);

            // 直接写入长度前缀
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position), value.Length);
            _position += 4;

            // 直接编码到缓冲区 [9](@ref)
            int actualBytes = Encoding.UTF8.GetBytes(
                value,
                _buffer.AsSpan(_position, maxLen)
            );
            _position += actualBytes;
            _length = Math.Max(_length, _position);
        }

        public void WriteBytes(byte[] value)
        {
            if (_isReadMode) throw new InvalidOperationException("Buffer in read mode");
            EnsureCapacity(value.Length + 4);

            // 写入长度前缀
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position), value.Length);
            _position += 4;

            // 批量复制 [8](@ref)
            value.CopyTo(_buffer.AsSpan(_position));
            _position += value.Length;
            _length = Math.Max(_length, _position);
        }

        // ===== 读取方法优化 =====
        public int ReadInt()
        {
            CheckReadMode(4);
            var value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_position));
            _position += 4;
            return value;
        }

        public uint ReadUint()
        {
            CheckReadMode(4);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_position));
            _position += 4;
            return value;
        }

        public long ReadLong()
        {
            CheckReadMode(8);
            var value = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_position));
            _position += 8;
            return value;
        }

        public float ReadFloat()
        {
            CheckReadMode(4);
            var value = BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(_position));
            _position += 4;
            return value;
        }

        public double ReadDouble()
        {
            CheckReadMode(8);
            var value = BinaryPrimitives.ReadDoubleLittleEndian(_buffer.AsSpan(_position));
            _position += 8;
            return value;
        }

        public string ReadString()
        {
            CheckReadMode(4);
            int length = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_position));
            _position += 4;

            CheckReadMode(length);
            string value = Encoding.UTF8.GetString(_buffer, _position, length); // 避免中间数组 [9](@ref)
            _position += length;
            return value;
        }

        public byte[] ReadBytes()
        {
            CheckReadMode(4);
            int length = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_position));
            _position += 4;

            CheckReadMode(length);
            byte[] result = new byte[length];
            Buffer.BlockCopy(_buffer, _position, result, 0, length); // 高效块复制
            _position += length;
            return result;
        }

        // ===== 辅助方法优化 =====
        public byte[] Bytes
        {
            get
            {
                byte[] result = new byte[_length];
                Buffer.BlockCopy(_buffer, 0, result, 0, _length);
                return result;
            }
        }

        public void ResetPosition() => _position = 0;

        public int Remaining() => _length - _position;

        public void Clear()
        {
            _position = 0;
            _length = 0;
            // 保留已分配的缓冲区供复用 [5,6](@ref)
        }

        // ===== 安全释放模式 =====
        public void Dispose()
        {
            if (_disposed) return;
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null!;
            _disposed = true;
        }

        // ===== 私有辅助方法 =====
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckReadMode(int required)
        {
            if (!_isReadMode) throw new InvalidOperationException("Buffer not in read mode");
            if (_position + required > _length) throw new EndOfStreamException();
        }
    }
}