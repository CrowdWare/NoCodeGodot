using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Runtime.UI;

public partial class DockSpace : VBoxContainer
{
    private readonly Dictionary<DockSlotId, TabContainer> _slotTabs = new();
    private readonly Dictionary<string, DockPanelState> _panels = new(StringComparer.OrdinalIgnoreCase);

    public override void _Ready()
    {
        CallDeferred(nameof(InitializeDocking));
    }

    public DockLayoutState CaptureLayoutState()
    {
        var panels = _panels.Values
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(p => new DockPanelState
            {
                Id = p.Id,
                Title = p.Title,
                CurrentSlot = p.CurrentSlot,
                LastDockedSlot = p.LastDockedSlot,
                IsFloating = p.IsFloating,
                IsClosed = p.IsClosed
            })
            .ToArray();

        return new DockLayoutState
        {
            UpdatedAtUtc = DateTime.UtcNow,
            Panels = panels
        };
    }

    public bool MovePanelToSlot(string panelId, DockSlotId slot)
    {
        if (string.IsNullOrWhiteSpace(panelId) || !_slotTabs.TryGetValue(slot, out var targetSlot))
        {
            return false;
        }

        var panel = FindPanel(panelId);
        if (panel is null)
        {
            return false;
        }

        DockPanelIntoSlot(panel, targetSlot, slot);
        return true;
    }

    private void InitializeDocking()
    {
        EnsureSlotContainers();

        var dockPanels = GetChildren()
            .OfType<DockPanel>()
            .ToArray();

        foreach (var panel in dockPanels)
        {
            var slot = panel.ResolveInitialSlot(DockSlotId.Center);
            var slotTabs = _slotTabs[slot];
            DockPanelIntoSlot(panel, slotTabs, slot);
        }
    }

    private void EnsureSlotContainers()
    {
        if (_slotTabs.Count > 0)
        {
            return;
        }

        var currentName = Name.ToString();
        Name = string.IsNullOrWhiteSpace(currentName) ? "DockSpace" : currentName;
        NodePropertyMapper.ApplyFillMaxSize(this);

        var topRow = new HBoxContainer { Name = "DockTopRow" };
        var bottomRow = new HBoxContainer { Name = "DockBottomRow" };
        NodePropertyMapper.ApplyFillMaxSize(topRow);
        NodePropertyMapper.ApplyFillMaxSize(bottomRow);

        AddChild(topRow);
        AddChild(bottomRow);

        AddSlot(topRow, DockSlotId.FarLeft, stretchRatio: 0.9f);
        AddSlot(topRow, DockSlotId.Left, stretchRatio: 1.1f);
        AddSlot(topRow, DockSlotId.Center, stretchRatio: 1.7f);
        AddSlot(topRow, DockSlotId.Right, stretchRatio: 1.1f);
        AddSlot(topRow, DockSlotId.FarRight, stretchRatio: 0.9f);

        AddSlot(bottomRow, DockSlotId.BottomFarLeft, stretchRatio: 0.9f);
        AddSlot(bottomRow, DockSlotId.BottomLeft, stretchRatio: 1.1f);
        AddSlot(bottomRow, DockSlotId.BottomRight, stretchRatio: 1.1f);
        AddSlot(bottomRow, DockSlotId.BottomFarRight, stretchRatio: 0.9f);
    }

    private void AddSlot(Container row, DockSlotId slot, float stretchRatio)
    {
        var tabs = new TabContainer
        {
            Name = $"DockSlot_{slot.ToSmlValue()}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = stretchRatio
        };

        _slotTabs[slot] = tabs;
        row.AddChild(tabs);
    }

    private void DockPanelIntoSlot(DockPanel panel, TabContainer slotTabs, DockSlotId slot)
    {
        if (panel.GetParent() is not TabContainer)
        {
            panel.Reparent(slotTabs);
        }
        else if (panel.GetParent() != slotTabs)
        {
            panel.Reparent(slotTabs);
        }

        NodePropertyMapper.ApplyFillMaxSize(panel);

        var tabIndex = slotTabs.GetTabIdxFromControl(panel);
        if (tabIndex >= 0)
        {
            slotTabs.SetTabTitle(tabIndex, panel.ResolvePanelTitle());
        }

        var panelId = panel.ResolvePanelId();
        _panels[panelId] = new DockPanelState
        {
            Id = panelId,
            Title = panel.ResolvePanelTitle(),
            CurrentSlot = slot,
            LastDockedSlot = slot,
            IsFloating = false,
            IsClosed = false
        };
    }

    private DockPanel? FindPanel(string panelId)
    {
        foreach (var tabs in _slotTabs.Values)
        {
            foreach (var child in tabs.GetChildren())
            {
                if (child is not DockPanel panel)
                {
                    continue;
                }

                if (string.Equals(panel.ResolvePanelId(), panelId, StringComparison.OrdinalIgnoreCase))
                {
                    return panel;
                }
            }
        }

        return null;
    }
}
