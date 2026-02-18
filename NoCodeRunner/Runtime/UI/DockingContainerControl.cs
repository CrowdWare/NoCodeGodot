using Godot;
using System;

namespace Runtime.UI;

public sealed partial class DockingContainerControl : PanelContainer
{
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

    public string GetDockSide()
    {
        if (HasMeta(NodePropertyMapper.MetaDockSide))
        {
            return (GetMeta(NodePropertyMapper.MetaDockSide).AsString() ?? string.Empty).Trim().ToLowerInvariant();
        }

        return "center";
    }

    public bool IsFlex()
    {
        if (HasMeta(NodePropertyMapper.MetaDockFlex))
        {
            return GetMeta(NodePropertyMapper.MetaDockFlex).AsBool();
        }

        return string.Equals(GetDockSide(), "center", StringComparison.OrdinalIgnoreCase);
    }

    public float GetFixedWidth()
    {
        if (HasMeta(NodePropertyMapper.MetaDockFixedWidth))
        {
            return Math.Max(0, GetMeta(NodePropertyMapper.MetaDockFixedWidth).AsInt32());
        }

        var minWidth = CustomMinimumSize.X;
        return minWidth > 0 ? minWidth : 240f;
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
