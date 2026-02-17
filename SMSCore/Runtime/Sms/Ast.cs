namespace Runtime.Sms;

public abstract record AstNode(Position? Position);
public abstract record Statement(Position? Position) : AstNode(Position);
public abstract record Expression(Position? Position) : AstNode(Position);

public sealed record ProgramNode(IReadOnlyList<Statement> Statements, Position? Position = null) : AstNode(Position);

public sealed record VarDeclaration(
    string Name,
    Expression Value,
    PropertyAccessor? Getter = null,
    PropertyAccessor? Setter = null,
    Position? Position = null) : Statement(Position);

public sealed record PropertyAccessor(string? ParameterName, Expression Body, Position? Position = null) : AstNode(Position);

public sealed record Assignment(Expression Target, Expression Value, Position? Position = null) : Statement(Position);
public sealed record ExpressionStatement(Expression Expression, Position? Position = null) : Statement(Position);
public sealed record IfStatement(Expression Condition, IReadOnlyList<Statement> ThenBranch, IReadOnlyList<Statement>? ElseBranch = null, Position? Position = null) : Statement(Position);
public sealed record IfExpression(Expression Condition, Expression ThenBranch, Expression ElseBranch, Position? Position = null) : Expression(Position);
public sealed record WhileStatement(Expression Condition, IReadOnlyList<Statement> Body, Position? Position = null) : Statement(Position);
public sealed record ForStatement(Statement? Init, Expression? Condition, Statement? Update, IReadOnlyList<Statement> Body, Position? Position = null) : Statement(Position);
public sealed record ForInStatement(string Variable, Expression Iterable, IReadOnlyList<Statement> Body, Position? Position = null) : Statement(Position);
public sealed record BreakStatement(Position? Position = null) : Statement(Position);
public sealed record ContinueStatement(Position? Position = null) : Statement(Position);
public sealed record ReturnStatement(Expression? Value, Position? Position = null) : Statement(Position);
public sealed record FunctionDeclaration(string Name, IReadOnlyList<string> Parameters, IReadOnlyList<Statement> Body, Position? Position = null) : Statement(Position);
public sealed record DataClassDeclaration(string Name, IReadOnlyList<string> Fields, Position? Position = null) : Statement(Position);
public sealed record EventHandlerDeclaration(
    string TargetId,
    string EventName,
    IReadOnlyList<string> Parameters,
    IReadOnlyList<Statement> Body,
    Position? Position = null) : Statement(Position);

public sealed record NumberLiteral(double Value, Position? Position = null) : Expression(Position);
public sealed record StringLiteral(string Value, Position? Position = null) : Expression(Position);
public sealed record InterpolatedStringLiteral(IReadOnlyList<StringPart> Parts, Position? Position = null) : Expression(Position);
public sealed record BooleanLiteral(bool Value, Position? Position = null) : Expression(Position);
public sealed record NullLiteral(Position? Position = null) : Expression(Position);
public sealed record Identifier(string Name, Position? Position = null) : Expression(Position);
public sealed record BinaryExpression(Expression Left, string Operator, Expression Right, Position? Position = null) : Expression(Position);
public sealed record UnaryExpression(string Operator, Expression Operand, Position? Position = null) : Expression(Position);
public sealed record PostfixExpression(Expression Operand, string Operator, Position? Position = null) : Expression(Position);
public sealed record FunctionCall(string Name, IReadOnlyList<Expression> Arguments, Position? Position = null) : Expression(Position);
public sealed record MethodCall(Expression Receiver, string Method, IReadOnlyList<Expression> Arguments, Position? Position = null) : Expression(Position);
public sealed record MemberAccess(Expression Receiver, string Member, Position? Position = null) : Expression(Position);
public sealed record AssignmentExpression(Expression Target, Expression Value, Position? Position = null) : Expression(Position);
public sealed record ArrayAccess(Expression Receiver, Expression Index, Position? Position = null) : Expression(Position);
public sealed record ArrayLiteral(IReadOnlyList<Expression> Elements, Position? Position = null) : Expression(Position);
public sealed record WhenExpression(Expression? Subject, IReadOnlyList<WhenBranch> Branches, Position? Position = null) : Expression(Position);
public sealed record WhenBranch(Expression? Condition, Expression Result, bool IsElse = false, Position? Position = null) : AstNode(Position);

public abstract record StringPart;
public sealed record TextPart(string Value) : StringPart;
public sealed record ExpressionPart(Expression Expr) : StringPart;
