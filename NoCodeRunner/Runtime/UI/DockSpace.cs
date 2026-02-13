using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Runtime.UI;

public partial class DockSpace : VBoxContainer
{
    private sealed record SideColumn(DockSlotId TopSlot, DockSlotId BottomSlot, VBoxContainer Column, TabContainer TopTabs, TabContainer BottomTabs);

    private readonly Dictionary<DockSlotId, TabContainer> _slotTabs = new();
    private readonly Dictionary<DockSlotId, SideColumn> _sideColumnsBySlot = new();
    private readonly Dictionary<string, DockPanel> _panelNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DockPanelState> _panels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Window> _floatingWindows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Action> _floatingCloseHandlers = new(StringComparer.OrdinalIgnoreCase);

    private MenuButton? _closedPanelsMenuButton;
    private bool _closedMenuConnected;

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

        if (_floatingWindows.TryGetValue(panelId, out var floatingWindow))
        {
            if (_floatingCloseHandlers.TryGetValue(panelId, out var closeHandler))
            {
                floatingWindow.CloseRequested -= closeHandler;
                _floatingCloseHandlers.Remove(panelId);
            }

            if (panel.GetParent() == floatingWindow)
            {
                floatingWindow.RemoveChild(panel);
            }

            _floatingWindows.Remove(panelId);
            floatingWindow.QueueFree();
        }

        DockPanelIntoSlot(panel, targetSlot, slot);
        return true;
    }

    public bool ClosePanel(string panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId)
            || !_panelNodes.TryGetValue(panelId, out var panel)
            || !_panels.TryGetValue(panelId, out var state))
        {
            return false;
        }

        if (_floatingWindows.TryGetValue(panelId, out var floatingWindow))
        {
            if (_floatingCloseHandlers.TryGetValue(panelId, out var closeHandler))
            {
                floatingWindow.CloseRequested -= closeHandler;
                _floatingCloseHandlers.Remove(panelId);
            }

            _floatingWindows.Remove(panelId);
            floatingWindow.QueueFree();
        }

        if (panel.GetParent() is not null)
        {
            panel.GetParent().RemoveChild(panel);
        }

        state.IsClosed = true;
        state.IsFloating = false;
        state.LastDockedSlot = state.CurrentSlot;
        RefreshSideColumnLayout(state.CurrentSlot);
        UpdateClosedPanelsMenu();
        return true;
    }

    public bool ReopenPanel(string panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId)
            || !_panelNodes.TryGetValue(panelId, out var panel)
            || !_panels.TryGetValue(panelId, out var state))
        {
            return false;
        }

        if (!state.IsClosed)
        {
            return true;
        }

        var targetSlot = state.LastDockedSlot;
        if (!_slotTabs.TryGetValue(targetSlot, out var slotTabs))
        {
            slotTabs = _slotTabs[DockSlotId.Center];
            targetSlot = DockSlotId.Center;
        }

        DockPanelIntoSlot(panel, slotTabs, targetSlot);
        state.IsClosed = false;
        state.IsFloating = false;
        UpdateClosedPanelsMenu();
        return true;
    }

    public bool MarkPanelFloating(string panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId)
            || !_panels.TryGetValue(panelId, out var state)
            || !_panelNodes.TryGetValue(panelId, out var panel))
        {
            return false;
        }

        if (_floatingWindows.ContainsKey(panelId))
        {
            return true;
        }

        state.LastDockedSlot = state.CurrentSlot;
        state.IsFloating = true;
        state.IsClosed = false;

        RefreshSideColumnLayout(state.CurrentSlot);

        if (panel.GetParent() is not null)
        {
            panel.GetParent().RemoveChild(panel);
        }

        var floatingWindow = new Window
        {
            Name = $"Floating_{panelId}",
            Title = state.Title,
            MinSize = new Vector2I(320, 220),
            Size = new Vector2I(540, 380),
            Unresizable = false
        };

        floatingWindow.AddChild(panel);
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.SetOffsetsPreset(Control.LayoutPreset.FullRect);
        NodePropertyMapper.ApplyFillMaxSize(panel);

        Action closeHandler = () => OnFloatingWindowCloseRequested(panelId);
        floatingWindow.CloseRequested += closeHandler;

        AddChild(floatingWindow);
        floatingWindow.PopupCentered();
        _floatingWindows[panelId] = floatingWindow;
        _floatingCloseHandlers[panelId] = closeHandler;

        RunnerLogger.Info("UI", $"Dock panel '{panelId}' floated into its own window.");
        return true;
    }

    public IReadOnlyList<DockPanelState> GetClosedPanels()
    {
        return _panels.Values
            .Where(p => p.IsClosed)
            .OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void InitializeDocking()
    {
        EnsureSlotContainers();

        var dockPanels = GetChildren()
            .OfType<DockPanel>()
            .ToArray();

        foreach (var panel in dockPanels)
        {
            var panelId = panel.ResolvePanelId();
            _panelNodes[panelId] = panel;
            panel.DockCommandRequested -= OnDockPanelCommandRequested;
            panel.DockCommandRequested += OnDockPanelCommandRequested;

            var slot = panel.ResolveInitialSlot(DockSlotId.Center);
            var slotTabs = _slotTabs[slot];
            DockPanelIntoSlot(panel, slotTabs, slot);
        }

        EnsureClosedPanelsMenuButton();
        UpdateClosedPanelsMenu();
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

        var columns = new HBoxContainer { Name = "DockColumns" };
        NodePropertyMapper.ApplyFillMaxSize(columns);
        AddChild(columns);

        AddDualColumn(columns, DockSlotId.FarLeft, DockSlotId.BottomFarLeft, stretchRatio: 0.9f);
        AddDualColumn(columns, DockSlotId.Left, DockSlotId.BottomLeft, stretchRatio: 1.1f);
        AddSlot(columns, DockSlotId.Center, stretchRatio: 1.7f);
        AddDualColumn(columns, DockSlotId.Right, DockSlotId.BottomRight, stretchRatio: 1.1f);
        AddDualColumn(columns, DockSlotId.FarRight, DockSlotId.BottomFarRight, stretchRatio: 0.9f);

        foreach (var sideColumn in _sideColumnsBySlot.Values.Distinct())
        {
            RefreshSideColumnLayout(sideColumn);
        }
    }

    private void EnsureClosedPanelsMenuButton()
    {
        if (_closedPanelsMenuButton is not null)
        {
            return;
        }

        _closedPanelsMenuButton = new MenuButton
        {
            Name = "ClosedDocksMenu",
            Text = "Docks",
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };

        AddChild(_closedPanelsMenuButton);
        MoveChild(_closedPanelsMenuButton, 0);
    }

    private void UpdateClosedPanelsMenu()
    {
        if (_closedPanelsMenuButton is null)
        {
            return;
        }

        var popup = _closedPanelsMenuButton.GetPopup();
        popup.Clear();

        var closedPanels = GetClosedPanels();
        if (closedPanels.Count == 0)
        {
            popup.AddItem("No closed docks");
            popup.SetItemDisabled(0, true);
            return;
        }

        for (var i = 0; i < closedPanels.Count; i++)
        {
            var panel = closedPanels[i];
            popup.AddItem(panel.Title, i);
        }

        if (!_closedMenuConnected)
        {
            popup.IdPressed += OnClosedPanelsMenuIdPressed;
            _closedMenuConnected = true;
        }
    }

    private void OnClosedPanelsMenuIdPressed(long id)
    {
        var closedPanels = GetClosedPanels();
        if (id < 0 || id >= closedPanels.Count)
        {
            return;
        }

        ReopenPanel(closedPanels[(int)id].Id);
    }

    private void OnDockPanelCommandRequested(string panelId, string command)
    {
        if (string.IsNullOrWhiteSpace(panelId) || string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        if (string.Equals(command, "closed", StringComparison.OrdinalIgnoreCase))
        {
            ClosePanel(panelId);
            return;
        }

        if (string.Equals(command, "floating", StringComparison.OrdinalIgnoreCase))
        {
            MarkPanelFloating(panelId);
            return;
        }

        if (DockSlotIdParser.TryParse(command, out var slot))
        {
            MovePanelToSlot(panelId, slot);
        }
    }

    private void OnFloatingWindowCloseRequested(string panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId)
            || !_floatingWindows.TryGetValue(panelId, out var floatingWindow)
            || !_panels.TryGetValue(panelId, out var state)
            || !_panelNodes.TryGetValue(panelId, out var panel))
        {
            return;
        }

        var targetSlot = state.LastDockedSlot;
        if (!_slotTabs.TryGetValue(targetSlot, out var targetTabs))
        {
            targetSlot = DockSlotId.Center;
            targetTabs = _slotTabs[targetSlot];
        }

        if (panel.GetParent() == floatingWindow)
        {
            floatingWindow.RemoveChild(panel);
        }

        if (_floatingCloseHandlers.TryGetValue(panelId, out var closeHandler))
        {
            floatingWindow.CloseRequested -= closeHandler;
            _floatingCloseHandlers.Remove(panelId);
        }

        _floatingWindows.Remove(panelId);
        floatingWindow.QueueFree();

        DockPanelIntoSlot(panel, targetTabs, targetSlot);
        state.IsFloating = false;
        state.IsClosed = false;
        state.CurrentSlot = targetSlot;
        state.LastDockedSlot = targetSlot;
        RunnerLogger.Info("UI", $"Dock panel '{panelId}' re-docked from floating window to '{targetSlot.ToSmlValue()}'.");
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

    private void AddDualColumn(HBoxContainer parentRow, DockSlotId topSlot, DockSlotId bottomSlot, float stretchRatio)
    {
        var column = new VBoxContainer
        {
            Name = $"DockColumn_{topSlot.ToSmlValue()}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = stretchRatio
        };

        parentRow.AddChild(column);

        AddSlot(column, topSlot, stretchRatio: 1f);
        AddSlot(column, bottomSlot, stretchRatio: 1f);

        var sideColumn = new SideColumn(
            TopSlot: topSlot,
            BottomSlot: bottomSlot,
            Column: column,
            TopTabs: _slotTabs[topSlot],
            BottomTabs: _slotTabs[bottomSlot]);

        _sideColumnsBySlot[topSlot] = sideColumn;
        _sideColumnsBySlot[bottomSlot] = sideColumn;
    }

    private void RefreshSideColumnLayout(DockSlotId slot)
    {
        if (_sideColumnsBySlot.TryGetValue(slot, out var sideColumn))
        {
            RefreshSideColumnLayout(sideColumn);
        }
    }

    private static void RefreshSideColumnLayout(SideColumn sideColumn)
    {
        var topHasPanels = sideColumn.TopTabs.GetTabCount() > 0;
        var bottomHasPanels = sideColumn.BottomTabs.GetTabCount() > 0;

        if (topHasPanels && bottomHasPanels)
        {
            sideColumn.Column.Visible = true;
            sideColumn.TopTabs.Visible = true;
            sideColumn.BottomTabs.Visible = true;
            sideColumn.TopTabs.SizeFlagsVertical = SizeFlags.ExpandFill;
            sideColumn.BottomTabs.SizeFlagsVertical = SizeFlags.ExpandFill;
            sideColumn.TopTabs.SizeFlagsStretchRatio = 1f;
            sideColumn.BottomTabs.SizeFlagsStretchRatio = 1f;
            return;
        }

        if (topHasPanels)
        {
            sideColumn.Column.Visible = true;
            sideColumn.TopTabs.Visible = true;
            sideColumn.BottomTabs.Visible = false;
            sideColumn.TopTabs.SizeFlagsVertical = SizeFlags.ExpandFill;
            sideColumn.TopTabs.SizeFlagsStretchRatio = 1f;
            return;
        }

        if (bottomHasPanels)
        {
            sideColumn.Column.Visible = true;
            sideColumn.TopTabs.Visible = false;
            sideColumn.BottomTabs.Visible = true;
            sideColumn.BottomTabs.SizeFlagsVertical = SizeFlags.ExpandFill;
            sideColumn.BottomTabs.SizeFlagsStretchRatio = 1f;
            return;
        }

        // Hide entire side column if it has no dock at all.
        sideColumn.Column.Visible = false;
        sideColumn.TopTabs.Visible = false;
        sideColumn.BottomTabs.Visible = false;
    }

    private void DockPanelIntoSlot(DockPanel panel, TabContainer slotTabs, DockSlotId slot)
    {
        var panelId = panel.ResolvePanelId();
        DockSlotId? previousSlot = null;
        if (_panels.TryGetValue(panelId, out var previousState))
        {
            previousSlot = previousState.CurrentSlot;
        }

        if (panel.GetParent() != slotTabs)
        {
            panel.GetParent()?.RemoveChild(panel);
            slotTabs.AddChild(panel);
        }

        NodePropertyMapper.ApplyFillMaxSize(panel);

        var tabIndex = slotTabs.GetTabIdxFromControl(panel);
        if (tabIndex >= 0)
        {
            slotTabs.SetTabTitle(tabIndex, panel.ResolvePanelTitle());
        }

        _panelNodes[panelId] = panel;

        if (!_panels.TryGetValue(panelId, out var state))
        {
            state = new DockPanelState
            {
                Id = panelId
            };
            _panels[panelId] = state;
        }

        state.Title = panel.ResolvePanelTitle();
        state.CurrentSlot = slot;
        state.LastDockedSlot = slot;
        state.IsFloating = false;
        state.IsClosed = false;

        _panels[panelId] = state;

        RefreshSideColumnLayout(slot);
        if (previousSlot is not null && previousSlot.Value != slot)
        {
            RefreshSideColumnLayout(previousSlot.Value);
        }

        UpdateClosedPanelsMenu();
    }

    private DockPanel? FindPanel(string panelId)
    {
        if (_panelNodes.TryGetValue(panelId, out var panel))
        {
            return panel;
        }

        return null;
    }
}
