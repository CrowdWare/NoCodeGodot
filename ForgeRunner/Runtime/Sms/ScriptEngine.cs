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

public sealed class ScriptEngine
{
    private readonly Interpreter _interpreter = new();

    public Value Execute(string source)
    {
        try
        {
            var tokens = new Lexer(source).Tokenize();
            var program = new Parser(tokens).Parse();
            return _interpreter.Execute(program);
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ScriptError($"Unexpected error during script execution: {ex.Message}", null, ex);
        }
    }

    public object? ExecuteAndGetDotNet(string source) => ValueUtils.ToDotNet(Execute(source));

    public void RegisterFunction(string name, NativeFunction function) => _interpreter.NativeFunctions.Register(name, function);

    public void RegisterFunction(string name, Func<IReadOnlyList<object?>, object?> function)
    {
        _interpreter.NativeFunctions.Register(name, args =>
        {
            var dotNetArgs = args.Select(ValueUtils.ToDotNet).ToList();
            var result = function(dotNetArgs);
            return ValueUtils.FromDotNet(result);
        });
    }

    public bool HasFunction(string name) => _interpreter.NativeFunctions.Has(name);

    public bool InvokeEvent(string targetId, string eventName, params object?[] args)
    {
        var values = args.Select(ValueUtils.FromDotNet).ToArray();
        return _interpreter.InvokeEvent(targetId, eventName, values);
    }

    public void SetSuperDispatcher(Action<string, string, IReadOnlyList<object?>>? dispatcher)
    {
        if (dispatcher is null)
        {
            _interpreter.SetSuperDispatcher(null);
            return;
        }

        _interpreter.SetSuperDispatcher((targetId, eventName, args) =>
        {
            var dotNetArgs = args.Select(ValueUtils.ToDotNet).ToList();
            dispatcher(targetId, eventName, dotNetArgs);
        });
    }

    public IReadOnlyCollection<string> FunctionNames => _interpreter.NativeFunctions.Names;

    public void ClearFunctions()
    {
        _interpreter.NativeFunctions.Clear();
        _interpreter.NativeFunctions.RegisterBuiltins();
    }

    public void ValidateSyntax(string source)
    {
        try
        {
            var tokens = new Lexer(source).Tokenize();
            _ = new Parser(tokens).Parse();
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ScriptError($"Unexpected error during syntax validation: {ex.Message}", null, ex);
        }
    }

    public string GetVersion() => "SMS Engine 1.1.0";
}
