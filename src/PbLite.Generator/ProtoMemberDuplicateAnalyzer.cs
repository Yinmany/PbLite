using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PbLite.Generator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ProtoMemberDuplicateAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "N3PROTO001";
        public const string PrivateNestedDiagnosticId = "N3PROTO002";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            "PbMember 序号重复",
            "[PbMember({0})] 序号与 '{1}' 重复.",
            "PbLite",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "同一个 [ProtoContract] 类中 [ProtoMember] 的序号不能重复.");

        private static readonly DiagnosticDescriptor PrivateNestedRule = new(
            PrivateNestedDiagnosticId,
            "PbContract 不能用于私有嵌套类型",
            "[PbContract] 不能用于私有嵌套类型 '{0}'，生成的序列化器无法访问该类型.",
            "PbLite",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "私有嵌套类型无法被生成的序列化器访问，请将类型改为非嵌套或提高可访问性.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, PrivateNestedRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var typeDecl = (TypeDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
            if (symbol == null) return;
            if (symbol.TypeKind != TypeKind.Class && symbol.TypeKind != TypeKind.Structure) return;
            if (!SymbolParser.HasProtoContract(symbol)) return;

            // N3PROTO002: private nested types are inaccessible to the generated serializer
            if (symbol.ContainingType != null && symbol.DeclaredAccessibility == Accessibility.Private)
            {
                var loc = symbol.Locations.FirstOrDefault();
                if (loc != null)
                    context.ReportDiagnostic(Diagnostic.Create(PrivateNestedRule, loc, symbol.Name));
                return;
            }

            var seen = new Dictionary<int, string>();

            foreach (var member in symbol.GetMembers())
            {
                int? order = null;

                if (member is IPropertySymbol { IsStatic: false } prop)
                {
                    order = GetProtoMemberOrder(prop.GetAttributes());
                }
                else if (member is IFieldSymbol { IsStatic: false } field)
                {
                    order = GetProtoMemberOrder(field.GetAttributes());
                }

                if (order == null) continue;

                if (seen.TryGetValue(order.Value, out var existingName))
                {
                    var location = member.Locations.FirstOrDefault();
                    if (location != null)
                    {
                        var diagnostic = Diagnostic.Create(Rule, location, order.Value, existingName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                else
                {
                    seen[order.Value] = member.Name;
                }
            }
        }

        private static int? GetProtoMemberOrder(ImmutableArray<AttributeData> attrs)
        {
            var attr = SymbolParser.GetProtoMemberAttribute(attrs);
            return attr != null ? (int)attr.ConstructorArguments[0].Value! : null;
        }
    }
}
