using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Core.Util;

public static class SpanExtension
{
    // ========== 写入方法 ==========

    /// <summary>写入字节</summary>
    public static void WriteByte(this Span<byte> span, byte value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(byte));
        span[offset++] = value;
    }

    /// <summary>写入字节数组</summary>
    public static void WriteBytes(this Span<byte> span, ReadOnlySpan<byte> value, ref int offset)
    {
        CheckSpace(span, ref offset, value.Length);
        value.CopyTo(span.Slice(offset));
        offset += value.Length;
    }

    /// <summary>写入Int16(小端序)</summary>
    public static void WriteInt16(this Span<byte> span, short value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(offset), value);
        offset += sizeof(short);
    }

    /// <summary>写入UInt16(小端序)</summary>
    public static void WriteUInt16(this Span<byte> span, ushort value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset), value);
        offset += sizeof(ushort);
    }

    /// <summary>写入Int32(小端序)</summary>
    public static void WriteInt32(this Span<byte> span, int value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset), value);
        offset += sizeof(int);
    }

    /// <summary>写入UInt32(小端序)</summary>
    public static void WriteUInt32(this Span<byte> span, uint value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset), value);
        offset += sizeof(uint);
    }

    /// <summary>写入Int64(小端序)</summary>
    public static void WriteInt64(this Span<byte> span, long value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(offset), value);
        offset += sizeof(long);
    }

    /// <summary>写入UInt64(小端序)</summary>
    public static void WriteUInt64(this Span<byte> span, ulong value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(ulong));
        BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(offset), value);
        offset += sizeof(ulong);
    }

    /// <summary>写入Float(小端序)</summary>
    public static void WriteFloat(this Span<byte> span, float value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(float));
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(offset), value);
        offset += sizeof(float);
    }

    /// <summary>写入Double(小端序)</summary>
    public static void WriteDouble(this Span<byte> span, double value, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(double));
        BinaryPrimitives.WriteDoubleLittleEndian(span.Slice(offset), value);
        offset += sizeof(double);
    }

    /// <summary>写入字符串(UTF8编码，不带长度前缀)</summary>
    public static void WriteString(this Span<byte> span, string value, ref int offset)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        CheckSpace(span, ref offset, byteCount);
        Encoding.UTF8.GetBytes(value, span.Slice(offset));
        offset += byteCount;
    }

    /// <summary>写入字符串(UTF8编码，带长度前缀)</summary>
    public static void WriteStringWithLength(this Span<byte> span, string value, ref int offset)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        CheckSpace(span, ref offset, sizeof(ushort) + byteCount);

        // 先写入长度(ushort)
        span.WriteUInt16((ushort)byteCount, ref offset);

        // 再写入字符串内容
        Encoding.UTF8.GetBytes(value, span.Slice(offset));
        offset += byteCount;
    }

    // ========== 读取方法 ==========

    /// <summary>读取字节</summary>
    public static byte ReadByte(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(byte));
        return span[offset++];
    }

    /// <summary>读取字节数组</summary>
    public static ReadOnlySpan<byte> ReadBytes(this ReadOnlySpan<byte> span, int length, ref int offset)
    {
        CheckSpace(span, ref offset, length);
        var result = span.Slice(offset, length);
        offset += length;
        return result;
    }

    /// <summary>读取Int16(小端序)</summary>
    public static short ReadInt16(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(short));
        var value = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset));
        offset += sizeof(short);
        return value;
    }

    /// <summary>读取UInt16(小端序)</summary>
    public static ushort ReadUInt16(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(ushort));
        var value = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset));
        offset += sizeof(ushort);
        return value;
    }

    /// <summary>读取Int32(小端序)</summary>
    public static int ReadInt32(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(int));
        var value = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset));
        offset += sizeof(int);
        return value;
    }

    /// <summary>读取UInt32(小端序)</summary>
    public static uint ReadUInt32(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(uint));
        var value = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset));
        offset += sizeof(uint);
        return value;
    }

    /// <summary>读取Int64(小端序)</summary>
    public static long ReadInt64(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(long));
        var value = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset));
        offset += sizeof(long);
        return value;
    }

    /// <summary>读取UInt64(小端序)</summary>
    public static ulong ReadUInt64(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(ulong));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(offset));
        offset += sizeof(ulong);
        return value;
    }

    /// <summary>读取Float(小端序)</summary>
    public static float ReadFloat(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(float));
        var value = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset));
        offset += sizeof(float);
        return value;
    }

    /// <summary>读取Double(小端序)</summary>
    public static double ReadDouble(this ReadOnlySpan<byte> span, ref int offset)
    {
        CheckSpace(span, ref offset, sizeof(double));
        var value = BinaryPrimitives.ReadDoubleLittleEndian(span.Slice(offset));
        offset += sizeof(double);
        return value;
    }

    /// <summary>读取字符串(UTF8编码，不带长度前缀)</summary>
    public static string ReadString(this ReadOnlySpan<byte> span, int length, ref int offset)
    {
        CheckSpace(span, ref offset, length);
        var value = Encoding.UTF8.GetString(span.Slice(offset, length));
        offset += length;
        return value;
    }

    /// <summary>读取字符串(UTF8编码，带长度前缀)</summary>
    public static string ReadStringWithLength(this ReadOnlySpan<byte> span, ref int offset)
    {
        // 先读取长度(ushort)
        ushort length = span.ReadUInt16(ref offset);

        // 再读取字符串内容
        return span.ReadString(length, ref offset);
    }

    // ========== 辅助方法 ==========

    private static void CheckSpace(Span<byte> span, ref int offset, int required)
    {
        if (offset < 0 || span.Length - offset < required)
            throw new ArgumentOutOfRangeException(nameof(offset),
                $"空间不足: 需要 {required} 字节 (当前: {offset}, 剩余: {span.Length - offset})");
    }

    private static void CheckSpace(ReadOnlySpan<byte> span, ref int offset, int required)
    {
        if (offset < 0 || span.Length - offset < required)
            throw new ArgumentOutOfRangeException(nameof(offset),
                $"空间不足: 需要 {required} 字节 (当前: {offset}, 剩余: {span.Length - offset})");
    }
}