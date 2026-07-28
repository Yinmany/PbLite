using System.Buffers;
using System.Collections.Generic;
using PbLite;
using ProtoBuf;

namespace PbLite.Tests.CrossTest
{
    /// <summary>
    /// 交叉兼容测试：ProtoLite 序列化 → protobuf-net 反序列化，以及反向。
    /// </summary>
    public class CrossCompatibilityTests
    {
        private static TestMessage MakeSample()
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
                StringField = "Cross Test",
                NullableInt = 99,
                NullableFloat = 1.5f,
                NullableBool = false,
                Nested = new InnerMessage { Id = 777, Name = "Nested" },
                IntList = { 1, 2, 3, 100, -50 },
                StringList = { "a", "bb", "ccc" },
                MessageList =
                {
                    new InnerMessage { Id = 1, Name = "first" },
                    new InnerMessage { Id = 2, Name = "second" }
                }
            };
            msg.IntStringMap[10] = "ten";
            msg.IntStringMap[20] = "twenty";
            msg.StringIntMap["x"] = 999;
            msg.StringIntMap["y"] = 0;
            return msg;
        }

        // ---- ProtoLite 写 → protobuf-net 读 ----

        [Fact]
        public void ProtoLite_To_ProtobufNet_Scalars()
        {
            var msg = MakeSample();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var pbResult = Serializer.Deserialize<PbTestMessage>(
                new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));

            Assert.Equal(-12345, pbResult.Int32Field);
            Assert.Equal(-9876543210L, pbResult.Int64Field);
            Assert.Equal(42_000_000u, pbResult.UInt32Field);
            Assert.Equal(18_000_000_000UL, pbResult.UInt64Field);
            Assert.Equal(3.14f, pbResult.FloatField);
            Assert.Equal(2.718281828459045, pbResult.DoubleField);
            Assert.True(pbResult.BoolField);
            Assert.Equal("Cross Test", pbResult.StringField);
        }

        [Fact]
        public void ProtoLite_To_ProtobufNet_Nullable()
        {
            var msg = MakeSample();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var pbResult = Serializer.Deserialize<PbTestMessage>(
                new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));

            Assert.Equal(99, pbResult.NullableInt);
            Assert.Equal(1.5f, pbResult.NullableFloat);
            Assert.False(pbResult.NullableBool);
        }

        [Fact]
        public void ProtoLite_To_ProtobufNet_Nested()
        {
            var msg = MakeSample();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var pbResult = Serializer.Deserialize<PbTestMessage>(
                new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));

            Assert.NotNull(pbResult.Nested);
            Assert.Equal(777, pbResult.Nested.Id);
            Assert.Equal("Nested", pbResult.Nested.Name);
        }

        [Fact]
        public void ProtoLite_To_ProtobufNet_PackedList()
        {
            var msg = MakeSample();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var pbResult = Serializer.Deserialize<PbTestMessage>(
                new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));

            Assert.Equal(new[] { 1, 2, 3, 100, -50 }, pbResult.IntList);
        }

        [Fact]
        public void ProtoLite_To_ProtobufNet_StringList()
        {
            var msg = MakeSample();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var pbResult = Serializer.Deserialize<PbTestMessage>(
                new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));

            Assert.Equal(new[] { "a", "bb", "ccc" }, pbResult.StringList);
        }

        [Fact]
        public void ProtoLite_To_ProtobufNet_MessageList()
        {
            var msg = MakeSample();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var pbResult = Serializer.Deserialize<PbTestMessage>(
                new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));

            Assert.Equal(2, pbResult.MessageList.Count);
            Assert.Equal(1, pbResult.MessageList[0].Id);
            Assert.Equal("first", pbResult.MessageList[0].Name);
            Assert.Equal(2, pbResult.MessageList[1].Id);
            Assert.Equal("second", pbResult.MessageList[1].Name);
        }

        [Fact]
        public void ProtoLite_To_ProtobufNet_Maps()
        {
            var msg = MakeSample();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);

            var pbResult = Serializer.Deserialize<PbTestMessage>(
                new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));

            Assert.Equal(2, pbResult.IntStringMap.Count);
            Assert.Equal("ten", pbResult.IntStringMap[10]);
            Assert.Equal("twenty", pbResult.IntStringMap[20]);

            Assert.Equal(2, pbResult.StringIntMap.Count);
            Assert.Equal(999, pbResult.StringIntMap["x"]);
            Assert.Equal(0, pbResult.StringIntMap["y"]);
        }

        // ---- protobuf-net 写 → ProtoLite 读 ----

        [Fact]
        public void ProtobufNet_To_ProtoLite_Scalars()
        {
            var pbMsg = new PbTestMessage
            {
                Int32Field = -12345,
                Int64Field = -9876543210L,
                UInt32Field = 42_000_000u,
                UInt64Field = 18_000_000_000UL,
                FloatField = 3.14f,
                DoubleField = 2.718281828459045,
                BoolField = true,
                StringField = "Reverse Test"
            };

            using var ms = new System.IO.MemoryStream();
            Serializer.Serialize(ms, pbMsg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(ms.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(-12345, result.Int32Field);
            Assert.Equal(-9876543210L, result.Int64Field);
            Assert.Equal(42_000_000u, result.UInt32Field);
            Assert.Equal(18_000_000_000UL, result.UInt64Field);
            Assert.Equal(3.14f, result.FloatField);
            Assert.Equal(2.718281828459045, result.DoubleField);
            Assert.True(result.BoolField);
            Assert.Equal("Reverse Test", result.StringField);
        }

        [Fact]
        public void ProtobufNet_To_ProtoLite_Nested()
        {
            var pbMsg = new PbTestMessage
            {
                Nested = new PbInnerMessage { Id = 42, Name = "From PbNet" }
            };

            using var ms = new System.IO.MemoryStream();
            Serializer.Serialize(ms, pbMsg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(ms.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.NotNull(result.Nested);
            Assert.Equal(42, result.Nested.Id);
            Assert.Equal("From PbNet", result.Nested.Name);
        }

        [Fact]
        public void ProtobufNet_To_ProtoLite_PackedList()
        {
            var pbMsg = new PbTestMessage
            {
                IntList = { 10, 20, 30 }
            };

            using var ms = new System.IO.MemoryStream();
            Serializer.Serialize(ms, pbMsg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(ms.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(new[] { 10, 20, 30 }, result.IntList);
        }

        [Fact]
        public void ProtobufNet_To_ProtoLite_UnpackedList()
        {
            // protobuf-net 对 packed=false 的 list 也应兼容
            var pbMsg = new PbTestMessage
            {
                StringList = { "x", "yy", "zzz" }
            };

            using var ms = new System.IO.MemoryStream();
            Serializer.Serialize(ms, pbMsg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(ms.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(new[] { "x", "yy", "zzz" }, result.StringList);
        }

        [Fact]
        public void ProtobufNet_To_ProtoLite_MessageList()
        {
            var pbMsg = new PbTestMessage();
            pbMsg.MessageList.Add(new PbInnerMessage { Id = 100, Name = "m1" });
            pbMsg.MessageList.Add(new PbInnerMessage { Id = 200, Name = "m2" });

            using var ms = new System.IO.MemoryStream();
            Serializer.Serialize(ms, pbMsg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(ms.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(2, result.MessageList.Count);
            Assert.Equal(100, result.MessageList[0].Id);
            Assert.Equal("m1", result.MessageList[0].Name);
            Assert.Equal(200, result.MessageList[1].Id);
            Assert.Equal("m2", result.MessageList[1].Name);
        }

        [Fact]
        public void ProtobufNet_To_ProtoLite_Maps()
        {
            var pbMsg = new PbTestMessage();
            pbMsg.IntStringMap[1] = "one";
            pbMsg.IntStringMap[2] = "two";
            pbMsg.StringIntMap["a"] = 10;
            pbMsg.StringIntMap["b"] = 20;

            using var ms = new System.IO.MemoryStream();
            Serializer.Serialize(ms, pbMsg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(ms.ToArray()));
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(2, result.IntStringMap.Count);
            Assert.Equal("one", result.IntStringMap[1]);
            Assert.Equal("two", result.IntStringMap[2]);

            Assert.Equal(2, result.StringIntMap.Count);
            Assert.Equal(10, result.StringIntMap["a"]);
            Assert.Equal(20, result.StringIntMap["b"]);
        }

        // ---- 完整双向往返 ----

        [Fact]
        public void FullRoundTrip_ProtoLite_Then_PbNet_Then_ProtoLite()
        {
            var original = MakeSample();

            // ProtoLite serialize
            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, original);

            // PbNet deserialize → PbNet serialize
            var pbResult = Serializer.Deserialize<PbTestMessage>(
                new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            using var ms = new System.IO.MemoryStream();
            Serializer.Serialize(ms, pbResult);

            // ProtoLite deserialize
            var reader = new ProtoReader(new ReadOnlySequence<byte>(ms.ToArray()));
            var final = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(original.Int32Field, final.Int32Field);
            Assert.Equal(original.Int64Field, final.Int64Field);
            Assert.Equal(original.UInt32Field, final.UInt32Field);
            Assert.Equal(original.UInt64Field, final.UInt64Field);
            Assert.Equal(original.FloatField, final.FloatField);
            Assert.Equal(original.DoubleField, final.DoubleField);
            Assert.Equal(original.BoolField, final.BoolField);
            Assert.Equal(original.StringField, final.StringField);
            Assert.Equal(original.NullableInt, final.NullableInt);
            Assert.Equal(original.NullableFloat, final.NullableFloat);
            Assert.Equal(original.NullableBool, final.NullableBool);
            Assert.Equal(original.Nested!.Id, final.Nested!.Id);
            Assert.Equal(original.Nested.Name, final.Nested.Name);
            Assert.Equal(original.IntList, final.IntList);
            Assert.Equal(original.StringList, final.StringList);
            Assert.Equal(original.MessageList.Count, final.MessageList.Count);
            Assert.Equal(original.MessageList[0].Id, final.MessageList[0].Id);
            Assert.Equal(original.MessageList[1].Id, final.MessageList[1].Id);
            Assert.Equal(original.IntStringMap, final.IntStringMap);
            Assert.Equal(original.StringIntMap, final.StringIntMap);
        }
    }
}
