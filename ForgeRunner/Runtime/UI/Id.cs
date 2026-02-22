/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of ForgeRunner.
 *
 *  ForgeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ForgeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ForgeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

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
