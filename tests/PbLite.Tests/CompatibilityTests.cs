using System.Buffers;
using PbLite;

namespace PbLite.Tests
{
    public class CompatibilityTests
    {
        [Fact]
        public void ForwardCompat_V1Data_DeserializedByV2()
        {
            var v1 = new V1Message { Id = 1, Name = "v1" };

            var writer = new ArrayBufferWriter<byte>();
            V1MessageSerializer.Instance.Serialize(writer, v1);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var v2 = (V2Message)V2MessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(1, v2.Id);
            Assert.Equal("v1", v2.Name);
            Assert.Equal(42, v2.NewField);
            Assert.Equal("default", v2.Extra);
        }

        [Fact]
        public void BackwardCompat_V2Data_DeserializedByV1()
        {
            var v2 = new V2Message { Id = 2, Name = "v2", NewField = 99, Extra = "extra" };

            var writer = new ArrayBufferWriter<byte>();
            V2MessageSerializer.Instance.Serialize(writer, v2);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var v1 = (V1Message)V1MessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(2, v1.Id);
            Assert.Equal("v2", v1.Name);
        }

        [Fact]
        public void UnknownFields_Skipped_AllWireTypes()
        {
            var writer = new ArrayBufferWriter<byte>();
            ProtoWriter.WriteInt32(writer, 1, 7);
            ProtoWriter.WriteInt32(writer, 100, 123);
            ProtoWriter.WriteFloat(writer, 101, 1.0f);
            ProtoWriter.WriteString(writer, 2, "ok");
            ProtoWriter.WriteBytes(writer, 102, new byte[] { 1, 2, 3 });
            ProtoWriter.WriteInt64(writer, 103, 999);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var v1 = (V1Message)V1MessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(7, v1.Id);
            Assert.Equal("ok", v1.Name);
        }

        [Fact]
        public void Reader_AtEnd_AfterDeserialize()
        {
            var msg = new V1Message { Id = 42, Name = "test" };

            var writer = new ArrayBufferWriter<byte>();
            V1MessageSerializer.Instance.Serialize(writer, msg);

            var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
            var result = (V1Message)V1MessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal(42, result.Id);
            Assert.Equal("test", result.Name);
            Assert.True(reader.End);
        }
    }
}
