using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Runtime.UI;

public static class CodeEditSyntaxRuntime
{
    public const string SyntaxAuto = "auto";
    public const string SyntaxSml = "sml";
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
        editor.SyntaxHighlighter = CreateHighlighter(editor, syntax);

        if (!string.Equals(previousSyntax, syntax, StringComparison.Ordinal))
        {
            RunnerLogger.Info("UI", $"CodeEdit syntax switched: '{previousSyntax}' -> '{syntax}'.");
        }

        editor.ScrollVertical = previousScroll;
        editor.ScrollHorizontal = previousScrollHorizontal;
        editor.QueueRedraw();
    }

    private static SyntaxHighlighter? CreateHighlighter(CodeEdit editor, string syntax)
    {
        if (string.Equals(syntax, SyntaxPlainText, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(syntax, SyntaxSml, StringComparison.OrdinalIgnoreCase))
        {
            return CreateSmlStructuralHighlighter(editor.Text);
        }

        if (syntax.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return CreateHighlighterFromRuleFile(syntax);
        }

        var mappedRulePath = syntax.ToLowerInvariant() switch
        {
            SyntaxSml => "res://syntax/sml_syntax.cs",
            SyntaxCs => "res://syntax/cs_syntax.cs",
            SyntaxMarkdown => "res://syntax/markdown_syntax.cs",
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
            SyntaxMarkdown => CreateMarkdownFallbackHighlighter(),
            _ => null
        };
    }

    private static SyntaxHighlighter CreateSmlStructuralHighlighter(string text)
    {
        var highlighter = new CodeHighlighter();
        highlighter.NumberColor = new Color(0.95f, 0.83f, 0.56f, 1f);
        highlighter.SymbolColor = new Color(0.82f, 0.84f, 0.88f, 1f);

        var nodeColor = new Color(0.49f, 0.86f, 0.79f, 1f);
        var propertyColor = new Color(0.55f, 0.72f, 1f, 1f);
        var literalColor = new Color(0.96f, 0.69f, 0.45f, 1f);
        var boolColor = new Color(0.83f, 0.72f, 0.97f, 1f);
        var commentColor = new Color(0.52f, 0.57f, 0.55f, 1f);

        highlighter.AddColorRegion("\"", "\"", literalColor);
        highlighter.AddColorRegion("//", "\n", commentColor, true);

        foreach (var nodeName in ExtractSmlNodeNames(text))
        {
            highlighter.AddKeywordColor(nodeName, nodeColor);
        }

        foreach (var propertyName in ExtractSmlPropertyNames(text))
        {
            highlighter.AddMemberKeywordColor(propertyName, propertyColor);
        }

        highlighter.AddKeywordColor("true", boolColor);
        highlighter.AddKeywordColor("false", boolColor);

        return highlighter;
    }

    private static SyntaxHighlighter? CreateHighlighterFromRuleFile(string rulePath)
    {
        var highlighter = new CodeHighlighter();
        highlighter.NumberColor = new Color(0.86f, 0.69f, 0.44f, 1f);
        highlighter.SymbolColor = new Color(0.76f, 0.76f, 0.76f, 1f);

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

            highlighter.AddKeywordColor(token, new Color(0.53f, 0.73f, 1f, 1f));
            hasKeyword = true;
        }

        if (hasKeyword)
        {
            return highlighter;
        }

        foreach (var line in File.ReadAllLines(globalPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            highlighter.AddKeywordColor(trimmed, new Color(0.53f, 0.73f, 1f, 1f));
            hasKeyword = true;
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
        highlighter.NumberColor = new Color(0.86f, 0.69f, 0.44f, 1f);
        highlighter.SymbolColor = new Color(0.76f, 0.76f, 0.76f, 1f);
        highlighter.AddKeywordColor("public", new Color(0.82f, 0.53f, 1f, 1f));
        highlighter.AddKeywordColor("private", new Color(0.82f, 0.53f, 1f, 1f));
        highlighter.AddKeywordColor("class", new Color(0.82f, 0.53f, 1f, 1f));
        highlighter.AddKeywordColor("void", new Color(0.82f, 0.53f, 1f, 1f));
        highlighter.AddKeywordColor("string", new Color(0.36f, 0.8f, 0.95f, 1f));
        highlighter.AddColorRegion("\"", "\"", new Color(0.63f, 0.85f, 0.58f, 1f));
        highlighter.AddColorRegion("//", "\n", new Color(0.5f, 0.56f, 0.5f, 1f), true);
        return highlighter;
    }

    private static SyntaxHighlighter CreateMarkdownFallbackHighlighter()
    {
        var highlighter = new CodeHighlighter();
        highlighter.NumberColor = new Color(0.86f, 0.69f, 0.44f, 1f);
        highlighter.SymbolColor = new Color(0.76f, 0.76f, 0.76f, 1f);
        highlighter.AddColorRegion("```", "```", new Color(0.53f, 0.73f, 1f, 1f));
        highlighter.AddColorRegion("`", "`", new Color(0.53f, 0.73f, 1f, 1f));
        highlighter.AddColorRegion("[", ")", new Color(0.63f, 0.85f, 0.58f, 1f));
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
            if (!string.Equals(activeSyntax, SyntaxSml, StringComparison.OrdinalIgnoreCase))
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

            editor.SyntaxHighlighter = CreateSmlStructuralHighlighter(editor.Text);
            editor.ScrollVertical = previousScroll;
            editor.ScrollHorizontal = previousScrollHorizontal;
            editor.QueueRedraw();
        };

        editor.SetMeta(MetaSyntaxHooked, Variant.From(true));
    }

    private static HashSet<string> ExtractSmlNodeNames(string text)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        foreach (Match match in Regex.Matches(text, @"(?m)^\s*([A-Za-z_][A-Za-z0-9_]*)\s*\{"))
        {
            var identifier = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                result.Add(identifier);
            }
        }

        return result;
    }

    private static HashSet<string> ExtractSmlPropertyNames(string text)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        foreach (Match match in Regex.Matches(text, @"(?m)^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:\s*"))
        {
            var identifier = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                result.Add(identifier);
            }
        }

        return result;
    }
}
