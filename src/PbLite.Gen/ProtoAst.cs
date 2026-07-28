using System.Collections.Generic;

namespace PbLite.Gen;

enum FieldLabel { Singular, Repeated, Optional }

sealed class ProtoFile
{
    public string Syntax { get; set; } = "proto3";
    public string Package { get; set; } = "";
    public string? CSharpNamespace { get; set; }
    public List<string> Imports { get; } = [];
    public Dictionary<string, string> Options { get; } = [];
    public List<ProtoMessage> Messages { get; } = [];
    public List<ProtoEnum> Enums { get; } = [];
}

sealed class ProtoMessage
{
    public string Name { get; set; } = "";
    public List<ProtoField> Fields { get; } = [];
    public List<ProtoMessage> NestedMessages { get; } = [];
    public List<ProtoEnum> NestedEnums { get; } = [];
    public List<string> ReservedNumbers { get; } = [];
    public List<string> ReservedNames { get; } = [];
    public Dictionary<string, string> Options { get; } = [];
    public bool Deprecated { get; set; }
}

sealed class ProtoField
{
    public string Name { get; set; } = "";
    public int Number { get; set; }
    public string ProtoType { get; set; } = "";
    public FieldLabel Label { get; set; } = FieldLabel.Singular;

    public bool IsMap { get; set; }
    public string? MapKeyType { get; set; }
    public string? MapValueType { get; set; }

    public bool Packed { get; set; } = true;
    public bool Deprecated { get; set; }
    public Dictionary<string, string> Options { get; } = [];
}

sealed class ProtoEnum
{
    public string Name { get; set; } = "";
    public List<ProtoEnumValue> Values { get; } = [];
    public bool Deprecated { get; set; }
}

sealed class ProtoEnumValue
{
    public string Name { get; set; } = "";
    public int Number { get; set; }
}
