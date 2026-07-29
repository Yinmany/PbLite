using System.Collections.Generic;
using PbLite;

namespace PbLite.Tests
{
    [PbContract]
    public class InnerMessage
    {
        [PbMember(1)] public int Id { get; set; }
        [PbMember(2)] public string Name { get; set; } = "";
    }

    [PbContract]
    public class TestMessage
    {
        // 标量
        [PbMember(1)] public int Int32Field { get; set; }
        [PbMember(2)] public long Int64Field { get; set; }
        [PbMember(3)] public uint UInt32Field { get; set; }
        [PbMember(4)] public ulong UInt64Field { get; set; }
        [PbMember(5)] public float FloatField { get; set; }
        [PbMember(6)] public double DoubleField { get; set; }
        [PbMember(7)] public bool BoolField { get; set; }
        [PbMember(8)] public string StringField { get; set; } = "";

        // Nullable
        [PbMember(9)] public int? NullableInt { get; set; }
        [PbMember(10)] public float? NullableFloat { get; set; }
        [PbMember(11)] public bool? NullableBool { get; set; }

        // 嵌套消息
        [PbMember(12)] public InnerMessage? Nested { get; set; }

        // List
        [PbMember(13)] public List<int> IntList { get; set; } = new();
        [PbMember(14)] public List<string> StringList { get; set; } = new();
        [PbMember(15)] public List<InnerMessage> MessageList { get; set; } = new();

        // Dictionary
        [PbMember(16)] public Dictionary<int, string> IntStringMap { get; set; } = new();
        [PbMember(17)] public Dictionary<string, int> StringIntMap { get; set; } = new();
    }

    [PbContract]
    public struct SimpleStruct
    {
        [PbMember(1)] public int Id { get; set; }
        [PbMember(2)] public string Name { get; set; }
    }

    [PbContract]
    public struct StructWithNested
    {
        [PbMember(1)] public int Value { get; set; }
        [PbMember(2)] public SimpleStruct? Nested { get; set; }
    }

    [PbContract]
    public struct StructWithScalars
    {
        [PbMember(1)] public int IntField { get; set; }
        [PbMember(2)] public long LongField { get; set; }
        [PbMember(3)] public float FloatField { get; set; }
        [PbMember(4)] public bool BoolField { get; set; }
        [PbMember(5)] public string? StringField { get; set; }
        [PbMember(6)] public int? NullableInt { get; set; }
    }

    /// <summary>
    /// Container for testing nested [PbContract] classes.
    /// </summary>
    public class OuterContainer
    {
        [PbContract]
        public class NestedMessage
        {
            [PbMember(1)] public int Id { get; set; }
            [PbMember(2)] public string Name { get; set; } = "";
            [PbMember(3)] public InnerMessage? Inner { get; set; }
            [PbMember(4)] public List<int> Numbers { get; set; } = new();
        }

        public class MiddleLevel
        {
            [PbContract]
            public class DeepNestedMessage
            {
                [PbMember(1)] public int Value { get; set; }
                [PbMember(2)] public string Text { get; set; } = "";
            }
        }
    }
}
