using System;
using System.Collections.Generic;
using System.Text;

namespace Runtime.Sml;

public sealed class SmlParser
{
    private readonly SmlLexer _lexer;
    private SmlToken _lookahead;
    private readonly SmlParserSchema _schema;

    public SmlParser(string text, SmlParserSchema? schema = null)
    {
        _lexer = new SmlLexer(text);
        _lookahead = _lexer.NextToken();
        _schema = schema ?? new SmlParserSchema();
    }

    public SmlDocument ParseDocument()
    {
        var document = new SmlDocument();
        SkipIgnorables();

        while (_lookahead.Kind != SmlTokenKind.Eof)
        {
            document.Roots.Add(ParseElement(document));
            SkipIgnorables();
        }

        return document;
    }

    private SmlNode ParseElement(SmlDocument document)
    {
        var nameToken = Expect(SmlTokenKind.Identifier, "Expected element name.");
        SkipIgnorables();
        Expect(SmlTokenKind.LBrace, "Expected '{' after element name.");

        var node = new SmlNode
        {
            Name = nameToken.Text,
            Line = nameToken.Line
        };

        WarnUnknownNodeIfNeeded(document, node.Name, node.Line);

        SkipIgnorables();
        while (_lookahead.Kind != SmlTokenKind.RBrace && _lookahead.Kind != SmlTokenKind.Eof)
        {
            var ident = Expect(SmlTokenKind.Identifier, "Expected property or nested element.");
            SkipIgnorables();

            if (_lookahead.Kind == SmlTokenKind.Colon)
            {
                Consume();
                SkipIgnorables();
                node.Properties[ident.Text] = ParseValue(ident.Text, document);
                SkipIgnorables();
                continue;
            }

            if (_lookahead.Kind != SmlTokenKind.LBrace)
            {
                throw Error($"Expected ':' or '{{' after '{ident.Text}'.");
            }

            Consume();
            var child = ParseElementBody(document, ident.Text, ident.Line);
            node.Children.Add(child);
            Expect(SmlTokenKind.RBrace, "Expected '}' at end of nested element.");
            SkipIgnorables();
        }

        Expect(SmlTokenKind.RBrace, "Expected '}' at end of element.");
        return node;
    }

    private SmlNode ParseElementBody(SmlDocument document, string name, int line)
    {
        var node = new SmlNode
        {
            Name = name,
            Line = line
        };

        WarnUnknownNodeIfNeeded(document, node.Name, node.Line);

        SkipIgnorables();
        while (_lookahead.Kind != SmlTokenKind.RBrace && _lookahead.Kind != SmlTokenKind.Eof)
        {
            var ident = Expect(SmlTokenKind.Identifier, "Expected property or nested element.");
            SkipIgnorables();

            if (_lookahead.Kind == SmlTokenKind.Colon)
            {
                Consume();
                SkipIgnorables();
                node.Properties[ident.Text] = ParseValue(ident.Text, document);
                SkipIgnorables();
                continue;
            }

            if (_lookahead.Kind != SmlTokenKind.LBrace)
            {
                throw Error($"Expected ':' or '{{' after '{ident.Text}'.");
            }

            Consume();
            var child = ParseElementBody(document, ident.Text, ident.Line);
            node.Children.Add(child);
            Expect(SmlTokenKind.RBrace, "Expected '}' at end of nested element.");
            SkipIgnorables();
        }

        return node;
    }

    private SmlValue ParseValue(string propertyName, SmlDocument document)
    {
        if (_lookahead.Kind == SmlTokenKind.String)
        {
            return SmlValue.FromString(Consume().Text);
        }

        if (_lookahead.Kind == SmlTokenKind.Bool)
        {
            var token = Consume();
            return SmlValue.FromBool(string.Equals(token.Text, "true", StringComparison.OrdinalIgnoreCase));
        }

        if (_lookahead.Kind == SmlTokenKind.Int)
        {
            var first = int.Parse(Consume().Text);
            SkipIgnorables();
            if (_lookahead.Kind != SmlTokenKind.Comma)
            {
                return SmlValue.FromInt(first);
            }

            Consume();
            SkipIgnorables();
            var secondToken = Expect(SmlTokenKind.Int, "Expected integer component after ','.");
            var second = int.Parse(secondToken.Text);
            SkipIgnorables();

            if (_lookahead.Kind == SmlTokenKind.Comma)
            {
                Consume();
                SkipIgnorables();
                var thirdToken = Expect(SmlTokenKind.Int, "Expected integer component after ','.");
                var third = int.Parse(thirdToken.Text);
                return SmlValue.FromVec3i(first, second, third);
            }

            return SmlValue.FromVec2i(first, second);
        }

        if (_lookahead.Kind == SmlTokenKind.Identifier)
        {
            var token = Consume();
            var propertyKind = _schema.GetPropertyKind(propertyName);

            if (propertyKind == SmlPropertyKind.Identifier)
            {
                return SmlValue.FromIdentifier(token.Text);
            }

            if (propertyKind == SmlPropertyKind.Enum)
            {
                if (_schema.TryResolveEnum(propertyName, token.Text, out var enumValue))
                {
                    return SmlValue.FromEnum(token.Text, enumValue);
                }

                document.Warnings.Add($"Unknown enum value '{token.Text}' for property '{propertyName}' at line {token.Line}.");
                return SmlValue.FromEnum(token.Text, null);
            }

            throw new SmlParseException(
                $"Unquoted identifier '{token.Text}' for property '{propertyName}' is not allowed. " +
                "Register the property as Identifier/Enum or use a quoted string. " +
                $"(line {token.Line}, col {token.Column})"
            );
        }

        throw Error("Expected value.");
    }

    private void SkipIgnorables()
    {
        while (_lookahead.Kind is SmlTokenKind.Whitespace or SmlTokenKind.LineComment or SmlTokenKind.BlockComment)
        {
            Consume();
        }
    }

    private SmlToken Expect(SmlTokenKind kind, string message)
    {
        if (_lookahead.Kind != kind)
        {
            throw Error(message);
        }

        return Consume();
    }

    private SmlToken Consume()
    {
        var current = _lookahead;
        _lookahead = _lexer.NextToken();
        return current;
    }

    private SmlParseException Error(string message)
    {
        return new SmlParseException($"{message} at line {_lookahead.Line}, col {_lookahead.Column}");
    }

    private void WarnUnknownNodeIfNeeded(SmlDocument document, string nodeName, int line)
    {
        if (!_schema.WarnOnUnknownNodes)
        {
            return;
        }

        if (_schema.IsKnownNode(nodeName))
        {
            return;
        }

        document.Warnings.Add($"Unknown node '{nodeName}' at line {line}. Node will be kept in AST.");
    }
}

internal enum SmlTokenKind
{
    Identifier,
    LBrace,
    RBrace,
    Colon,
    Comma,
    String,
    Int,
    Bool,
    Whitespace,
    LineComment,
    BlockComment,
    Eof
}

internal readonly record struct SmlToken(SmlTokenKind Kind, string Text, int Line, int Column);

internal sealed class SmlLexer
{
    private readonly string _input;
    private int _index;
    private int _line = 1;
    private int _column = 1;

    public SmlLexer(string input)
    {
        _input = input ?? string.Empty;
    }

    public SmlToken NextToken()
    {
        if (IsEof)
        {
            return new SmlToken(SmlTokenKind.Eof, string.Empty, _line, _column);
        }

        var startLine = _line;
        var startColumn = _column;
        var c = Peek();

        if (char.IsWhiteSpace(c))
        {
            var text = ReadWhile(char.IsWhiteSpace);
            return new SmlToken(SmlTokenKind.Whitespace, text, startLine, startColumn);
        }

        if (c == '/' && Peek(1) == '/')
        {
            var sb = new StringBuilder();
            while (!IsEof && Peek() != '\n')
            {
                sb.Append(Advance());
            }

            return new SmlToken(SmlTokenKind.LineComment, sb.ToString(), startLine, startColumn);
        }

        if (c == '/' && Peek(1) == '*')
        {
            var sb = new StringBuilder();
            sb.Append(Advance());
            sb.Append(Advance());

            while (!IsEof)
            {
                if (Peek() == '*' && Peek(1) == '/')
                {
                    sb.Append(Advance());
                    sb.Append(Advance());
                    break;
                }

                sb.Append(Advance());
            }

            return new SmlToken(SmlTokenKind.BlockComment, sb.ToString(), startLine, startColumn);
        }

        if (c == '{')
        {
            Advance();
            return new SmlToken(SmlTokenKind.LBrace, "{", startLine, startColumn);
        }

        if (c == '}')
        {
            Advance();
            return new SmlToken(SmlTokenKind.RBrace, "}", startLine, startColumn);
        }

        if (c == ':')
        {
            Advance();
            return new SmlToken(SmlTokenKind.Colon, ":", startLine, startColumn);
        }

        if (c == ',')
        {
            Advance();
            return new SmlToken(SmlTokenKind.Comma, ",", startLine, startColumn);
        }

        if (c == '"')
        {
            return ReadString(startLine, startColumn);
        }

        if (char.IsLetter(c) || c == '_')
        {
            var ident = ReadIdentifier();
            if (string.Equals(ident, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(ident, "false", StringComparison.OrdinalIgnoreCase))
            {
                return new SmlToken(SmlTokenKind.Bool, ident, startLine, startColumn);
            }

            return new SmlToken(SmlTokenKind.Identifier, ident, startLine, startColumn);
        }

        if (char.IsDigit(c) || c == '-')
        {
            return ReadNumber(startLine, startColumn);
        }

        throw new SmlParseException($"Unexpected character '{c}' at line {startLine}, col {startColumn}");
    }

    private SmlToken ReadString(int startLine, int startColumn)
    {
        Advance(); // opening quote
        var sb = new StringBuilder();

        while (!IsEof)
        {
            var c = Peek();
            if (c == '"')
            {
                Advance();
                return new SmlToken(SmlTokenKind.String, sb.ToString(), startLine, startColumn);
            }

            if (c == '\\')
            {
                Advance();
                if (IsEof)
                {
                    break;
                }

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '"' => '"',
                    '\\' => '\\',
                    _ => escaped
                });
                continue;
            }

            sb.Append(Advance());
        }

        throw new SmlParseException($"Unterminated string literal at line {startLine}, col {startColumn}");
    }

    private string ReadIdentifier()
    {
        var sb = new StringBuilder();
        while (!IsEof)
        {
            var c = Peek();
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
            {
                break;
            }

            sb.Append(Advance());
        }

        return sb.ToString();
    }

    private SmlToken ReadNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        if (Peek() == '-')
        {
            sb.Append(Advance());
        }

        while (!IsEof)
        {
            var c = Peek();
            if (char.IsDigit(c))
            {
                sb.Append(Advance());
                continue;
            }

            if (c == '.')
            {
                throw new SmlParseException($"Float values are not supported. Use scaled integers instead (line {startLine}, col {startColumn}).");
            }

            break;
        }

        var text = sb.ToString();
        return new SmlToken(SmlTokenKind.Int, text, startLine, startColumn);
    }

    private string ReadWhile(Func<char, bool> predicate)
    {
        var sb = new StringBuilder();
        while (!IsEof && predicate(Peek()))
        {
            sb.Append(Advance());
        }

        return sb.ToString();
    }

    private bool IsEof => _index >= _input.Length;

    private char Peek(int offset = 0)
    {
        var index = _index + offset;
        if (index < 0 || index >= _input.Length)
        {
            return '\0';
        }

        return _input[index];
    }

    private char Advance()
    {
        if (IsEof)
        {
            return '\0';
        }

        var c = _input[_index++];
        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }
}
