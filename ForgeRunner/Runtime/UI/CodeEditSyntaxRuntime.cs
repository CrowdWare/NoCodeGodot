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
using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Runtime.UI;

public static partial class CodeEditSyntaxRuntime
{
    public const string SyntaxAuto = "auto";
    public const string SyntaxSml = "sml";
    public const string SyntaxSms = "sms";
    public const string SyntaxCs = "cs";
    public const string SyntaxMarkdown = "markdown";
    public const string SyntaxPlainText = "plain_text";

    private const string MetaRequestedSyntax = "sml_codeedit_requestedSyntax";
    private const string MetaActiveSyntax = "sml_codeedit_activeSyntax";
    private const string MetaAssociatedPath = "sml_codeedit_associatedPath";
    private const string MetaSyntaxHooked = "sml_codeedit_syntaxHooked";

    public static void SetSyntax(CodeEdit editor, string? syntax)
    {
        EnsureSyntaxHook(editor);
        var requested = NormalizeSyntaxValue(syntax);
        editor.SetMeta(MetaRequestedSyntax, Variant.From(requested));
        RecomputeWithoutPath(editor);
    }

    public static void Load(CodeEdit editor, string path)
    {
        EnsureSyntaxHook(editor);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        editor.SetMeta(MetaAssociatedPath, Variant.From(path));
        RecomputeFromPath(editor, path);
    }

    public static void SaveAs(CodeEdit editor, string path)
    {
        EnsureSyntaxHook(editor);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        editor.SetMeta(MetaAssociatedPath, Variant.From(path));
        RecomputeFromPath(editor, path);
    }

    public static void Save_As(CodeEdit editor, string path)
    {
        SaveAs(editor, path);
    }

    private static void RecomputeWithoutPath(CodeEdit editor)
    {
        var associatedPath = GetMetaString(editor, MetaAssociatedPath);
        if (!string.IsNullOrWhiteSpace(associatedPath))
        {
            RecomputeFromPath(editor, associatedPath);
            return;
        }

        var requestedSyntax = GetMetaString(editor, MetaRequestedSyntax);
        if (string.IsNullOrWhiteSpace(requestedSyntax))
        {
            requestedSyntax = SyntaxAuto;
        }

        var syntax = requestedSyntax switch
        {
            SyntaxSml => SyntaxSml,
            SyntaxSms => SyntaxSms,
            SyntaxCs => SyntaxCs,
            SyntaxMarkdown => SyntaxMarkdown,
            _ when requestedSyntax.StartsWith("res://", StringComparison.OrdinalIgnoreCase) => requestedSyntax,
            _ => SyntaxPlainText
        };

        ApplyResolvedSyntax(editor, syntax);
    }

    private static void RecomputeFromPath(CodeEdit editor, string path)
    {
        var resolved = DetectSyntaxByExtension(path);
        ApplyResolvedSyntax(editor, resolved);
    }

    private static string DetectSyntaxByExtension(string path)
    {
        var extension = Path.GetExtension(path)?.ToLowerInvariant() ?? string.Empty;
        return extension switch
        {
            ".sml" => SyntaxSml,
            ".sms" => SyntaxSms,
            ".cs" => SyntaxCs,
            ".md" => SyntaxMarkdown,
            ".markdown" => SyntaxMarkdown,
            _ => SyntaxPlainText
        };
    }

    private static void ApplyResolvedSyntax(CodeEdit editor, string syntax)
    {
        EnsureSyntaxHook(editor);
        var previousSyntax = GetMetaString(editor, MetaActiveSyntax);
        var previousScroll = editor.ScrollVertical;
        var previousScrollHorizontal = editor.ScrollHorizontal;

        if (editor.SyntaxHighlighter is not null)
        {
            var old = editor.SyntaxHighlighter;
            editor.SyntaxHighlighter = null;
            old.Dispose();
        }

        editor.SetMeta(MetaActiveSyntax, Variant.From(syntax));
        ApplyCodeEditorTheme(editor);
        editor.SyntaxHighlighter = CreateHighlighter(syntax);

        if (!string.Equals(previousSyntax, syntax, StringComparison.Ordinal))
        {
            RunnerLogger.Info("UI", $"CodeEdit syntax switched: '{previousSyntax}' -> '{syntax}'.");
        }

        editor.ScrollVertical = previousScroll;
        editor.ScrollHorizontal = previousScrollHorizontal;
        editor.QueueRedraw();
    }

    private static SyntaxHighlighter? CreateHighlighter(string syntax)
    {
        if (string.Equals(syntax, SyntaxPlainText, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(syntax, SyntaxSml, StringComparison.OrdinalIgnoreCase))
        {
            return CreateSmlStructuralHighlighter();
        }

        if (string.Equals(syntax, SyntaxMarkdown, StringComparison.OrdinalIgnoreCase))
        {
            return CreateMarkdownHighlighter();
        }

        if (syntax.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return CreateHighlighterFromRuleFile(syntax);
        }

        var mappedRulePath = syntax.ToLowerInvariant() switch
        {
            SyntaxSml => "res://syntax/sml_syntax.cs",
            SyntaxSms => "res://syntax/sms_syntax.cs",
            SyntaxCs => "res://syntax/cs_syntax.cs",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(mappedRulePath))
        {
            var fromRuleFile = CreateHighlighterFromRuleFile(mappedRulePath);
            if (fromRuleFile is not null)
            {
                return fromRuleFile;
            }
        }

        return syntax.ToLowerInvariant() switch
        {
            SyntaxCs => CreateCsFallbackHighlighter(),
            SyntaxMarkdown => CreateMarkdownHighlighter(),
            _ => null
        };
    }

    private static SmlSyntaxHighlighter CreateSmlStructuralHighlighter()
    {
        return new SmlSyntaxHighlighter();
    }

    private static SyntaxHighlighter? CreateHighlighterFromRuleFile(string rulePath)
    {
        var highlighter = new CodeHighlighter();
        highlighter.NumberColor = new Color(0.97f, 0.82f, 0.58f, 1f);
        highlighter.SymbolColor = new Color(0.83f, 0.86f, 0.90f, 1f);
        highlighter.FunctionColor = new Color(0.94f, 0.96f, 0.99f, 1f);
        highlighter.MemberVariableColor = new Color(0.80f, 0.90f, 1f, 1f);
        highlighter.AddColorRegion("\"", "\"", new Color(0.745f, 0.537f, 0.435f, 1f));
        highlighter.AddColorRegion("//", string.Empty, new Color(0.58f, 0.64f, 0.60f, 1f), true);

        
        var keywordColor = new Color(0.357f, 0.533f, 0.769f, 1f);
        
        var globalPath = ProjectSettings.GlobalizePath(rulePath);
        if (!File.Exists(globalPath))
        {
            RunnerLogger.Warn("UI", $"Syntax rule file '{rulePath}' not found. Falling back to plain text.");
            highlighter.Dispose();
            return null;
        }

        var content = File.ReadAllText(globalPath);
        var hasKeyword = false;

        foreach (Match match in Regex.Matches(content, "\"([^\"\\r\\n]+)\""))
        {
            var token = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            highlighter.AddKeywordColor(token, keywordColor);
            hasKeyword = true;
        }

        if (!hasKeyword)
        {
            foreach (var line in File.ReadAllLines(globalPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                highlighter.AddKeywordColor(trimmed, keywordColor);
                hasKeyword = true;
            }
        }

        if (!hasKeyword)
        {
            highlighter.Dispose();
            RunnerLogger.Warn("UI", $"Syntax rule file '{rulePath}' contains no usable rules. Falling back to plain text.");
            return null;
        }

        return highlighter;
    }

    private static SyntaxHighlighter CreateCsFallbackHighlighter()
    {
        var highlighter = new CodeHighlighter();
        highlighter.NumberColor = new Color(0.97f, 0.82f, 0.58f, 1f);
        highlighter.SymbolColor = new Color(0.83f, 0.86f, 0.90f, 1f);
        highlighter.FunctionColor = new Color(0.94f, 0.96f, 0.99f, 1f);
        highlighter.MemberVariableColor = new Color(0.80f, 0.90f, 1f, 1f);
        highlighter.AddKeywordColor("public", new Color(0.86f, 0.70f, 1f, 1f));
        highlighter.AddKeywordColor("private", new Color(0.86f, 0.70f, 1f, 1f));
        highlighter.AddKeywordColor("class", new Color(0.86f, 0.70f, 1f, 1f));
        highlighter.AddKeywordColor("void", new Color(0.86f, 0.70f, 1f, 1f));
        highlighter.AddKeywordColor("string", new Color(0.58f, 0.84f, 1f, 1f));
        highlighter.AddColorRegion("\"", "\"", new Color(0.73f, 0.92f, 0.67f, 1f));
        highlighter.AddColorRegion("//", string.Empty, new Color(0.58f, 0.64f, 0.60f, 1f), true);
        return highlighter;
    }

    private static SyntaxHighlighter CreateMarkdownHighlighter()
    {
        var highlighter = new CodeHighlighter();
        highlighter.NumberColor = new Color(0.97f, 0.82f, 0.58f, 1f);
        highlighter.SymbolColor = new Color(0.86f, 0.88f, 0.92f, 1f);
        highlighter.FunctionColor = new Color(0.94f, 0.96f, 0.99f, 1f);
        highlighter.MemberVariableColor = new Color(0.94f, 0.96f, 0.99f, 1f);

        var h1Color = new Color(0.92f, 0.72f, 1f, 1f);
        var h2Color = new Color(0.82f, 0.76f, 1f, 1f);
        var h3Color = new Color(0.75f, 0.82f, 1f, 1f);
        var boldItalicColor = new Color(1f, 0.82f, 0.66f, 1f);
        var boldColor = new Color(0.98f, 0.84f, 0.65f, 1f);
        var italicColor = new Color(0.96f, 0.78f, 0.72f, 1f);

        // Markdown emphasis: longest token first to avoid partial matches.
        highlighter.AddColorRegion("***", "***", boldItalicColor);
        highlighter.AddColorRegion("**", "**", boldColor);
        highlighter.AddColorRegion("*", "*", italicColor);

        // Markdown headings by level: color line from marker to line end.
        highlighter.AddColorRegion("### ", string.Empty, h3Color, true);
        highlighter.AddColorRegion("## ", string.Empty, h2Color, true);
        highlighter.AddColorRegion("# ", string.Empty, h1Color, true);

        return highlighter;
    }

    private static string NormalizeSyntaxValue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return SyntaxAuto;
        }

        var value = raw.Trim();
        if (value.StartsWith("res:/", StringComparison.OrdinalIgnoreCase))
        {
            return "res://" + value[5..].TrimStart('/');
        }

        return value.ToLowerInvariant() switch
        {
            SyntaxAuto => SyntaxAuto,
            SyntaxSml => SyntaxSml,
            SyntaxSms => SyntaxSms,
            SyntaxCs => SyntaxCs,
            SyntaxMarkdown => SyntaxMarkdown,
            _ => value
        };
    }

    private static string GetMetaString(Control control, string key)
    {
        return control.HasMeta(key) ? control.GetMeta(key).AsString() : string.Empty;
    }

    private static void EnsureSyntaxHook(CodeEdit editor)
    {
        if (editor.HasMeta(MetaSyntaxHooked) && editor.GetMeta(MetaSyntaxHooked).AsBool())
        {
            return;
        }

        editor.TextChanged += () =>
        {
            var activeSyntax = GetMetaString(editor, MetaActiveSyntax);
            var isSml = string.Equals(activeSyntax, SyntaxSml, StringComparison.OrdinalIgnoreCase);
            var isSms = string.Equals(activeSyntax, SyntaxSms, StringComparison.OrdinalIgnoreCase);
            if (!isSml && !isSms)
            {
                return;
            }

            var previousScroll = editor.ScrollVertical;
            var previousScrollHorizontal = editor.ScrollHorizontal;

            if (editor.SyntaxHighlighter is not null)
            {
                var old = editor.SyntaxHighlighter;
                editor.SyntaxHighlighter = null;
                old.Dispose();
            }

            editor.SyntaxHighlighter = isSml
                ? CreateSmlStructuralHighlighter()
                : CreateHighlighter(SyntaxSms);

            editor.ScrollVertical = previousScroll;
            editor.ScrollHorizontal = previousScrollHorizontal;
            editor.QueueRedraw();
        };

        editor.SetMeta(MetaSyntaxHooked, Variant.From(true));
    }

    private static void ApplyCodeEditorTheme(CodeEdit editor)
    {
        editor.AddThemeColorOverride("font_color", new Color(0.96f, 0.97f, 0.99f, 1f));
        editor.AddThemeColorOverride("font_readonly_color", new Color(0.89f, 0.92f, 0.95f, 1f));
        editor.AddThemeColorOverride("caret_color", new Color(0.98f, 0.98f, 1f, 1f));
        editor.AddThemeColorOverride("selection_color", new Color(0.30f, 0.46f, 0.68f, 0.58f));
        editor.AddThemeColorOverride("current_line_color", new Color(1f, 1f, 1f, 0.06f));
        editor.AddThemeColorOverride("line_number_color", new Color(0.74f, 0.78f, 0.84f, 1f));
    }

    private sealed partial class SmlSyntaxHighlighter : SyntaxHighlighter
    {
        private static readonly Color DefaultColor  = new(0.96f,  0.97f,  0.99f,  1f);
        private static readonly Color NodeColor     = new(0.337f, 0.612f, 0.839f, 1f); // #569CD6 - constant.language
        private static readonly Color PropertyColor = new(0.612f, 0.863f, 0.996f, 1f); // #9CDCFE - variable.other.property
        private static readonly Color LiteralColor  = new(0.808f, 0.569f, 0.471f, 1f); // #CE9178 - string.quoted
        private static readonly Color BoolColor     = new(0.337f, 0.612f, 0.839f, 1f); // #569CD6 - constant.language (same scope as nodes)
        private static readonly Color CommentColor  = new(0.416f, 0.600f, 0.333f, 1f); // #6A9955 - comment
        private static readonly Color NumberColor   = new(0.710f, 0.808f, 0.659f, 1f); // #B5CEA8 - constant.numeric
        private static readonly Color ResourceColor = new(0.56f,  0.93f,  0.56f,  1f); // #8FED8F - resource link

        private static readonly Regex NodeLineRegex      = new(@"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*\{",                          RegexOptions.Compiled);
        private static readonly Regex AttachedKeyRegex   = new(@"^\s*([A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*)\s*:", RegexOptions.Compiled);
        private static readonly Regex PropertyKeyRegex   = new(@"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:",                         RegexOptions.Compiled);
        private static readonly Regex InlinePropKeyRegex = new(@"(?<![A-Za-z_\d\.])([A-Za-z_][A-Za-z0-9_]*)\s*:(?!:)",      RegexOptions.Compiled);
        private static readonly Regex ResourceRefRegex   = new(@"@[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]+)+",    RegexOptions.Compiled);
        private static readonly Regex StringRegex        = new(@"""(?:[^""\\]|\\.)*""",                                      RegexOptions.Compiled);
        private static readonly Regex NumberRegex        = new(@"(?<![A-Za-z_.\d])-?\d+(?:\.\d+)?(?![A-Za-z_.\d])",         RegexOptions.Compiled);
        private static readonly Regex BoolRegex          = new(@"(?<![A-Za-z_])(true|false)(?![A-Za-z_])",                  RegexOptions.Compiled);

        public override Godot.Collections.Dictionary _GetLineSyntaxHighlighting(int line)
        {
            var result   = new Godot.Collections.Dictionary();
            var textEdit = GetTextEdit();
            if (textEdit == null) return result;

            var text = textEdit.GetLine(line);
            if (string.IsNullOrEmpty(text)) return result;

            Mark(result, 0, DefaultColor);

            var commentAt = FindCommentStart(text);
            var code      = commentAt >= 0 ? text[..commentAt] : text;

            var nodeMatch = NodeLineRegex.Match(code);
            if (nodeMatch.Success)
            {
                var g = nodeMatch.Groups[1];
                Mark(result, g.Index, NodeColor);
                Mark(result, g.Index + g.Length, DefaultColor);
                var braceIdx = code.IndexOf('{', g.Index + g.Length);
                if (braceIdx >= 0 && braceIdx + 1 < code.Length)
                    HighlightInlineContent(code, braceIdx + 1, result);
            }
            else
            {
                var keyMatch = AttachedKeyRegex.Match(code);
                if (!keyMatch.Success)
                    keyMatch = PropertyKeyRegex.Match(code);

                if (keyMatch.Success)
                {
                    var g = keyMatch.Groups[1];
                    Mark(result, g.Index, PropertyColor);
                    var colon = code.IndexOf(':', g.Index + g.Length);
                    if (colon >= 0)
                    {
                        Mark(result, colon, DefaultColor);
                        HighlightValue(code, colon + 1, result);
                    }
                }
            }

            if (commentAt >= 0)
                Mark(result, commentAt, CommentColor);

            return result;
        }

        private static void HighlightInlineContent(string code, int from, Godot.Collections.Dictionary result)
        {
            if (from >= code.Length) return;
            var slice = code[from..];
            var spans = new List<(int S, int E, Color C)>();

            // String literals first – protect their content from being re-colored.
            foreach (Match m in StringRegex.Matches(slice))
                spans.Add((from + m.Index, from + m.Index + m.Length, LiteralColor));
            foreach (Match m in ResourceRefRegex.Matches(slice))
                spans.Add((from + m.Index, from + m.Index + m.Length, ResourceColor));
            foreach (Match m in InlinePropKeyRegex.Matches(slice))
            {
                var g = m.Groups[1];
                spans.Add((from + g.Index, from + g.Index + g.Length, PropertyColor));
            }
            foreach (Match m in BoolRegex.Matches(slice))
                spans.Add((from + m.Index, from + m.Index + m.Length, BoolColor));
            foreach (Match m in NumberRegex.Matches(slice))
                spans.Add((from + m.Index, from + m.Index + m.Length, NumberColor));

            spans.Sort((a, b) => a.S.CompareTo(b.S));
            var cursor = from;
            foreach (var (s, e, color) in spans)
            {
                if (s < cursor) continue;
                Mark(result, s, color);
                Mark(result, e, DefaultColor);
                cursor = e;
            }
        }

        private static void HighlightValue(string code, int from, Godot.Collections.Dictionary result)
        {
            if (from >= code.Length) return;
            var slice = code[from..];
            var spans = new List<(int S, int E, Color C)>();

            foreach (Match m in ResourceRefRegex.Matches(slice))
                spans.Add((from + m.Index, from + m.Index + m.Length, ResourceColor));
            foreach (Match m in StringRegex.Matches(slice))
                spans.Add((from + m.Index, from + m.Index + m.Length, LiteralColor));
            foreach (Match m in BoolRegex.Matches(slice))
                spans.Add((from + m.Index, from + m.Index + m.Length, BoolColor));
            foreach (Match m in NumberRegex.Matches(slice))
                spans.Add((from + m.Index, from + m.Index + m.Length, NumberColor));

            spans.Sort((a, b) => a.S.CompareTo(b.S));
            var cursor = from;
            foreach (var (s, e, color) in spans)
            {
                if (s < cursor) continue;
                Mark(result, s, color);
                Mark(result, e, DefaultColor);
                cursor = e;
            }
        }

        private static int FindCommentStart(string text)
        {
            var inStr = false;
            for (var i = 0; i < text.Length - 1; i++)
            {
                if (text[i] == '"' && (i == 0 || text[i - 1] != '\\'))
                    inStr = !inStr;
                if (!inStr && text[i] == '/' && text[i + 1] == '/')
                    return i;
            }
            return -1;
        }

        private static void Mark(Godot.Collections.Dictionary result, int col, Color color)
        {
            var entry = new Godot.Collections.Dictionary();
            entry["color"] = color;
            result[col] = entry;
        }
    }
}