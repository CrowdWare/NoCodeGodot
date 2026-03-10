/*
#############################################################################
# Copyright (C) 2026 CrowdWare
#
# This file is part of Forge.
#
# SPDX-License-Identifier: GPL-3.0-or-later OR LicenseRef-CrowdWare-Commercial
#
# Forge is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# Forge is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with Forge. If not, see <https://www.gnu.org/licenses/>.
#
# Commercial licensing is available from CrowdWare for proprietary use.
#############################################################################
*/

using Runtime.Sms;
using Xunit;

namespace SMSCore.Tests;

public sealed class SmsParserNumbersTests
{
    [Fact]
    public void Parse_MultilineGlobalFunctionCall_Parses()
    {
        const string source = """
        var r = foo(
            a,
            b,
            c
        )
        """;

        var program = new Parser(new Lexer(source).Tokenize()).Parse();
        var decl = Assert.IsType<VarDeclaration>(Assert.Single(program.Statements));
        var call = Assert.IsType<FunctionCall>(decl.Value);
        Assert.Equal("foo", call.Name);
        Assert.Equal(3, call.Arguments.Count);
    }

    [Fact]
    public void Parse_MultilineMethodCall_Parses()
    {
        const string source = """
        var r = obj.call(
            a,
            b
        )
        """;

        var program = new Parser(new Lexer(source).Tokenize()).Parse();
        var decl = Assert.IsType<VarDeclaration>(Assert.Single(program.Statements));
        var call = Assert.IsType<MethodCall>(decl.Value);
        Assert.Equal("call", call.Method);
        Assert.Equal(2, call.Arguments.Count);
    }

    [Fact]
    public void Parse_MultilineNestedCalls_Parses()
    {
        const string source = """
        var r = outer(
            inner(
                a,
                b
            ),
            c
        )
        """;

        var program = new Parser(new Lexer(source).Tokenize()).Parse();
        var decl = Assert.IsType<VarDeclaration>(Assert.Single(program.Statements));
        var outer = Assert.IsType<FunctionCall>(decl.Value);
        Assert.Equal(2, outer.Arguments.Count);
        _ = Assert.IsType<FunctionCall>(outer.Arguments[0]);
    }

    [Theory]
    [InlineData("var x = 1.0")]
    [InlineData("var x = .5")]
    [InlineData("var x = 3.14")]
    public void Parse_DoubleLiterals_AreAccepted(string source)
    {
        var program = new Parser(new Lexer(source).Tokenize()).Parse();
        var decl = Assert.IsType<VarDeclaration>(Assert.Single(program.Statements));
        _ = Assert.IsType<NumberLiteral>(decl.Value);
    }

    [Fact]
    public void Parse_IntegerLiteral_UsesIntegerNode()
    {
        const string source = "var x = 42";
        var program = new Parser(new Lexer(source).Tokenize()).Parse();
        var decl = Assert.IsType<VarDeclaration>(Assert.Single(program.Statements));
        _ = Assert.IsType<IntegerLiteral>(decl.Value);
    }

    [Theory]
    [InlineData("var x = 1e3")]
    [InlineData("var x = 1E3")]
    [InlineData("var x = 1e-2")]
    public void Parse_Exponentials_AreRejected(string source)
    {
        var ex = Assert.Throws<LexError>(() => new Lexer(source).Tokenize());
        Assert.Contains("Exponential number syntax is not supported", ex.Message);
    }

    [Fact]
    public void Runtime_Division_UsesFloatingPoint()
    {
        var engine = new ScriptEngine();
        var result = engine.ExecuteAndGetDotNet("var x = 5 / 2\nx");
        Assert.IsType<double>(result);
        Assert.Equal(2.5d, (double)result!, 6);
    }
}
