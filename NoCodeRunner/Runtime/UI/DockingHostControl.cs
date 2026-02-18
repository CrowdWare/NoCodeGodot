using Godot;
using System;
using System.Collections.Generic;

namespace Runtime.UI;

public sealed partial class DockingHostControl : Container
{
    private sealed class LayoutItem
    {
        public required DockingContainerControl Container;
        public required string Side;
        public required bool Flex;
        public required float Width;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren)
        {
            ArrangeChildren();
        }
    }

    private void ArrangeChildren()
    {
        var all = new List<LayoutItem>();
        for (var i = 0; i < GetChildCount(); i++)
        {
            if (GetChild(i) is not DockingContainerControl child || !child.Visible)
            {
                continue;
            }

            var side = child.GetDockSide();
            var flex = child.IsFlex();
            var width = child.GetFixedWidth();
            all.Add(new LayoutItem
            {
                Container = child,
                Side = side,
                Flex = flex,
                Width = width
            });
        }

        if (all.Count == 0)
        {
            return;
        }

        var left = new List<LayoutItem>();
        var center = new List<LayoutItem>();
        var right = new List<LayoutItem>();

        foreach (var item in all)
        {
            switch (item.Side)
            {
                case "left":
                    left.Add(item);
                    break;
                case "right":
                    right.Add(item);
                    break;
                default:
                    center.Add(item);
                    break;
            }
        }

        var gap = 0f;
        if (HasMeta(NodePropertyMapper.MetaDockGap))
        {
            gap = Math.Max(0, GetMeta(NodePropertyMapper.MetaDockGap).AsInt32());
        }

        var widthTotal = Size.X;
        var heightTotal = Size.Y;
        var visibleCount = all.Count;
        var totalGaps = Math.Max(0, visibleCount - 1) * gap;

        var leftFixed = 0f;
        foreach (var item in left)
        {
            leftFixed += item.Width;
        }

        var rightFixed = 0f;
        foreach (var item in right)
        {
            rightFixed += item.Width;
        }

        var centerFixed = 0f;
        var centerFlexCount = 0;
        foreach (var item in center)
        {
            if (item.Flex)
            {
                centerFlexCount++;
            }
            else
            {
                centerFixed += item.Width;
            }
        }

        var rest = widthTotal - leftFixed - rightFixed - centerFixed - totalGaps;
        var flexWidth = centerFlexCount > 0 ? Math.Max(0, rest / centerFlexCount) : 0f;

        var cursorX = 0f;
        Action<LayoutItem, float> place = (item, itemWidth) =>
        {
            FitChildInRect(item.Container, new Rect2(cursorX, 0, Math.Max(0, itemWidth), Math.Max(0, heightTotal)));
            cursorX += itemWidth + gap;
        };

        foreach (var item in left)
        {
            place(item, item.Width);
        }

        foreach (var item in center)
        {
            place(item, item.Flex ? flexWidth : item.Width);
        }

        foreach (var item in right)
        {
            place(item, item.Width);
        }
    }
}
