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
