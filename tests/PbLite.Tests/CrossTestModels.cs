using System.Collections.Generic;

namespace PbLite.Tests.CrossTest
{
    [global::ProtoBuf.ProtoContract]
    public class PbInnerMessage
    {
        [global::ProtoBuf.ProtoMember(1)] public int Id { get; set; }
        [global::ProtoBuf.ProtoMember(2)] public string Name { get; set; } = "";
    }

    [global::ProtoBuf.ProtoContract]
    public class PbTestMessage
    {
        [global::ProtoBuf.ProtoMember(1)] public int Int32Field { get; set; }
        [global::ProtoBuf.ProtoMember(2)] public long Int64Field { get; set; }
        [global::ProtoBuf.ProtoMember(3)] public uint UInt32Field { get; set; }
        [global::ProtoBuf.ProtoMember(4)] public ulong UInt64Field { get; set; }
        [global::ProtoBuf.ProtoMember(5)] public float FloatField { get; set; }
        [global::ProtoBuf.ProtoMember(6)] public double DoubleField { get; set; }
        [global::ProtoBuf.ProtoMember(7)] public bool BoolField { get; set; }
        [global::ProtoBuf.ProtoMember(8)] public string StringField { get; set; } = "";

        [global::ProtoBuf.ProtoMember(9)] public int? NullableInt { get; set; }
        [global::ProtoBuf.ProtoMember(10)] public float? NullableFloat { get; set; }
        [global::ProtoBuf.ProtoMember(11)] public bool? NullableBool { get; set; }

        [global::ProtoBuf.ProtoMember(12)] public PbInnerMessage? Nested { get; set; }

        [global::ProtoBuf.ProtoMember(13)] public List<int> IntList { get; set; } = new();
        [global::ProtoBuf.ProtoMember(14)] public List<string> StringList { get; set; } = new();
        [global::ProtoBuf.ProtoMember(15)] public List<PbInnerMessage> MessageList { get; set; } = new();

        [global::ProtoBuf.ProtoMember(16)] public Dictionary<int, string> IntStringMap { get; set; } = new();
        [global::ProtoBuf.ProtoMember(17)] public Dictionary<string, int> StringIntMap { get; set; } = new();
    }
}
