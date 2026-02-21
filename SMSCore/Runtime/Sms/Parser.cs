/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of SMSCore.
 *
 *  SMSCore is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  SMSCore is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with SMSCore.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace Runtime.Sms;

public sealed class Parser(IReadOnlyList<Token> tokens)
{
    private readonly IReadOnlyList<Token> _tokens = tokens;
    private int _current;
    private readonly HashSet<string> _eventHandlerKeys = new(StringComparer.Ordinal);

    public ProgramNode Parse()
    {
        var statements = new List<Statement>();
        _eventHandlerKeys.Clear();
        while (!IsAtEnd())
        {
            if (Check(TokenType.Newline) || Check(TokenType.Semicolon))
            {
                Advance();
                continue;
            }

            var statement = ParseStatement();
            if (statement is EventHandlerDeclaration handler)
            {
                var key = $"{handler.TargetId}.{handler.EventName}";
                if (!_eventHandlerKeys.Add(key))
                {
                    throw new ParseError($"Duplicate event handler '{key}'", handler.Position);
                }
            }

            statements.Add(statement);
        }

        return new ProgramNode(statements);
    }

    private Statement ParseStatement()
    {
        if (Match(TokenType.Var)) return ParseVarDeclaration();
        if (Match(TokenType.Fun)) return ParseFunctionDeclaration();
        if (Match(TokenType.Data)) return ParseDataClassDeclaration();
        if (Match(TokenType.If)) return ParseIfStatement();
        if (Match(TokenType.While)) return ParseWhileStatement();
        if (Match(TokenType.For)) return ParseForStatement();
        if (Match(TokenType.Break)) return ParseBreakStatement();
        if (Match(TokenType.Continue)) return ParseContinueStatement();
        if (Match(TokenType.Return)) return ParseReturnStatement();
        if (Match(TokenType.On)) return ParseEventHandlerDeclaration();

        var checkpoint = _current;
        try
        {
            return ParseAssignmentStatement();
        }
        catch (ParseError)
        {
            _current = checkpoint;
            return ParseExpressionStatement();
        }
    }

    private Statement ParseVarDeclaration()
    {
        var pos = Previous().Position;
        var name = Consume(TokenType.Identifier, "Expected variable name").Text;
        Consume(TokenType.Assign, "Expected '=' after variable name");
        var value = ParseExpression();
        SkipNewlines();

        PropertyAccessor? getter = null;
        PropertyAccessor? setter = null;

        while (Match(TokenType.Get, TokenType.Set))
        {
            var keyword = Previous().Type;
            if (keyword == TokenType.Get)
            {
                if (getter is not null)
                {
                    throw new ParseError("Multiple getters defined for property", Previous().Position);
                }

                getter = ParseAccessor(isGetter: true);
            }
            else
            {
                if (setter is not null)
                {
                    throw new ParseError("Multiple setters defined for property", Previous().Position);
                }

                setter = ParseAccessor(isGetter: false);
            }
        }

        SkipNewlines();
        return new VarDeclaration(name, value, getter, setter, pos);
    }

    private PropertyAccessor ParseAccessor(bool isGetter)
    {
        var pos = Previous().Position;
        Consume(TokenType.LeftParen, $"Expected '(' after {(isGetter ? "get" : "set")}");
        string? parameterName = null;
        if (isGetter)
        {
            Consume(TokenType.RightParen, "Expected ')' after get");
        }
        else
        {
            parameterName = Consume(TokenType.Identifier, "Expected parameter name in setter").Text;
            Consume(TokenType.RightParen, "Expected ')' after setter parameter");
        }

        Consume(TokenType.Assign, $"Expected '=' after {(isGetter ? "get" : "set")} accessor");
        var body = ParseExpression();
        SkipNewlines();
        return new PropertyAccessor(parameterName, body, pos);
    }

    private Statement ParseFunctionDeclaration()
    {
        var pos = Previous().Position;
        var name = Consume(TokenType.Identifier, "Expected function name").Text;
        Consume(TokenType.LeftParen, "Expected '(' after function name");
        var parameters = new List<string>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                parameters.Add(Consume(TokenType.Identifier, "Expected parameter name").Text);
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expected ')' after parameters");
        Consume(TokenType.LeftBrace, "Expected '{' before function body");
        var body = ParseBlock();
        return new FunctionDeclaration(name, parameters, body, pos);
    }

    private Statement ParseEventHandlerDeclaration()
    {
        var pos = Previous().Position;
        var targetToken = Consume(TokenType.Identifier, "Expected target id after 'on'");
        EnsureLowerCamelCase(targetToken, "target id");

        Consume(TokenType.Dot, "Expected '.' after event target id");

        var eventToken = Consume(TokenType.Identifier, "Expected event name after '.'");
        EnsureLowerCamelCase(eventToken, "event name");

        Consume(TokenType.LeftParen, "Expected '(' after event name");

        var parameters = new List<string>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                var parameterToken = Consume(TokenType.Identifier, "Expected parameter name");
                EnsureLowerCamelCase(parameterToken, "parameter");
                parameters.Add(parameterToken.Text);
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expected ')' after event parameters");
        Consume(TokenType.LeftBrace, "Expected '{' before event handler body");

        var body = ParseBlock();
        return new EventHandlerDeclaration(targetToken.Text, eventToken.Text, parameters, body, pos);
    }

    private Statement ParseDataClassDeclaration()
    {
        var pos = Previous().Position;
        Consume(TokenType.Class, "Expected 'class' after 'data'");
        var name = Consume(TokenType.Identifier, "Expected class name").Text;
        Consume(TokenType.LeftParen, "Expected '(' after class name");
        var fields = new List<string>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                fields.Add(Consume(TokenType.Identifier, "Expected field name").Text);
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expected ')' after fields");
        SkipNewlines();
        return new DataClassDeclaration(name, fields, pos);
    }

    private Statement ParseIfStatement()
    {
        var pos = Previous().Position;
        Consume(TokenType.LeftParen, "Expected '(' after 'if'");
        var condition = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after if condition");
        Consume(TokenType.LeftBrace, "Expected '{' after if condition");
        var thenBranch = ParseBlock();

        IReadOnlyList<Statement>? elseBranch = null;
        if (Match(TokenType.Else))
        {
            if (Match(TokenType.If))
            {
                elseBranch = [ParseIfStatement()];
            }
            else
            {
                Consume(TokenType.LeftBrace, "Expected '{' after 'else'");
                elseBranch = ParseBlock();
            }
        }

        return new IfStatement(condition, thenBranch, elseBranch, pos);
    }

    private Statement ParseWhileStatement()
    {
        var pos = Previous().Position;
        Consume(TokenType.LeftParen, "Expected '(' after 'while'");
        var condition = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after while condition");
        Consume(TokenType.LeftBrace, "Expected '{' after while condition");
        var body = ParseBlock();
        return new WhileStatement(condition, body, pos);
    }

    private Statement ParseForStatement()
    {
        var pos = Previous().Position;
        Consume(TokenType.LeftParen, "Expected '(' after 'for'");
        if (Check(TokenType.Identifier))
        {
            var checkpoint = _current;
            var variable = Advance().Text;
            if (Match(TokenType.In))
            {
                var iterable = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after for-in");
                Consume(TokenType.LeftBrace, "Expected '{' after for-in");
                var body = ParseBlock();
                return new ForInStatement(variable, iterable, body, pos);
            }

            _current = checkpoint;
        }

        Statement? init = null;
        if (!Match(TokenType.Semicolon))
        {
            init = Match(TokenType.Var) ? ParseVarDeclaration() : ParseAssignmentStatement();
            Consume(TokenType.Semicolon, "Expected ';' after for loop initializer");
        }

        Expression? condition = null;
        if (!Check(TokenType.Semicolon))
        {
            condition = ParseExpression();
        }

        Consume(TokenType.Semicolon, "Expected ';' after for loop condition");

        Statement? update = null;
        if (!Check(TokenType.RightParen))
        {
            update = ParseAssignmentStatement();
        }

        Consume(TokenType.RightParen, "Expected ')' after for clauses");
        Consume(TokenType.LeftBrace, "Expected '{' after for");
        var loopBody = ParseBlock();
        return new ForStatement(init, condition, update, loopBody, pos);
    }

    private Statement ParseBreakStatement()
    {
        var pos = Previous().Position;
        SkipNewlines();
        return new BreakStatement(pos);
    }

    private Statement ParseContinueStatement()
    {
        var pos = Previous().Position;
        SkipNewlines();
        return new ContinueStatement(pos);
    }

    private Statement ParseReturnStatement()
    {
        var pos = Previous().Position;
        var value = Check(TokenType.Newline) || IsAtEnd() ? null : ParseExpression();
        SkipNewlines();
        return new ReturnStatement(value, pos);
    }

    private Statement ParseAssignmentStatement()
    {
        var expr = ParseExpression();
        if (expr is AssignmentExpression assignment)
        {
            SkipNewlines();
            return new Assignment(assignment.Target, assignment.Value, assignment.Position);
        }

        throw new ParseError("Expected assignment", expr.Position ?? Peek().Position);
    }

    private Statement ParseExpressionStatement()
    {
        var expr = ParseExpression();
        SkipNewlines();
        return new ExpressionStatement(expr, expr.Position);
    }

    private List<Statement> ParseBlock()
    {
        var statements = new List<Statement>();
        SkipNewlines();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline) || Match(TokenType.Semicolon))
            {
                continue;
            }

            statements.Add(ParseStatement());
        }

        Consume(TokenType.RightBrace, "Expected '}' after block");
        return statements;
    }

    private Expression ParseExpression() => ParseAssignmentExpression();

    private Expression ParseAssignmentExpression()
    {
        var expr = ParseOr();
        if (Match(TokenType.Assign))
        {
            var value = ParseAssignmentExpression();
            var pos = expr.Position ?? Previous().Position;
            if (expr is Identifier or MemberAccess or ArrayAccess)
            {
                return new AssignmentExpression(expr, value, pos);
            }

            throw new ParseError("Invalid assignment target", pos);
        }

        return expr;
    }

    private Expression ParseOr()
    {
        var expr = ParseAnd();
        while (Match(TokenType.Or))
        {
            var op = Previous().Text;
            var right = ParseAnd();
            expr = new BinaryExpression(expr, op, right, expr.Position);
        }

        return expr;
    }

    private Expression ParseAnd()
    {
        var expr = ParseEquality();
        while (Match(TokenType.And))
        {
            var op = Previous().Text;
            var right = ParseEquality();
            expr = new BinaryExpression(expr, op, right, expr.Position);
        }

        return expr;
    }

    private Expression ParseEquality()
    {
        var expr = ParseComparison();
        while (Match(TokenType.Equals, TokenType.NotEquals))
        {
            var op = Previous().Text;
            var right = ParseComparison();
            expr = new BinaryExpression(expr, op, right, expr.Position);
        }

        return expr;
    }

    private Expression ParseComparison()
    {
        var expr = ParseTerm();
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous().Text;
            var right = ParseTerm();
            expr = new BinaryExpression(expr, op, right, expr.Position);
        }

        return expr;
    }

    private Expression ParseTerm()
    {
        var expr = ParseFactor();
        while (Match(TokenType.Minus, TokenType.Plus))
        {
            var op = Previous().Text;
            var right = ParseFactor();
            expr = new BinaryExpression(expr, op, right, expr.Position);
        }

        return expr;
    }

    private Expression ParseFactor()
    {
        var expr = ParseUnary();
        while (Match(TokenType.Divide, TokenType.Multiply))
        {
            var op = Previous().Text;
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right, expr.Position);
        }

        return expr;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenType.Not, TokenType.Minus, TokenType.Plus))
        {
            var op = Previous().Text;
            var right = ParseUnary();
            return new UnaryExpression(op, right, Previous().Position);
        }

        return ParseIfExpression();
    }

    private Expression ParseIfExpression()
    {
        if (!Match(TokenType.If))
        {
            return ParseWhenExpression();
        }

        var start = Previous().Position;
        Consume(TokenType.LeftParen, "Expected '(' after 'if'");
        var condition = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after if condition");
        SkipNewlines();
        var thenBranch = ParseExpression();
        if (!Match(TokenType.Else))
        {
            throw new ParseError("If expression requires 'else' branch", start);
        }

        SkipNewlines();
        var elseBranch = ParseExpression();
        return new IfExpression(condition, thenBranch, elseBranch, start);
    }

    private Expression ParseWhenExpression()
    {
        if (!Match(TokenType.When))
        {
            return ParsePostfix();
        }

        var start = Previous().Position;
        Expression? subject = null;
        if (Match(TokenType.LeftParen))
        {
            subject = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after when subject");
        }

        Consume(TokenType.LeftBrace, "Expected '{' after when");
        SkipNewlines();
        var branches = new List<WhenBranch>();
        var elseSeen = false;

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Newline))
            {
                continue;
            }

            var isElse = Match(TokenType.Else);
            Expression? condition;
            Position? branchPos;
            if (isElse)
            {
                if (elseSeen)
                {
                    throw new ParseError("Multiple 'else' branches in when", Previous().Position);
                }

                elseSeen = true;
                condition = null;
                branchPos = Previous().Position;
            }
            else
            {
                condition = ParseExpression();
                branchPos = condition.Position;
            }

            Consume(TokenType.Arrow, "Expected '->' after when condition");
            var result = ParseExpression();
            branches.Add(new WhenBranch(condition, result, isElse, branchPos));
            SkipNewlines();
        }

        Consume(TokenType.RightBrace, "Expected '}' after when branches");
        return new WhenExpression(subject, branches, start);
    }

    private Expression ParsePostfix()
    {
        var expr = ParseCall();
        if (Match(TokenType.Increment, TokenType.Decrement))
        {
            return new PostfixExpression(expr, Previous().Text, expr.Position);
        }

        return expr;
    }

    private Expression ParseCall()
    {
        var expr = ParsePrimary();
        while (true)
        {
            if (Match(TokenType.LeftParen))
            {
                var args = ParseArguments();
                if (expr is Identifier ident)
                {
                    expr = new FunctionCall(ident.Name, args, expr.Position);
                }
                else
                {
                    throw new ParseError("Invalid function call", expr.Position);
                }

                continue;
            }

            if (Match(TokenType.Dot))
            {
                var name = Consume(TokenType.Identifier, "Expected property name after '.'").Text;
                if (Match(TokenType.LeftParen))
                {
                    expr = new MethodCall(expr, name, ParseArguments(), expr.Position);
                }
                else
                {
                    expr = new MemberAccess(expr, name, expr.Position);
                }

                continue;
            }

            if (Match(TokenType.LeftBracket))
            {
                var index = ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after array index");
                expr = new ArrayAccess(expr, index, expr.Position);
                continue;
            }

            break;
        }

        return expr;
    }

    private Expression ParsePrimary()
    {
        if (Match(TokenType.Boolean))
        {
            return new BooleanLiteral(bool.Parse(Previous().Text), Previous().Position);
        }

        if (Match(TokenType.Null))
        {
            return new NullLiteral(Previous().Position);
        }

        if (Match(TokenType.Number))
        {
            var token = Previous();
            if (token.Text.Contains('.', StringComparison.Ordinal))
            {
                throw new ParseError("Double/float literals are not supported. Use integer values only.", token.Position);
            }

            return new NumberLiteral(double.Parse(token.Text, System.Globalization.CultureInfo.InvariantCulture), token.Position);
        }

        if (Match(TokenType.String))
        {
            return new StringLiteral(Previous().Text, Previous().Position);
        }

        if (Match(TokenType.InterpolatedString))
        {
            return ParseInterpolatedString(Previous());
        }

        if (Match(TokenType.Identifier))
        {
            return new Identifier(Previous().Text, Previous().Position);
        }

        if (Match(TokenType.LeftParen))
        {
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");
            return expr;
        }

        if (Match(TokenType.LeftBracket))
        {
            var startPos = Previous().Position;
            var elements = new List<Expression>();
            if (!Check(TokenType.RightBracket))
            {
                do
                {
                    elements.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightBracket, "Expected ']' after array elements");
            return new ArrayLiteral(elements, startPos);
        }

        var unexpected = Peek();
        throw new ParseError($"Expected expression, found {unexpected.Type}", unexpected.Position);
    }

    private Expression ParseInterpolatedString(Token token)
    {
        var encodedParts = token.Text.Split("\u0001", StringSplitOptions.None);
        var parts = new List<StringPart>();
        foreach (var encodedPart in encodedParts)
        {
            if (encodedPart.StartsWith("TEXT:", StringComparison.Ordinal))
            {
                var text = encodedPart[5..];
                if (text.Length > 0)
                {
                    parts.Add(new TextPart(text));
                }

                continue;
            }

            if (encodedPart.StartsWith("EXPR:", StringComparison.Ordinal))
            {
                var exprText = encodedPart[5..];
                if (exprText.Length > 0)
                {
                    var exprTokens = new Lexer(exprText).Tokenize();
                    var expr = new Parser(exprTokens).ParseExpression();
                    parts.Add(new ExpressionPart(expr));
                }
            }
        }

        return new InterpolatedStringLiteral(parts, token.Position);
    }

    private List<Expression> ParseArguments()
    {
        var args = new List<Expression>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                args.Add(ParseExpression());
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expected ')' after arguments");
        return args;
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (!Check(type))
            {
                continue;
            }

            Advance();
            return true;
        }

        return false;
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;
    private Token Advance() { if (!IsAtEnd()) _current++; return Previous(); }
    private bool IsAtEnd() => Peek().Type == TokenType.Eof;
    private Token Peek() => _tokens[_current];
    private Token Previous() => _tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        var currentToken = Peek();
        var position = currentToken.Type == TokenType.Eof && _current > 0 ? Previous().Position : currentToken.Position;
        throw new ParseError(message, position);
    }

    private void SkipNewlines()
    {
        while (Match(TokenType.Newline))
        {
        }
    }

    private static void EnsureLowerCamelCase(Token token, string role)
    {
        var text = token.Text;
        if (string.IsNullOrEmpty(text) || !char.IsLower(text[0]))
        {
            throw new ParseError($"Invalid {role} '{text}'. Expected lowerCamelCase", token.Position);
        }

        for (var i = 1; i < text.Length; i++)
        {
            if (!char.IsLetterOrDigit(text[i]))
            {
                throw new ParseError($"Invalid {role} '{text}'. Expected lowerCamelCase", token.Position);
            }
        }
    }
}
