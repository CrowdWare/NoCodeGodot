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
    On,
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
