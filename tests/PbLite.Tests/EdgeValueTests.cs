using System.Buffers;
using PbLite;

namespace PbLite.Tests
{
    public class EdgeValueTests
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
        public void Float_SpecialValues_RoundTrip()
        {
            var msg = new TestMessage
            {
                FloatField = float.NaN,
                DoubleField = double.PositiveInfinity
            };
            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);
            Assert.Equal(float.NaN, result.FloatField);
            Assert.Equal(double.PositiveInfinity, result.DoubleField);
        }

        [Fact]
        public void Float_NegativeZero_RoundTrip()
        {
            var msg = new TestMessage { FloatField = -0.0f };
            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);
            Assert.Equal(-0.0f, result.FloatField);
        }

        [Fact]
        public void MaxVarInt_RoundTrip()
        {
            var msg = new TestMessage
            {
                UInt64Field = ulong.MaxValue,
                Int64Field = long.MinValue
            };
            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);
            Assert.Equal(ulong.MaxValue, result.UInt64Field);
            Assert.Equal(long.MinValue, result.Int64Field);
        }

        [Fact]
        public void LongString_RoundTrip()
        {
            var msg = new TestMessage { StringField = new string('A', 10_000) };
            var (result, _) = RoundTrip(TestMessageSerializer.Instance, msg);
            Assert.Equal(10_000, result.StringField.Length);
        }
    }
}
