using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PbLite.Gen;

class ProtoParser
{
    private readonly List<Token> _tokens;
    private int _pos;

    public ProtoParser(List<Token> tokens)
    {
        _tokens = tokens;
        _pos = 0;
    }

    private Token Current => _tokens[_pos];
    private Token Peek(int offset = 0) =>
        _tokens[Math.Min(_pos + offset, _tokens.Count - 1)];

    private Token Advance()
    {
        var t = Current;
        if (_pos < _tokens.Count - 1) _pos++;
        return t;
    }

    private Token Expect(TokenType type)
    {
        if (Current.Type != type)
            throw new ProtoParseException(
                $"Expected {type} but got {Current.Type} '{Current.Value}'",
                Current.Line, Current.Column);
        return Advance();
    }

    private Token ExpectSymbol(string symbol)
    {
        if (Current.Type != TokenType.Symbol || Current.Value != symbol)
            throw new ProtoParseException(
                $"Expected '{symbol}' but got '{Current.Value}'",
                Current.Line, Current.Column);
        return Advance();
    }

    private bool MatchKeyword(string keyword) =>
        Current.Type == TokenType.Identifier && Current.Value == keyword;

    private bool MatchSymbol(string symbol) =>
        Current.Type == TokenType.Symbol && Current.Value == symbol;

    public ProtoFile Parse()
    {
        var file = new ProtoFile();

        // syntax = "proto3";
        if (MatchKeyword("syntax"))
        {
            Advance();
            ExpectSymbol("=");
            file.Syntax = Expect(TokenType.String).Value;
            ExpectSymbol(";");
        }

        while (Current.Type != TokenType.EndOfFile)
        {
            if (MatchKeyword("package")) ParsePackage(file);
            else if (MatchKeyword("import")) ParseImport(file);
            else if (MatchKeyword("option")) ParseFileOption(file);
            else if (MatchKeyword("message")) file.Messages.Add(ParseMessage());
            else if (MatchKeyword("enum")) file.Enums.Add(ParseEnum());
            else if (MatchKeyword("extend")) SkipBlock();
            else if (MatchSymbol(";")) Advance();
            else throw new ProtoParseException(
                $"Unexpected token '{Current.Value}'", Current.Line, Current.Column);
        }

        return file;
    }

    private void ParsePackage(ProtoFile file)
    {
        Advance();
        file.Package = ReadFullIdent();
        ExpectSymbol(";");
    }

    private void ParseImport(ProtoFile file)
    {
        Advance();
        if (MatchKeyword("public") || MatchKeyword("weak"))
            Advance();
        file.Imports.Add(Expect(TokenType.String).Value);
        ExpectSymbol(";");
    }

    private void ParseFileOption(ProtoFile file)
    {
        var opt = ParseOption();
        if (opt == null) return;

        file.Options[opt.Value.name] = opt.Value.value;
        if (opt.Value.name == "csharp_namespace")
            file.CSharpNamespace = opt.Value.value;
    }

    /// <summary>
    /// Parse a single `option name = value;` statement.
    /// Handles string, number, identifier, and aggregate `{ ... }` values.
    /// Returns null for custom options `option (xxx) = ...;` which are skipped.
    /// </summary>
    private (string name, string value)? ParseOption()
    {
        Advance(); // "option"

        // Skip custom options: option (xxx) = ...;
        if (MatchSymbol("("))
        {
            while (!MatchSymbol(";") && Current.Type != TokenType.EndOfFile)
                Advance();
            if (MatchSymbol(";")) Advance();
            return null;
        }

        string name = ReadFullIdent();
        ExpectSymbol("=");
        string value = ReadOptionValue();
        ExpectSymbol(";");
        return (name, value);
    }

    /// <summary>
    /// Read an option value: string, number, identifier, or aggregate `{ ... }`.
    /// </summary>
    private string ReadOptionValue()
    {
        // Aggregate value: { a: 1 b: "x" c: true }
        if (MatchSymbol("{"))
        {
            int depth = 0;
            var sb = new StringBuilder();
            do
            {
                if (MatchSymbol("{")) depth++;
                else if (MatchSymbol("}")) depth--;
                sb.Append(Current.Value);
                Advance();
            } while (depth > 0 && Current.Type != TokenType.EndOfFile);
            return sb.ToString();
        }

        // Simple value: string, number, or identifier
        string value = Current.Value;
        Advance();
        return value;
    }

    private ProtoMessage ParseMessage()
    {
        Advance(); // "message"
        var msg = new ProtoMessage { Name = ExpectIdent() };
        ExpectSymbol("{");

        while (!MatchSymbol("}"))
        {
            if (MatchKeyword("message")) msg.NestedMessages.Add(ParseMessage());
            else if (MatchKeyword("enum")) msg.NestedEnums.Add(ParseEnum());
            else if (MatchKeyword("reserved")) ParseReserved(msg);
            else if (MatchKeyword("option"))
            {
                var opt = ParseOption();
                if (opt == null) continue;
                msg.Options[opt.Value.name] = opt.Value.value;
                if (opt.Value.name == "deprecated")
                    msg.Deprecated = opt.Value.value == "true";
            }
            else if (MatchKeyword("map")) msg.Fields.Add(ParseMapField());
            else if (MatchKeyword("repeated") || MatchKeyword("optional"))
                msg.Fields.Add(ParseField());
            else if (MatchKeyword("oneof")) SkipBlock();
            else if (MatchKeyword("extend")) SkipBlock();
            else if (MatchKeyword("extensions")) SkipUntilSemicolon();
            else if (MatchSymbol(";")) Advance();
            else msg.Fields.Add(ParseField());
        }
        ExpectSymbol("}");
        return msg;
    }

    private ProtoField ParseField()
    {
        var field = new ProtoField();

        if (MatchKeyword("repeated"))
        {
            field.Label = FieldLabel.Repeated;
            Advance();
        }
        else if (MatchKeyword("optional"))
        {
            field.Label = FieldLabel.Optional;
            Advance();
        }

        field.ProtoType = ReadType();
        field.Name = ExpectIdent();
        ExpectSymbol("=");
        field.Number = ExpectIntNumber();

        if (MatchSymbol("["))
        {
            Advance();
            ParseFieldOptions(field);
            ExpectSymbol("]");
        }
        ExpectSymbol(";");
        return field;
    }

    private ProtoField ParseMapField()
    {
        Advance(); // "map"
        ExpectSymbol("<");
        var field = new ProtoField
        {
            IsMap = true,
            MapKeyType = ReadType(),
            Label = FieldLabel.Singular,
        };
        ExpectSymbol(",");
        field.MapValueType = ReadType();
        ExpectSymbol(">");
        field.Name = ExpectIdent();
        ExpectSymbol("=");
        field.Number = ExpectIntNumber();

        if (MatchSymbol("["))
        {
            Advance();
            ParseFieldOptions(field);
            ExpectSymbol("]");
        }
        ExpectSymbol(";");
        return field;
    }

    private void ParseFieldOptions(ProtoField field)
    {
        do
        {
            string name = ExpectIdent();
            ExpectSymbol("=");
            string value = ReadOptionValue();

            field.Options[name] = value;
            if (name == "packed")
                field.Packed = value == "true";
            else if (name == "deprecated")
                field.Deprecated = value == "true";

            if (MatchSymbol(",")) Advance();
            else break;
        } while (true);
    }

    private ProtoEnum ParseEnum()
    {
        Advance(); // "enum"
        var en = new ProtoEnum { Name = ExpectIdent() };
        ExpectSymbol("{");

        while (!MatchSymbol("}"))
        {
            if (MatchKeyword("option"))
            {
                var opt = ParseOption();
                if (opt == null) continue;
                if (opt.Value.name == "deprecated")
                    en.Deprecated = opt.Value.value == "true";
                continue;
            }
            if (MatchSymbol(";")) { Advance(); continue; }

            string name = ExpectIdent();
            ExpectSymbol("=");
            int number = ExpectIntNumber();

            // skip enum value options [opt = val, ...]
            if (MatchSymbol("["))
            {
                Advance();
                while (!MatchSymbol("]") && Current.Type != TokenType.EndOfFile)
                    Advance();
                if (MatchSymbol("]")) Advance();
            }
            ExpectSymbol(";");
            en.Values.Add(new ProtoEnumValue { Name = name, Number = number });
        }
        ExpectSymbol("}");
        return en;
    }

    private void ParseReserved(ProtoMessage msg)
    {
        Advance(); // "reserved"

        if (Current.Type == TokenType.String)
        {
            msg.ReservedNames.Add(Expect(TokenType.String).Value);
            while (MatchSymbol(","))
            {
                Advance();
                msg.ReservedNames.Add(Expect(TokenType.String).Value);
            }
        }
        else
        {
            do
            {
                string start = Expect(TokenType.Number).Value;
                if (MatchKeyword("to"))
                {
                    Advance();
                    string end = MatchKeyword("max") ? "max" : Expect(TokenType.Number).Value;
                    if (MatchKeyword("max")) Advance();
                    msg.ReservedNumbers.Add($"{start} to {end}");
                }
                else
                {
                    msg.ReservedNumbers.Add(start);
                }
                if (MatchSymbol(",")) Advance();
                else break;
            } while (true);
        }
        ExpectSymbol(";");
    }

    private string ReadFullIdent()
    {
        var sb = new StringBuilder();
        sb.Append(ExpectIdent());
        while (MatchSymbol("."))
        {
            sb.Append('.');
            Advance();
            sb.Append(ExpectIdent());
        }
        return sb.ToString();
    }

    private string ReadType()
    {
        bool leadingDot = MatchSymbol(".");
        if (leadingDot) Advance();

        var sb = new StringBuilder();
        if (leadingDot) sb.Append('.');
        sb.Append(ExpectIdent());

        while (MatchSymbol("."))
        {
            sb.Append('.');
            Advance();
            sb.Append(ExpectIdent());
        }
        return sb.ToString();
    }

    private string ExpectIdent()
    {
        if (Current.Type != TokenType.Identifier)
            throw new ProtoParseException(
                $"Expected identifier but got {Current.Type} '{Current.Value}'",
                Current.Line, Current.Column);
        return Advance().Value;
    }

    private int ExpectIntNumber()
    {
        bool negative = MatchSymbol("-");
        if (negative) Advance();
        int value = ParseInt(Expect(TokenType.Number).Value);
        return negative ? -value : value;
    }

    private static int ParseInt(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToInt32(value, 16);
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    private void SkipBlock()
    {
        Advance(); // keyword
        ReadType(); // type name
        ExpectSymbol("{");
        int depth = 1;
        while (depth > 0 && Current.Type != TokenType.EndOfFile)
        {
            if (MatchSymbol("{")) depth++;
            else if (MatchSymbol("}")) depth--;
            Advance();
        }
    }

    private void SkipUntilSemicolon()
    {
        Advance();
        while (!MatchSymbol(";") && Current.Type != TokenType.EndOfFile)
            Advance();
        if (MatchSymbol(";")) Advance();
    }
}
