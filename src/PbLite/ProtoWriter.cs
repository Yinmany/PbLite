using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace PbLite
{
    public static class ProtoWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTag<T>(T writer, int fieldNumber, WireType wireType) where T : IBufferWriter<byte>
        {
            WriteVarInt32(writer, ProtoWire.MakeTag(fieldNumber, wireType));
        }

        #region Primitive

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32<T>(T writer, int fieldNumber, int value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Varint);
            if (value < 0)
                WriteVarInt64(writer, (ulong)value);
            else
                WriteVarInt32(writer, (uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32<T>(T writer, int fieldNumber, uint value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Varint);
            WriteVarInt32(writer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64<T>(T writer, int fieldNumber, long value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Varint);
            WriteVarInt64(writer, (ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64<T>(T writer, int fieldNumber, ulong value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Varint);
            WriteVarInt64(writer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSInt32<T>(T writer, int fieldNumber, int value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Varint);
            WriteVarInt32(writer, ZigZag32(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSInt64<T>(T writer, int fieldNumber, long value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Varint);
            WriteVarInt64(writer, ZigZag64(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool<T>(T writer, int fieldNumber, bool value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Varint);
            WriteVarInt32(writer, value ? 1u : 0u);
        }
        #endregion

        #region Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFixed32<T>(T writer, int fieldNumber, int value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Fixed32);
            WriteFixed32(writer, (uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSFixed32<T>(T writer, int fieldNumber, int value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.SFixed32);
            WriteFixed32(writer, (uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFixed64<T>(T writer, int fieldNumber, long value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Fixed64);
            WriteFixed64(writer, (ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSFixed64<T>(T writer, int fieldNumber, long value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.SFixed64);
            WriteFixed64(writer, (ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat<T>(T writer, int fieldNumber, float value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Float);
            WriteFixed32(writer, (uint)BitConverter.SingleToInt32Bits(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble<T>(T writer, int fieldNumber, double value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.Double);
            WriteFixed64(writer, (ulong)BitConverter.DoubleToInt64Bits(value));
        }
        #endregion

        #region LengthDelimited
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString<T>(T writer, int fieldNumber, string value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.LengthDelimited);
            int byteCount = Encoding.UTF8.GetByteCount(value);
            WriteVarInt32(writer, (uint)byteCount);

            Span<byte> span = writer.GetSpan(byteCount);
            if (span.Length >= byteCount)
            {
                Encoding.UTF8.GetBytes(value, span);
                writer.Advance(byteCount);
                return;
            }

            WriteStringSlow(writer, value);
        }

        private static void WriteStringSlow<T>(T writer, string value) where T : IBufferWriter<byte>
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            ReadOnlySpan<char> chars = value.AsSpan();
            while (!chars.IsEmpty)
            {
                Span<byte> bytes = writer.GetSpan(4);
                encoder.Convert(chars, bytes, false, out int charsUsed, out int bytesUsed, out _);
                if (bytesUsed == 0 && charsUsed == 0)
                    throw new InvalidOperationException("IBufferWriter returned insufficient buffer.");
                writer.Advance(bytesUsed);
                chars = chars[charsUsed..];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes<T>(T writer, int fieldNumber, ReadOnlySpan<byte> value) where T : IBufferWriter<byte>
        {
            WriteTag(writer, fieldNumber, WireType.LengthDelimited);
            WriteVarInt32(writer, (uint)value.Length);

            while (!value.IsEmpty)
            {
                Span<byte> dst = writer.GetSpan();
                int copyCount = Math.Min(dst.Length, value.Length);
                value[..copyCount].CopyTo(dst);
                writer.Advance(copyCount);
                value = value[copyCount..];
            }
        }

        #endregion

        #region Size 辅助方法
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetVarInt32Size(uint value)
        {
            int size = 1;
            while (value >= 0x80) { size++; value >>= 7; }
            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetVarInt64Size(ulong value)
        {
            int size = 1;
            while (value >= 0x80) { size++; value >>= 7; }
            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTagSize(int fieldNumber, WireType wireType)
        {
            return GetVarInt32Size(ProtoWire.MakeTag(fieldNumber, wireType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetStringSize(string value)
        {
            return Encoding.UTF8.GetByteCount(value);
        }
        #endregion

        #region 辅助方法
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ZigZag32(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ZigZag64(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt64<T>(T writer, ulong value) where T : IBufferWriter<byte>
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt32<T>(T writer, uint value) where T : IBufferWriter<byte>
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFixed32<T>(T writer, uint value) where T : IBufferWriter<byte>
        {
            Span<byte> span = writer.GetSpan(4);
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            span[3] = (byte)(value >> 24);
            writer.Advance(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFixed64<T>(T writer, ulong value) where T : IBufferWriter<byte>
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
        }
        #endregion
    }
}
