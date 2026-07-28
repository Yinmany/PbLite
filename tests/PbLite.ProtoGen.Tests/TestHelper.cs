using System.Linq;

namespace PbLite.ProtoGen.Tests;

static class TestHelper
{
    /// <summary>
    /// Run proto source through the full Lexer → Parser → Emitter pipeline,
    /// return the generated C# code.
    /// </summary>
    public static string Generate(string protoSource, string? overrideNamespace = null)
    {
        var lexer = new ProtoLexer(protoSource);
        var tokens = lexer.Tokenize();
        var parser = new ProtoParser(tokens);
        var protoFile = parser.Parse();

        var emitter = new CSharpEmitter();
        var outputs = emitter.Emit(protoFile, "test", overrideNamespace, multipleFiles: false);
        return outputs.First().Content;
    }
}
