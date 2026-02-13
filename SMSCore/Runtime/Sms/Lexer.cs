using System.Text;

namespace Runtime.Sms;

public sealed class Lexer(string input)
{
    private readonly string _input = input ?? string.Empty;
    private int _current;

    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.Ordinal)
    {
        ["fun"] = TokenType.Fun,
        ["var"] = TokenType.Var,
        ["get"] = TokenType.Get,
        ["set"] = TokenType.Set,
        ["when"] = TokenType.When,
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["while"] = TokenType.While,
        ["for"] = TokenType.For,
        ["in"] = TokenType.In,
        ["break"] = TokenType.Break,
        ["continue"] = TokenType.Continue,
        ["return"] = TokenType.Return,
        ["true"] = TokenType.Boolean,
        ["false"] = TokenType.Boolean,
        ["null"] = TokenType.Null,
        ["data"] = TokenType.Data,
        ["class"] = TokenType.Class
    };

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd())
            {
                break;
            }

            tokens.Add(NextToken());
        }

        tokens.Add(new Token(TokenType.Eof, string.Empty, PositionFromIndex(_current)));
        return tokens;
    }

    private Token NextToken()
    {
        var start = _current;
        var startPos = PositionFromIndex(start);
        var c = Advance();

        return c switch
        {
            '(' => new Token(TokenType.LeftParen, "(", startPos),
            ')' => new Token(TokenType.RightParen, ")", startPos),
            '{' => new Token(TokenType.LeftBrace, "{", startPos),
            '}' => new Token(TokenType.RightBrace, "}", startPos),
            '[' => new Token(TokenType.LeftBracket, "[", startPos),
            ']' => new Token(TokenType.RightBracket, "]", startPos),
            ',' => new Token(TokenType.Comma, ",", startPos),
            '.' => new Token(TokenType.Dot, ".", startPos),
            ';' => new Token(TokenType.Semicolon, ";", startPos),
            '+' => Match('+') ? new Token(TokenType.Increment, "++", startPos) : new Token(TokenType.Plus, "+", startPos),
            '-' => Match('>')
                ? new Token(TokenType.Arrow, "->", startPos)
                : Match('-')
                    ? new Token(TokenType.Decrement, "--", startPos)
                    : new Token(TokenType.Minus, "-", startPos),
            '*' => new Token(TokenType.Multiply, "*", startPos),
            '/' => ParseSlashOrComment(startPos),
            '=' => Match('=') ? new Token(TokenType.Equals, "==", startPos) : new Token(TokenType.Assign, "=", startPos),
            '!' => Match('=') ? new Token(TokenType.NotEquals, "!=", startPos) : new Token(TokenType.Not, "!", startPos),
            '<' => Match('=') ? new Token(TokenType.LessEqual, "<=", startPos) : new Token(TokenType.Less, "<", startPos),
            '>' => Match('=') ? new Token(TokenType.GreaterEqual, ">=", startPos) : new Token(TokenType.Greater, ">", startPos),
            '&' => Match('&') ? new Token(TokenType.And, "&&", startPos) : throw new LexError("Unexpected character '&' - did you mean '&&'?", startPos),
            '|' => Match('|') ? new Token(TokenType.Or, "||", startPos) : throw new LexError("Unexpected character '|' - did you mean '||'?", startPos),
            '\n' => new Token(TokenType.Newline, "\n", startPos),
            '"' => ParseString(startPos),
            _ => char.IsDigit(c)
                ? ParseNumber(start, startPos)
                : (char.IsLetter(c) || c == '_')
                    ? ParseIdentifier(start, startPos)
                    : throw new LexError($"Unexpected character '{c}'", startPos)
        };
    }

    private Token ParseSlashOrComment(Position startPos)
    {
        if (Match('/'))
        {
            while (Peek() != '\n' && !IsAtEnd())
            {
                Advance();
            }

            SkipWhitespace();
            return NextToken();
        }

        if (Match('*'))
        {
            while (!IsAtEnd())
            {
                if (Peek() == '*' && PeekNext() == '/')
                {
                    Advance();
                    Advance();
                    SkipWhitespace();
                    return NextToken();
                }

                Advance();
            }

            throw new LexError("Unterminated block comment - missing */", startPos);
        }

        return new Token(TokenType.Divide, "/", startPos);
    }

    private Token ParseString(Position startPos)
    {
        var parts = new List<string>();
        var hasInterpolation = ScanStringForInterpolation(parts);
        return hasInterpolation
            ? new Token(TokenType.InterpolatedString, string.Join("\u0001", parts), startPos)
            : new Token(TokenType.String, parts.Count == 0 ? string.Empty : parts[0], startPos);
    }

    private bool ScanStringForInterpolation(List<string> parts)
    {
        var currentPart = new StringBuilder();
        var hasInterpolation = false;

        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd())
                {
                    throw new LexError("Unterminated string literal - missing closing quote", PositionFromIndex(_current));
                }

                var escaped = Advance();
                currentPart.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '\\' => '\\',
                    '"' => '"',
                    '$' => '$',
                    _ => escaped
                });
                continue;
            }

            if (Peek() == '$')
            {
                if (PeekNext() == '{')
                {
                    hasInterpolation = true;
                    FlushTextPart(currentPart, parts);

                    Advance();
                    Advance();
                    var exprStart = _current;
                    var braceCount = 1;
                    while (braceCount > 0 && !IsAtEnd())
                    {
                        if (Peek() == '{')
                        {
                            braceCount++;
                        }
                        else if (Peek() == '}')
                        {
                            braceCount--;
                        }

                        Advance();
                    }

                    if (braceCount > 0)
                    {
                        throw new LexError("Unterminated string interpolation - missing }", PositionFromIndex(_current));
                    }

                    var exprText = _input.Substring(exprStart, _current - exprStart - 1);
                    parts.Add($"EXPR:{exprText}");
                    continue;
                }

                if (char.IsLetter(PeekNext()) || PeekNext() == '_')
                {
                    hasInterpolation = true;
                    FlushTextPart(currentPart, parts);
                    Advance();

                    var identStart = _current;
                    while (char.IsLetterOrDigit(Peek()) || Peek() == '_')
                    {
                        Advance();
                    }

                    var identifier = _input.Substring(identStart, _current - identStart);
                    if (string.IsNullOrEmpty(identifier))
                    {
                        throw new LexError("Invalid interpolation - expected identifier after $", PositionFromIndex(_current));
                    }

                    parts.Add($"EXPR:{identifier}");
                    continue;
                }
            }

            currentPart.Append(Advance());
        }

        if (IsAtEnd())
        {
            throw new LexError("Unterminated string literal - missing closing quote", PositionFromIndex(_current));
        }

        FlushTextPart(currentPart, parts);
        Advance();

        if (!hasInterpolation)
        {
            if (parts.Count == 0)
            {
                parts.Add(string.Empty);
            }
            else if (parts[0].StartsWith("TEXT:", StringComparison.Ordinal))
            {
                parts[0] = parts[0][5..];
            }
        }

        return hasInterpolation;
    }

    private static void FlushTextPart(StringBuilder currentPart, List<string> parts)
    {
        if (currentPart.Length == 0)
        {
            return;
        }

        parts.Add($"TEXT:{currentPart}");
        currentPart.Clear();
    }

    private Token ParseNumber(int start, Position startPos)
    {
        while (char.IsDigit(Peek()))
        {
            Advance();
        }

        if (Peek() == '.' && char.IsDigit(PeekNext()))
        {
            Advance();
            while (char.IsDigit(Peek()))
            {
                Advance();
            }
        }

        return new Token(TokenType.Number, _input[start.._current], startPos);
    }

    private Token ParseIdentifier(int start, Position startPos)
    {
        while (char.IsLetterOrDigit(Peek()) || Peek() == '_')
        {
            Advance();
        }

        var text = _input[start.._current];
        return new Token(Keywords.TryGetValue(text, out var type) ? type : TokenType.Identifier, text, startPos);
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd())
        {
            if (Peek() is ' ' or '\t' or '\r')
            {
                Advance();
                continue;
            }

            break;
        }
    }

    private char Advance() => IsAtEnd() ? '\0' : _input[_current++];

    private bool Match(char expected)
    {
        if (IsAtEnd() || _input[_current] != expected)
        {
            return false;
        }

        _current++;
        return true;
    }

    private char Peek() => IsAtEnd() ? '\0' : _input[_current];
    private char PeekNext() => _current + 1 >= _input.Length ? '\0' : _input[_current + 1];
    private bool IsAtEnd() => _current >= _input.Length;

    private Position PositionFromIndex(int index)
    {
        var line = 1;
        var col = 1;
        for (var i = 0; i < index && i < _input.Length; i++)
        {
            if (_input[i] == '\n')
            {
                line++;
                col = 1;
            }
            else
            {
                col++;
            }
        }

        return new Position(line, col);
    }
}
