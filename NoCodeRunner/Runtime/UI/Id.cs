using System;
using System.Collections.Generic;

namespace Runtime.UI;

public readonly struct Id : IEquatable<Id>
{
    public int Value { get; }

    public Id(int value)
    {
        Value = value;
    }

    public bool IsSet => Value != 0;

    public override string ToString() => Value.ToString();

    public bool Equals(Id other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Id other && Equals(other);
    public override int GetHashCode() => Value;

    public static bool operator ==(Id left, Id right) => left.Equals(right);
    public static bool operator !=(Id left, Id right) => !left.Equals(right);
}

public readonly struct ToggleId : IEquatable<ToggleId>
{
    public int Value { get; }

    public ToggleId(int value)
    {
        Value = value;
    }

    public bool IsSet => Value != 0;

    public override string ToString() => Value.ToString();

    public bool Equals(ToggleId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ToggleId other && Equals(other);
    public override int GetHashCode() => Value;

    public static bool operator ==(ToggleId left, ToggleId right) => left.Equals(right);
    public static bool operator !=(ToggleId left, ToggleId right) => !left.Equals(right);
}

public static class IdRuntimeScope
{
    private static readonly Dictionary<string, int> NameToValue = new(StringComparer.Ordinal);
    private static int _counter;

    public static void Reset()
    {
        NameToValue.Clear();
        _counter = 0;
    }

    public static Id GetOrCreate(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return new Id(0);
        }

        if (!NameToValue.TryGetValue(identifier, out var value))
        {
            value = ++_counter;
            NameToValue[identifier] = value;
        }

        return new Id(value);
    }

    public static bool TryResolve(string identifier, out Id id)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            id = new Id(0);
            return false;
        }

        if (!NameToValue.TryGetValue(identifier, out var value))
        {
            id = new Id(0);
            return false;
        }

        id = new Id(value);
        return true;
    }
}
