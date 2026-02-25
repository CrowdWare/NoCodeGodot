/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of SMLCore.
 *
 *  SMLCore is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  SMLCore is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with SMLCore.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace Runtime.Sml;

public sealed class SmlParser
{
    private readonly SmlLexer _lexer;
    private SmlToken _lookahead;
    private readonly SmlParserSchema _schema;
    private readonly Dictionary<string, int> _seenIdsInDocument = new(StringComparer.Ordinal);
    private static readonly Regex IdPattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public SmlParser(string text, SmlParserSchema? schema = null)
    {
        _lexer = new SmlLexer(text);
        _lookahead = _lexer.NextToken();
        _schema = schema ?? new SmlParserSchema();
    }

    public SmlDocument ParseDocument()
    {
        var document = new SmlDocument();
        _seenIdsInDocument.Clear();
        SkipIgnorables();

        while (_lookahead.Kind != SmlTokenKind.Eof)
        {
            var element = ParseElement(document);
            if (IsResourceNamespace(element.Name))
            {
                document.Resources[element.Name] = element;
            }
            else
            {
                document.Roots.Add(element);
            }
            SkipIgnorables();
        }

        ValidateResourceRefs(document);

        return document;
    }

    private static bool IsResourceNamespace(string name) =>
        string.Equals(name, "Strings", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Colors", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Icons", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Layouts", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Fonts", StringComparison.OrdinalIgnoreCase);

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
                var dotIdx = ident.Text.IndexOf('.');
                if (dotIdx > 0 && dotIdx < ident.Text.Length - 1)
                {
                    var qualifier = ident.Text[..dotIdx];
                    var propName = ident.Text[(dotIdx + 1)..];
                    var attachedValue = ParseValue(propName, document, node, ident.Line);
                    if (!node.AttachedProperties.TryGetValue(qualifier, out var attachedDict))
                    {
                        attachedDict = new Dictionary<string, SmlValue>(StringComparer.OrdinalIgnoreCase);
                        node.AttachedProperties[qualifier] = attachedDict;
                    }
                    attachedDict[propName] = attachedValue;
                }
                else
                {
                    node.Properties[ident.Text] = ParseValue(ident.Text, document, node, ident.Line);
                    node.PropertyLines[ident.Text] = ident.Line;
                }
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
                var dotIdx = ident.Text.IndexOf('.');
                if (dotIdx > 0 && dotIdx < ident.Text.Length - 1)
                {
                    var qualifier = ident.Text[..dotIdx];
                    var propName = ident.Text[(dotIdx + 1)..];
                    var attachedValue = ParseValue(propName, document, node, ident.Line);
                    if (!node.AttachedProperties.TryGetValue(qualifier, out var attachedDict))
                    {
                        attachedDict = new Dictionary<string, SmlValue>(StringComparer.OrdinalIgnoreCase);
                        node.AttachedProperties[qualifier] = attachedDict;
                    }
                    attachedDict[propName] = attachedValue;
                }
                else
                {
                    node.Properties[ident.Text] = ParseValue(ident.Text, document, node, ident.Line);
                    node.PropertyLines[ident.Text] = ident.Line;
                }
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

    private SmlValue ParseValue(string propertyName, SmlDocument document, SmlNode node, int propertyLine)
    {
        var propertyKind = _schema.GetPropertyKind(propertyName);

        if (_lookahead.Kind == SmlTokenKind.At)
        {
            return ParseResourceRef(propertyName, document, node, propertyLine);
        }

        if (_lookahead.Kind == SmlTokenKind.String)
        {
            if (propertyKind == SmlPropertyKind.Id)
            {
                throw new SmlParseException($"Property '{propertyName}' must be an unquoted identifier symbol (numeric identifier allowed) (line {propertyLine}).");
            }

            return SmlValue.FromString(Consume().Text);
        }

        if (_lookahead.Kind == SmlTokenKind.Bool)
        {
            var token = Consume();
            return SmlValue.FromBool(string.Equals(token.Text, "true", StringComparison.OrdinalIgnoreCase));
        }

        if (_lookahead.Kind is SmlTokenKind.Int or SmlTokenKind.Float)
        {
            var firstNumberToken = Consume();
            if (propertyKind == SmlPropertyKind.Id)
            {
                if (firstNumberToken.Kind == SmlTokenKind.Float)
                {
                    throw new SmlParseException($"Property '{propertyName}' id must be an integer numeric identifier, not float (line {propertyLine}).");
                }

                if (_lookahead.Kind == SmlTokenKind.Comma)
                {
                    throw new SmlParseException($"Property '{propertyName}' id must be a single value, not a tuple (line {propertyLine}).");
                }

                EnsureUniqueIdInDocument(firstNumberToken.Text, node, firstNumberToken.Line);
                return SmlValue.FromIdentifier(firstNumberToken.Text);
            }

            if (firstNumberToken.Kind == SmlTokenKind.Float)
            {
                if (_lookahead.Kind == SmlTokenKind.Comma)
                {
                    throw new SmlParseException(
                        $"Property '{propertyName}' does not support float tuple values. Use integer tuple values (line {_lookahead.Line}, col {_lookahead.Column}).");
                }

                return SmlValue.FromFloat(ParseFloatLiteral(firstNumberToken, propertyName));
            }

            var values = new List<int> { ParseIntLiteral(firstNumberToken, propertyName) };
            SkipIgnorables();

            while (_lookahead.Kind == SmlTokenKind.Comma)
            {
                Consume();
                SkipIgnorables();
                var componentToken = Expect(SmlTokenKind.Int, "Expected integer component after ','.");
                values.Add(ParseIntLiteral(componentToken, propertyName));
                SkipIgnorables();
            }

            if (propertyName.Equals("padding", StringComparison.OrdinalIgnoreCase))
            {
                return values.Count switch
                {
                    1 => SmlValue.FromPadding(values[0], values[0], values[0], values[0]),
                    2 => SmlValue.FromPadding(values[0], values[1], values[0], values[1]),
                    4 => SmlValue.FromPadding(values[0], values[1], values[2], values[3]),
                    3 => throw new SmlParseException(
                        $"Property 'padding' must contain 1, 2, or 4 integer values (top,right,bottom,left). 3 values are not supported. (line {_lookahead.Line}, col {_lookahead.Column})"),
                    _ => throw new SmlParseException(
                        $"Property 'padding' must contain 1, 2, or 4 integer values. Found {values.Count}. (line {_lookahead.Line}, col {_lookahead.Column})")
                };
            }

            return values.Count switch
            {
                1 => SmlValue.FromInt(values[0]),
                2 => SmlValue.FromVec2i(values[0], values[1]),
                3 => SmlValue.FromVec3i(values[0], values[1], values[2]),
                _ => throw new SmlParseException(
                    $"Property '{propertyName}' supports at most 3 integer tuple values (x,y,z). Found {values.Count}. (line {_lookahead.Line}, col {_lookahead.Column})")
            };
        }

        if (_lookahead.Kind == SmlTokenKind.Identifier)
        {
            if (propertyName.Equals("anchors", StringComparison.OrdinalIgnoreCase))
            {
                return ParseAnchorsValue();
            }

            var token = Consume();

            if (propertyKind == SmlPropertyKind.Identifier)
            {
                return SmlValue.FromIdentifier(token.Text);
            }

            if (propertyKind == SmlPropertyKind.Id)
            {
                if (!IdPattern.IsMatch(token.Text))
                {
                    throw new SmlParseException(
                        $"Invalid id '{token.Text}' for property '{propertyName}'. " +
                        "Expected ^[A-Za-z_][A-Za-z0-9_]*$ (line " + token.Line + ", col " + token.Column + ")");
                }

                EnsureUniqueIdInDocument(token.Text, node, token.Line);
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

    private void EnsureUniqueIdInDocument(string id, SmlNode node, int line)
    {
        if (_seenIdsInDocument.TryGetValue(id, out var firstLine))
        {
            throw new SmlParseException(
                $"Duplicate id '{id}' in SML document scope (first at line {firstLine}, duplicate at line {line}, node '{node.Name}').");
        }

        _seenIdsInDocument[id] = line;
    }

    private static int ParseIntLiteral(SmlToken token, string propertyName)
    {
        if (int.TryParse(token.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new SmlParseException(
            $"Invalid integer literal '{token.Text}' for property '{propertyName}' (line {token.Line}, col {token.Column}).");
    }

    private static double ParseFloatLiteral(SmlToken token, string propertyName)
    {
        if (double.TryParse(token.Text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new SmlParseException(
            $"Invalid float literal '{token.Text}' for property '{propertyName}' (line {token.Line}, col {token.Column}).");
    }

    private SmlValue ParseResourceRef(string propertyName, SmlDocument document, SmlNode node, int propertyLine)
    {
        Consume(); // consume '@'
        var refToken = Expect(SmlTokenKind.Identifier, "Expected resource reference (e.g. @Strings.greeting) after '@'.");
        var dotIdx = refToken.Text.IndexOf('.');
        if (dotIdx < 1 || dotIdx >= refToken.Text.Length - 1)
        {
            throw new SmlParseException(
                $"Resource reference '@{refToken.Text}' must be in the form '@Namespace.key' " +
                $"(line {refToken.Line}, col {refToken.Column}).");
        }

        var ns = refToken.Text[..dotIdx];
        var path = refToken.Text[(dotIdx + 1)..];

        SkipIgnorables();
        SmlValue? fallback = null;
        if (_lookahead.Kind == SmlTokenKind.Comma)
        {
            Consume(); // consume ','
            SkipIgnorables();
            fallback = ParseFallbackLiteral(propertyName);
        }

        return SmlValue.FromResourceRef(ns, path, fallback);
    }

    private SmlValue ParseFallbackLiteral(string propertyName)
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
            var token = Consume();
            return SmlValue.FromInt(ParseIntLiteral(token, propertyName));
        }

        if (_lookahead.Kind == SmlTokenKind.Float)
        {
            var token = Consume();
            return SmlValue.FromFloat(ParseFloatLiteral(token, propertyName));
        }

        throw Error($"Expected a literal fallback value (string, bool, number) after ',' in resource reference for '{propertyName}'.");
    }

    private static void ValidateResourceRefs(SmlDocument document)
    {
        foreach (var root in document.Roots)
        {
            ValidateResourceRefsInNode(document, root);
        }
    }

    private static void ValidateResourceRefsInNode(SmlDocument document, SmlNode node)
    {
        foreach (var (propName, value) in node.Properties)
        {
            if (value.Kind == SmlValueKind.ResourceRef)
            {
                ValidateResourceRef(document, node, propName, (SmlResourceRef)value.Value);
            }
        }

        foreach (var (qualifier, props) in node.AttachedProperties)
        {
            foreach (var (propName, value) in props)
            {
                if (value.Kind == SmlValueKind.ResourceRef)
                {
                    ValidateResourceRef(document, node, $"{qualifier}.{propName}", (SmlResourceRef)value.Value);
                }
            }
        }

        foreach (var child in node.Children)
        {
            ValidateResourceRefsInNode(document, child);
        }
    }

    private static void ValidateResourceRef(SmlDocument document, SmlNode node, string propName, SmlResourceRef res)
    {
        if (!document.Resources.TryGetValue(res.Namespace, out var nsNode))
        {
            // Well-known namespaces (Strings, Colors, Icons, Layouts) are resolved externally
            // from resource files at runtime — no warning needed for missing inline block.
            if (IsResourceNamespace(res.Namespace))
                return;

            // Unknown namespace — only warn if no fallback is provided
            if (res.Fallback is null)
            {
                document.Warnings.Add(
                    $"Unknown resource namespace '@{res.Namespace}' in '{node.Name}.{propName}' (line {node.Line}). " +
                    "Consider adding a fallback value or defining the resource block.");
            }

            return;
        }

        if (!nsNode.Properties.ContainsKey(res.Path) && res.Fallback is null)
        {
            document.Warnings.Add(
                $"Unknown resource key '@{res.Namespace}.{res.Path}' in '{node.Name}.{propName}' (line {node.Line}). " +
                "No fallback provided.");
        }
    }

    private SmlValue ParseAnchorsValue()
    {
        var anchors = new List<string>();

        anchors.Add(Expect(SmlTokenKind.Identifier, "Expected anchor name.").Text);
        SkipIgnorables();

        while (_lookahead.Kind is SmlTokenKind.Pipe or SmlTokenKind.Comma)
        {
            Consume();
            SkipIgnorables();
            anchors.Add(Expect(SmlTokenKind.Identifier, "Expected anchor name after separator.").Text);
            SkipIgnorables();
        }

        return SmlValue.FromString(string.Join(",", anchors));
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

        if (IsResourceNamespace(nodeName))
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
    Pipe,
    At,
    String,
    Int,
    Float,
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

        if (c == '|')
        {
            Advance();
            return new SmlToken(SmlTokenKind.Pipe, "|", startLine, startColumn);
        }

        if (c == '@')
        {
            Advance();
            return new SmlToken(SmlTokenKind.At, "@", startLine, startColumn);
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

        if (char.IsDigit(c) || c == '-' || (c == '.' && char.IsDigit(Peek(1))))
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

        var hasDigitsBeforeDot = false;
        while (!IsEof && char.IsDigit(Peek()))
        {
            hasDigitsBeforeDot = true;
            sb.Append(Advance());
        }

        var hasDot = false;
        var hasDigitsAfterDot = false;
        if (!IsEof && Peek() == '.')
        {
            hasDot = true;
            sb.Append(Advance());

            while (!IsEof && char.IsDigit(Peek()))
            {
                hasDigitsAfterDot = true;
                sb.Append(Advance());
            }

            if (!hasDigitsAfterDot)
            {
                throw new SmlParseException($"Invalid float literal '{sb}' at line {startLine}, col {startColumn}. Missing digits after '.'.");
            }
        }

        if (!hasDigitsBeforeDot && !hasDigitsAfterDot)
        {
            throw new SmlParseException($"Invalid numeric literal at line {startLine}, col {startColumn}.");
        }

        if (!IsEof && Peek() == '.')
        {
            throw new SmlParseException($"Invalid numeric literal '{sb}.' at line {startLine}, col {startColumn}. Multiple dots are not allowed.");
        }

        if (!IsEof && (Peek() == 'e' || Peek() == 'E'))
        {
            throw new SmlParseException($"Invalid numeric literal '{sb}': exponent notation is not supported (line {startLine}, col {startColumn}).");
        }

        if (!IsEof && char.IsLetter(Peek()))
        {
            throw new SmlParseException($"Invalid numeric literal '{sb}': numeric suffixes are not supported (line {startLine}, col {startColumn}).");
        }

        var text = sb.ToString();
        return new SmlToken(hasDot ? SmlTokenKind.Float : SmlTokenKind.Int, text, startLine, startColumn);
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
