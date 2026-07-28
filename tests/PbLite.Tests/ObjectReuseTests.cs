using System.Buffers;
using PbLite;

namespace PbLite.Tests
{
    public class ObjectReuseTests
    {
        [Fact]
        public void Deserialize_IntoExistingObject_OverwritesFields()
        {
            var existing = new TestMessage
            {
                Int32Field = 111,
                StringField = "old",
                IntList = { 999 },
                Nested = new InnerMessage { Id = 1, Name = "old" }
            };

            var msg = new TestMessage
            {
                Int32Field = 222,
                StringField = "new"
            };

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, existing);

            Assert.Same(existing, result);
            Assert.Equal(222, result.Int32Field);
            Assert.Equal("new", result.StringField);
        }

        [Fact]
        public void Deserialize_IntoExistingObject_NestedReused()
        {
            var existingNested = new InnerMessage { Id = 0, Name = "old" };
            var existing = new TestMessage { Nested = existingNested };

            var msg = new TestMessage
            {
                Nested = new InnerMessage { Id = 5, Name = "updated" }
            };

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, existing);

            Assert.NotNull(result.Nested);
            Assert.Equal(5, result.Nested.Id);
            Assert.Equal("updated", result.Nested.Name);
        }

        [Fact]
        public void Deserialize_NullValue_CreatesNew()
        {
            var msg = new TestMessage { Int32Field = 42 };

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.NotNull(result);
            Assert.Equal(42, result.Int32Field);
        }
    }
}
