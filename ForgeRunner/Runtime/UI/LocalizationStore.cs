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
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.UI;

public sealed class LocalizationStore
{
    private readonly Dictionary<string, string> _fallback;
    private readonly Dictionary<string, string> _localized;

    private LocalizationStore(string languageCode, Dictionary<string, string> fallback, Dictionary<string, string> localized)
    {
        LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
        _fallback = fallback;
        _localized = localized;
    }

    public string LanguageCode { get; }

    public static LocalizationStore Empty { get; } = new("en", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public static async Task<LocalizationStore> LoadAsync(
        RunnerUriResolver resolver,
        string uiSmlUri,
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        var lang = NormalizeLanguage(languageCode);
        var fallback = await TryLoadStringsFileAsync(resolver, uiSmlUri, "strings.sml", cancellationToken);
        var localized = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await TryLoadStringsFileAsync(resolver, uiSmlUri, $"strings-{lang}.sml", cancellationToken);

        return new LocalizationStore(lang, fallback, localized);
    }

    public bool TryTranslate(string key, out string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = string.Empty;
            return false;
        }

        if (_localized.TryGetValue(key, out value))
        {
            return true;
        }

        if (_fallback.TryGetValue(key, out value))
        {
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static async Task<Dictionary<string, string>> TryLoadStringsFileAsync(
        RunnerUriResolver resolver,
        string uiSmlUri,
        string relativeFile,
        CancellationToken cancellationToken)
    {
        try
        {
            var source = await resolver.LoadTextAsync(relativeFile, uiSmlUri, cancellationToken);
            return ParseStringsMap(source, relativeFile);
        }
        catch (FileNotFoundException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("i18n", $"Failed loading localization file '{relativeFile}'.", ex);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Dictionary<string, string> ParseStringsMap(string source, string fileName)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parser = new SmlParser(source, SmlSchemaFactory.CreateDefault());
        var document = parser.ParseDocument();

        // Strings blocks are parsed as resource namespaces (document.Resources), not roots.
        // Fall back to document.Roots for compatibility with plain-node strings files.
        SmlNode? stringsNode = null;
        if (document.Resources.TryGetValue("Strings", out var resourceStrings))
        {
            stringsNode = resourceStrings;
        }
        else
        {
            foreach (var root in document.Roots)
            {
                if (string.Equals(root.Name, "Strings", StringComparison.OrdinalIgnoreCase))
                {
                    stringsNode = root;
                    break;
                }
            }
            stringsNode ??= document.Roots.Count > 0 ? document.Roots[0] : null;
        }

        if (stringsNode is null)
        {
            return map;
        }

        foreach (var (key, value) in stringsNode.Properties)
        {
            try
            {
                map[key] = value.AsStringOrThrow(key);
            }
            catch
            {
                RunnerLogger.Warn("i18n", $"Skipping non-string key '{key}' in '{fileName}'.");
            }
        }

        return map;
    }

    private static string NormalizeLanguage(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        }

        var normalized = (languageCode ?? "en").Trim();
        var dashIndex = normalized.IndexOfAny(['-', '_']);
        if (dashIndex > 0)
        {
            normalized = normalized[..dashIndex];
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "en";
        }

        return normalized.ToLowerInvariant();
    }
}
