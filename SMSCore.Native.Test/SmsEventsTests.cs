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

public sealed class SmsEventsTests
{
    [Fact]
    public void Parse_WithEventHandlers_ParsesTopLevelHandlers()
    {
        const string source = """
        on open.pressed() { project.open() }

        on mainWindow.sizeChanged(w, h) {
            log.info(w)
            super(w, h)
        }
        """;

        var tokens = new Lexer(source).Tokenize();
        var program = new Parser(tokens).Parse();

        Assert.Equal(2, program.Statements.Count);

        var first = Assert.IsType<EventHandlerDeclaration>(program.Statements[0]);
        Assert.Equal("open", first.TargetId);
        Assert.Equal("pressed", first.EventName);
        Assert.Empty(first.Parameters);

        var second = Assert.IsType<EventHandlerDeclaration>(program.Statements[1]);
        Assert.Equal("mainWindow", second.TargetId);
        Assert.Equal("sizeChanged", second.EventName);
        Assert.Equal(["w", "h"], second.Parameters);
    }

    [Theory]
    [InlineData("on Open.pressed() { }")]
    [InlineData("on open.Pressed() { }")]
    [InlineData("on open.pressed(,) { }")]
    [InlineData("on open.pressed(a b) { }")]
    [InlineData("on open.pressed(a,) { }")]
    public void Parse_WithInvalidEventSyntax_Throws(string source)
    {
        var ex = Assert.Throws<ParseError>(() => new Parser(new Lexer(source).Tokenize()).Parse());
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void Parse_WithDuplicateEventHandlers_Throws()
    {
        const string source = """
        on open.pressed() { }
        on open.pressed() { }
        """;

        var ex = Assert.Throws<ParseError>(() => new Parser(new Lexer(source).Tokenize()).Parse());
        Assert.Contains("Duplicate event handler", ex.Message);
    }

    [Fact]
    public void Runtime_InvokeEvent_WithNoHandler_ReturnsFalse()
    {
        var engine = new ScriptEngine();
        engine.Execute("var x = 1");

        var handled = engine.InvokeEvent("open", "pressed");

        Assert.False(handled);
    }

    [Fact]
    public void Runtime_InvokeEvent_BindsArgsAndExecutesBody()
    {
        const string source = """
        on mainWindow.sizeChanged(w, h) {
            capture(w)
            capture(h)
        }
        """;

        var seen = new List<object?>();
        var engine = new ScriptEngine();
        engine.RegisterFunction("capture", (IReadOnlyList<object?> args) =>
        {
            foreach (var arg in args)
            {
                seen.Add(arg);
            }

            return null;
        });

        engine.Execute(source);
        var handled = engine.InvokeEvent("mainWindow", "sizeChanged", 10, 20);

        Assert.True(handled);
        Assert.Equal([10L, 20L], seen);
    }

    [Fact]
    public void Runtime_InvokeEvent_WithArgCountMismatch_Throws()
    {
        var engine = new ScriptEngine();
        engine.Execute("on mainWindow.sizeChanged(w, h) { }");

        var ex = Assert.Throws<RuntimeError>(() => engine.InvokeEvent("mainWindow", "sizeChanged", 10));
        Assert.Contains("expects 2 args, got 1", ex.Message);
    }

    [Fact]
    public void Runtime_SuperOutsideHandler_Throws()
    {
        var engine = new ScriptEngine();

        var ex = Assert.Throws<RuntimeError>(() => engine.Execute("fun run() { super() } run()"));
        Assert.Contains("only be used inside an event handler", ex.Message);
    }

    [Fact]
    public void Runtime_SuperInsideHandler_UsesDispatcher()
    {
        var engine = new ScriptEngine();
        engine.Execute("on open.pressed(value) { super(value) }");

        string? target = null;
        string? evt = null;
        List<object?>? args = null;

        engine.SetSuperDispatcher((targetId, eventName, dispatchArgs) =>
        {
            target = targetId;
            evt = eventName;
            args = dispatchArgs.ToList();
        });

        var handled = engine.InvokeEvent("open", "pressed", 42);

        Assert.True(handled);
        Assert.Equal("open", target);
        Assert.Equal("pressed", evt);
        Assert.NotNull(args);
        Assert.Equal([42L], args!);
    }

    [Fact]
    public void Runtime_SuperInsideHandler_WithoutDispatcher_Throws()
    {
        var engine = new ScriptEngine();
        engine.Execute("on open.pressed() { super() }");

        var ex = Assert.Throws<RuntimeError>(() => engine.InvokeEvent("open", "pressed"));
        Assert.Contains("super dispatcher is not configured", ex.Message);
    }
}
