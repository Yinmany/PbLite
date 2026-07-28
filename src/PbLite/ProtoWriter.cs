using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace PbLite
{
    public static class ProtoWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteTag<T>(T writer, int fieldNumber, WireType wireType) where T : IBufferWriter<byte>
        {
            return WriteVarInt32(writer, ProtoWire.MakeTag(fieldNumber, wireType));
        }

        #region Primitive

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt32<T>(T writer, int fieldNumber, int value) where T : IBufferWriter<byte>
        {
            int written = WriteTag(writer, fieldNumber, WireType.Varint);
            if (value < 0)
                written += WriteVarInt64(writer, (ulong)value);
            else
                written += WriteVarInt32(writer, (uint)value);
            return written;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt32<T>(T writer, int fieldNumber, uint value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Varint) + WriteVarInt32(writer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt64<T>(T writer, int fieldNumber, long value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Varint) + WriteVarInt64(writer, (ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt64<T>(T writer, int fieldNumber, ulong value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Varint) + WriteVarInt64(writer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteSInt32<T>(T writer, int fieldNumber, int value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Varint) + WriteVarInt32(writer, ZigZag32(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteSInt64<T>(T writer, int fieldNumber, long value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Varint) + WriteVarInt64(writer, ZigZag64(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteBool<T>(T writer, int fieldNumber, bool value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Varint) + WriteVarInt32(writer, value ? 1u : 0u);
        }
        #endregion

        #region Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteFixed32<T>(T writer, int fieldNumber, int value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Fixed32) + WriteFixed32(writer, (uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteSFixed32<T>(T writer, int fieldNumber, int value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.SFixed32) + WriteFixed32(writer, (uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteFixed64<T>(T writer, int fieldNumber, long value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Fixed64) + WriteFixed64(writer, (ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteSFixed64<T>(T writer, int fieldNumber, long value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.SFixed64) + WriteFixed64(writer, (ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteFloat<T>(T writer, int fieldNumber, float value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Float) + WriteFixed32(writer, (uint)BitConverter.SingleToInt32Bits(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteDouble<T>(T writer, int fieldNumber, double value) where T : IBufferWriter<byte>
        {
            return WriteTag(writer, fieldNumber, WireType.Double) + WriteFixed64(writer, (ulong)BitConverter.DoubleToInt64Bits(value));
        }
        #endregion

        #region LengthDelimited
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteString<T>(T writer, int fieldNumber, string value) where T : IBufferWriter<byte>
        {
            int written = WriteTag(writer, fieldNumber, WireType.LengthDelimited);
            int byteCount = Encoding.UTF8.GetByteCount(value);
            written += WriteVarInt32(writer, (uint)byteCount);

            Span<byte> span = writer.GetSpan(byteCount);
            if (span.Length >= byteCount)
            {
                int w = Encoding.UTF8.GetBytes(value, span);
                writer.Advance(w);
                return written + w;
            }

            return written + WriteStringSlow(writer, value);
        }

        private static int WriteStringSlow<T>(T writer, string value) where T : IBufferWriter<byte>
        {
            int total = 0;
            Encoder encoder = Encoding.UTF8.GetEncoder();
            ReadOnlySpan<char> chars = value.AsSpan();
            while (!chars.IsEmpty)
            {
                Span<byte> bytes = writer.GetSpan(4);
                encoder.Convert(chars, bytes, false, out int charsUsed, out int bytesUsed, out _);
                if (bytesUsed == 0 && charsUsed == 0)
                    throw new InvalidOperationException("IBufferWriter returned insufficient buffer.");
                writer.Advance(bytesUsed);
                total += bytesUsed;
                chars = chars[charsUsed..];
            }
            return total;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteBytes<T>(T writer, int fieldNumber, ReadOnlySpan<byte> value) where T : IBufferWriter<byte>
        {
            int written = WriteTag(writer, fieldNumber, WireType.LengthDelimited);
            written += WriteVarInt32(writer, (uint)value.Length);

            while (!value.IsEmpty)
            {
                Span<byte> dst = writer.GetSpan();
                int copyCount = Math.Min(dst.Length, value.Length);
                value[..copyCount].CopyTo(dst);
                writer.Advance(copyCount);
                written += copyCount;
                value = value[copyCount..];
            }
            return written;
        }

        /// <summary>
        /// 将 payload 长度回填到 5 字节固定 varint 中。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillFixedVarInt(Span<byte> span, int value)
        {
            span[0] = (byte)((value & 0x7F) | 0x80);
            span[1] = (byte)(((value >> 7) & 0x7F) | 0x80);
            span[2] = (byte)(((value >> 14) & 0x7F) | 0x80);
            span[3] = (byte)(((value >> 21) & 0x7F) | 0x80);
            span[4] = (byte)((value >> 28) & 0x7F);
        }

        #endregion

        #region 辅助方法
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ZigZag32(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ZigZag64(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteVarInt64<T>(T writer, ulong value) where T : IBufferWriter<byte>
        {
            Span<byte> span = writer.GetSpan(10);
            int offset = 0;
            while (value >= 0x80)
            {
                span[offset++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            }
            span[offset++] = (byte)value;
            writer.Advance(offset);
            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteVarInt32<T>(T writer, uint value) where T : IBufferWriter<byte>
        {
            Span<byte> span = writer.GetSpan(5);
            int offset = 0;
            while (value >= 0x80)
            {
                span[offset++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            }
            span[offset++] = (byte)value;
            writer.Advance(offset);
            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteFixed32<T>(T writer, uint value) where T : IBufferWriter<byte>
        {
            Span<byte> span = writer.GetSpan(4);
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            span[3] = (byte)(value >> 24);
            writer.Advance(4);
            return 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteFixed64<T>(T writer, ulong value) where T : IBufferWriter<byte>
        {
            Span<byte> span = writer.GetSpan(8);
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            span[3] = (byte)(value >> 24);
            span[4] = (byte)(value >> 32);
            span[5] = (byte)(value >> 40);
            span[6] = (byte)(value >> 48);
            span[7] = (byte)(value >> 56);
            writer.Advance(8);
            return 8;
        }
        #endregion
    }
}
