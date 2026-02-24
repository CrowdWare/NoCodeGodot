/*
 * Copyright (C) 2026 CrowdWare
 */

using Godot;

namespace Runtime.UI;

/// <summary>
/// Custom dock tab bar renderer that bypasses StyleBox theme rendering
/// to avoid gamma/sRGB color shifts.
/// </summary>
public sealed partial class DockTabBarControl : TabBar
{
    private const int TabCornerRadius = 6;

    public override void _Ready()
    {
        ApplyFlatOverrides();
    }

    public override void _Draw()
    {
        var fallback = GetThemeColor("dock_background", "DockingContainerControl");
        var dock = GetParent()?.GetParent()?.GetParent() as DockingContainerControl;
        var metaBgColor = dock is not null && dock.HasMeta("bgColor")
            ? dock.GetMeta("bgColor").AsString()
            : null;

        var baseColor = DockingContainerControl.ResolveBackgroundColor(metaBgColor, fallback);
        DrawRect(new Rect2(Vector2.Zero, Size), baseColor);

        var font = GetThemeFont("font");
        if (font is null)
        {
            return;
        }

        var fontSize = GetThemeFontSize("font_size");
        if (fontSize <= 0)
        {
            fontSize = 13;
        }

        var accentColor = ResolveAccentColor(baseColor);
        var selectedTextColor = new Color(0.9882353f, 1f, 1f, 1f);
        var unselectedTextColor = new Color(0.69803923f, 0.7411765f, 0.81960785f, 1f);
        var fontAscent = font.GetAscent(fontSize);
        var fontDescent = font.GetDescent(fontSize);

        for (var i = 0; i < GetTabCount(); i++)
        {
            var rect = GetTabRect(i);
            var isSelected = i == CurrentTab;
            var tabFillColor = isSelected ? accentColor : baseColor;

            DrawRoundedTopTab(rect, tabFillColor, TabCornerRadius);

            var text = GetTabTitle(i);
            var textColor = isSelected ? selectedTextColor : unselectedTextColor;
            var baselineY = rect.Position.Y + ((rect.Size.Y + fontAscent - fontDescent) * 0.5f);
            var textPos = new Vector2(rect.Position.X + 8f, baselineY);
            var maxTextWidth = rect.Size.X - 16f;
            var drawText = TrimToFit(font, text, fontSize, maxTextWidth);
            DrawString(font, textPos, drawText, HorizontalAlignment.Left, maxTextWidth, fontSize, textColor);
        }
    }

    private void DrawRoundedTopTab(Rect2 rect, Color fillColor, int radius)
    {
        var r = Mathf.Min(radius, Mathf.Min((int)(rect.Size.X * 0.5f), (int)(rect.Size.Y * 0.5f)));
        if (r <= 0)
        {
            DrawRect(rect, fillColor);
            return;
        }

        // Lower body (full width, square bottom)
        DrawRect(new Rect2(rect.Position.X, rect.Position.Y + r, rect.Size.X, rect.Size.Y - r), fillColor);
        // Top middle strip between rounded corners
        DrawRect(new Rect2(rect.Position.X + r, rect.Position.Y, rect.Size.X - (r * 2), r), fillColor);
        // Rounded top corners
        DrawCircle(new Vector2(rect.Position.X + r, rect.Position.Y + r), r, fillColor);
        DrawCircle(new Vector2(rect.End.X - r, rect.Position.Y + r), r, fillColor);
    }

    private Color ResolveAccentColor(Color fallback)
    {
        var accent = GetThemeColor("accent_color", "VScrollBar");
        return accent.A <= 0.001f ? fallback : accent;
    }

    private static string TrimToFit(Font font, string text, int fontSize, float maxWidth)
    {
        if (maxWidth <= 8f)
        {
            return string.Empty;
        }

        if (font.GetStringSize(text, HorizontalAlignment.Left, -1f, fontSize).X <= maxWidth)
        {
            return text;
        }

        const string ellipsis = "...";
        var low = 0;
        var high = text.Length;
        var best = ellipsis;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            var candidate = text[..mid] + ellipsis;
            var width = font.GetStringSize(candidate, HorizontalAlignment.Left, -1f, fontSize).X;
            if (width <= maxWidth)
            {
                best = candidate;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return best;
    }

    private void ApplyFlatOverrides()
    {
        var empty = new StyleBoxEmpty();
        AddThemeStyleboxOverride("tabbar_background", empty);
        AddThemeStyleboxOverride("tab_selected", empty);
        AddThemeStyleboxOverride("tab_unselected", empty);
        AddThemeStyleboxOverride("tab_hovered", empty);

        var transparent = new Color(1f, 1f, 1f, 0f);
        AddThemeColorOverride("font_selected_color", transparent);
        AddThemeColorOverride("font_unselected_color", transparent);
        AddThemeColorOverride("font_hovered_color", transparent);
        AddThemeColorOverride("font_disabled_color", transparent);
    }
}
