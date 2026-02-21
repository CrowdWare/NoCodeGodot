/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of SMLCore.
 *
 *  SMLCore is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  SMLCore is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with SMLCore.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Runtime.Sml;

public enum SmlValueKind
{
    Int,
    Float,
    Bool,
    String,
    Identifier,
    Enum,
    Vec2i,
    Vec3i,
    Padding
}

public readonly record struct SmlVec2i(int X, int Y);
public readonly record struct SmlVec3i(int X, int Y, int Z);
public readonly record struct SmlEnumValue(string Name, int? Value);
public readonly record struct SmlPadding(int Top, int Right, int Bottom, int Left);

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
    public static SmlValue FromFloat(double value) => new(SmlValueKind.Float, value);
    public static SmlValue FromBool(bool value) => new(SmlValueKind.Bool, value);
    public static SmlValue FromString(string value) => new(SmlValueKind.String, value);
    public static SmlValue FromIdentifier(string value) => new(SmlValueKind.Identifier, value);
    public static SmlValue FromEnum(string enumName, int? enumValue) => new(SmlValueKind.Enum, new SmlEnumValue(enumName, enumValue));
    public static SmlValue FromVec2i(int x, int y) => new(SmlValueKind.Vec2i, new SmlVec2i(x, y));
    public static SmlValue FromVec3i(int x, int y, int z) => new(SmlValueKind.Vec3i, new SmlVec3i(x, y, z));
    public static SmlValue FromPadding(int top, int right, int bottom, int left) => new(SmlValueKind.Padding, new SmlPadding(top, right, bottom, left));

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

    public double AsDoubleOrThrow(string propertyName)
    {
        return Kind switch
        {
            SmlValueKind.Int => (int)Value,
            SmlValueKind.Float => (double)Value,
            _ => throw new SmlParseException($"Property '{propertyName}' must be numeric.")
        };
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

    public SmlVec2i AsVec2iOrThrow(string propertyName)
    {
        return Kind == SmlValueKind.Vec2i
            ? (SmlVec2i)Value
            : throw new SmlParseException($"Property '{propertyName}' must be a 2D integer vector (x, y).");
    }

    public SmlPadding AsPaddingOrThrow(string propertyName)
    {
        return Kind == SmlValueKind.Padding
            ? (SmlPadding)Value
            : throw new SmlParseException($"Property '{propertyName}' must be padding with 1, 2, or 4 integer values.");
    }
}

public sealed class SmlNode
{
    public required string Name { get; init; }
    public required int Line { get; init; }
    public Dictionary<string, SmlValue> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> PropertyLines { get; } = new(StringComparer.OrdinalIgnoreCase);
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
