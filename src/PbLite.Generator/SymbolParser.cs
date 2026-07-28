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
            var classDecl = (ClassDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
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
                    var attr = GetProtoMember(prop.GetAttributes());
                    if (attr == null) continue;

                    bool isReadOnly = prop.SetMethod == null || prop.IsReadOnly;
                    members.Add(new MemberInfo(prop.Name, prop.Type, attr.Value, isReadOnly));
                }
                else if (member is IFieldSymbol { IsStatic: false } field)
                {
                    var attr = GetProtoMember(field.GetAttributes());
                    if (attr == null) continue;

                    members.Add(new MemberInfo(field.Name, field.Type, attr.Value, field.IsReadOnly));
                }
            }

            if (members.Count == 0) return null;

            string ns = symbol.ContainingNamespace?.IsGlobalNamespace == true
                ? ""
                : symbol.ContainingNamespace?.ToDisplayString() ?? "";

            return new TypeInfo(symbol.Name, ns, members);
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

        private static bool IsProtoContract(INamedTypeSymbol? attrClass)
        {
            if (attrClass == null) return false;
            return attrClass.Name is ProtoContractName or ProtoContractShort;
        }

        private static int? GetProtoMember(ImmutableArray<AttributeData> attrs)
        {
            foreach (var attr in attrs)
            {
                if (attr.AttributeClass?.Name is ProtoMemberName or ProtoMemberShort)
                {
                    if (attr.ConstructorArguments.Length > 0)
                        return (int)attr.ConstructorArguments[0].Value!;
                }
            }
            return null;
        }
    }
}
