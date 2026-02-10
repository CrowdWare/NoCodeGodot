using System;
using System.Collections.Generic;

namespace Runtime.Sml;

public enum SmlPropertyKind
{
    Default,
    Identifier,
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
