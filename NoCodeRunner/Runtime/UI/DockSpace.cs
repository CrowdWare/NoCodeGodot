using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Runtime.UI;

public partial class DockSpace : VBoxContainer
{
    private sealed class DockLayoutDocument
    {
        public int LayoutVersion { get; set; }
        public DockLayoutState Layout { get; set; } = new();
    }

    private sealed record SideColumn(DockSlotId TopSlot, DockSlotId BottomSlot, VBoxContainer Column, TabContainer TopTabs, TabContainer BottomTabs);

    private const int CurrentLayoutVersion = 1;
    private const string DefaultLayoutFileName = "dock_layout.json";
    private static readonly JsonSerializerOptions LayoutJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly Dictionary<DockSlotId, TabContainer> _slotTabs = new();
    private readonly Dictionary<DockSlotId, SideColumn> _sideColumnsBySlot = new();
    private readonly Dictionary<string, DockPanel> _panelNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DockPanelState> _panels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Window> _floatingWindows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Action> _floatingCloseHandlers = new(StringComparer.OrdinalIgnoreCase);

    private MenuButton? _closedPanelsMenuButton;
    private bool _closedMenuConnected;
    private bool _isDockDragActive;
    private bool _isDockDragCandidate;
    private Vector2 _dragStartGlobal;
    private DockSlotId _dragSourceSlot;
    private string _dragPreviewTitle = "Dock";
    private TabContainer? _currentDragTarget;
    private PanelContainer? _dragGhost;
    private Label? _dragGhostLabel;
    private const float DragStartThreshold = 8f;
    private const float MinDropTargetWidth = 120f;
    private const float MinDropTargetHeight = 80f;
    private DockLayoutState? _defaultLayoutState;
    private readonly List<(string PanelId, DockSlotId Slot, int TabIndex)> _pendingLockedPanelRestores = new();
    private bool _isRestoringLockedPanels;

    public override void _Ready()
    {
        CallDeferred(nameof(InitializeDocking));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
        {
            EndDockDrag();
        }

        base._UnhandledInput(@event);
    }

    public DockLayoutState CaptureLayoutState()
    {
        ReconcilePanelStatesFromUi();

        var panels = _panels.Values
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(p => new DockPanelState
            {
                Id = p.Id,
                Title = p.Title,
                CurrentSlot = p.CurrentSlot,
                LastDockedSlot = p.LastDockedSlot,
                TabIndex = p.TabIndex,
                IsActiveTab = p.IsActiveTab,
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

    public bool SaveLayout(string? absolutePath = null)
    {
        try
        {
            var path = ResolveLayoutPath(absolutePath);
            var document = new DockLayoutDocument
            {
                LayoutVersion = CurrentLayoutVersion,
                Layout = CaptureLayoutState()
            };

            var json = JsonSerializer.Serialize(document, LayoutJsonOptions);
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
            RunnerLogger.Info("UI", $"Dock layout saved to '{path}'.");
            return true;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("UI", "SaveLayout failed.", ex);
            return false;
        }
    }

    public bool LoadLayout(string? absolutePath = null)
    {
        try
        {
            var path = ResolveLayoutPath(absolutePath);
            if (!File.Exists(path))
            {
                RunnerLogger.Info("UI", $"No persisted dock layout found at '{path}'.");
                return false;
            }

            var json = File.ReadAllText(path);
            var document = JsonSerializer.Deserialize<DockLayoutDocument>(json, LayoutJsonOptions);
            if (document is null)
            {
                RunnerLogger.Warn("UI", "LoadLayout failed: parsed document is null. Resetting to default layout.");
                ResetDefaultLayout();
                return false;
            }

            if (document.LayoutVersion != CurrentLayoutVersion)
            {
                RunnerLogger.Warn("UI", $"LoadLayout layoutVersion mismatch (saved={document.LayoutVersion}, expected={CurrentLayoutVersion}). Resetting to default layout.");
                ResetDefaultLayout();
                return false;
            }

            if (!ValidateLayout(document.Layout, out var reason))
            {
                RunnerLogger.Warn("UI", $"LoadLayout validation failed: {reason}. Resetting to default layout.");
                ResetDefaultLayout();
                return false;
            }

            ApplyLayout(document.Layout);
            RunnerLogger.Info("UI", $"Dock layout loaded from '{path}'.");
            return true;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("UI", "LoadLayout failed. Resetting to default layout.", ex);
            ResetDefaultLayout();
            return false;
        }
    }

    public void ResetDefaultLayout()
    {
        if (_defaultLayoutState is null)
        {
            RunnerLogger.Warn("UI", "ResetDefaultLayout requested but no default snapshot is available yet.");
            return;
        }

        ApplyLayout(CloneLayoutState(_defaultLayoutState));
        RunnerLogger.Info("UI", "Dock layout reset to default snapshot.");
    }

    public bool MovePanelToSlot(string panelId, DockSlotId slot)
    {
        return MovePanelToSlotInternal(panelId, slot, ignoreDockable: false);
    }

    private bool MovePanelToSlotInternal(string panelId, DockSlotId slot, bool ignoreDockable)
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

        if (!ignoreDockable
            && !panel.IsDockable()
            && _panels.TryGetValue(panelId, out var panelState)
            && panelState.CurrentSlot != slot)
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

        _defaultLayoutState ??= CloneLayoutState(CaptureLayoutState());
        LoadLayout();
        UpdateAllSlotInteractionRules();
    }

    private void ApplyLayout(DockLayoutState layout)
    {
        ReconcilePanelStatesFromUi();

        // Step 1: ensure all panels are docked (clear closed/floating states) before deterministic re-apply.
        foreach (var panelId in _panelNodes.Keys.ToArray())
        {
            if (_floatingWindows.ContainsKey(panelId))
            {
                MovePanelToSlot(panelId, _panels.TryGetValue(panelId, out var existing) ? existing.LastDockedSlot : DockSlotId.Center);
            }

            if (_panels.TryGetValue(panelId, out var state) && state.IsClosed)
            {
                ReopenPanel(panelId);
            }
        }

        // Step 2: place all non-floating/non-closed panels into target slots.
        foreach (var panel in layout.Panels
                     .Where(p => !p.IsFloating && !p.IsClosed)
                     .OrderBy(p => p.CurrentSlot)
                     .ThenBy(p => p.TabIndex))
        {
            if (_panelNodes.ContainsKey(panel.Id))
            {
                MovePanelToSlot(panel.Id, panel.CurrentSlot);
            }
        }

        // Step 3: reorder tabs deterministically within each slot.
        foreach (DockSlotId slot in Enum.GetValues(typeof(DockSlotId)))
        {
            if (!_slotTabs.TryGetValue(slot, out var tabs) || !IsAlive(tabs))
            {
                continue;
            }

            var desiredOrder = layout.Panels
                .Where(p => p.CurrentSlot == slot && !p.IsFloating && !p.IsClosed)
                .OrderBy(p => p.TabIndex)
                .Select(p => p.Id)
                .ToArray();

            for (var i = 0; i < desiredOrder.Length; i++)
            {
                var panelId = desiredOrder[i];
                if (!_panelNodes.TryGetValue(panelId, out var panel) || panel.GetParent() != tabs)
                {
                    continue;
                }

                tabs.MoveChild(panel, i);
            }

            var active = layout.Panels.FirstOrDefault(p => p.CurrentSlot == slot && p.IsActiveTab && !p.IsFloating && !p.IsClosed);
            if (active is not null && _panelNodes.TryGetValue(active.Id, out var activePanel) && activePanel.GetParent() == tabs)
            {
                var activeIndex = tabs.GetTabIdxFromControl(activePanel);
                if (activeIndex >= 0)
                {
                    tabs.CurrentTab = activeIndex;
                }
            }
        }

        // Step 4: floating and closed flags.
        foreach (var panel in layout.Panels.Where(p => p.IsFloating && !p.IsClosed))
        {
            MarkPanelFloating(panel.Id);
        }

        foreach (var panel in layout.Panels.Where(p => p.IsClosed))
        {
            ClosePanel(panel.Id);
        }

        ReconcilePanelStatesFromUi();
    }

    private bool ValidateLayout(DockLayoutState layout, out string reason)
    {
        reason = string.Empty;
        if (layout.Panels is null)
        {
            reason = "Panels collection is null.";
            return false;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var panel in layout.Panels)
        {
            if (string.IsNullOrWhiteSpace(panel.Id))
            {
                reason = "Panel id is empty.";
                return false;
            }

            if (!seen.Add(panel.Id))
            {
                reason = $"Duplicate panel id '{panel.Id}'.";
                return false;
            }

            if (!_panelNodes.ContainsKey(panel.Id))
            {
                reason = $"Unknown panel id '{panel.Id}'.";
                return false;
            }

            if (panel.IsClosed && panel.IsFloating)
            {
                reason = $"Panel '{panel.Id}' cannot be both closed and floating.";
                return false;
            }
        }

        return true;
    }

    private static DockLayoutState CloneLayoutState(DockLayoutState source)
    {
        return new DockLayoutState
        {
            UpdatedAtUtc = source.UpdatedAtUtc,
            Panels = source.Panels.Select(p => new DockPanelState
            {
                Id = p.Id,
                Title = p.Title,
                CurrentSlot = p.CurrentSlot,
                LastDockedSlot = p.LastDockedSlot,
                TabIndex = p.TabIndex,
                IsActiveTab = p.IsActiveTab,
                IsFloating = p.IsFloating,
                IsClosed = p.IsClosed
            }).ToArray()
        };
    }

    private static string ResolveLayoutPath(string? absolutePath)
    {
        if (!string.IsNullOrWhiteSpace(absolutePath))
        {
            return absolutePath;
        }

        return ProjectSettings.GlobalizePath($"user://{DefaultLayoutFileName}");
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

        tabs.DragToRearrangeEnabled = true;
        tabs.TabsRearrangeGroup = 1;
        tabs.TabChanged += _ => ReconcilePanelStatesFromUi();
        tabs.ChildEnteredTree += _ => ReconcilePanelStatesFromUi();
        tabs.ChildExitingTree += _ => ReconcilePanelStatesFromUi();
        tabs.ChildOrderChanged += () => ReconcilePanelStatesFromUi();
        tabs.GuiInput += e => OnSlotGuiInput(slot, tabs, e);

        _slotTabs[slot] = tabs;
        row.AddChild(tabs);
    }

    private void OnSlotGuiInput(DockSlotId slot, TabContainer tabs, InputEvent @event)
    {
        if (!IsAlive(tabs))
        {
            return;
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            UpdateSlotInteractionRules(tabs);
        }

        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                if (press.Position.Y <= 32f)
                {
                    if (!IsCurrentTabDockable(tabs))
                    {
                        tabs.DragToRearrangeEnabled = false;
                        return;
                    }

                    tabs.DragToRearrangeEnabled = true;
                    _isDockDragCandidate = true;
                    _dragStartGlobal = press.GlobalPosition;
                    _dragSourceSlot = slot;
                    _dragPreviewTitle = ResolveCurrentTabTitle(tabs);
                }
                break;

            case InputEventMouseMotion motion when _isDockDragCandidate && !_isDockDragActive:
                if (motion.GlobalPosition.DistanceTo(_dragStartGlobal) >= DragStartThreshold)
                {
                    BeginDockDrag();
                    UpdateDockDragFeedback(motion.GlobalPosition);
                }
                break;

            case InputEventMouseMotion motion when _isDockDragActive:
                UpdateDockDragFeedback(motion.GlobalPosition);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                tabs.DragToRearrangeEnabled = true;
                EndDockDrag();
                break;
        }
    }

    private void BeginDockDrag()
    {
        _isDockDragActive = true;
        EnsureDragGhost();
        if (_dragGhost is not null)
        {
            _dragGhost.Visible = true;
        }

        Input.SetDefaultCursorShape(Input.CursorShape.PointingHand);
    }

    private void EndDockDrag()
    {
        _isDockDragCandidate = false;
        if (!_isDockDragActive)
        {
            return;
        }

        _isDockDragActive = false;
        _currentDragTarget = null;
        if (_dragGhost is not null)
        {
            _dragGhost.Visible = false;
        }

        ResetAllSlotHighlighting();
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);

        // Let Godot finish internal tab move first, then reconcile runtime state.
        CallDeferred(nameof(ReconcilePanelStatesFromUi));
    }

    private void UpdateDockDragFeedback(Vector2 globalMouse)
    {
        if (!_isDockDragActive)
        {
            return;
        }

        UpdateDragGhost(globalMouse);

        TabContainer? hoveredTarget = null;
        var hoveredValid = false;

        foreach (var tabs in _slotTabs.Values)
        {
            if (!IsAlive(tabs) || !tabs.Visible)
            {
                continue;
            }

            var rect = tabs.GetGlobalRect();
            if (!rect.HasPoint(globalMouse))
            {
                continue;
            }

            hoveredTarget = tabs;
            hoveredValid = rect.Size.X >= MinDropTargetWidth
                          && rect.Size.Y >= MinDropTargetHeight
                          && IsSlotDropTargetEnabled(tabs);
            break;
        }

        _currentDragTarget = hoveredTarget;
        ApplySlotHighlighting(hoveredTarget, hoveredValid);
        Input.SetDefaultCursorShape(hoveredValid ? Input.CursorShape.PointingHand : Input.CursorShape.Forbidden);
    }

    private void EnsureDragGhost()
    {
        if (_dragGhost is not null)
        {
            _dragGhostLabel!.Text = _dragPreviewTitle;
            return;
        }

        _dragGhost = new PanelContainer
        {
            Name = "DockDragGhost",
            TopLevel = true,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
            Modulate = new Color(1f, 1f, 1f, 0.9f),
            CustomMinimumSize = new Vector2(180f, 36f)
        };

        _dragGhostLabel = new Label
        {
            Text = _dragPreviewTitle,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        var content = new MarginContainer();
        content.AddThemeConstantOverride("margin_left", 8);
        content.AddThemeConstantOverride("margin_right", 8);
        content.AddThemeConstantOverride("margin_top", 6);
        content.AddThemeConstantOverride("margin_bottom", 6);
        content.AddChild(_dragGhostLabel);

        _dragGhost.AddChild(content);
        AddChild(_dragGhost);
    }

    private void UpdateDragGhost(Vector2 globalMouse)
    {
        if (_dragGhost is null)
        {
            return;
        }

        _dragGhost.Position = globalMouse + new Vector2(14f, 10f);
        if (_dragGhostLabel is not null)
        {
            _dragGhostLabel.Text = _dragPreviewTitle;
        }
    }

    private void ResetAllSlotHighlighting()
    {
        foreach (var tabs in _slotTabs.Values)
        {
            if (!IsAlive(tabs))
            {
                continue;
            }

            tabs.SelfModulate = Colors.White;
        }
    }

    private void ApplySlotHighlighting(TabContainer? hovered, bool isValid)
    {
        foreach (var tabs in _slotTabs.Values)
        {
            if (!IsAlive(tabs))
            {
                continue;
            }

            if (tabs == hovered)
            {
                tabs.SelfModulate = isValid
                    ? new Color(0.78f, 1.0f, 0.78f, 1f)
                    : new Color(1.0f, 0.78f, 0.78f, 1f);
            }
            else
            {
                tabs.SelfModulate = Colors.White;
            }
        }
    }

    private static string ResolveCurrentTabTitle(TabContainer tabs)
    {
        var current = tabs.CurrentTab;
        if (current >= 0 && current < tabs.GetTabCount())
        {
            return tabs.GetTabTitle(current);
        }

        return "Dock";
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
        if (!IsAlive(sideColumn.Column)
            || !IsAlive(sideColumn.TopTabs)
            || !IsAlive(sideColumn.BottomTabs))
        {
            return;
        }

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

    private void ReconcilePanelStatesFromUi()
    {
        if (!IsInsideTree())
        {
            return;
        }

        var restoreCandidates = new List<(string PanelId, DockSlotId Slot, int TabIndex)>();

        foreach (var (slot, tabs) in _slotTabs)
        {
            if (!IsAlive(tabs))
            {
                continue;
            }

            var activeTab = tabs.CurrentTab;
            var index = 0;
            foreach (var child in tabs.GetChildren())
            {
                if (child is not DockPanel panel)
                {
                    continue;
                }
                var panelId = panel.ResolvePanelId();
                var hasState = _panels.TryGetValue(panelId, out var state);

                if (panel.IsDockable() == false && hasState && state is not null)
                {
                    if (state.CurrentSlot != slot || state.TabIndex != index)
                    {
                        restoreCandidates.Add((panelId, state.CurrentSlot, state.TabIndex));
                    }
                }

                if (!hasState || state is null)
                {
                    state = new DockPanelState { Id = panelId };
                    _panels[panelId] = state;
                }

                state.Title = panel.ResolvePanelTitle();
                state.CurrentSlot = slot;
                state.LastDockedSlot = slot;
                state.TabIndex = index;
                state.IsActiveTab = index == activeTab;
                state.IsFloating = false;
                state.IsClosed = false;

                _panelNodes[panelId] = panel;
                index++;
            }

            RefreshSideColumnLayout(slot);
        }

        UpdateAllSlotInteractionRules();

        if (restoreCandidates.Count > 0)
        {
            EnqueueLockedPanelRestores(restoreCandidates);
        }
    }

    private void EnqueueLockedPanelRestores(IEnumerable<(string PanelId, DockSlotId Slot, int TabIndex)> candidates)
    {
        foreach (var candidate in candidates)
        {
            _pendingLockedPanelRestores.RemoveAll(x => string.Equals(x.PanelId, candidate.PanelId, StringComparison.OrdinalIgnoreCase));
            _pendingLockedPanelRestores.Add(candidate);
        }

        if (!_isRestoringLockedPanels)
        {
            CallDeferred(nameof(RestoreLockedPanelsDeferred));
        }
    }

    private void RestoreLockedPanelsDeferred()
    {
        if (_isRestoringLockedPanels || _pendingLockedPanelRestores.Count == 0)
        {
            return;
        }

        _isRestoringLockedPanels = true;
        try
        {
            var pending = _pendingLockedPanelRestores.ToArray();
            _pendingLockedPanelRestores.Clear();

            foreach (var item in pending)
            {
                if (!_panelNodes.TryGetValue(item.PanelId, out var panel)
                    || !IsAlive(panel)
                    || !_slotTabs.TryGetValue(item.Slot, out var tabs)
                    || !IsAlive(tabs))
                {
                    continue;
                }

                if (panel.GetParent() != tabs)
                {
                    MovePanelToSlotInternal(item.PanelId, item.Slot, ignoreDockable: true);
                }

                if (panel.GetParent() == tabs)
                {
                    var maxIndex = Math.Max(0, tabs.GetChildCount() - 1);
                    var clamped = Math.Clamp(item.TabIndex, 0, maxIndex);
                    tabs.CallDeferred("move_child", panel, clamped);
                }
            }
        }
        finally
        {
            _isRestoringLockedPanels = false;
            CallDeferred(nameof(ReconcilePanelStatesFromUi));
        }
    }

    private bool IsCurrentTabDockable(TabContainer tabs)
    {
        var panel = GetCurrentTabPanel(tabs);
        return panel is null || panel.IsDockable();
    }

    private void UpdateSlotInteractionRules(TabContainer tabs)
    {
        tabs.DragToRearrangeEnabled = IsCurrentTabDockable(tabs);
        tabs.TabsRearrangeGroup = IsSlotDropTargetEnabled(tabs) ? 1 : -1;
    }

    private void UpdateAllSlotInteractionRules()
    {
        foreach (var tabs in _slotTabs.Values)
        {
            if (!IsAlive(tabs))
            {
                continue;
            }

            UpdateSlotInteractionRules(tabs);
        }
    }

    private static bool IsSlotDropTargetEnabled(TabContainer tabs)
    {
        var panel = GetCurrentTabPanel(tabs);
        if (panel is null)
        {
            return true;
        }

        return panel.IsDropTarget();
    }

    private static DockPanel? GetCurrentTabPanel(TabContainer tabs)
    {
        var current = tabs.CurrentTab;
        if (current < 0 || current >= tabs.GetTabCount())
        {
            return null;
        }

        var control = tabs.GetTabControl(current);
        return control as DockPanel;
    }

    private static bool IsAlive(GodotObject? instance)
    {
        return instance is not null && GodotObject.IsInstanceValid(instance);
    }
}
