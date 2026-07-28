namespace PbLite.Gen;

static class TypeMapper
{
    /// <summary>
    /// proto3 标量类型 → C# 类型。非标量返回 null。
    /// </summary>
    public static string? ScalarToCSharp(string protoType) => protoType switch
    {
        "double"   => "double",
        "float"    => "float",
        "int32"    => "int",
        "int64"    => "long",
        "uint32"   => "uint",
        "uint64"   => "ulong",
        "sint32"   => "int",
        "sint64"   => "long",
        "fixed32"  => "uint",
        "fixed64"  => "ulong",
        "sfixed32" => "int",
        "sfixed64" => "long",
        "bool"     => "bool",
        "string"   => "string",
        "bytes"    => "byte[]",
        _ => null
    };

    /// <summary>
    /// proto3 标量类型 → PbWire 编码。非标量返回 null。
    /// </summary>
    public static string? GetWire(string protoType) => protoType switch
    {
        "sint32" or "sint64"                     => "PbWire.ZigZag",
        "fixed32" or "sfixed32" or "float"      => "PbWire.Fixed32",
        "fixed64" or "sfixed64" or "double"     => "PbWire.Fixed64",
        "int32" or "int64" or "uint32" or "uint64" or "bool" => "PbWire.Varint",
        _ => null
    };

    /// <summary>
    /// 是否需要非默认 Wire（即非 Varint）。
    /// </summary>
    public static bool NeedsWireAttribute(string protoType) => protoType switch
    {
        "sint32" or "sint64" or "fixed32" or "fixed64" or "sfixed32" or "sfixed64" => true,
        _ => false
    };

    public static bool IsScalar(string protoType) => protoType switch
    {
        "double" or "float" or "int32" or "int64" or "uint32" or "uint64"
        or "sint32" or "sint64" or "fixed32" or "fixed64"
        or "sfixed32" or "sfixed64" or "bool" or "string" or "bytes"
            => true,
        _ => false
    };

    /// <summary>
    /// 是否映射到 C# 值类型（用于 optional 时决定是否加 ?）
    /// </summary>
    public static bool IsValueTypeScalar(string protoType) => protoType switch
    {
        "double" or "float" or "int32" or "int64" or "uint32" or "uint64"
        or "sint32" or "sint64" or "fixed32" or "fixed64"
        or "sfixed32" or "sfixed64" or "bool"
            => true,
        _ => false
    };
}
