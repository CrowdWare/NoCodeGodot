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

public enum SmlPropertyKind
{
    Default,
    Identifier,
    Id,
    Enum
}

public sealed class SmlParserSchema
{
    private readonly Dictionary<string, SmlPropertyKind> _propertyKinds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, int>> _enumMaps = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _knownNodes = new(StringComparer.OrdinalIgnoreCase);

    public bool WarnOnUnknownNodes { get; set; } = true;

    public void RegisterKnownNode(string nodeName)
    {
        _knownNodes.Add(nodeName);
    }

    public void RegisterIdentifierProperty(string propertyName)
    {
        _propertyKinds[propertyName] = SmlPropertyKind.Identifier;
    }

    public void RegisterIdProperty(string propertyName = "id")
    {
        _propertyKinds[propertyName] = SmlPropertyKind.Id;
    }

    public void RegisterEnumValue(string propertyName, string enumName, int enumValue)
    {
        _propertyKinds[propertyName] = SmlPropertyKind.Enum;

        if (!_enumMaps.TryGetValue(propertyName, out var map))
        {
            map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _enumMaps[propertyName] = map;
        }

        map[enumName] = enumValue;
    }

    public SmlPropertyKind GetPropertyKind(string propertyName)
    {
        return _propertyKinds.TryGetValue(propertyName, out var kind)
            ? kind
            : SmlPropertyKind.Default;
    }

    public bool TryResolveEnum(string propertyName, string enumName, out int enumValue)
    {
        enumValue = default;
        return _enumMaps.TryGetValue(propertyName, out var map)
            && map.TryGetValue(enumName, out enumValue);
    }

    public bool IsKnownNode(string nodeName)
    {
        return _knownNodes.Contains(nodeName);
    }
}
