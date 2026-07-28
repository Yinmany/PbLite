using System.Buffers;
using PbLite;

namespace PbLite.Tests
{
    public class CollectionEdgeTests
    {
        private static (T result, int bytes) RoundTrip<T>(IProtoSerializer serializer, T value)
        {
            var writer = new ArrayBufferWriter<byte>();
            int written = serializer.Serialize(writer, value!);
            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (T)serializer.Deserialize(ref reader, null);
            return (result, written);
        }

        [Fact]
        public void EmptyPackedList_NotSerialized()
        {
            var msg = new CollectionOnlyMessage { IntList = new() };

            var writer = new ArrayBufferWriter<byte>();
            int written = CollectionOnlyMessageSerializer.Instance.Serialize(writer, msg);

            Assert.Equal(0, written);
        }

        [Fact]
        public void EmptyUnpackedList_NotSerialized()
        {
            var msg = new CollectionOnlyMessage { StringList = new() };

            var writer = new ArrayBufferWriter<byte>();
            int written = CollectionOnlyMessageSerializer.Instance.Serialize(writer, msg);

            Assert.Equal(0, written);
        }

        [Fact]
        public void Map_Empty_NotSerialized()
        {
            var msg = new CollectionOnlyMessage { IntStringMap = new() };

            var writer = new ArrayBufferWriter<byte>();
            int written = CollectionOnlyMessageSerializer.Instance.Serialize(writer, msg);

            Assert.Equal(0, written);
        }

        [Fact]
        public void Map_DuplicateKey_LastWins()
        {
            // 用两个 entry 写同一 key，验证后者覆盖前者
            var msg = new CollectionOnlyMessage();
            msg.IntStringMap[1] = "first";
            msg.IntStringMap[1] = "second"; // 同 key 覆盖

            var writer = new ArrayBufferWriter<byte>();
            int written = CollectionOnlyMessageSerializer.Instance.Serialize(writer, msg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (CollectionOnlyMessage)CollectionOnlyMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Single(result.IntStringMap);
            Assert.Equal("second", result.IntStringMap[1]);
        }

        [Fact]
        public void PackedList_WithZeroValues_RoundTrip()
        {
            var msg = new CollectionOnlyMessage { IntList = { 0, 0, 0 } };

            var (result, _) = RoundTrip(CollectionOnlyMessageSerializer.Instance, msg);

            Assert.Equal(new[] { 0, 0, 0 }, result.IntList);
        }

        [Fact]
        public void LargePackedList_RoundTrip()
        {
            var msg = new CollectionOnlyMessage();
            for (int i = 0; i < 1000; i++)
                msg.IntList.Add(i);

            // 预分配足够大的 buffer，避免 ArrayBufferWriter 扩容导致 span 失效
            var writer = new ArrayBufferWriter<byte>(8192);
            CollectionOnlyMessageSerializer.Instance.Serialize(writer, msg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (CollectionOnlyMessage)CollectionOnlyMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(1000, result.IntList.Count);
            Assert.Equal(0, result.IntList[0]);
            Assert.Equal(999, result.IntList[999]);
        }
    }
}
