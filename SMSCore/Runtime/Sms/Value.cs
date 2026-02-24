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

public abstract record Value
{
    public abstract object? ToDotNet();
    public abstract bool IsTruthy();

    public override string ToString() => this switch
    {
        NumberValue n => n.Value % 1.0 == 0.0 ? ((long)n.Value).ToString() : n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        StringValue s => s.Value,
        BooleanValue b => b.Value.ToString(),
        NullValue => "null",
        ArrayValue a => $"[{string.Join(", ", a.Elements)}]",
        ObjectValue o => $"{o.ClassName}{{{string.Join(", ", o.Fields.Select(x => $"{x.Key}={x.Value}"))}}}",
        NativeFunctionValue => "<native-function>",
        _ => base.ToString() ?? string.Empty
    };
}

public sealed record NumberValue(double Value) : Value
{
    public override object ToDotNet() => Value;
    public override bool IsTruthy() => Value != 0.0;
    public int ToInt() => (int)Value;
    public override string ToString() => base.ToString();
}

public sealed record StringValue(string Value) : Value
{
    public override object ToDotNet() => Value;
    public override bool IsTruthy() => Value.Length > 0;
    public override string ToString() => base.ToString();
}

public sealed record BooleanValue(bool Value) : Value
{
    public override object ToDotNet() => Value;
    public override bool IsTruthy() => Value;
    public override string ToString() => base.ToString();
}

public sealed record NullValue : Value
{
    public static readonly NullValue Instance = new();
    private NullValue() { }
    public override object? ToDotNet() => null;
    public override bool IsTruthy() => false;
    public override string ToString() => base.ToString();
}

public sealed record ArrayValue(List<Value> Elements) : Value
{
    public ArrayValue() : this([]) { }
    public override object ToDotNet() => Elements.Select(x => x.ToDotNet()).ToList();
    public override bool IsTruthy() => Elements.Count > 0;
    public int Size() => Elements.Count;
    public Value Get(int index) => index >= 0 && index < Elements.Count ? Elements[index] : NullValue.Instance;
    public override string ToString() => base.ToString();
    public void Set(int index, Value value)
    {
        if (index >= 0 && index < Elements.Count)
        {
            Elements[index] = value;
        }
    }
}

public delegate Value ObjectFieldGetter();
public delegate void ObjectFieldSetter(Value value);

public sealed record ObjectValue(
    string ClassName,
    Dictionary<string, Value> Fields,
    Dictionary<string, ObjectFieldGetter>? DynamicGetters = null,
    Dictionary<string, ObjectFieldSetter>? DynamicSetters = null) : Value
{
    public override object ToDotNet() => Fields.ToDictionary(kv => kv.Key, kv => kv.Value.ToDotNet());
    public override bool IsTruthy() => true;
    public Value GetField(string name)
    {
        if (DynamicGetters is not null && DynamicGetters.TryGetValue(name, out var getter))
        {
            return getter();
        }

        return Fields.TryGetValue(name, out var value) ? value : NullValue.Instance;
    }

    public override string ToString() => base.ToString();

    public void SetField(string name, Value value)
    {
        if (DynamicSetters is not null && DynamicSetters.TryGetValue(name, out var setter))
        {
            setter(value);
            return;
        }

        Fields[name] = value;
    }
}

public sealed record NativeFunctionValue(NativeFunction Function) : Value
{
    public override object ToDotNet() => Function;
    public override bool IsTruthy() => true;
    public override string ToString() => base.ToString();
}

public static class ValueUtils
{
    public static Value FromDotNet(object? value) => value switch
    {
        null => NullValue.Instance,
        bool b => new BooleanValue(b),
        int i => new NumberValue(i),
        long l => new NumberValue(l),
        float f => new NumberValue(f),
        double d => new NumberValue(d),
        string s => new StringValue(s),
        IEnumerable<object?> e => new ArrayValue(e.Select(FromDotNet).ToList()),
        _ => new StringValue(value.ToString() ?? string.Empty)
    };

    public static object? ToDotNet(Value value) => value.ToDotNet();
    public static bool EqualsValue(Value left, Value right) => Equals(left, right);
    public static bool IsTruthy(Value value) => value.IsTruthy();
}
