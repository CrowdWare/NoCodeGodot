using Godot;
using Runtime.Logging;
using Runtime.Sml;
using Runtime.ThreeD;
using System;

namespace Runtime.UI;

public sealed class NodePropertyMapper
{
    public const string MetaId = "sml:id";
    public const string MetaAction = "sml:action";
    public const string MetaClicked = "sml:clicked";

    public void Apply(Control control, string propertyName, SmlValue value)
    {
        switch (propertyName.ToLowerInvariant())
        {
            case "text":
            case "label":
            case "title":
                ApplyTextLike(control, value.AsStringOrThrow(propertyName));
                return;

            case "spacing":
                ApplySpacing(control, value.AsIntOrThrow(propertyName));
                return;

            case "width":
                control.CustomMinimumSize = new Vector2(value.AsIntOrThrow(propertyName), control.CustomMinimumSize.Y);
                return;

            case "height":
                control.CustomMinimumSize = new Vector2(control.CustomMinimumSize.X, value.AsIntOrThrow(propertyName));
                return;

            case "left":
                control.Position = new Vector2(value.AsIntOrThrow(propertyName), control.Position.Y);
                return;

            case "top":
                control.Position = new Vector2(control.Position.X, value.AsIntOrThrow(propertyName));
                return;

            case "fontsize":
            case "fontsizepx":
                ApplyFontSize(control, value.AsIntOrThrow(propertyName));
                return;

            case "halign":
            case "horizontalalignment":
                ApplyHorizontalAlignment(control, value.AsIntOrThrow(propertyName));
                return;

            case "fillmaxsize":
                ApplyFillMaxSize(control, ToBoolOrThrow(value, propertyName));
                return;

            case "id":
                var idValue = value.AsStringOrThrow(propertyName);
                control.SetMeta(MetaId, Variant.From(idValue));

                if (control is Viewport3DControl viewport)
                {
                    viewport.SmlId = idValue;
                }
                return;

            case "action":
                control.SetMeta(MetaAction, Variant.From(value.AsStringOrThrow(propertyName)));
                return;

            case "clicked":
                control.SetMeta(MetaClicked, Variant.From(value.AsStringOrThrow(propertyName)));
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
                    ApplyVideoSource(videoPlayer, value.AsStringOrThrow(propertyName));
                    return;
                }
                break;

            case "multiline":
                if (control is TextEdit textEdit)
                {
                    textEdit.WrapMode = ToBoolOrThrow(value, propertyName)
                        ? TextEdit.LineWrappingMode.Boundary
                        : TextEdit.LineWrappingMode.None;
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
                    viewportModel.SetModelSource(value.AsStringOrThrow(propertyName));
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

    private static void ApplyTextLike(Control control, string text)
    {
        switch (control)
        {
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
                if (control is PanelContainer)
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

    private static void ApplyVideoSource(VideoStreamPlayer player, string source)
    {
        if (!source.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            && !source.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            RunnerLogger.Warn("UI", $"Video source '{source}' is not loadable by Godot ResourceLoader. Use res:// or user:// path.");
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

    private static void ApplyFontSize(Control control, int fontSize)
    {
        switch (control)
        {
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

    public static void ApplyFillMaxSize(Control control, bool enabled = true)
    {
        if (!enabled)
        {
            return;
        }

        control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        control.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        if (control.GetParent() is not Container)
        {
            control.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            control.SetOffsetsPreset(Control.LayoutPreset.FullRect);
        }
    }
}
