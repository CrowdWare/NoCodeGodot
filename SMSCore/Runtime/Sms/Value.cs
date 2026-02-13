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
}

public sealed record StringValue(string Value) : Value
{
    public override object ToDotNet() => Value;
    public override bool IsTruthy() => Value.Length > 0;
}

public sealed record BooleanValue(bool Value) : Value
{
    public override object ToDotNet() => Value;
    public override bool IsTruthy() => Value;
}

public sealed record NullValue : Value
{
    public static readonly NullValue Instance = new();
    private NullValue() { }
    public override object? ToDotNet() => null;
    public override bool IsTruthy() => false;
}

public sealed record ArrayValue(List<Value> Elements) : Value
{
    public ArrayValue() : this([]) { }
    public override object ToDotNet() => Elements.Select(x => x.ToDotNet()).ToList();
    public override bool IsTruthy() => Elements.Count > 0;
    public int Size() => Elements.Count;
    public Value Get(int index) => index >= 0 && index < Elements.Count ? Elements[index] : NullValue.Instance;
    public void Set(int index, Value value)
    {
        if (index >= 0 && index < Elements.Count)
        {
            Elements[index] = value;
        }
    }
}

public sealed record ObjectValue(string ClassName, Dictionary<string, Value> Fields) : Value
{
    public override object ToDotNet() => Fields.ToDictionary(kv => kv.Key, kv => kv.Value.ToDotNet());
    public override bool IsTruthy() => true;
    public Value GetField(string name) => Fields.TryGetValue(name, out var value) ? value : NullValue.Instance;
    public void SetField(string name, Value value) => Fields[name] = value;
}

public sealed record NativeFunctionValue(NativeFunction Function) : Value
{
    public override object ToDotNet() => Function;
    public override bool IsTruthy() => true;
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
