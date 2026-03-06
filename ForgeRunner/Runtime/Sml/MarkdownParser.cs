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
using System.Text;

namespace Runtime.Sml;

public enum MarkdownBlockKind
{
    Heading,
    Paragraph,
    ListItem,
    Image,
    CodeFence
}

public sealed class MarkdownBlock
{
    public MarkdownBlockKind Kind { get; init; }
    public string Text { get; set; } = string.Empty;
    public int HeadingLevel { get; set; }
    public string AltText { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MarkdownParseResult
{
    public List<MarkdownBlock> Blocks { get; } = [];
    public List<string> Warnings { get; } = [];
}

public static class MarkdownParser
{
    public static MarkdownParseResult Parse(string text)
    {
        var result = new MarkdownParseResult();
        var lines = Normalize(text).Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            if (TryParseCodeFence(lines, ref i, out var code))
            {
                result.Blocks.Add(code);
                TryParsePropertyBlock(lines, ref i, code, result.Warnings);
                continue;
            }

            if (TryParseHeading(line, out var heading))
            {
                result.Blocks.Add(heading);
                i++;
                TryParsePropertyBlock(lines, ref i, heading, result.Warnings);
                continue;
            }

            if (TryParseImage(line, out var image))
            {
                result.Blocks.Add(image);
                i++;
                TryParsePropertyBlock(lines, ref i, image, result.Warnings);
                continue;
            }

            if (TryParseListItem(line, out var listItem))
            {
                result.Blocks.Add(listItem);
                i++;
                TryParsePropertyBlock(lines, ref i, listItem, result.Warnings);
                continue;
            }

            var paragraph = ParseParagraph(lines, ref i);
            result.Blocks.Add(paragraph);
            TryParsePropertyBlock(lines, ref i, paragraph, result.Warnings);
        }

        return result;
    }

    private static string Normalize(string text) => (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');

    private static bool TryParseHeading(string line, out MarkdownBlock block)
    {
        block = null!;
        var trimmed = line.TrimStart();
        var level = 0;
        while (level < trimmed.Length && level < 3 && trimmed[level] == '#')
        {
            level++;
        }

        if (level == 0 || level >= trimmed.Length || trimmed[level] != ' ')
        {
            return false;
        }

        block = new MarkdownBlock
        {
            Kind = MarkdownBlockKind.Heading,
            HeadingLevel = level,
            Text = trimmed[(level + 1)..]
        };

        return true;
    }

    private static bool TryParseImage(string line, out MarkdownBlock block)
    {
        block = null!;
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("![", StringComparison.Ordinal) || !trimmed.EndsWith(")", StringComparison.Ordinal))
        {
            return false;
        }

        var altClose = trimmed.IndexOf(']');
        var openParen = trimmed.IndexOf('(', altClose + 1);
        if (altClose < 2 || openParen < 0)
        {
            return false;
        }

        block = new MarkdownBlock
        {
            Kind = MarkdownBlockKind.Image,
            AltText = trimmed[2..altClose],
            Source = trimmed[(openParen + 1)..^1].Trim()
        };
        return !string.IsNullOrWhiteSpace(block.Source);
    }

    private static bool TryParseListItem(string line, out MarkdownBlock block)
    {
        block = null!;
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
            return false;
        }

        block = new MarkdownBlock
        {
            Kind = MarkdownBlockKind.ListItem,
            Text = trimmed[2..]
        };
        return true;
    }

    private static bool TryParseCodeFence(string[] lines, ref int index, out MarkdownBlock block)
    {
        block = null!;
        var line = lines[index].TrimStart();
        if (!line.StartsWith("```", StringComparison.Ordinal))
        {
            return false;
        }

        var language = line.Length > 3 ? line[3..].Trim() : string.Empty;
        var sb = new StringBuilder();
        index++;

        while (index < lines.Length)
        {
            var current = lines[index];
            if (current.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                index++;
                break;
            }

            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append(current);
            index++;
        }

        block = new MarkdownBlock
        {
            Kind = MarkdownBlockKind.CodeFence,
            Language = language,
            Text = sb.ToString()
        };
        return true;
    }

    private static MarkdownBlock ParseParagraph(string[] lines, ref int index)
    {
        var sb = new StringBuilder();
        var appendNewlineBeforeNext = false;

        while (index < lines.Length)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            var trimmed = line.TrimEnd();
            var hardBreak = line.EndsWith("  ", StringComparison.Ordinal);
            if (sb.Length > 0)
            {
                sb.Append(appendNewlineBeforeNext ? '\n' : ' ');
            }

            sb.Append(trimmed);
            appendNewlineBeforeNext = hardBreak;
            index++;

            if (index < lines.Length)
            {
                var peek = lines[index].TrimStart();
                if (peek.StartsWith("#", StringComparison.Ordinal)
                    || peek.StartsWith("- ", StringComparison.Ordinal)
                    || peek.StartsWith("![", StringComparison.Ordinal)
                    || peek.StartsWith("```", StringComparison.Ordinal))
                {
                    break;
                }
            }
        }

        return new MarkdownBlock
        {
            Kind = MarkdownBlockKind.Paragraph,
            Text = sb.ToString()
        };
    }

    private static void TryParsePropertyBlock(
        string[] lines,
        ref int index,
        MarkdownBlock block,
        List<string> warnings)
    {
        var probe = index;

        if (probe >= lines.Length || lines[probe].Trim() != "{")
        {
            return;
        }

        probe++;
        while (probe < lines.Length)
        {
            var line = lines[probe].Trim();
            if (line == "}")
            {
                index = probe + 1;
                return;
            }

            if (line.Length == 0)
            {
                probe++;
                continue;
            }

            var sep = line.IndexOf(':');
            if (sep <= 0)
            {
                warnings.Add($"Invalid markdown property line '{line}'.");
                probe++;
                continue;
            }

            var key = line[..sep].Trim();
            var value = line[(sep + 1)..].Trim();
            block.Properties[key] = value;
            probe++;
        }

        warnings.Add("Unterminated markdown property block.");
        index = probe;
    }
}
