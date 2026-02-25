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

using Runtime.Assets;
using Runtime.Logging;
using Runtime.Sml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.UI;

/// <summary>
/// Loads design tokens (Colors, Layouts) from theme.sml files and injects them
/// into a document's resource dictionary so that @Colors.* and @Layouts.* references resolve.
///
/// Resolution priority (highest first):
///   1. Inline block in the SML document (Colors { } / Layouts { })
///   2. App-level theme.sml (same directory as app.sml)
///   3. ForgeRunner default theme (res://theme.sml)
/// </summary>
public sealed class ThemeStore
{
    private readonly Dictionary<string, SmlValue> _colors;
    private readonly Dictionary<string, SmlValue> _layouts;

    private ThemeStore(Dictionary<string, SmlValue> colors, Dictionary<string, SmlValue> layouts)
    {
        _colors = colors;
        _layouts = layouts;
    }

    public static ThemeStore Empty { get; } = new(
        new Dictionary<string, SmlValue>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, SmlValue>(StringComparer.OrdinalIgnoreCase));

    public static async Task<ThemeStore> LoadAsync(
        RunnerUriResolver resolver,
        string uiSmlUri,
        CancellationToken cancellationToken = default)
    {
        // 1. Load default theme from the ForgeRunner project (res://theme.sml)
        var defaultColors = new Dictionary<string, SmlValue>(StringComparer.OrdinalIgnoreCase);
        var defaultLayouts = new Dictionary<string, SmlValue>(StringComparer.OrdinalIgnoreCase);
        await TryLoadAndExtractAsync(resolver, "res://theme.sml", null, defaultColors, defaultLayouts, cancellationToken);

        // 2. Load optional app-level override (theme.sml next to app.sml)
        var appColors = new Dictionary<string, SmlValue>(StringComparer.OrdinalIgnoreCase);
        var appLayouts = new Dictionary<string, SmlValue>(StringComparer.OrdinalIgnoreCase);
        await TryLoadAndExtractAsync(resolver, "theme.sml", uiSmlUri, appColors, appLayouts, cancellationToken);

        // 3. Merge: app override wins over default
        var mergedColors = Merge(defaultColors, appColors);
        var mergedLayouts = Merge(defaultLayouts, appLayouts);

        return new ThemeStore(mergedColors, mergedLayouts);
    }

    /// <summary>
    /// Injects theme tokens into the document's resource dictionary.
    /// Existing keys in inline namespace blocks (Colors/Layouts in the SML) are not overwritten.
    /// </summary>
    public void InjectIntoResources(Dictionary<string, SmlNode> resources)
    {
        InjectNamespace(resources, "Colors", _colors);
        InjectNamespace(resources, "Layouts", _layouts);
    }

    private static void InjectNamespace(
        Dictionary<string, SmlNode> resources,
        string ns,
        Dictionary<string, SmlValue> tokens)
    {
        if (tokens.Count == 0) return;

        if (!resources.TryGetValue(ns, out var nsNode))
        {
            nsNode = new SmlNode { Name = ns, Line = 0 };
            resources[ns] = nsNode;
        }

        foreach (var (key, value) in tokens)
        {
            if (!nsNode.Properties.ContainsKey(key))
                nsNode.Properties[key] = value;
        }
    }

    private static Dictionary<string, SmlValue> Merge(
        Dictionary<string, SmlValue> defaults,
        Dictionary<string, SmlValue> overrides)
    {
        var result = new Dictionary<string, SmlValue>(defaults, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in overrides)
            result[key] = value;
        return result;
    }

    private static async Task TryLoadAndExtractAsync(
        RunnerUriResolver resolver,
        string relativeFile,
        string? baseUri,
        Dictionary<string, SmlValue> colors,
        Dictionary<string, SmlValue> layouts,
        CancellationToken cancellationToken)
    {
        try
        {
            var source = await resolver.LoadTextAsync(relativeFile, baseUri, cancellationToken);
            var parser = new SmlParser(source, SmlSchemaFactory.CreateDefault());
            var doc = parser.ParseDocument();

            if (doc.Resources.TryGetValue("Colors", out var colorsNode))
                foreach (var (key, value) in colorsNode.Properties)
                    colors[key] = value;

            if (doc.Resources.TryGetValue("Layouts", out var layoutsNode))
                foreach (var (key, value) in layoutsNode.Properties)
                    layouts[key] = value;
        }
        catch (FileNotFoundException)
        {
            // Optional file — silently skip
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("theme", $"Failed loading theme file '{relativeFile}'.", ex);
        }
    }
}
