namespace PbLite.ProtoGen.Tests;

public class EndToEndTests
{
    // ─── Namespace ───────────────────────────────────────────

    [Fact]
    public void Namespace_FromPackage()
    {
        var cs = Generate("syntax=\"proto3\"; package foo.bar; message M { int32 v = 1; }");
        Assert.Contains("namespace Foo.Bar", cs);
    }

    [Fact]
    public void Namespace_FromCSharpNamespaceOption()
    {
        var proto = """
            syntax = "proto3";
            package foo.bar;
            option csharp_namespace = "Custom.NS";
            message M { int32 v = 1; }
            """;
        var cs = Generate(proto);
        Assert.Contains("namespace Custom.NS", cs);
        Assert.DoesNotContain("namespace Foo.Bar", cs);
    }

    [Fact]
    public void Namespace_FromOverride()
    {
        var cs = Generate("syntax=\"proto3\"; package foo; message M { int32 v = 1; }", "Overridden");
        Assert.Contains("namespace Overridden", cs);
        Assert.DoesNotContain("namespace Foo", cs);
    }

    [Fact]
    public void Namespace_NoPackage()
    {
        var cs = Generate("syntax=\"proto3\"; message M { int32 v = 1; }");
        Assert.DoesNotContain("namespace", cs);
    }

    // ─── Scalars & Wire ──────────────────────────────────────

    [Theory]
    [InlineData("int32", "int", "")]
    [InlineData("int64", "long", "")]
    [InlineData("uint32", "uint", "")]
    [InlineData("uint64", "ulong", "")]
    [InlineData("bool", "bool", "")]
    [InlineData("float", "float", "")]
    [InlineData("double", "double", "")]
    [InlineData("string", "string", "")]
    public void Scalar_Types(string protoType, string csharpType, string wire)
    {
        var cs = Generate($"syntax=\"proto3\"; message M {{ {protoType} v = 1; }}");
        Assert.Contains($"[PbMember(1{wire})]", cs);
        Assert.Contains($"public {csharpType} V", cs);
    }

    [Theory]
    [InlineData("sint32", "int", "PbWire.ZigZag")]
    [InlineData("sint64", "long", "PbWire.ZigZag")]
    [InlineData("fixed32", "uint", "PbWire.Fixed32")]
    [InlineData("fixed64", "ulong", "PbWire.Fixed64")]
    [InlineData("sfixed32", "int", "PbWire.Fixed32")]
    [InlineData("sfixed64", "long", "PbWire.Fixed64")]
    public void WireTypes_EmitWireAttribute(string protoType, string csharpType, string wire)
    {
        var cs = Generate($"syntax=\"proto3\"; message M {{ {protoType} v = 1; }}");
        Assert.Contains($"[PbMember(1, Wire = {wire})]", cs);
        Assert.Contains($"public {csharpType} V", cs);
    }

    // ─── Optional ────────────────────────────────────────────

    [Fact]
    public void Optional_ValueType_BecomesNullable()
    {
        var cs = Generate("syntax=\"proto3\"; message M { optional int32 v = 1; }");
        Assert.Contains("public int? V;", cs);
    }

    [Fact]
    public void Optional_ReferenceType_NoNullable()
    {
        var cs = Generate("syntax=\"proto3\"; message M { optional string v = 1; }");
        Assert.Contains("public string V;", cs);
    }

    [Fact]
    public void Optional_NoInitializer()
    {
        var cs = Generate("syntax=\"proto3\"; message M { optional int32 v = 1; }");
        Assert.DoesNotContain("V = ", cs);
    }

    // ─── Repeated ────────────────────────────────────────────

    [Fact]
    public void Repeated_BecomesList()
    {
        var cs = Generate("syntax=\"proto3\"; message M { repeated int32 v = 1; }");
        Assert.Contains("public List<int> V = new List<int>();", cs);
    }

    [Fact]
    public void Repeated_String_BecomesList()
    {
        var cs = Generate("syntax=\"proto3\"; message M { repeated string v = 1; }");
        Assert.Contains("public List<string> V = new List<string>();", cs);
    }

    [Fact]
    public void Repeated_Sint_EmitsWireAttribute()
    {
        var cs = Generate("syntax=\"proto3\"; message M { repeated sint32 v = 1; }");
        Assert.Contains("[PbMember(1, Wire = PbWire.ZigZag)]", cs);
    }

    // ─── Map ─────────────────────────────────────────────────

    [Fact]
    public void Map_BecomesDictionary()
    {
        var cs = Generate("syntax=\"proto3\"; message M { map<string, int32> v = 1; }");
        Assert.Contains("public Dictionary<string, int> V = new Dictionary<string, int>();", cs);
    }

    [Fact]
    public void Map_NoWireAttribute()
    {
        var cs = Generate("syntax=\"proto3\"; message M { map<string, sint32> v = 1; }");
        Assert.Contains("[PbMember(1)]", cs);
    }

    // ─── Enum ────────────────────────────────────────────────

    [Fact]
    public void Enum_GeneratedCorrectly()
    {
        var cs = Generate("""
            syntax = "proto3";
            message M { Color c = 1; }
            enum Color { RED = 0; GREEN = 1; BLUE = 2; }
            """);
        Assert.Contains("public enum Color", cs);
        Assert.Contains("Red = 0", cs);
        Assert.Contains("Green = 1", cs);
        Assert.Contains("Blue = 2", cs);
        Assert.Contains("public Color C;", cs);
    }

    [Fact]
    public void Enum_BeforeMessage()
    {
        var cs = Generate("""
            syntax = "proto3";
            enum Status { UNKNOWN = 0; ACTIVE = 1; }
            message M { Status s = 1; }
            """);
        int enumPos = cs.IndexOf("public enum Status");
        int msgPos = cs.IndexOf("[PbContract]");
        Assert.True(enumPos < msgPos);
    }

    // ─── Bytes ───────────────────────────────────────────────

    [Fact]
    public void Bytes_GeneratesByteArray()
    {
        var cs = Generate("syntax=\"proto3\"; message M { bytes v = 1; }");
        Assert.Contains("public byte[] V = Array.Empty<byte>();", cs);
    }

    // ─── Nested Messages ─────────────────────────────────────

    [Fact]
    public void NestedMessage_Flattened()
    {
        var cs = Generate("""
            syntax = "proto3";
            message Outer {
                message Inner { int32 id = 1; }
                Inner detail = 1;
            }
            """);
        Assert.Contains("public partial class Outer", cs);
        Assert.Contains("public partial class Inner", cs);
        Assert.Contains("public Inner Detail;", cs);
    }

    [Fact]
    public void NestedEnum_Flattened()
    {
        var cs = Generate("""
            syntax = "proto3";
            message Outer {
                enum State { ACTIVE = 0; INACTIVE = 1; }
                State s = 1;
            }
            """);
        Assert.Contains("public enum State", cs);
        Assert.Contains("public State S;", cs);
    }

    // ─── Reserved ────────────────────────────────────────────

    [Fact]
    public void ReservedFields_Skipped()
    {
        var cs = Generate("""
            syntax = "proto3";
            message M {
                reserved 1, 2, 15;
                reserved "old_field";
                string current = 3;
            }
            """);
        Assert.Contains("[PbMember(3)]", cs);
        Assert.DoesNotContain("[PbMember(1)]", cs);
        Assert.DoesNotContain("[PbMember(2)]", cs);
    }

    // ─── Options ─────────────────────────────────────────────

    [Fact]
    public void Deprecated_Message_EmitsObsolete()
    {
        var cs = Generate("""
            syntax = "proto3";
            message M {
                option deprecated = true;
                int32 v = 1;
            }
            """);
        Assert.Contains("[Obsolete]", cs);
    }

    [Fact]
    public void Deprecated_Field_EmitsObsolete()
    {
        var cs = Generate("syntax=\"proto3\"; message M { int32 v = 1 [deprecated = true]; }");
        Assert.Contains("[Obsolete]", cs);
        Assert.Contains("[PbMember(1)]", cs);
    }

    [Fact]
    public void Deprecated_Enum_EmitsObsolete()
    {
        var cs = Generate("""
            syntax = "proto3";
            enum E {
                option deprecated = true;
                A = 0;
            }
            """);
        Assert.Contains("[Obsolete]", cs);
    }

    [Fact]
    public void UnknownOption_Ignored()
    {
        var cs = Generate("syntax=\"proto3\"; option java_package=\"com.test\"; message M { int32 v = 1; }");
        Assert.Contains("public partial class M", cs);
    }

    [Fact]
    public void UnknownFieldOption_Ignored()
    {
        var cs = Generate("syntax=\"proto3\"; message M { string v = 1 [json_name=\"custom\"]; }");
        Assert.Contains("public string V = \"\";", cs);
    }

    // ─── Naming ──────────────────────────────────────────────

    [Fact]
    public void SnakeCase_ConvertedToPascalCase()
    {
        var cs = Generate("syntax=\"proto3\"; message M { int32 player_id = 1; }");
        Assert.Contains("public int PlayerId;", cs);
    }

    [Fact]
    public void MultipleMessages()
    {
        var cs = Generate("""
            syntax = "proto3";
            message A { int32 v = 1; }
            message B { string v = 1; }
            """);
        Assert.Contains("public partial class A", cs);
        Assert.Contains("public partial class B", cs);
    }

    // ─── Header ──────────────────────────────────────────────

    [Fact]
    public void Output_HasAutoGeneratedHeader()
    {
        var cs = Generate("syntax=\"proto3\"; message M { int32 v = 1; }");
        Assert.Contains("// <auto-generated />", cs);
        Assert.Contains("#nullable enable", cs);
    }

    [Fact]
    public void Output_HasUsings()
    {
        var cs = Generate("syntax=\"proto3\"; message M { int32 v = 1; }");
        Assert.Contains("using System;", cs);
        Assert.Contains("using PbLite;", cs);
        Assert.Contains("using System.Collections.Generic;", cs);
    }

    // ─── Edge Cases ──────────────────────────────────────────

    [Fact]
    public void FieldNumber_LargeNumber()
    {
        var cs = Generate("syntax=\"proto3\"; message M { int32 v = 536870911; }");
        Assert.Contains("[PbMember(536870911)]", cs);
    }

    [Fact]
    public void HexFieldNumber()
    {
        var cs = Generate("syntax=\"proto3\"; message M { int32 v = 0x10; }");
        Assert.Contains("[PbMember(16)]", cs);
    }

    [Fact]
    public void Import_Statement_Parsed()
    {
        var cs = Generate("""
            syntax = "proto3";
            import "google/protobuf/any.proto";
            message M { int32 v = 1; }
            """);
        Assert.Contains("public partial class M", cs);
    }

    [Fact]
    public void Oneof_Skipped()
    {
        var cs = Generate("""
            syntax = "proto3";
            message M {
                oneof choice {
                    int32 a = 1;
                    string b = 2;
                }
                int32 c = 3;
            }
            """);
        Assert.Contains("[PbMember(3)]", cs);
        Assert.DoesNotContain("[PbMember(1)]", cs);
        Assert.DoesNotContain("[PbMember(2)]", cs);
    }

    private static string Generate(string proto, string? ns = null) => TestHelper.Generate(proto, ns);
}
