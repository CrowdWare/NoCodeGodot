using Godot;
using Runtime.Logging;
using System;

namespace Runtime.UI;

public static class LayoutRuntime
{
    private const string MetaBaseLeft = "sml_appBaseLeft";
    private const string MetaBaseRight = "sml_appBaseRight";
    private const string MetaBaseTop = "sml_appBaseTop";
    private const string MetaBaseBottom = "sml_appBaseBottom";
    private const string MetaBaseWidth = "sml_appBaseWidth";
    private const string MetaBaseHeight = "sml_appBaseHeight";

    public static void Apply(Control root)
    {
        if (root is null)
        {
            return;
        }

        ApplyRecursive(root);
    }

    private static void ApplyRecursive(Control control)
    {
        var layoutMode = GetLayoutMode(control);
        if (layoutMode == "app")
        {
            ResolveAppChildren(control);
        }
        else if (layoutMode == "document")
        {
            ResolveDocumentDefaults(control);
        }

        foreach (Node child in control.GetChildren())
        {
            if (child is Control childControl)
            {
                ApplyRecursive(childControl);
            }
        }
    }

    private static void ResolveDocumentDefaults(Control control)
    {
        var parent = control.GetParent() as Control;
        if (parent is null)
        {
            return;
        }

        var nodeName = GetMetaString(control, NodePropertyMapper.MetaNodeName);
        if (!IsDocumentNode(nodeName))
        {
            return;
        }

        var parentMode = GetLayoutMode(parent);
        if (parentMode == "app")
        {
            control.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            control.SetOffsetsPreset(Control.LayoutPreset.FullRect);
            control.Position = Vector2.Zero;
            control.Size = parent.Size;
            NodePropertyMapper.ApplyFillMaxSize(control);

            RunnerLogger.Info("UI", $"[layout/document] node={nodeName}, parentMode=app, size={control.Size.X:0}x{control.Size.Y:0}");
        }
    }

    private static void ResolveAppChildren(Control parent)
    {
        foreach (Node childNode in parent.GetChildren())
        {
            if (childNode is not Control child)
            {
                continue;
            }

            if (!HasAnyAppGeometryMetadata(child))
            {
                continue;
            }

            EnsureBaseMetrics(child, parent.Size);

            var width = Math.Max(0f, child.GetMeta(MetaBaseWidth).AsSingle());
            var height = Math.Max(0f, child.GetMeta(MetaBaseHeight).AsSingle());
            var left = child.GetMeta(MetaBaseLeft).AsSingle();
            var right = child.GetMeta(MetaBaseRight).AsSingle();
            var top = child.GetMeta(MetaBaseTop).AsSingle();
            var bottom = child.GetMeta(MetaBaseBottom).AsSingle();

            var anchorLeft = GetMetaBool(child, NodePropertyMapper.MetaAnchorLeft);
            var anchorRight = GetMetaBool(child, NodePropertyMapper.MetaAnchorRight);
            var anchorTop = GetMetaBool(child, NodePropertyMapper.MetaAnchorTop);
            var anchorBottom = GetMetaBool(child, NodePropertyMapper.MetaAnchorBottom);
            var centerX = GetMetaBool(child, NodePropertyMapper.MetaCenterX);
            var centerY = GetMetaBool(child, NodePropertyMapper.MetaCenterY);

            float x;
            if (centerX)
            {
                x = (parent.Size.X - width) * 0.5f;
            }
            else if (anchorLeft && anchorRight)
            {
                x = left;
                width = Math.Max(0f, parent.Size.X - left - right);
            }
            else if (!anchorLeft && anchorRight)
            {
                x = parent.Size.X - right - width;
            }
            else
            {
                x = left;
            }

            float y;
            if (centerY)
            {
                y = (parent.Size.Y - height) * 0.5f;
            }
            else if (anchorTop && anchorBottom)
            {
                y = top;
                height = Math.Max(0f, parent.Size.Y - top - bottom);
            }
            else if (!anchorTop && anchorBottom)
            {
                y = parent.Size.Y - bottom - height;
            }
            else
            {
                y = top;
            }

            child.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            child.SetOffsetsPreset(Control.LayoutPreset.TopLeft);
            child.Position = new Vector2(x, y);
            child.Size = new Vector2(width, height);

            RunnerLogger.Info(
                "UI",
                $"[layout/app] child={GetMetaString(child, NodePropertyMapper.MetaNodeName)}, parent={GetMetaString(parent, NodePropertyMapper.MetaNodeName)}, rect={x:0},{y:0},{width:0},{height:0}, anchors(LRTB)={anchorLeft}/{anchorRight}/{anchorTop}/{anchorBottom}, centerXY={centerX}/{centerY}"
            );
        }
    }

    private static bool HasAnyAppGeometryMetadata(Control control)
    {
        return control.HasMeta(NodePropertyMapper.MetaX)
               || control.HasMeta(NodePropertyMapper.MetaY)
               || control.HasMeta(NodePropertyMapper.MetaWidth)
               || control.HasMeta(NodePropertyMapper.MetaHeight)
               || control.HasMeta(NodePropertyMapper.MetaAnchorLeft)
               || control.HasMeta(NodePropertyMapper.MetaAnchorRight)
               || control.HasMeta(NodePropertyMapper.MetaAnchorTop)
               || control.HasMeta(NodePropertyMapper.MetaAnchorBottom)
               || control.HasMeta(NodePropertyMapper.MetaCenterX)
               || control.HasMeta(NodePropertyMapper.MetaCenterY);
    }

    private static void EnsureBaseMetrics(Control child, Vector2 parentSize)
    {
        if (child.HasMeta(MetaBaseLeft)
            && child.HasMeta(MetaBaseRight)
            && child.HasMeta(MetaBaseTop)
            && child.HasMeta(MetaBaseBottom)
            && child.HasMeta(MetaBaseWidth)
            && child.HasMeta(MetaBaseHeight))
        {
            return;
        }

        var x = child.HasMeta(NodePropertyMapper.MetaX) ? child.GetMeta(NodePropertyMapper.MetaX).AsSingle() : child.Position.X;
        var y = child.HasMeta(NodePropertyMapper.MetaY) ? child.GetMeta(NodePropertyMapper.MetaY).AsSingle() : child.Position.Y;
        var width = child.HasMeta(NodePropertyMapper.MetaWidth)
            ? child.GetMeta(NodePropertyMapper.MetaWidth).AsSingle()
            : Math.Max(child.Size.X, child.CustomMinimumSize.X);
        var height = child.HasMeta(NodePropertyMapper.MetaHeight)
            ? child.GetMeta(NodePropertyMapper.MetaHeight).AsSingle()
            : Math.Max(child.Size.Y, child.CustomMinimumSize.Y);

        child.SetMeta(MetaBaseLeft, Variant.From(x));
        child.SetMeta(MetaBaseTop, Variant.From(y));
        child.SetMeta(MetaBaseWidth, Variant.From(width));
        child.SetMeta(MetaBaseHeight, Variant.From(height));
        child.SetMeta(MetaBaseRight, Variant.From(parentSize.X - x - width));
        child.SetMeta(MetaBaseBottom, Variant.From(parentSize.Y - y - height));
    }

    private static bool IsDocumentNode(string nodeName)
    {
        return nodeName.Equals("Page", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Column", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Row", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Markdown", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Box", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLayoutMode(Control control)
    {
        return control.HasMeta(NodePropertyMapper.MetaLayoutMode)
            ? control.GetMeta(NodePropertyMapper.MetaLayoutMode).AsString().ToLowerInvariant()
            : "app";
    }

    private static string GetMetaString(Control control, string key)
    {
        return control.HasMeta(key) ? control.GetMeta(key).AsString() : control.GetType().Name;
    }

    private static bool GetMetaBool(Control control, string key)
    {
        return control.HasMeta(key) && control.GetMeta(key).AsBool();
    }
}
