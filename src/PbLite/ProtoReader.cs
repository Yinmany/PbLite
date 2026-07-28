using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace PbLite
{
    public ref struct ProtoReader
    {
        private SequenceReader<byte> _reader;

        public ProtoReader(ReadOnlySequence<byte> sequence)
        {
            _reader = new SequenceReader<byte>(sequence);
        }

        public bool End => _reader.End;

        #region VarInt
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadVarInt64()
        {
            ulong value = 0;
            int shift = 0;
            while (true)
            {
                if (!_reader.TryRead(out byte b))
                    throw new InvalidOperationException("Invalid varint");

                // 第 10 字节（shift==63）只有最低 1 位有效，恶意数据静默截断即可，不做防御性校验
                // if (shift == 63 && (b & 0x7E) != 0)
                //     throw new InvalidOperationException("Varint overflow");

                value |= ((ulong)(b & 0x7F)) << shift;

                if ((b & 0x80) == 0) return value;

                shift += 7;
                if (shift >= 64)
                    throw new InvalidOperationException("Varint overflow");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadVarInt32()
        {
            return (uint)ReadVarInt64();
        }
        #endregion

        #region Primitive

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            return (int)ReadVarInt64();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            return (long)ReadVarInt64();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            return ReadVarInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            return ReadVarInt64();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadSInt32()
        {
            uint value = ReadVarInt32();
            return (int)((value >> 1) ^ (uint)-(int)(value & 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadSInt64()
        {
            ulong value = ReadVarInt64();
            return (long)((value >> 1) ^ (ulong)-(long)(value & 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool()
        {
            return ReadVarInt32() != 0;
        }
        #endregion

        #region Fixed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadFixed32()
        {
            if (!_reader.TryReadLittleEndian(out int value))
                throw new InvalidOperationException();
            return (uint)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadFixed64()
        {
            if (!_reader.TryReadLittleEndian(out long value))
                throw new InvalidOperationException();
            return (ulong)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat()
        {
            return BitConverter.Int32BitsToSingle((int)ReadFixed32());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            return BitConverter.Int64BitsToDouble((long)ReadFixed64());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadSFixed32()
        {
            return (int)ReadFixed32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadSFixed64()
        {
            return (long)ReadFixed64();
        }

        #endregion

        #region LengthDelimited
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySequence<byte> ReadBytes()
        {
            int length = checked((int)ReadVarInt32());
            EnsureRemaining(length);
            SequencePosition start = _reader.Position;
            _reader.Advance(length);

            ReadOnlySequence<byte> result = _reader.Sequence.Slice(start, length);
            return result;
        }

        public string ReadString()
        {
            ReadOnlySequence<byte> bytes = ReadBytes();

            if (bytes.IsSingleSegment)
            {
                return Encoding.UTF8.GetString(bytes.FirstSpan);
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        #endregion

        #region Field
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadTag(out int fieldNumber, out WireType wireType)
        {
            uint tag = ReadVarInt32();
            fieldNumber = (int)(tag >> 3);
            wireType = (WireType)(tag & 7);
            return tag;
        }
        #endregion

        #region Skip
        public void SkipField(WireType wireType)
        {
            switch (wireType)
            {
                case WireType.Varint:
                ReadVarInt64();
                break;

                case WireType.Fixed64:
                EnsureRemaining(8);
                _reader.Advance(8);
                break;

                case WireType.LengthDelimited:
                int length =
                    checked((int)ReadVarInt32());
                EnsureRemaining(length);
                _reader.Advance(length);
                break;

                case WireType.Fixed32:
                EnsureRemaining(4);
                _reader.Advance(4);
                break;

                default:
                throw new NotSupportedException($"Unsupported wire type {wireType}");
            }
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureRemaining(int count)
        {
            if (_reader.Remaining < count)
                throw new InvalidOperationException($"Unexpected end of data: need {count} bytes, only {_reader.Remaining} remaining.");
        }
    }
}
