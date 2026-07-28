using System.Buffers;
using System.Collections.Generic;
using PbLite;

namespace PbLite.Tests
{
    public class SerializeDeserializeTests
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
        public void Scalars_RoundTrip()
        {
            var msg = new TestMessage
            {
                Int32Field = -12345,
                Int64Field = -9876543210L,
                UInt32Field = 42_000_000u,
                UInt64Field = 18_000_000_000UL,
                FloatField = 3.14f,
                DoubleField = 2.718281828459045,
                BoolField = true,
                StringField = "Hello ProtoLite"
            };

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal(-12345, result.Int32Field);
            Assert.Equal(-9876543210L, result.Int64Field);
            Assert.Equal(42_000_000u, result.UInt32Field);
            Assert.Equal(18_000_000_000UL, result.UInt64Field);
            Assert.Equal(3.14f, result.FloatField);
            Assert.Equal(2.718281828459045, result.DoubleField);
            Assert.True(result.BoolField);
            Assert.Equal("Hello ProtoLite", result.StringField);
        }

        [Fact]
        public void Nullable_Null_NotSerialized()
        {
            var msg = new TestMessage();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);
            int written = writer.WrittenCount;

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Null(result.NullableInt);
            Assert.Null(result.NullableFloat);
            Assert.Null(result.NullableBool);
            // nullable 字段为 null 时不写入，只有 StringField("") 和空集合占少量字节
            Assert.True(written < 30);
        }

        [Fact]
        public void Nullable_WithValue_RoundTrip()
        {
            var msg = new TestMessage
            {
                NullableInt = 99,
                NullableFloat = 1.5f,
                NullableBool = false
            };

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal(99, result.NullableInt);
            Assert.Equal(1.5f, result.NullableFloat);
            Assert.False(result.NullableBool);
        }

        [Fact]
        public void NestedMessage_RoundTrip()
        {
            var msg = new TestMessage
            {
                Nested = new InnerMessage { Id = 777, Name = "Nested" }
            };

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.NotNull(result.Nested);
            Assert.Equal(777, result.Nested.Id);
            Assert.Equal("Nested", result.Nested.Name);
        }

        [Fact]
        public void IntList_RoundTrip()
        {
            var msg = new TestMessage
            {
                IntList = { 1, 2, 3, 100, -50 }
            };

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal(new[] { 1, 2, 3, 100, -50 }, result.IntList);
        }

        [Fact]
        public void StringList_RoundTrip()
        {
            var msg = new TestMessage
            {
                StringList = { "a", "bb", "ccc" }
            };

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal(new[] { "a", "bb", "ccc" }, result.StringList);
        }

        [Fact]
        public void MessageList_RoundTrip()
        {
            var msg = new TestMessage
            {
                MessageList =
                {
                    new InnerMessage { Id = 1, Name = "first" },
                    new InnerMessage { Id = 2, Name = "second" }
                }
            };

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal(2, result.MessageList.Count);
            Assert.Equal(1, result.MessageList[0].Id);
            Assert.Equal("first", result.MessageList[0].Name);
            Assert.Equal(2, result.MessageList[1].Id);
            Assert.Equal("second", result.MessageList[1].Name);
        }

        [Fact]
        public void IntStringMap_RoundTrip()
        {
            var msg = new TestMessage();
            msg.IntStringMap[10] = "ten";
            msg.IntStringMap[20] = "twenty";

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal(2, result.IntStringMap.Count);
            Assert.Equal("ten", result.IntStringMap[10]);
            Assert.Equal("twenty", result.IntStringMap[20]);
        }

        [Fact]
        public void StringIntMap_RoundTrip()
        {
            var msg = new TestMessage();
            msg.StringIntMap["a"] = 1;
            msg.StringIntMap["b"] = 2;

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal(2, result.StringIntMap.Count);
            Assert.Equal(1, result.StringIntMap["a"]);
            Assert.Equal(2, result.StringIntMap["b"]);
        }

        [Fact]
        public void AllFields_RoundTrip()
        {
            var msg = new TestMessage
            {
                Int32Field = int.MaxValue,
                Int64Field = long.MinValue,
                UInt32Field = uint.MaxValue,
                UInt64Field = ulong.MaxValue,
                FloatField = float.MinValue,
                DoubleField = double.Epsilon,
                BoolField = true,
                StringField = "全面测试 🎮",
                NullableInt = int.MinValue,
                NullableFloat = float.NaN,
                NullableBool = true,
                Nested = new InnerMessage { Id = 42, Name = "嵌套" },
                IntList = { -1, 0, 1 },
                StringList = { "", "test" },
                MessageList = { new InnerMessage { Id = 100, Name = "msg" } }
            };
            msg.IntStringMap[1] = "one";
            msg.IntStringMap[2] = "two";
            msg.StringIntMap["x"] = 999;

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal(int.MaxValue, result.Int32Field);
            Assert.Equal(long.MinValue, result.Int64Field);
            Assert.Equal(uint.MaxValue, result.UInt32Field);
            Assert.Equal(ulong.MaxValue, result.UInt64Field);
            Assert.Equal(float.MinValue, result.FloatField);
            Assert.Equal(double.Epsilon, result.DoubleField);
            Assert.True(result.BoolField);
            Assert.Equal("全面测试 🎮", result.StringField);
            Assert.Equal(int.MinValue, result.NullableInt);
            Assert.Equal(float.NaN, result.NullableFloat);
            Assert.True(result.NullableBool);
            Assert.Equal(42, result.Nested.Id);
            Assert.Equal("嵌套", result.Nested.Name);
            Assert.Equal(new[] { -1, 0, 1 }, result.IntList);
            Assert.Equal(new[] { "", "test" }, result.StringList);
            Assert.Single(result.MessageList);
            Assert.Equal(100, result.MessageList[0].Id);
            Assert.Equal("msg", result.MessageList[0].Name);
            Assert.Equal("one", result.IntStringMap[1]);
            Assert.Equal("two", result.IntStringMap[2]);
            Assert.Equal(999, result.StringIntMap["x"]);
        }

        [Fact]
        public void EmptyString_RoundTrip()
        {
            var msg = new TestMessage { StringField = "" };

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Equal("", result.StringField);
        }

        [Fact]
        public void EmptyCollections_RoundTrip()
        {
            var msg = new TestMessage();

            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);

            Assert.Empty(result.IntList);
            Assert.Empty(result.StringList);
            Assert.Empty(result.MessageList);
            Assert.Empty(result.IntStringMap);
            Assert.Empty(result.StringIntMap);
        }

        [Fact]
        public void NestedClass_Scalars_RoundTrip()
        {
            var msg = new OuterContainer.NestedMessage
            {
                Id = 42,
                Name = "nested"
            };

            var (result, _) = RoundTrip(OuterContainer_NestedMessageSerializer.Instance, msg);

            Assert.Equal(42, result.Id);
            Assert.Equal("nested", result.Name);
            Assert.Null(result.Inner);
        }

        [Fact]
        public void NestedClass_WithInnerMessage_RoundTrip()
        {
            var msg = new OuterContainer.NestedMessage
            {
                Id = 1,
                Name = "outer",
                Inner = new InnerMessage { Id = 2, Name = "inner" },
                Numbers = { 10, 20, 30 }
            };

            var (result, _) = RoundTrip(OuterContainer_NestedMessageSerializer.Instance, msg);

            Assert.Equal(1, result.Id);
            Assert.Equal("outer", result.Name);
            Assert.NotNull(result.Inner);
            Assert.Equal(2, result.Inner.Id);
            Assert.Equal("inner", result.Inner.Name);
            Assert.Equal(new[] { 10, 20, 30 }, result.Numbers);
        }

        [Fact]
        public void DeepNestedClass_RoundTrip()
        {
            var msg = new OuterContainer.MiddleLevel.DeepNestedMessage
            {
                Value = 999,
                Text = "deep"
            };

            var (result, _) = RoundTrip(
                OuterContainer_MiddleLevel_DeepNestedMessageSerializer.Instance, msg);

            Assert.Equal(999, result.Value);
            Assert.Equal("deep", result.Text);
        }
    }
}
