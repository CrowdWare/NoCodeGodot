using System;
using System.Collections.Generic;

namespace Runtime.Sml;

public enum SmlValueKind
{
    Int,
    Bool,
    String,
    Identifier,
    Enum,
    Vec2i,
    Vec3i
}

public readonly record struct SmlVec2i(int X, int Y);
public readonly record struct SmlVec3i(int X, int Y, int Z);
public readonly record struct SmlEnumValue(string Name, int? Value);

public sealed class SmlValue
{
    private SmlValue(SmlValueKind kind, object value)
    {
        Kind = kind;
        Value = value;
    }

    public SmlValueKind Kind { get; }
    public object Value { get; }

    public static SmlValue FromInt(int value) => new(SmlValueKind.Int, value);
    public static SmlValue FromBool(bool value) => new(SmlValueKind.Bool, value);
    public static SmlValue FromString(string value) => new(SmlValueKind.String, value);
    public static SmlValue FromIdentifier(string value) => new(SmlValueKind.Identifier, value);
    public static SmlValue FromEnum(string enumName, int? enumValue) => new(SmlValueKind.Enum, new SmlEnumValue(enumName, enumValue));
    public static SmlValue FromVec2i(int x, int y) => new(SmlValueKind.Vec2i, new SmlVec2i(x, y));
    public static SmlValue FromVec3i(int x, int y, int z) => new(SmlValueKind.Vec3i, new SmlVec3i(x, y, z));

    public string AsStringOrThrow(string propertyName)
    {
        return Kind switch
        {
            SmlValueKind.String => (string)Value,
            SmlValueKind.Identifier => (string)Value,
            SmlValueKind.Enum => ((SmlEnumValue)Value).Name,
            _ => throw new SmlParseException($"Property '{propertyName}' must be a string/identifier.")
        };
    }

    public int AsIntOrThrow(string propertyName)
    {
        return Kind == SmlValueKind.Int
            ? (int)Value
            : throw new SmlParseException($"Property '{propertyName}' must be an integer.");
    }

    public bool AsBoolOrThrow(string propertyName)
    {
        return Kind == SmlValueKind.Bool
            ? (bool)Value
            : throw new SmlParseException($"Property '{propertyName}' must be a boolean.");
    }

    public long AsLongOrThrow(string propertyName)
    {
        return Kind == SmlValueKind.Int
            ? (int)Value
            : throw new SmlParseException($"Property '{propertyName}' must be an integer.");
    }

    public int AsEnumIntOrThrow(string propertyName)
    {
        if (Kind != SmlValueKind.Enum)
        {
            throw new SmlParseException($"Property '{propertyName}' must be an enum value.");
        }

        var enumValue = (SmlEnumValue)Value;
        if (enumValue.Value is null)
        {
            throw new SmlParseException($"Enum '{enumValue.Name}' for property '{propertyName}' is not registered.");
        }

        return enumValue.Value.Value;
    }
}

public sealed class SmlNode
{
    public required string Name { get; init; }
    public required int Line { get; init; }
    public Dictionary<string, SmlValue> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SmlNode> Children { get; } = [];

    public bool TryGetProperty(string key, out SmlValue value)
    {
        return Properties.TryGetValue(key, out value!);
    }

    public SmlValue GetRequiredProperty(string key)
    {
        if (!Properties.TryGetValue(key, out var value))
        {
            throw new SmlParseException($"Missing required property '{key}' in node '{Name}' (line {Line}).");
        }

        return value;
    }
}

public sealed class SmlDocument
{
    public List<SmlNode> Roots { get; } = [];
    public List<string> Warnings { get; } = [];
}

public sealed class SmlParseException : Exception
{
    public SmlParseException(string message) : base(message)
    {
    }
}
