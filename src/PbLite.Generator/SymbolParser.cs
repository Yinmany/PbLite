using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PbLite.Generator
{
    internal static class SymbolParser
    {
        private const string ProtoContractName = "PbContractAttribute";
        private const string ProtoContractShort = "PbContract";
        private const string ProtoMemberName = "PbMemberAttribute";
        private const string ProtoMemberShort = "PbMember";

        public static TypeInfo? GetTypeInfo(GeneratorSyntaxContext ctx)
        {
            var typeDecl = (TypeDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
            if (symbol == null) return null;

            var protoContract = symbol.GetAttributes()
                .FirstOrDefault(a => IsProtoContract(a.AttributeClass));
            if (protoContract == null) return null;

            if (symbol.AllInterfaces.Any(i => i.Name == "IProtoSerializer" && i.ContainingNamespace?.Name == "ProtoLite"))
                return null;

            var members = new List<MemberInfo>();
            foreach (var member in symbol.GetMembers())
            {
                if (member is IPropertySymbol { IsStatic: false } prop)
                {
                    var attr = GetProtoMemberAttribute(prop.GetAttributes());
                    if (attr != null)
                        members.Add(new MemberInfo(prop.Name, prop.Type, (int)attr.ConstructorArguments[0].Value!, GetWire(attr)));
                }
                else if (member is IFieldSymbol { IsStatic: false } field)
                {
                    var attr = GetProtoMemberAttribute(field.GetAttributes());
                    if (attr != null)
                        members.Add(new MemberInfo(field.Name, field.Type, (int)attr.ConstructorArguments[0].Value!, GetWire(attr)));
                }
            }

            string ns = symbol.ContainingNamespace?.IsGlobalNamespace == true
                ? ""
                : symbol.ContainingNamespace?.ToDisplayString() ?? "";

            string fullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string serializerName = GetSerializerName(symbol);
            string serializerFullName = GetSerializerFullName(symbol);

            return new TypeInfo(ns, fullyQualifiedName,
                serializerName, serializerFullName, members, isValueType: symbol.IsValueType);
        }

        public static bool HasProtoContract(ITypeSymbol type)
        {
            return type.GetAttributes().Any(a => IsProtoContract(a.AttributeClass));
        }

        public static ITypeSymbol? GetNullableUnderlyingType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol { IsGenericType: true } named &&
                named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                return named.TypeArguments[0];
            }
            return null;
        }

        public static CollectionKind GetCollectionInfo(ITypeSymbol type,
            out ITypeSymbol? elementType, out ITypeSymbol? keyType, out ITypeSymbol? valueType)
        {
            elementType = null;
            keyType = null;
            valueType = null;

            if (type is not INamedTypeSymbol { IsGenericType: true } named)
                return CollectionKind.None;

            if (named.Name == "List" && named.TypeArguments.Length == 1)
            {
                elementType = named.TypeArguments[0];
                return CollectionKind.List;
            }

            if (named.Name == "Dictionary" && named.TypeArguments.Length == 2)
            {
                keyType = named.TypeArguments[0];
                valueType = named.TypeArguments[1];
                return CollectionKind.Map;
            }

            return CollectionKind.None;
        }

        public static bool IsByteArray(ITypeSymbol type)
        {
            return type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte };
        }

        /// <summary>
        /// Returns true if the type is a scalar that can be packed (int, long, uint, ulong,
        /// float, double, bool, or enum — including Nullable&lt;T&gt; variants).
        /// </summary>
        public static bool IsPackedScalar(ITypeSymbol type)
        {
            ITypeSymbol? underlying = GetNullableUnderlyingType(type);
            ITypeSymbol effective = underlying ?? type;
            SpecialType st = effective.SpecialType;
            return st is SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_UInt32 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double
                or SpecialType.System_Boolean
                || effective.TypeKind == TypeKind.Enum;
        }

        /// <summary>
        /// Returns the enum type symbol if the type is an enum or Nullable&lt;Enum&gt;,
        /// otherwise null.
        /// </summary>
        public static ITypeSymbol? GetEnumType(ITypeSymbol type)
        {
            ITypeSymbol? underlying = GetNullableUnderlyingType(type);
            ITypeSymbol effective = underlying ?? type;
            return effective.TypeKind == TypeKind.Enum ? effective : null;
        }

        /// <summary>
        /// Returns the containing type path (e.g. "Outer.Middle" for Outer.Middle.Inner),
        /// or "" if the type is not nested.
        /// </summary>
        public static string GetContainingTypePath(ITypeSymbol type)
        {
            if (type.ContainingType == null) return "";
            var parts = new List<string>();
            var current = type.ContainingType;
            while (current != null)
            {
                parts.Insert(0, current.Name);
                current = current.ContainingType;
            }
            return string.Join(".", parts);
        }

        /// <summary>
        /// Computes a unique serializer class name for the type.
        /// Non-nested: "TypeNameSerializer"
        /// Nested: "Outer_Inner_TypeNameSerializer"
        /// </summary>
        public static string GetSerializerName(ITypeSymbol type)
        {
            string path = GetContainingTypePath(type);
            string baseName = string.IsNullOrEmpty(path)
                ? type.Name
                : path.Replace(".", "_") + "_" + type.Name;
            return baseName + "Serializer";
        }

        /// <summary>
        /// Computes the fully qualified serializer reference (e.g. "global::Ns.Outer_InnerSerializer").
        /// </summary>
        public static string GetSerializerFullName(ITypeSymbol type)
        {
            string name = GetSerializerName(type);
            string ns = type.ContainingNamespace?.IsGlobalNamespace == true
                ? ""
                : type.ContainingNamespace?.ToDisplayString() ?? "";
            return string.IsNullOrEmpty(ns)
                ? $"global::{name}"
                : $"global::{ns}.{name}";
        }

        private static bool IsProtoContract(INamedTypeSymbol? attrClass)
        {
            if (attrClass == null) return false;
            return attrClass.Name is ProtoContractName or ProtoContractShort;
        }

        internal static AttributeData? GetProtoMemberAttribute(ImmutableArray<AttributeData> attrs)
        {
            foreach (var attr in attrs)
            {
                if (attr.AttributeClass?.Name is ProtoMemberName or ProtoMemberShort)
                {
                    if (attr.ConstructorArguments.Length > 0)
                        return attr;
                }
            }
            return null;
        }

        private static PbWire GetWire(AttributeData attr)
        {
            foreach (var kvp in attr.NamedArguments)
            {
                if (kvp.Key == "Wire" && kvp.Value.Value is int wireValue)
                    return (PbWire)wireValue;
            }
            return PbWire.Varint;
        }
    }
}
