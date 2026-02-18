using Godot;
using System;

namespace Runtime.UI;

public sealed partial class DockingContainerControl : PanelContainer
{
    public enum DockSideKind
    {
        Center,
        FarLeft,
        FarLeftBottom,
        Left,
        LeftBottom,
        Right,
        RightBottom,
        FarRight,
        FarRightBottom
    }

    private const float DefaultMinFixedWidth = 140f;
    private TabContainer? _tabHost;

    public override void _Ready()
    {
        EnsureTabHost();
        EnsureDockDefaults();
    }

    public TabContainer EnsureTabHost()
    {
        if (_tabHost is not null && GodotObject.IsInstanceValid(_tabHost))
        {
            return _tabHost;
        }

        for (var i = 0; i < GetChildCount(); i++)
        {
            if (GetChild(i) is TabContainer existing)
            {
                _tabHost = existing;
                _tabHost.SetMeta(NodePropertyMapper.MetaDockingTabHost, Variant.From(true));
                return _tabHost;
            }
        }

        _tabHost = new TabContainer
        {
            Name = string.IsNullOrWhiteSpace(Name) ? "DockTabs" : $"{Name}_Tabs",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            DragToRearrangeEnabled = true,
            TabsRearrangeGroup = 1
        };

        _tabHost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _tabHost.SetMeta(NodePropertyMapper.MetaDockingTabHost, Variant.From(true));
        AddChild(_tabHost);
        MoveChild(_tabHost, 0);
        return _tabHost;
    }

    public void AddDockTab(Control child, string title)
    {
        var tabs = EnsureTabHost();
        tabs.AddChild(child);
        var tabIndex = tabs.GetTabIdxFromControl(child);
        if (tabIndex >= 0)
        {
            tabs.SetTabTitle(tabIndex, title);
        }
    }

    public DockSideKind GetDockSideKind()
    {
        if (!HasMeta(NodePropertyMapper.MetaDockSide))
        {
            return DockSideKind.Center;
        }

        var side = (GetMeta(NodePropertyMapper.MetaDockSide).AsString() ?? string.Empty).Trim().ToLowerInvariant();
        return side switch
        {
            "farleft" => DockSideKind.FarLeft,
            "farleftbottom" => DockSideKind.FarLeftBottom,
            "left" => DockSideKind.Left,
            "leftbottom" => DockSideKind.LeftBottom,
            "right" => DockSideKind.Right,
            "rightbottom" => DockSideKind.RightBottom,
            "farright" => DockSideKind.FarRight,
            "farrightbottom" => DockSideKind.FarRightBottom,
            _ => DockSideKind.Center
        };
    }

    public string GetDockSide()
    {
        return GetDockSideKind() switch
        {
            DockSideKind.FarLeft => "farleft",
            DockSideKind.FarLeftBottom => "farleftbottom",
            DockSideKind.Left => "left",
            DockSideKind.LeftBottom => "leftbottom",
            DockSideKind.Right => "right",
            DockSideKind.RightBottom => "rightbottom",
            DockSideKind.FarRight => "farright",
            DockSideKind.FarRightBottom => "farrightbottom",
            _ => "center"
        };
    }

    public bool IsFlex()
    {
        if (HasMeta(NodePropertyMapper.MetaDockFlex))
        {
            return GetMeta(NodePropertyMapper.MetaDockFlex).AsBool();
        }

        return GetDockSideKind() == DockSideKind.Center;
    }

    public float GetFixedWidth()
    {
        var minFixedWidth = GetMinFixedWidth();

        if (HasMeta(NodePropertyMapper.MetaDockFixedWidth))
        {
            var width = Math.Max(0, GetMeta(NodePropertyMapper.MetaDockFixedWidth).AsInt32());
            return Math.Max(minFixedWidth, width);
        }

        var minWidth = CustomMinimumSize.X;
        var fallback = minWidth > 0 ? minWidth : 240f;
        return Math.Max(minFixedWidth, fallback);
    }

    public float GetMinFixedWidth()
    {
        if (HasMeta(NodePropertyMapper.MetaDockMinFixedWidth))
        {
            return Math.Max(40f, GetMeta(NodePropertyMapper.MetaDockMinFixedWidth).AsInt32());
        }

        return DefaultMinFixedWidth;
    }

    public void SetFixedWidth(float width)
    {
        var clamped = Math.Max(GetMinFixedWidth(), width);
        SetMeta(NodePropertyMapper.MetaDockFixedWidth, Variant.From((int)MathF.Round(clamped)));
    }

    public bool IsCloseable()
    {
        if (HasMeta(NodePropertyMapper.MetaDockCloseable))
        {
            return GetMeta(NodePropertyMapper.MetaDockCloseable).AsBool();
        }

        return true;
    }

    private void EnsureDockDefaults()
    {
        var tabs = EnsureTabHost();

        if (HasMeta(NodePropertyMapper.MetaDockDragToRearrangeEnabled))
        {
            tabs.DragToRearrangeEnabled = GetMeta(NodePropertyMapper.MetaDockDragToRearrangeEnabled).AsBool();
        }

        if (HasMeta(NodePropertyMapper.MetaDockTabsRearrangeGroup))
        {
            tabs.TabsRearrangeGroup = GetMeta(NodePropertyMapper.MetaDockTabsRearrangeGroup).AsInt32();
        }
    }
}
