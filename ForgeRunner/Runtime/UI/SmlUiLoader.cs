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

using Godot;
using Runtime.Assets;
using Runtime.Logging;
using Runtime.Sml;
using Runtime.ThreeD;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.UI;

public sealed class SmlUiLoader
{
    private readonly NodeFactoryRegistry _registry;
    private readonly NodePropertyMapper _propertyMapper;
    private readonly AnimationControlApi _animationApi;
    private readonly Action<UiActionDispatcher>? _configureActions;
    private readonly RunnerUriResolver _uriResolver;

    public SmlUiLoader(
        NodeFactoryRegistry registry,
        NodePropertyMapper propertyMapper,
        AnimationControlApi? animationApi = null,
        Action<UiActionDispatcher>? configureActions = null,
        RunnerUriResolver? uriResolver = null)
    {
        _registry = registry;
        _propertyMapper = propertyMapper;
        _animationApi = animationApi ?? new AnimationControlApi();
        _configureActions = configureActions;
        _uriResolver = uriResolver ?? new RunnerUriResolver();
    }

    public async Task<Control> LoadFromUriAsync(string uri, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("UI URI must not be empty.", nameof(uri));
        }

        IdRuntimeScope.Reset();

        var normalizedUri = _uriResolver.ResolveReference(uri);
        var content = await _uriResolver.LoadTextAsync(normalizedUri, cancellationToken: cancellationToken);

        var schema = SmlSchemaFactory.CreateDefault();
        var parser = new SmlParser(content, schema);
        var document = parser.ParseDocument();
        var localization = await LocalizationStore.LoadAsync(_uriResolver, normalizedUri, cancellationToken: cancellationToken);
        await PreprocessMarkdownNodesAsync(document, normalizedUri, localization, cancellationToken);
        var assetPathResolver = CreateAssetPathResolver(normalizedUri);

        var builder = new SmlUiBuilder(_registry, _propertyMapper, _animationApi, localization, assetPathResolver);
        _configureActions?.Invoke(builder.Actions);
        return builder.Build(document);
    }

    private Func<string, string> CreateAssetPathResolver(string baseUri)
    {
        return source =>
        {
            try
            {
                if (source.StartsWith("appres://", StringComparison.OrdinalIgnoreCase)
                    || source.StartsWith("appres:/", StringComparison.OrdinalIgnoreCase))
                {
                    var tail = source[(source.IndexOf(':') + 1)..].TrimStart('/', '\\');
                    return $"res://{tail}";
                }

                return _uriResolver.ResolveForResourceLoadAsync(source, baseUri).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                RunnerLogger.Warn("UI", $"Asset path resolve failed for '{source}' (base '{baseUri}')", ex);
                return source;
            }
        };
    }

    private async Task PreprocessMarkdownNodesAsync(
        SmlDocument document,
        string documentUri,
        LocalizationStore localization,
        CancellationToken cancellationToken)
    {
        foreach (var root in document.Roots)
        {
            await PreprocessMarkdownNodeRecursiveAsync(root, documentUri, document, localization, cancellationToken);
        }
    }

    private async Task PreprocessMarkdownNodeRecursiveAsync(
        SmlNode node,
        string currentBaseUri,
        SmlDocument document,
        LocalizationStore localization,
        CancellationToken cancellationToken)
    {
        if (string.Equals(node.Name, "Markdown", StringComparison.OrdinalIgnoreCase))
        {
            await PreprocessMarkdownContentAsync(node, currentBaseUri, document, localization, cancellationToken);
        }

        foreach (var child in node.Children)
        {
            await PreprocessMarkdownNodeRecursiveAsync(child, currentBaseUri, document, localization, cancellationToken);
        }
    }

    private async Task PreprocessMarkdownContentAsync(
        SmlNode node,
        string currentBaseUri,
        SmlDocument document,
        LocalizationStore localization,
        CancellationToken cancellationToken)
    {
        if (node.TryGetProperty("textKey", out var textKeyValue))
        {
            var textKey = textKeyValue.AsStringOrThrow("textKey");
            if (!string.IsNullOrWhiteSpace(textKey) && localization.TryTranslate(textKey, out var localizedMarkdown))
            {
                node.Properties["text"] = SmlValue.FromString(localizedMarkdown);
            }
        }

        var hasText = node.TryGetProperty("text", out _);
        var hasSrc = node.TryGetProperty("src", out var srcValue);

        if (hasText && hasSrc)
        {
            document.Warnings.Add("Markdown node has both 'text' and 'src'. Preferring 'text'.");
        }

        if (!hasText && !hasSrc)
        {
            document.Warnings.Add("Markdown node requires exactly one of 'text' or 'src'. Rendering empty content.");
            node.Properties["text"] = SmlValue.FromString(string.Empty);
            node.Properties["markdownBaseUri"] = SmlValue.FromString(currentBaseUri);
            return;
        }

        if (hasText)
        {
            node.Properties["markdownBaseUri"] = SmlValue.FromString(currentBaseUri);
            RenderMarkdownChildren(node, document);
            node.Properties.Remove("src");
            node.Properties.Remove("markdownBaseUri");
            node.Properties.Remove("text");
            return;
        }

        var rawSource = srcValue!.AsStringOrThrow("src");
        var resolvedSource = _uriResolver.ResolveReference(rawSource, currentBaseUri);
        node.Properties["markdownBaseUri"] = SmlValue.FromString(resolvedSource);

        try
        {
            var markdown = await _uriResolver.LoadTextAsync(rawSource, currentBaseUri, cancellationToken);
            node.Properties["text"] = SmlValue.FromString(markdown);
        }
        catch (Exception ex)
        {
            document.Warnings.Add($"Markdown src load failed for '{rawSource}': {ex.Message}");
            node.Properties["text"] = SmlValue.FromString(string.Empty);
        }

        RenderMarkdownChildren(node, document);
        node.Properties.Remove("src");
        node.Properties.Remove("markdownBaseUri");
        node.Properties.Remove("text");
    }

    private void RenderMarkdownChildren(SmlNode node, SmlDocument document)
    {
        var markdownText = node.GetRequiredProperty("text").AsStringOrThrow("text");
        var markdownBase = node.GetRequiredProperty("markdownBaseUri").AsStringOrThrow("markdownBaseUri");
        var parseResult = MarkdownParser.Parse(markdownText);
        foreach (var warning in parseResult.Warnings)
        {
            document.Warnings.Add($"Markdown: {warning}");
        }

        node.Children.Clear();
        foreach (var block in parseResult.Blocks)
        {
            var rendered = RenderMarkdownBlock(block, markdownBase);
            if (rendered is not null)
            {
                PropagateMarkdownInteractionProperties(node, rendered);
                node.Children.Add(rendered);
            }
        }
    }

    private static void PropagateMarkdownInteractionProperties(SmlNode sourceMarkdownNode, SmlNode renderedChild)
    {
        if (sourceMarkdownNode.TryGetProperty("mouseFilter", out var mouseFilter)
            && !renderedChild.Properties.ContainsKey("mouseFilter"))
        {
            renderedChild.Properties["mouseFilter"] = mouseFilter;
        }

        foreach (var child in renderedChild.Children)
        {
            PropagateMarkdownInteractionProperties(sourceMarkdownNode, child);
        }
    }

    private SmlNode? RenderMarkdownBlock(MarkdownBlock block, string markdownBaseUri)
    {
        switch (block.Kind)
        {
            case MarkdownBlockKind.Heading:
                {
                    var heading = NewNode("MarkdownLabel");
                    heading.Properties["text"] = SmlValue.FromString(ToBbCode(block.Text));
                    heading.Properties["role"] = SmlValue.FromString(block.HeadingLevel switch
                    {
                        1 => "heading1",
                        2 => "heading2",
                        _ => "heading3"
                    });
                    heading.Properties["wrap"] = SmlValue.FromBool(true);
                    heading.Properties["fontSize"] = SmlValue.FromInt(block.HeadingLevel switch
                    {
                        1 => 32,
                        2 => 26,
                        _ => 22
                    });
                    ApplyMarkdownProperties(heading, block.Properties, markdownBaseUri);
                    return heading;
                }

            case MarkdownBlockKind.Paragraph:
                {
                    var paragraph = NewNode("MarkdownLabel");
                    paragraph.Properties["text"] = SmlValue.FromString(ToBbCode(block.Text));
                    paragraph.Properties["role"] = SmlValue.FromString("paragraph");
                    paragraph.Properties["wrap"] = SmlValue.FromBool(true);
                    ApplyMarkdownProperties(paragraph, block.Properties, markdownBaseUri);
                    return paragraph;
                }

            case MarkdownBlockKind.ListItem:
                {
                    var row = NewNode("HBoxContainer");

                    var bullet = NewNode("Label");
                    bullet.Properties["text"] = SmlValue.FromString("•");
                    bullet.Properties["width"] = SmlValue.FromInt(18);

                    var text = NewNode("MarkdownLabel");
                    text.Properties["text"] = SmlValue.FromString(ToBbCode(block.Text));
                    text.Properties["wrap"] = SmlValue.FromBool(true);
                    text.Properties["sizeFlagsHorizontal"] = SmlValue.FromInt(3);
                    text.Properties["sizeFlagsVertical"] = SmlValue.FromInt(3);

                    row.Children.Add(bullet);
                    row.Children.Add(text);
                    ApplyMarkdownProperties(row, block.Properties, markdownBaseUri);
                    return row;
                }

            case MarkdownBlockKind.Image:
                {
                    var image = NewNode("Image");
                    image.Properties["src"] = SmlValue.FromString(_uriResolver.ResolveReference(block.Source, markdownBaseUri));
                    image.Properties["alt"] = SmlValue.FromString(block.AltText);
                    ApplyMarkdownProperties(image, block.Properties, markdownBaseUri);
                    return image;
                }

            case MarkdownBlockKind.CodeFence:
                {
                    const int codeFontSize = 16;

                    var container = NewNode("PanelContainer");
                    container.Properties["role"] = SmlValue.FromString("codeblock");
                    container.Properties["sizeFlagsHorizontal"] = SmlValue.FromInt(3); // ExpandFill
                    container.Properties["sizeFlagsVertical"] = SmlValue.FromInt(1);   // Fill

                    var code = NewNode("MarkdownLabel");
                    code.Properties["role"] = SmlValue.FromString("code");
                    code.Properties["text"] = SmlValue.FromString($"[code]{EscapeBbCode(block.Text)}[/code]");
                    code.Properties["wrap"] = SmlValue.FromBool(false);
                    code.Properties["font"] = SmlValue.FromString("appres://assets/fonts/JetBrainsMono-Regular.ttf");
                    code.Properties["fontSize"] = SmlValue.FromInt(codeFontSize);
                    code.Properties["sizeFlagsHorizontal"] = SmlValue.FromInt(3); // ExpandFill
                    code.Properties["sizeFlagsVertical"] = SmlValue.FromInt(1);   // Fill
                    container.Children.Add(code);
                    ApplyMarkdownProperties(container, block.Properties, markdownBaseUri);
                    return container;
                }

            default:
                return null;
        }
    }

    private static SmlNode NewNode(string name)
    {
        return new SmlNode { Name = name, Line = 0 };
    }

    private void ApplyMarkdownProperties(SmlNode node, Dictionary<string, string> markdownProperties, string markdownBaseUri)
    {
        foreach (var (key, rawValue) in markdownProperties)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            switch (key.ToLowerInvariant())
            {
                case "id":
                case "align":
                case "color":
                case "role":
                    node.Properties[key] = SmlValue.FromString(rawValue);
                    break;

                case "size":
                    if (TryParseVec2(rawValue, out var x, out var y))
                    {
                        node.Properties["width"] = SmlValue.FromInt(x);
                        node.Properties["height"] = SmlValue.FromInt(y);
                    }
                    break;

                case "src":
                    node.Properties[key] = SmlValue.FromString(_uriResolver.ResolveReference(rawValue, markdownBaseUri));
                    break;
            }
        }
    }

    private static bool TryParseVec2(string raw, out int x, out int y)
    {
        x = 0;
        y = 0;
        var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
    }

    private static string NormalizeCodeFenceLanguageToSyntax(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return string.Empty;
        }

        return language.Trim().ToLowerInvariant() switch
        {
            "sml" => "sml",
            "sms" => "sms",
            "cs" => "cs",
            "csharp" => "cs",
            "c#" => "cs",
            "md" => "markdown",
            "markdown" => "markdown",
            _ => string.Empty
        };
    }

    private static int CalculateCodeFenceHeight(string codeText, int fontSize)
    {
        var normalized = (codeText ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);
        normalized = normalized.TrimEnd('\n');
        var lineCount = string.IsNullOrEmpty(normalized)
            ? 1
            : normalized.Split('\n').Length;

        var font = GD.Load<Font>("res://assets/fonts/JetBrainsMono-Regular.ttf") ?? CreateMonospaceSystemFontForMetrics();
        var lineHeight = Math.Max(1f, font.GetHeight(fontSize));

        const int verticalPadding = 12;
        const int framePadding = 4;

        var totalHeight = (lineHeight * lineCount) + (verticalPadding * 2) + framePadding;
        return Math.Max(56, (int)MathF.Ceiling(totalHeight));
    }

    private static Font CreateMonospaceSystemFontForMetrics()
    {
        return new SystemFont
        {
            FontNames = new[]
            {
                "Menlo",
                "Monaco",
                "Consolas",
                "Courier New",
                "DejaVu Sans Mono",
                "monospace"
            }
        };
    }

    private static string ToBbCode(string text)
    {
        var withEmoji = ReplaceEmojiNames(text ?? string.Empty);
        var escaped = EscapeBbCode(withEmoji);

        // Order matters: first bold+italic, then bold, then italic.
        escaped = Regex.Replace(escaped, "\\*\\*\\*([^*]+)\\*\\*\\*", "[b][i]$1[/i][/b]");
        escaped = Regex.Replace(escaped, "\\*\\*([^*]+)\\*\\*", "[b]$1[/b]");
        escaped = Regex.Replace(escaped, "\\*([^*]+)\\*", "[i]$1[/i]");

        return escaped;
    }

    private static string EscapeBbCode(string input)
    {
        return input
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal);
    }

    private static string ReplaceEmojiNames(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["smile"] = "😄",
            ["warning"] = "⚠️",
            ["rocket"] = "🚀",
            ["heart"] = "❤️"
        };

        return Regex.Replace(input, ":([a-zA-Z0-9_+-]+):", m =>
        {
            var key = m.Groups[1].Value;
            return map.TryGetValue(key, out var emoji) ? emoji : m.Value;
        });
    }
}
