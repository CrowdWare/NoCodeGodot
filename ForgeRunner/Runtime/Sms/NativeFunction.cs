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

public delegate Value NativeFunction(IReadOnlyList<Value> args);

public sealed class NativeFunctionRegistry
{
    private readonly Dictionary<string, NativeFunction> _functions = new(StringComparer.Ordinal);

    public void Register(string name, NativeFunction function) => _functions[name] = function;

    public NativeFunction? Get(string name) => _functions.TryGetValue(name, out var function) ? function : null;

    public bool Has(string name) => _functions.ContainsKey(name);

    public IReadOnlyCollection<string> Names => _functions.Keys;

    public void Clear() => _functions.Clear();

    public void RegisterBuiltins()
    {
        Register("toString", args => args.Count > 0 ? new StringValue(args[0].ToString()) : new StringValue(string.Empty));
        Register("size", args => args.Count > 0 && args[0] is ArrayValue array ? new IntegerValue(array.Size()) : new IntegerValue(0));
        Register("isNumber", args => new BooleanValue(args.Count > 0 && ValueUtils.IsNumeric(args[0])));
        Register("isString", args => new BooleanValue(args.Count > 0 && args[0] is StringValue));
        Register("isBoolean", args => new BooleanValue(args.Count > 0 && args[0] is BooleanValue));
        Register("isNull", args => new BooleanValue(args.Count > 0 && args[0] is NullValue));
        Register("isArray", args => new BooleanValue(args.Count > 0 && args[0] is ArrayValue));
        Register("abs", args =>
        {
            if (args.Count == 0)
            {
                return new IntegerValue(0);
            }
            if (args[0] is IntegerValue i)
            {
                if (i.Value == long.MinValue)
                {
                    return new NumberValue(Math.Abs((double)i.Value));
                }
                return new IntegerValue(Math.Abs(i.Value));
            }
            if (args[0] is NumberValue n)
            {
                return new NumberValue(Math.Abs(n.Value));
            }
            return new IntegerValue(0);
        });
        Register("min", args =>
        {
            if (args.Count < 2)
            {
                return new IntegerValue(0);
            }
            if (args[0] is IntegerValue ai && args[1] is IntegerValue bi)
            {
                return new IntegerValue(Math.Min(ai.Value, bi.Value));
            }
            if (ValueUtils.TryGetDouble(args[0], out var ad) && ValueUtils.TryGetDouble(args[1], out var bd))
            {
                return new NumberValue(Math.Min(ad, bd));
            }
            return new IntegerValue(0);
        });
        Register("max", args =>
        {
            if (args.Count < 2)
            {
                return new IntegerValue(0);
            }
            if (args[0] is IntegerValue ai && args[1] is IntegerValue bi)
            {
                return new IntegerValue(Math.Max(ai.Value, bi.Value));
            }
            if (ValueUtils.TryGetDouble(args[0], out var ad) && ValueUtils.TryGetDouble(args[1], out var bd))
            {
                return new NumberValue(Math.Max(ad, bd));
            }
            return new IntegerValue(0);
        });
        Register("print", args =>
        {
            if (args.Count > 0)
            {
                Console.Write(args[0].ToString());
            }

            return NullValue.Instance;
        });
    }
}
