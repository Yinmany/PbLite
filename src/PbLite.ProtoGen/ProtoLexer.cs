using System;
using System.Collections.Generic;
using System.Text;

namespace PbLite.ProtoGen;

enum TokenType { Identifier, Number, String, Symbol, EndOfFile }

readonly struct Token(TokenType type, string value, int line, int column)
{
    public TokenType Type { get; } = type;
    public string Value { get; } = value;
    public int Line { get; } = line;
    public int Column { get; } = column;

    public override string ToString() => $"{Type}('{Value}') at {Line}:{Column}";
}

class ProtoParseException(string message, int line, int column)
    : Exception($"Proto parse error at line {line}, column {column}: {message}")
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}

class ProtoLexer
{
    private readonly string _source;
    private int _pos;
    private int _line = 1;
    private int _col = 1;

    public ProtoLexer(string source)
    {
        _source = source;
        _pos = 0;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (_pos < _source.Length)
        {
            SkipWhitespaceAndComments();
            if (_pos >= _source.Length) break;

            char c = _source[_pos];
            if (char.IsLetter(c) || c == '_')
                tokens.Add(ReadIdentifier());
            else if (char.IsDigit(c))
                tokens.Add(ReadNumber());
            else if (c == '"' || c == '\'')
                tokens.Add(ReadString());
            else
                tokens.Add(ReadSymbol());
        }
        tokens.Add(new Token(TokenType.EndOfFile, "", _line, _col));
        return tokens;
    }

    private void SkipWhitespaceAndComments()
    {
        while (_pos < _source.Length)
        {
            char c = _source[_pos];
            if (c == ' ' || c == '\t' || c == '\r')
            {
                _pos++; _col++;
            }
            else if (c == '\n')
            {
                _pos++; _line++; _col = 1;
            }
            else if (c == '/' && Peek(1) == '/')
            {
                _pos += 2; _col += 2;
                while (_pos < _source.Length && _source[_pos] != '\n')
                {
                    _pos++; _col++;
                }
            }
            else if (c == '/' && Peek(1) == '*')
            {
                _pos += 2; _col += 2;
                while (_pos + 1 < _source.Length && !(_source[_pos] == '*' && _source[_pos + 1] == '/'))
                {
                    if (_source[_pos] == '\n') { _line++; _col = 1; }
                    else _col++;
                    _pos++;
                }
                if (_pos + 1 < _source.Length) { _pos += 2; _col += 2; }
            }
            else break;
        }
    }

    private char Peek(int offset) => _pos + offset < _source.Length ? _source[_pos + offset] : '\0';

    private Token ReadIdentifier()
    {
        int startLine = _line, startCol = _col;
        var sb = new StringBuilder();
        while (_pos < _source.Length)
        {
            char c = _source[_pos];
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c); _pos++; _col++;
            }
            else break;
        }
        return new Token(TokenType.Identifier, sb.ToString(), startLine, startCol);
    }

    private Token ReadNumber()
    {
        int startLine = _line, startCol = _col;
        var sb = new StringBuilder();

        if (_source[_pos] == '0' && Peek(1) is 'x' or 'X')
        {
            sb.Append(_source[_pos]); sb.Append(_source[_pos + 1]);
            _pos += 2; _col += 2;
            while (_pos < _source.Length && IsHexDigit(_source[_pos]))
            {
                sb.Append(_source[_pos]); _pos++; _col++;
            }
        }
        else
        {
            while (_pos < _source.Length && (char.IsDigit(_source[_pos]) || _source[_pos] == '.'))
            {
                sb.Append(_source[_pos]); _pos++; _col++;
            }
            if (_pos < _source.Length && (_source[_pos] == 'e' || _source[_pos] == 'E'))
            {
                sb.Append(_source[_pos]); _pos++; _col++;
                if (_pos < _source.Length && (_source[_pos] == '+' || _source[_pos] == '-'))
                {
                    sb.Append(_source[_pos]); _pos++; _col++;
                }
                while (_pos < _source.Length && char.IsDigit(_source[_pos]))
                {
                    sb.Append(_source[_pos]); _pos++; _col++;
                }
            }
        }
        return new Token(TokenType.Number, sb.ToString(), startLine, startCol);
    }

    private static bool IsHexDigit(char c) =>
        char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private Token ReadString()
    {
        int startLine = _line, startCol = _col;
        char quote = _source[_pos];
        _pos++; _col++;
        var sb = new StringBuilder();
        while (_pos < _source.Length && _source[_pos] != quote)
        {
            if (_source[_pos] == '\\' && _pos + 1 < _source.Length)
            {
                char escaped = _source[_pos + 1];
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '\\' => '\\',
                    '\'' => '\'',
                    '"' => '"',
                    _ => escaped,
                });
                _pos += 2; _col += 2;
            }
            else
            {
                if (_source[_pos] == '\n') { _line++; _col = 1; }
                else _col++;
                sb.Append(_source[_pos]); _pos++;
            }
        }
        if (_pos < _source.Length) { _pos++; _col++; }
        return new Token(TokenType.String, sb.ToString(), startLine, startCol);
    }

    private Token ReadSymbol()
    {
        int startLine = _line, startCol = _col;
        char c = _source[_pos];
        _pos++; _col++;
        return new Token(TokenType.Symbol, c.ToString(), startLine, startCol);
    }
}
