using System.Buffers;
using System.Collections.Generic;
using PbLite;

namespace PbLite.Tests
{
    public enum TestColor
    {
        Red = 0,
        Green = 1,
        Blue = 2,
    }

    [PbContract]
    public class EnumBytesMessage
    {
        [PbMember(1)] public TestColor Color { get; set; }
        [PbMember(2)] public byte[] Data { get; set; } = System.Array.Empty<byte>();
        [PbMember(3)] public TestColor? OptionalColor { get; set; }
        [PbMember(4)] public byte[]? OptionalData { get; set; }
        [PbMember(5)] public List<TestColor> ColorList { get; set; } = new();
        [PbMember(6)] public List<byte[]> DataList { get; set; } = new();
        [PbMember(7)] public Dictionary<TestColor, string> ColorStringMap { get; set; } = new();
    }

    public class EnumBytesTests
    {
        private static (T result, int bytes) RoundTrip<T>(IProtoSerializer serializer, T value)
        {
            var writer = new ArrayBufferWriter<byte>();
            serializer.Serialize(writer, value!);
            int written = writer.WrittenCount;

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (T)serializer.Deserialize(ref reader, null);
            return (result, written);
        }

        [Fact]
        public void Enum_RoundTrip()
        {
            var msg = new EnumBytesMessage
            {
                Color = TestColor.Blue,
                Data = new byte[] { 1, 2, 3, 4, 5 },
            };
            var (result, _) = RoundTrip(EnumBytesMessageSerializer.Instance, msg);

            Assert.Equal(TestColor.Blue, result.Color);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, result.Data);
        }

        [Fact]
        public void Enum_Default_NotSerialized()
        {
            var msg = new EnumBytesMessage
            {
                Color = TestColor.Red, // default
                Data = new byte[] { 1 },
            };
            var (_, written) = RoundTrip(EnumBytesMessageSerializer.Instance, msg);

            // Only Data field should be serialized (field 2, tag + length + 1 byte = 3 bytes)
            Assert.Equal(3, written);
        }

        [Fact]
        public void OptionalEnum_RoundTrip()
        {
            var msg = new EnumBytesMessage
            {
                OptionalColor = TestColor.Green,
            };
            var (result, _) = RoundTrip(EnumBytesMessageSerializer.Instance, msg);

            Assert.Equal(TestColor.Green, result.OptionalColor);
        }

        [Fact]
        public void OptionalEnum_Null_NotSerialized()
        {
            var msg = new EnumBytesMessage
            {
                OptionalColor = null,
            };
            var (_, written) = RoundTrip(EnumBytesMessageSerializer.Instance, msg);

            Assert.Equal(0, written);
        }

        [Fact]
        public void Bytes_Empty_NotSerialized()
        {
            var msg = new EnumBytesMessage
            {
                Data = System.Array.Empty<byte>(),
            };
            var (_, written) = RoundTrip(EnumBytesMessageSerializer.Instance, msg);

            Assert.Equal(0, written);
        }

        [Fact]
        public void EnumList_Packed_RoundTrip()
        {
            var msg = new EnumBytesMessage
            {
                ColorList = new List<TestColor> { TestColor.Red, TestColor.Green, TestColor.Blue },
            };
            var (result, _) = RoundTrip(EnumBytesMessageSerializer.Instance, msg);

            Assert.Equal(3, result.ColorList.Count);
            Assert.Equal(TestColor.Red, result.ColorList[0]);
            Assert.Equal(TestColor.Green, result.ColorList[1]);
            Assert.Equal(TestColor.Blue, result.ColorList[2]);
        }

        [Fact]
        public void BytesList_RoundTrip()
        {
            var msg = new EnumBytesMessage
            {
                DataList = new List<byte[]>
                {
                    new byte[] { 0xDE, 0xAD },
                    new byte[] { 0xBE, 0xEF, 0x00 },
                },
            };
            var (result, _) = RoundTrip(EnumBytesMessageSerializer.Instance, msg);

            Assert.Equal(2, result.DataList.Count);
            Assert.Equal(new byte[] { 0xDE, 0xAD }, result.DataList[0]);
            Assert.Equal(new byte[] { 0xBE, 0xEF, 0x00 }, result.DataList[1]);
        }

        [Fact]
        public void EnumStringMap_RoundTrip()
        {
            var msg = new EnumBytesMessage
            {
                ColorStringMap = new Dictionary<TestColor, string>
                {
                    [TestColor.Red] = "r",
                    [TestColor.Blue] = "b",
                },
            };
            var (result, _) = RoundTrip(EnumBytesMessageSerializer.Instance, msg);

            Assert.Equal(2, result.ColorStringMap.Count);
            Assert.Equal("r", result.ColorStringMap[TestColor.Red]);
            Assert.Equal("b", result.ColorStringMap[TestColor.Blue]);
        }
    }
}
