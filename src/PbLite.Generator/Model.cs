using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace PbLite.Generator
{
    internal enum CollectionKind { None, List, Map }

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

        public MemberInfo(string name, ITypeSymbol type, int order, bool isReadOnly)
        {
            Name = name;
            Type = type;
            Order = order;
            IsReadOnly = isReadOnly;
        }
    }
}
