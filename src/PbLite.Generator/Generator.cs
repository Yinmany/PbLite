using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace PbLite.Generator
{
    [Generator]
    public class ProtoSerializerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                    transform: static (ctx, ct) => SymbolParser.GetTypeInfo(ctx))
                .Where(static info => info != null)!;

            context.RegisterSourceOutput(typeDeclarations, SerializerEmitter.Generate);

            var allTypes = typeDeclarations.Collect();
            var registryInput = allTypes.Combine(context.CompilationProvider);
            context.RegisterSourceOutput(registryInput, (ctx, pair) =>
                SerializerEmitter.GenerateRegistry(ctx, pair.Left, pair.Right));
        }
    }
}
