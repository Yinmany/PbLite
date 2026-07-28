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
        public List<MemberInfo> Members { get; }

        public TypeInfo(string name, string ns, List<MemberInfo> members)
        {
            Name = name;
            Namespace = ns;
            Members = members;
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
