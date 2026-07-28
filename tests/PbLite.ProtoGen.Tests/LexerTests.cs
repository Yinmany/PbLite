using System.Linq;

namespace PbLite.ProtoGen.Tests;

public class LexerTests
{
    private static List<Token> Lex(string source) => new ProtoLexer(source).Tokenize();

    [Fact]
    public void Tokenize_BasicKeywords()
    {
        var tokens = Lex("syntax = \"proto3\";");
        var types = tokens.Select(t => t.Type).ToList();
        Assert.Equal(TokenType.Identifier, types[0]);
        Assert.Equal("syntax", tokens[0].Value);
        Assert.Equal(TokenType.Symbol, types[1]);
        Assert.Equal("=", tokens[1].Value);
        Assert.Equal(TokenType.String, types[2]);
        Assert.Equal("proto3", tokens[2].Value);
        Assert.Equal(TokenType.Symbol, types[3]);
        Assert.Equal(";", tokens[3].Value);
    }

    [Fact]
    public void Tokenize_LineComment_Skipped()
    {
        var tokens = Lex("// this is a comment\nmessage Foo;");
        var ids = tokens.Where(t => t.Type == TokenType.Identifier).Select(t => t.Value).ToList();
        Assert.Equal(new[] { "message", "Foo" }, ids);
    }

    [Fact]
    public void Tokenize_BlockComment_Skipped()
    {
        var tokens = Lex("/* block\ncomment */\nmessage Foo;");
        var ids = tokens.Where(t => t.Type == TokenType.Identifier).Select(t => t.Value).ToList();
        Assert.Equal(new[] { "message", "Foo" }, ids);
    }

    [Fact]
    public void Tokenize_HexNumber()
    {
        var tokens = Lex("field = 0xFF;");
        Assert.Equal(TokenType.Number, tokens[2].Type);
        Assert.Equal("0xFF", tokens[2].Value);
    }

    [Fact]
    public void Tokenize_SingleQuotedString()
    {
        var tokens = Lex("option x = 'hello';");
        Assert.Equal(TokenType.String, tokens[3].Type);
        Assert.Equal("hello", tokens[3].Value);
    }

    [Fact]
    public void Tokenize_EscapedString()
    {
        var tokens = Lex("option x = \"a\\nb\";");
        Assert.Equal("a\nb", tokens[3].Value);
    }

    [Fact]
    public void Tokenize_DottedIdentifier()
    {
        var tokens = Lex("foo.bar.Baz");
        // Lexer treats dots as Symbols, identifiers separately
        var ids = tokens.Where(t => t.Type == TokenType.Identifier).Select(t => t.Value).ToList();
        Assert.Equal(new[] { "foo", "bar", "Baz" }, ids);
    }

    [Fact]
    public void Tokenize_NumberWithDecimal()
    {
        var tokens = Lex("x = 3.14;");
        Assert.Equal(TokenType.Number, tokens[2].Type);
        Assert.Equal("3.14", tokens[2].Value);
    }

    [Fact]
    public void Tokenize_EndOfFile()
    {
        var tokens = Lex(";");
        Assert.Equal(TokenType.EndOfFile, tokens[^1].Type);
    }
}
