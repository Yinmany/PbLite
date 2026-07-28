using System.Buffers;
using PbLite;

namespace PbLite.Tests
{
    /// <summary>
    /// 仅含集合字段的消息，用于隔离测试集合行为。
    /// </summary>
    [PbContract]
    public class CollectionOnlyMessage
    {
        [PbMember(1)] public List<int> IntList { get; set; } = new();
        [PbMember(2)] public List<string> StringList { get; set; } = new();
        [PbMember(3)] public Dictionary<int, string> IntStringMap { get; set; } = new();
    }

    public class Proto3SemanticsTests
    {
        [Fact]
        public void EmptyMessage_ScalarDefaults_AreSkipped()
        {
            // proto3 语义：非 nullable 标量的默认值（int=0, bool=false 等）不写入
            var msg = new TestMessage();

            var writer = new ArrayBufferWriter<byte>();
            TestMessageSerializer.Instance.Serialize(writer, msg);
            int written = writer.WrittenCount;

            // 所有标量字段都是默认值，string("") 跳过，nullable null 跳过，空集合跳过
            Assert.Equal(0, written);
        }

        [Fact]
        public void EmptyMessage_DeserializesToDefaults()
        {
            var reader = new ProtoReader(ReadOnlySequence<byte>.Empty);
            var result = (TestMessage)TestMessageSerializer.Instance.Deserialize(ref reader, null);

            Assert.Equal("", result.StringField);
            Assert.Null(result.NullableInt);
            Assert.Empty(result.IntList);
        }
    }
}
