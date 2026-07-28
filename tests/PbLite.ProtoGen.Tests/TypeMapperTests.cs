namespace PbLite.ProtoGen.Tests;

public class TypeMapperTests
{
    [Theory]
    [InlineData("int32", "int")]
    [InlineData("int64", "long")]
    [InlineData("uint32", "uint")]
    [InlineData("uint64", "ulong")]
    [InlineData("sint32", "int")]
    [InlineData("sint64", "long")]
    [InlineData("fixed32", "uint")]
    [InlineData("fixed64", "ulong")]
    [InlineData("sfixed32", "int")]
    [InlineData("sfixed64", "long")]
    [InlineData("bool", "bool")]
    [InlineData("float", "float")]
    [InlineData("double", "double")]
    [InlineData("string", "string")]
    [InlineData("bytes", "byte[]")]
    public void ScalarToCSharp_KnownTypes(string protoType, string expected)
    {
        Assert.Equal(expected, TypeMapper.ScalarToCSharp(protoType));
    }

    [Fact]
    public void ScalarToCSharp_NonScalar_ReturnsNull()
    {
        Assert.Null(TypeMapper.ScalarToCSharp("MyMessage"));
        Assert.Null(TypeMapper.ScalarToCSharp("foo.bar.Baz"));
    }

    [Theory]
    [InlineData("sint32", "PbWire.ZigZag")]
    [InlineData("sint64", "PbWire.ZigZag")]
    [InlineData("fixed32", "PbWire.Fixed32")]
    [InlineData("fixed64", "PbWire.Fixed64")]
    [InlineData("sfixed32", "PbWire.Fixed32")]
    [InlineData("sfixed64", "PbWire.Fixed64")]
    [InlineData("int32", "PbWire.Varint")]
    [InlineData("int64", "PbWire.Varint")]
    [InlineData("uint32", "PbWire.Varint")]
    [InlineData("uint64", "PbWire.Varint")]
    [InlineData("bool", "PbWire.Varint")]
    public void GetWire_KnownTypes(string protoType, string expected)
    {
        Assert.Equal(expected, TypeMapper.GetWire(protoType));
    }

    [Theory]
    [InlineData("sint32", true)]
    [InlineData("sint64", true)]
    [InlineData("fixed32", true)]
    [InlineData("fixed64", true)]
    [InlineData("sfixed32", true)]
    [InlineData("sfixed64", true)]
    [InlineData("int32", false)]
    [InlineData("bool", false)]
    [InlineData("string", false)]
    [InlineData("MyMessage", false)]
    public void NeedsWireAttribute(string protoType, bool expected)
    {
        Assert.Equal(expected, TypeMapper.NeedsWireAttribute(protoType));
    }

    [Theory]
    [InlineData("int32", true)]
    [InlineData("double", true)]
    [InlineData("bool", true)]
    [InlineData("sint32", true)]
    [InlineData("string", false)]
    [InlineData("bytes", false)]
    [InlineData("MyMessage", false)]
    public void IsValueTypeScalar(string protoType, bool expected)
    {
        Assert.Equal(expected, TypeMapper.IsValueTypeScalar(protoType));
    }
}
