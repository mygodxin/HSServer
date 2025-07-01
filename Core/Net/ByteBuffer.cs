using System;
using System.IO;
using System.Text;

namespace Core.Net
{
    public class ByteBuffer : IDisposable
    {
        private MemoryStream stream;
        private BinaryWriter writer;
        private BinaryReader reader;
        private int bytesLen;

        // 构造函数 - 写入模式
        public ByteBuffer(int len = 64)
        {
            bytesLen = len;
            stream = new MemoryStream(bytesLen);
            writer = new BinaryWriter(stream);
        }

        // 构造函数 - 读取模式
        public ByteBuffer(byte[] data)
        {
            bytesLen = data.Length;
            var readOnlyBuffer = new byte[data.Length];
            Array.Copy(data, readOnlyBuffer, data.Length);
            stream = new MemoryStream(readOnlyBuffer);
            reader = new BinaryReader(stream);
        }

        // 写入 int (4字节)
        public void WriteInt(int value)
        {
            writer.Write(value);
        }

        // 读取 int (4字节)
        public int ReadInt()
        {
            return reader.ReadInt32();
        }

        // 写入无符号 int (4字节)
        public void WriteUint(uint value)
        {
            writer.Write(value);
        }

        // 读取无符号 int (4字节)
        public uint ReadUint()
        {
            return reader.ReadUInt32();
        }

        // 写入 long (8字节)
        public void WriteLong(long value)
        {
            writer.Write(value);
        }

        // 读取 long (8字节)
        public long ReadLong()
        {
            return reader.ReadInt64();
        }

        // 写入字符串 (UTF-8编码，带长度前缀)
        public void WriteString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        // 读取字符串 (UTF-8编码，带长度前缀)
        public string ReadString()
        {
            int length = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        // 写入 byte 数组 (带长度前缀)
        public void WriteBytes(byte[] value)
        {
            writer.Write(value.Length);
            writer.Write(value);
        }

        // 读取 byte 数组 (带长度前缀)
        public byte[] ReadBytes()
        {
            int length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        // 获取写入的数据
        public byte[] ToArray()
        {
            writer.Flush();
            return stream.ToArray();
        }

        // 重置读取位置
        public void ResetPosition()
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        // 获取剩余可读字节数
        public int Remaining()
        {
            return (int)(stream.Length - stream.Position);
        }

        // 实现 IDisposable 接口
        public void Dispose()
        {
            writer?.Dispose();
            reader?.Dispose();
            stream?.Dispose();
        }

        // 清空缓冲区，准备重新使用
        public void Clear()
        {
            stream = new MemoryStream(bytesLen);
            writer = new BinaryWriter(stream);

            stream.SetLength(0);
            stream.Position = 0;
        }
    }
}
