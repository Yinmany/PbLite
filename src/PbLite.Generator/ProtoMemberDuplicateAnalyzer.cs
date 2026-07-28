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

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            "PbMember 序号重复",
            "[PbMember({0})] 序号与 '{1}' 重复.",
            "PbLite",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "同一个 [ProtoContract] 类中 [ProtoMember] 的序号不能重复.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (symbol == null) return;
            if (symbol.TypeKind != TypeKind.Class) return;
            if (!SymbolParser.HasProtoContract(symbol)) return;

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
            foreach (var attr in attrs)
            {
                if (attr.AttributeClass?.Name is "PbMemberAttribute" or "PbMember")
                {
                    if (attr.ConstructorArguments.Length > 0)
                        return (int)attr.ConstructorArguments[0].Value!;
                }
            }
            return null;
        }
    }
}
