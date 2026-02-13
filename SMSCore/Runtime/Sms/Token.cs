namespace Runtime.Sms;

public enum TokenType
{
    Number,
    String,
    InterpolatedString,
    Boolean,
    Null,
    Identifier,
    Var,
    Fun,
    Get,
    Set,
    When,
    If,
    Else,
    While,
    For,
    In,
    Break,
    Continue,
    Return,
    Data,
    Class,
    Plus,
    Minus,
    Multiply,
    Divide,
    Assign,
    Equals,
    NotEquals,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    Not,
    And,
    Or,
    Increment,
    Decrement,
    Arrow,
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Comma,
    Dot,
    Semicolon,
    Newline,
    Eof
}

public readonly record struct Token(TokenType Type, string Text, Position Position)
{
    public override string ToString() => $"{Type}('{Text}') at {Position}";
}
