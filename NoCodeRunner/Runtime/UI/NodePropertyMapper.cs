using Godot;
using Runtime.Generated;
using Runtime.Logging;
using Runtime.Sml;
using Runtime.ThreeD;
using System;
using System.Globalization;
using System.Collections.Generic;

namespace Runtime.UI;

public sealed class NodePropertyMapper
{
    public const string MetaId = "sml_id";
    public const string MetaIdValue = "sml_id_value";
    public const string MetaAction = "sml_action";
    public const string MetaClicked = "sml_clicked";
    public const string MetaClickedIdValue = "sml_clicked_id_value";
    public const string MetaNodeName = "sml_nodeName";
    public const string MetaX = "sml_x";
    public const string MetaY = "sml_y";
    public const string MetaWidth = "sml_width";
    public const string MetaHeight = "sml_height";
    public const string MetaCenterX = "sml_centerX";
    public const string MetaCenterY = "sml_centerY";
    public const string MetaAnchorLeft = "sml_anchorLeft";
    public const string MetaAnchorRight = "sml_anchorRight";
    public const string MetaAnchorTop = "sml_anchorTop";
    public const string MetaAnchorBottom = "sml_anchorBottom";
    public const string MetaScrollable = "sml_scrollable";
    public const string MetaScrollbarWidth = "sml_scrollBarWidth";
    public const string MetaScrollbarHeight = "sml_scrollBarHeight";
    public const string MetaScrollbarPosition = "sml_scrollBarPosition";
    public const string MetaScrollbarVisible = "sml_scrollBarVisible";
    public const string MetaScrollbarVisibleOnScroll = "sml_scrollBarVisibleOnScroll";
    public const string MetaScrollbarFadeOutTime = "sml_scrollBarFadeOutTime";
    public const string MetaWindowMinSizeX = "sml_windowMinSizeX";
    public const string MetaWindowMinSizeY = "sml_windowMinSizeY";
    public const string MetaWindowTitle = "sml_windowTitle";
    public const string MetaWindowPosX = "sml_windowPosX";
    public const string MetaWindowPosY = "sml_windowPosY";
    public const string MetaWindowSizeX = "sml_windowSizeX";
    public const string MetaWindowSizeY = "sml_windowSizeY";
    public const string MetaPaddingTop = "sml_paddingTop";
    public const string MetaPaddingRight = "sml_paddingRight";
    public const string MetaPaddingBottom = "sml_paddingBottom";
    public const string MetaPaddingLeft = "sml_paddingLeft";
    public const string MetaTreeRowHeight = "sml_treeRowHeight";
    public const string MetaTreeShowGuides = "sml_treeShowGuides";
    public const string MetaTreeHideRoot = "sml_treeHideRoot";
    public const string MetaTreeIndent = "sml_treeIndent";
    public const string MetaMenuPreferGlobal = "sml_menuPreferGlobal";
    public const string MetaMenuShortcut = "sml_menuShortcut";

    private static readonly IReadOnlyDictionary<string, Action<Control, SmlValue, string>> SimplePropertyHandlers
        = BuildSimplePropertyHandlers();

    public void Apply(Control control, string propertyName, SmlValue value, Func<string, string>? resolveAssetPath = null)
    {
        if (TryApplyGeneratedLayoutAlias(control, propertyName, value))
        {
            return;
        }

        if (TryApplySimpleProperty(control, propertyName, value))
        {
            return;
        }

        switch (propertyName.ToLowerInvariant())
        {
            case "text":
            case "label":
                ApplyTextLike(control, value.AsStringOrThrow(propertyName));
                return;

            case "title":
                var title = value.AsStringOrThrow(propertyName);
                if (IsWindowNode(control))
                {
                    control.SetMeta(MetaWindowTitle, Variant.From(title));
                }
                else
                {
                    ApplyTextLike(control, title);
                }
                return;

            case "wrap":
                ApplyWrap(control, ToBoolOrThrow(value, propertyName));
                return;

            case "role":
                var role = value.AsStringOrThrow(propertyName);
                control.SetMeta("sml_role", Variant.From(role));
                ApplyRoleStyle(control, role);
                return;

            case "align":
                ApplyAlign(control, value.AsStringOrThrow(propertyName));
                return;

            case "color":
                ApplyColor(control, value.AsStringOrThrow(propertyName), propertyName);
                return;

            case "spacing":
                ApplySpacing(control, value.AsIntOrThrow(propertyName));
                return;

            case "padding":
                var padding = value.AsPaddingOrThrow(propertyName);
                control.SetMeta(MetaPaddingTop, Variant.From(padding.Top));
                control.SetMeta(MetaPaddingRight, Variant.From(padding.Right));
                control.SetMeta(MetaPaddingBottom, Variant.From(padding.Bottom));
                control.SetMeta(MetaPaddingLeft, Variant.From(padding.Left));
                return;

            case "anchors":
                ApplyAnchors(control, value.AsStringOrThrow(propertyName));
                return;

            case "minsize":
                var minSize = value.AsVec2iOrThrow(propertyName);
                control.SetMeta(MetaWindowMinSizeX, Variant.From(minSize.X));
                control.SetMeta(MetaWindowMinSizeY, Variant.From(minSize.Y));
                return;

            case "fontsize":
            case "fontsizepx":
                ApplyFontSize(control, value.AsIntOrThrow(propertyName));
                return;

            case "font":
            case "fontsource":
                ApplyFont(control, value.AsStringOrThrow(propertyName), resolveAssetPath);
                return;

            case "halign":
            case "horizontalalignment":
                ApplyHorizontalAlignment(control, value.AsIntOrThrow(propertyName));
                return;

            case "sizeflagshorizontal":
                control.SizeFlagsHorizontal = (Control.SizeFlags)value.AsIntOrThrow(propertyName);
                return;

            case "sizeflagsvertical":
                control.SizeFlagsVertical = (Control.SizeFlags)value.AsIntOrThrow(propertyName);
                return;

            case "id":
                var idValue = value.AsStringOrThrow(propertyName);
                control.SetMeta(MetaId, Variant.From(idValue));
                var controlId = IdRuntimeScope.GetOrCreate(idValue);
                control.SetMeta(MetaIdValue, Variant.From(controlId.Value));

                if (control is Viewport3DControl viewport)
                {
                    viewport.SetSmlId(idValue);
                }
                return;

            case "clicked":
                var clickedValue = value.AsStringOrThrow(propertyName);
                control.SetMeta(MetaClicked, Variant.From(clickedValue));
                if (clickedValue.IndexOf(':') < 0)
                {
                    var clickedId = IdRuntimeScope.GetOrCreate(clickedValue);
                    control.SetMeta(MetaClickedIdValue, Variant.From(clickedId.Value));
                }
                return;

            case "autoplay":
                if (control is VideoStreamPlayer videoAutoplay)
                {
                    videoAutoplay.Autoplay = ToBoolOrThrow(value, propertyName);
                    return;
                }
                break;

            case "source":
            case "url":
                if (control is VideoStreamPlayer videoPlayer)
                {
                    var rawSource = value.AsStringOrThrow(propertyName);
                    var resolvedSource = ResolveAssetPath(rawSource, resolveAssetPath);
                    ApplyVideoSource(videoPlayer, resolvedSource, rawSource);
                    return;
                }
                break;

            case "src":
                if (control is TextureRect textureRect)
                {
                    var rawSource = value.AsStringOrThrow(propertyName);
                    var resolvedSource = ResolveAssetPath(rawSource, resolveAssetPath);
                    ApplyImageSource(textureRect, resolvedSource, rawSource);
                    return;
                }
                break;

            case "alt":
                control.SetMeta("sml_alt", Variant.From(value.AsStringOrThrow(propertyName)));
                return;

            case "multiline":
                if (control is TextEdit textEdit)
                {
                    textEdit.WrapMode = ToBoolOrThrow(value, propertyName)
                        ? TextEdit.LineWrappingMode.Boundary
                        : TextEdit.LineWrappingMode.None;
                    return;
                }
                break;

            case "editable":
                if (control is TextEdit editableTextEdit)
                {
                    editableTextEdit.Editable = ToBoolOrThrow(value, propertyName);
                    return;
                }
                break;

            case "readonly":
                if (control is TextEdit readOnlyTextEdit)
                {
                    readOnlyTextEdit.Editable = !ToBoolOrThrow(value, propertyName);
                    return;
                }
                break;

            case "syntax":
                if (control is CodeEdit codeEditor)
                {
                    CodeEditSyntaxRuntime.SetSyntax(codeEditor, value.AsStringOrThrow(propertyName));
                    return;
                }
                break;

            case "min":
            case "minvalue":
                if (control is Godot.Range minRange)
                {
                    minRange.MinValue = value.AsIntOrThrow(propertyName);
                    return;
                }
                break;

            case "max":
            case "maxvalue":
                if (control is Godot.Range maxRange)
                {
                    maxRange.MaxValue = value.AsIntOrThrow(propertyName);
                    return;
                }
                break;

            case "step":
                if (control is Godot.Range stepRange)
                {
                    stepRange.Step = value.AsIntOrThrow(propertyName);
                    return;
                }
                break;

            case "value":
                if (control is Godot.Range valueRange)
                {
                    valueRange.Value = value.AsIntOrThrow(propertyName);
                    return;
                }
                break;

            case "model":
            case "modelsource":
                if (control is Viewport3DControl viewportModel)
                {
                    var rawModelSource = value.AsStringOrThrow(propertyName);
                    var resolvedModelSource = ResolveAssetPath(rawModelSource, resolveAssetPath);
                    viewportModel.SetModelSource(resolvedModelSource);
                    return;
                }
                break;

            case "animation":
            case "animationsource":
                if (control is Viewport3DControl viewportAnimationSource)
                {
                    var rawAnimationSource = value.AsStringOrThrow(propertyName);
                    var resolvedAnimationSource = ResolveAssetPath(rawAnimationSource, resolveAssetPath);
                    viewportAnimationSource.SetAnimationSource(resolvedAnimationSource);
                    return;
                }
                break;

            case "playanimation":
                if (control is Viewport3DControl viewportPlayAnimation)
                {
                    viewportPlayAnimation.SetPlayAnimation(value.AsIntOrThrow(propertyName));
                    return;
                }
                break;

            case "playfirstanimation":
            case "autoplayanimation":
                if (control is Viewport3DControl viewportAnim)
                {
                    viewportAnim.SetPlayFirstAnimationOnLoad(ToBoolOrThrow(value, propertyName));
                    return;
                }
                break;

            case "defaultanimation":
                if (control is Viewport3DControl viewportDefaultAnim)
                {
                    viewportDefaultAnim.SetDefaultAnimation(value.AsStringOrThrow(propertyName));
                    return;
                }
                break;

            case "playloop":
                if (control is Viewport3DControl viewportPlayLoop)
                {
                    viewportPlayLoop.SetPlayLoop(ToBoolOrThrow(value, propertyName));
                    return;
                }
                break;

            case "cameradistance":
                if (control is Viewport3DControl viewportCamera)
                {
                    viewportCamera.SetCameraDistance(value.AsIntOrThrow(propertyName));
                    return;
                }
                break;

            case "lightenergy":
                if (control is Viewport3DControl viewportLight)
                {
                    viewportLight.SetLightEnergy(value.AsIntOrThrow(propertyName));
                    return;
                }
                break;

            default:
                RunnerLogger.Warn("UI", $"Unsupported property '{propertyName}' on node '{control.GetType().Name}'.");
                return;
        }

        RunnerLogger.Warn("UI", $"Property '{propertyName}' ignored for node type '{control.GetType().Name}'.");
    }

    private static void ApplyAnchors(Control control, string anchorsRaw)
    {
        var normalized = anchorsRaw
            .Replace("|", ",", StringComparison.Ordinal)
            .Replace(";", ",", StringComparison.Ordinal)
            .ToLowerInvariant();

        var left = false;
        var right = false;
        var top = false;
        var bottom = false;

        foreach (var token in normalized.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (token)
            {
                case "left":
                    left = true;
                    break;
                case "right":
                    right = true;
                    break;
                case "top":
                    top = true;
                    break;
                case "bottom":
                    bottom = true;
                    break;
            }
        }

        control.SetMeta(MetaAnchorLeft, Variant.From(left));
        control.SetMeta(MetaAnchorRight, Variant.From(right));
        control.SetMeta(MetaAnchorTop, Variant.From(top));
        control.SetMeta(MetaAnchorBottom, Variant.From(bottom));
    }

    private static IReadOnlyDictionary<string, Action<Control, SmlValue, string>> BuildSimplePropertyHandlers()
    {
        return new Dictionary<string, Action<Control, SmlValue, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["preferglobalmenu"] = (control, value, propertyName) => SetMetaBool(control, MetaMenuPreferGlobal, value, propertyName),
            ["shortcut"] = (control, value, propertyName) => SetMetaString(control, MetaMenuShortcut, value, propertyName),
            ["anchorleft"] = (control, value, propertyName) => SetMetaBool(control, MetaAnchorLeft, value, propertyName),
            ["anchorright"] = (control, value, propertyName) => SetMetaBool(control, MetaAnchorRight, value, propertyName),
            ["anchortop"] = (control, value, propertyName) => SetMetaBool(control, MetaAnchorTop, value, propertyName),
            ["anchorbottom"] = (control, value, propertyName) => SetMetaBool(control, MetaAnchorBottom, value, propertyName),
            ["centerx"] = (control, value, propertyName) => SetMetaBool(control, MetaCenterX, value, propertyName),
            ["centery"] = (control, value, propertyName) => SetMetaBool(control, MetaCenterY, value, propertyName),
            ["scrollable"] = (control, value, propertyName) => SetMetaBool(control, MetaScrollable, value, propertyName),
            ["scrollbarwidth"] = (control, value, propertyName) => SetMetaInt(control, MetaScrollbarWidth, value, propertyName),
            ["scrollbarheight"] = (control, value, propertyName) => SetMetaInt(control, MetaScrollbarHeight, value, propertyName),
            ["scrollbarposition"] = (control, value, propertyName) => SetMetaString(control, MetaScrollbarPosition, value, propertyName),
            ["scrollbarvisible"] = (control, value, propertyName) => SetMetaBool(control, MetaScrollbarVisible, value, propertyName),
            ["scrollbarvisibleonscroll"] = (control, value, propertyName) => SetMetaBool(control, MetaScrollbarVisibleOnScroll, value, propertyName),
            ["scrollbarfadeouttime"] = (control, value, propertyName) => SetMetaInt(control, MetaScrollbarFadeOutTime, value, propertyName),
            ["rowheight"] = (control, value, propertyName) => SetMetaInt(control, MetaTreeRowHeight, value, propertyName),
            ["showguides"] = (control, value, propertyName) => SetMetaBool(control, MetaTreeShowGuides, value, propertyName),
            ["hideroot"] = (control, value, propertyName) => SetMetaBool(control, MetaTreeHideRoot, value, propertyName),
            ["indent"] = (control, value, propertyName) => SetMetaInt(control, MetaTreeIndent, value, propertyName),
            ["action"] = (control, value, propertyName) => SetMetaString(control, MetaAction, value, propertyName)
        };
    }

    private static bool TryApplySimpleProperty(Control control, string propertyName, SmlValue value)
    {
        if (!SimplePropertyHandlers.TryGetValue(propertyName, out var handler))
        {
            return false;
        }

        handler(control, value, propertyName);
        return true;
    }

    private static void SetMetaBool(Control control, string metaName, SmlValue value, string propertyName)
        => control.SetMeta(metaName, Variant.From(ToBoolOrThrow(value, propertyName)));

    private static void SetMetaInt(Control control, string metaName, SmlValue value, string propertyName)
        => control.SetMeta(metaName, Variant.From(value.AsIntOrThrow(propertyName)));

    private static void SetMetaString(Control control, string metaName, SmlValue value, string propertyName)
        => control.SetMeta(metaName, Variant.From(value.AsStringOrThrow(propertyName)));

    private static bool TryApplyGeneratedLayoutAlias(Control control, string propertyName, SmlValue value)
    {
        if (!SchemaLayoutAliases.TryGet(propertyName, out var aliasDef))
        {
            return false;
        }

        if (!AppliesTo(control, aliasDef.AppliesTo))
        {
            return false;
        }

        switch (aliasDef.Canonical.ToLowerInvariant())
        {
            case "size":
                ApplySizeAlias(control, value, propertyName, aliasDef.Mode);
                return true;

            case "position":
                ApplyPositionAlias(control, value, propertyName, aliasDef.Mode);
                return true;

            default:
                return false;
        }
    }

    private static bool AppliesTo(Control control, string[] appliesTo)
    {
        if (appliesTo.Length == 0)
        {
            return true;
        }

        foreach (var targetType in appliesTo)
        {
            if (string.IsNullOrWhiteSpace(targetType))
            {
                continue;
            }

            if (string.Equals(targetType, "Control", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(targetType, "Window", StringComparison.OrdinalIgnoreCase) && IsWindowNode(control))
            {
                return true;
            }

            if (control.HasMeta(MetaNodeName)
                && string.Equals(control.GetMeta(MetaNodeName).AsString(), targetType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplySizeAlias(Control control, SmlValue value, string propertyName, string mode)
    {
        var normalizedMode = mode.ToLowerInvariant();
        if (normalizedMode == "whole")
        {
            var size = value.AsVec2iOrThrow(propertyName);
            SetWidth(control, size.X);
            SetHeight(control, size.Y);
            return;
        }

        if (normalizedMode == "x")
        {
            SetWidth(control, value.AsIntOrThrow(propertyName));
            return;
        }

        if (normalizedMode == "y")
        {
            SetHeight(control, value.AsIntOrThrow(propertyName));
            return;
        }

        RunnerLogger.Warn("UI", $"Unknown layout alias mode '{mode}' for canonical 'size'.");
    }

    private static void ApplyPositionAlias(Control control, SmlValue value, string propertyName, string mode)
    {
        var normalizedMode = mode.ToLowerInvariant();
        if (normalizedMode == "whole")
        {
            var pos = value.AsVec2iOrThrow(propertyName);
            SetPosX(control, pos.X);
            SetPosY(control, pos.Y);
            return;
        }

        if (normalizedMode == "x")
        {
            SetPosX(control, value.AsIntOrThrow(propertyName));
            return;
        }

        if (normalizedMode == "y")
        {
            SetPosY(control, value.AsIntOrThrow(propertyName));
            return;
        }

        RunnerLogger.Warn("UI", $"Unknown layout alias mode '{mode}' for canonical 'position'.");
    }

    private static void SetWidth(Control control, int width)
    {
        control.SetMeta(MetaWidth, Variant.From(width));
        control.CustomMinimumSize = new Vector2(width, control.CustomMinimumSize.Y);
        if (IsWindowNode(control))
        {
            control.SetMeta(MetaWindowSizeX, Variant.From(width));
        }
    }

    private static void SetHeight(Control control, int height)
    {
        control.SetMeta(MetaHeight, Variant.From(height));
        control.CustomMinimumSize = new Vector2(control.CustomMinimumSize.X, height);
        if (IsWindowNode(control))
        {
            control.SetMeta(MetaWindowSizeY, Variant.From(height));
        }
    }

    private static void SetPosX(Control control, int x)
    {
        control.SetMeta(MetaX, Variant.From(x));
        control.Position = new Vector2(x, control.Position.Y);
        if (IsWindowNode(control))
        {
            control.SetMeta(MetaWindowPosX, Variant.From(x));
        }
    }

    private static void SetPosY(Control control, int y)
    {
        control.SetMeta(MetaY, Variant.From(y));
        control.Position = new Vector2(control.Position.X, y);
        if (IsWindowNode(control))
        {
            control.SetMeta(MetaWindowPosY, Variant.From(y));
        }
    }

    private static bool IsWindowNode(Control control)
    {
        return control.HasMeta(MetaNodeName)
            && string.Equals(control.GetMeta(MetaNodeName).AsString(), "Window", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyTextLike(Control control, string text)
    {
        switch (control)
        {
            case RichTextLabel richText:
                richText.BbcodeEnabled = true;
                richText.Text = text;
                break;
            case Label label:
                label.Text = text;
                break;
            case Button button:
                button.Text = text;
                break;
            case TextEdit textEdit:
                textEdit.Text = text;
                break;
            default:
                if (control is PanelContainer or Panel)
                {
                    // no-op for now
                    return;
                }

                RunnerLogger.Warn("UI", $"Text-like property ignored for node type '{control.GetType().Name}'.");
                break;
        }
    }

    private static void ApplySpacing(Control control, int spacing)
    {
        switch (control)
        {
            case BoxContainer box:
                box.AddThemeConstantOverride("separation", spacing);
                break;
            default:
                RunnerLogger.Warn("UI", $"Property 'spacing' ignored for node type '{control.GetType().Name}'.");
                break;
        }
    }

    private static void ApplyWrap(Control control, bool wrap)
    {
        switch (control)
        {
            case RichTextLabel richText:
                richText.AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
                break;

            case Label label:
                label.AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
                break;

            case TextEdit textEdit:
                textEdit.WrapMode = wrap ? TextEdit.LineWrappingMode.Boundary : TextEdit.LineWrappingMode.None;
                break;

            default:
                RunnerLogger.Warn("UI", $"Property 'wrap' ignored for node type '{control.GetType().Name}'.");
                break;
        }
    }

    private static void ApplyAlign(Control control, string align)
    {
        var normalized = align.Trim().ToLowerInvariant();
        var value = normalized switch
        {
            "center" => 1,
            "right" => 2,
            _ => 0
        };

        ApplyHorizontalAlignment(control, value);
    }

    private static void ApplyRoleStyle(Control control, string role)
    {
        if (!string.Equals(role, "code", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (control is not TextEdit textEdit)
        {
            return;
        }

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.11f, 0.11f, 0.13f, 1f),
            BorderColor = new Color(0f, 0f, 0f, 0f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0
        };
        style.SetBorderWidthAll(0);

        textEdit.AddThemeStyleboxOverride("normal", style);
        textEdit.AddThemeStyleboxOverride("read_only", style);
        textEdit.AddThemeStyleboxOverride("focus", style);
    }

    private static void ApplyColor(Control control, string rawColor, string propertyName)
    {
        if (!TryParseColor(rawColor, out var color))
        {
            throw new SmlParseException($"Property '{propertyName}' must be a color in #RRGGBB or #AARRGGBB format.");
        }

        switch (control)
        {
            case RichTextLabel richText:
                richText.AddThemeColorOverride("default_color", color);
                break;

            case Label label:
                label.AddThemeColorOverride("font_color", color);
                break;

            case Button button:
                button.AddThemeColorOverride("font_color", color);
                break;

            default:
                RunnerLogger.Warn("UI", $"Property 'color' ignored for node type '{control.GetType().Name}'.");
                break;
        }
    }

    private static bool TryParseColor(string raw, out Color color)
    {
        color = Colors.White;
        if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith('#'))
        {
            return false;
        }

        var hex = raw[1..];
        if (hex.Length == 6)
        {
            if (!byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
                || !byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
                || !byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                return false;
            }

            color = Color.Color8(r, g, b, 255);
            return true;
        }

        if (hex.Length == 8)
        {
            if (!byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var a)
                || !byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
                || !byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
                || !byte.TryParse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                return false;
            }

            color = Color.Color8(r, g, b, a);
            return true;
        }

        return false;
    }

    private static string ResolveAssetPath(string source, Func<string, string>? resolveAssetPath)
    {
        if (resolveAssetPath is null)
        {
            return source;
        }

        try
        {
            var resolved = resolveAssetPath(source);
            if (!string.Equals(resolved, source, StringComparison.Ordinal))
            {
                RunnerLogger.Info("UI", $"Resolved asset path '{source}' -> '{resolved}'.");
            }

            return resolved;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("UI", $"Asset path resolver failed for '{source}'", ex);
            return source;
        }
    }

    private static void ApplyVideoSource(VideoStreamPlayer player, string source, string originalSource)
    {
        if (!source.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            && !source.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            RunnerLogger.Warn("UI", $"Video source '{originalSource}' resolved to '{source}', but is not loadable by Godot ResourceLoader. Use res:// or user:// path.");
            return;
        }

        var stream = GD.Load<VideoStream>(source);
        if (stream is null)
        {
            RunnerLogger.Warn("UI", $"Could not load video stream '{source}'.");
            return;
        }

        player.Stream = stream;
    }

    private static void ApplyImageSource(TextureRect image, string source, string originalSource)
    {
        if (!source.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            && !source.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            RunnerLogger.Warn("UI", $"Image source '{originalSource}' resolved to '{source}', but is not loadable by Godot ResourceLoader. Use res:// or user:// path.");
            return;
        }

        var texture = GD.Load<Texture2D>(source);
        if (texture is null)
        {
            RunnerLogger.Warn("UI", $"Could not load image '{source}'.");
            return;
        }

        image.Texture = texture;
        image.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
        image.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
    }

    private static void ApplyFontSize(Control control, int fontSize)
    {
        switch (control)
        {
            case RichTextLabel richText:
                richText.AddThemeFontSizeOverride("normal_font_size", fontSize);
                break;

            case Label label:
                label.AddThemeFontSizeOverride("font_size", fontSize);
                break;
            case Button button:
                button.AddThemeFontSizeOverride("font_size", fontSize);
                break;
            case TextEdit textEdit:
                textEdit.AddThemeFontSizeOverride("font_size", fontSize);
                break;
            default:
                RunnerLogger.Warn("UI", $"Property 'fontSize' ignored for node type '{control.GetType().Name}'.");
                break;
        }
    }

    private static void ApplyFont(Control control, string rawFontPath, Func<string, string>? resolveAssetPath)
    {
        var fontPath = ResolveAssetPath(rawFontPath, resolveAssetPath);
        Font font;
        var loadedFont = GD.Load<FontFile>(fontPath);
        if (loadedFont is not null)
        {
            font = loadedFont;
        }
        else if (IsLikelyAppMonospaceFont(rawFontPath, fontPath))
        {
            font = CreateMonospaceSystemFont();
            RunnerLogger.Info("UI", $"Font '{fontPath}' not found. Falling back to system monospace font.");
        }
        else
        {
            RunnerLogger.Warn("UI", $"Could not load font '{fontPath}'.");
            return;
        }

        switch (control)
        {
            case RichTextLabel richText:
                richText.AddThemeFontOverride("normal_font", font);
                break;
            case Label label:
                label.AddThemeFontOverride("font", font);
                break;
            case Button button:
                button.AddThemeFontOverride("font", font);
                break;
            case TextEdit textEdit:
                textEdit.AddThemeFontOverride("font", font);
                break;
            default:
                RunnerLogger.Warn("UI", $"Property 'font' ignored for node type '{control.GetType().Name}'.");
                break;
        }
    }

    private static bool IsLikelyAppMonospaceFont(string rawFontPath, string resolvedFontPath)
    {
        static string Normalize(string path) => path.Replace('\\', '/').ToLowerInvariant();
        var raw = Normalize(rawFontPath);
        var resolved = Normalize(resolvedFontPath);
        return raw.EndsWith("anonymous.ttf", StringComparison.Ordinal)
            || resolved.EndsWith("anonymous.ttf", StringComparison.Ordinal);
    }

    private static Font CreateMonospaceSystemFont()
    {
        var font = new SystemFont
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
        return font;
    }

    private static void ApplyHorizontalAlignment(Control control, int alignment)
    {
        var horizontalAlignment = alignment switch
        {
            1 => HorizontalAlignment.Center,
            2 => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };

        switch (control)
        {
            case RichTextLabel richText:
                richText.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                richText.HorizontalAlignment = horizontalAlignment;
                break;

            case Label label:
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                label.HorizontalAlignment = horizontalAlignment;
                break;
            case Button button:
                button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                button.Alignment = horizontalAlignment;
                break;
            default:
                RunnerLogger.Warn("UI", $"Property 'halign' ignored for node type '{control.GetType().Name}'.");
                break;
        }
    }

    private static bool ToBoolOrThrow(SmlValue value, string propertyName)
    {
        return value.Kind == SmlValueKind.Bool
            ? (bool)value.Value
            : throw new SmlParseException($"Property '{propertyName}' must be a boolean.");
    }

}
