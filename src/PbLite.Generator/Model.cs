using Microsoft.CodeAnalysis;
using System.Collections.Generic;
namespace PbLite.Generator
{
    internal enum CollectionKind { None, List, Map }

    /// <summary>
    /// Mirror of PbLite.PbWire for use within the generator.
    /// </summary>
    internal enum PbWire { Varint = 0, ZigZag = 1, Fixed32 = 2, Fixed64 = 3 }

    internal sealed class TypeInfo
    {
        public string Name { get; }
        public string Namespace { get; }
        public string FullyQualifiedName { get; }
        public string SerializerName { get; }
        public string SerializerFullName { get; }
        public List<MemberInfo> Members { get; }
        public bool IsValueType { get; }

        public TypeInfo(string name, string ns, string fullyQualifiedName,
            string serializerName, string serializerFullName, List<MemberInfo> members, bool isValueType = false)
        {
            Name = name;
            Namespace = ns;
            FullyQualifiedName = fullyQualifiedName;
            SerializerName = serializerName;
            SerializerFullName = serializerFullName;
            Members = members;
            IsValueType = isValueType;
        }
    }

    internal sealed class MemberInfo
    {
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public int Order { get; }
        public bool IsReadOnly { get; }
        public PbWire Wire { get; }

        public MemberInfo(string name, ITypeSymbol type, int order, bool isReadOnly, PbWire wire)
        {
            Name = name;
            Type = type;
            Order = order;
            IsReadOnly = isReadOnly;
            Wire = wire;
        }
    }
}
