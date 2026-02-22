/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of ForgeRunner.
 *
 *  ForgeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ForgeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ForgeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Runtime.UI;

public sealed partial class DockingHostControl : Container
{
    private const float DefaultHandleWidth = 6f;
    private static readonly Color HandleVisibleColor = new(0f, 0f, 0f, 0.35f);
    private static readonly Color HandleHiddenColor = new(1f, 1f, 1f, 0f);

    private sealed class LayoutItem
    {
        public required DockingContainerControl Container;
        public required DockingContainerControl.DockSideKind Side;
        public required bool Flex;
        public required float Width;
    }

    private sealed class DockColumn
    {
        public DockingContainerControl? Top;
        public DockingContainerControl? Bottom;
        public bool IsFlex;
        public float Width;

        public bool HasVisibleTop => Top is not null && Top.Visible;
        public bool HasVisibleBottom => Bottom is not null && Bottom.Visible;
        public bool HasVisible => HasVisibleTop || HasVisibleBottom;

        public DockingContainerControl? PrimaryVisible => HasVisibleTop ? Top : (HasVisibleBottom ? Bottom : null);

        public IEnumerable<DockingContainerControl> VisibleContainers()
        {
            if (HasVisibleTop && Top is not null)
            {
                yield return Top;
            }

            if (HasVisibleBottom && Bottom is not null)
            {
                yield return Bottom;
            }
        }
    }

    private sealed class GapSegment
    {
        public required Rect2 Rect;
        public DockingContainerControl? LeftNeighbor;
        public DockingContainerControl? RightNeighbor;
    }

    private sealed partial class DockResizeHandleControl : ColorRect
    {
        public DockingContainerControl? LeftNeighbor;
        public DockingContainerControl? RightNeighbor;
        public Action<DockResizeHandleControl, InputEvent>? InputHandler;

        public override void _GuiInput(InputEvent @event)
        {
            InputHandler?.Invoke(this, @event);
        }
    }

    private readonly Dictionary<ulong, Rect2> _rightGapByContainer = [];
    private readonly Dictionary<ulong, List<DockingContainerControl>> _widthLinkedByContainer = [];
    private readonly List<DockResizeHandleControl> _handles = [];
    private int _lastLoggedHandleCount = -1;
    private int _lastLoggedInteriorGapCount = -1;
    private DockResizeHandleControl? _activeHandle;
    private DockingContainerControl? _activeTargetContainer;
    private bool _activeTargetUsesPositiveDelta;
    private float _dragStartMouseX;
    private float _dragStartWidth;
    private DockLayoutState? _defaultLayoutState;
    private readonly Dictionary<string, HiddenPanelState> _hiddenPanelsByTitle = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HiddenPanelState> _hiddenPanelsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _menuItemToPanel = new(StringComparer.OrdinalIgnoreCase);

    private sealed class DockLayoutState
    {
        public List<DockPanelState> Panels { get; set; } = [];
    }

    private sealed class DockPanelState
    {
        public string Key { get; set; } = string.Empty;
        public string DockSide { get; set; } = "center";
        public bool Visible { get; set; }
        public int FixedWidth { get; set; }
        public List<string> Tabs { get; set; } = [];
        public string CurrentTabTitle { get; set; } = string.Empty;
    }

    private sealed class HiddenPanelState
    {
        public required Control Panel;
        public required DockingContainerControl LastContainer;
        public required string LastDockSide;
        public required string Title;
    }

    public override void _Ready()
    {
        _defaultLayoutState = CaptureLayoutState();
    }

    public bool IsHandleDragActive => _activeHandle is not null;

    public bool HidePanel(string position)
    {
        var targets = FindContainersByPosition(position);
        var changed = false;
        foreach (var target in targets)
        {
            if (!target.Visible)
            {
                continue;
            }

            target.Visible = false;
            changed = true;
        }

        if (changed)
        {
            QueueSort();
        }

        return changed;
    }

    public bool ShowPanel(string position)
    {
        var targets = FindContainersByPosition(position);
        var changed = false;
        foreach (var target in targets)
        {
            if (target.Visible)
            {
                continue;
            }

            target.Visible = true;
            changed = true;
        }

        if (changed)
        {
            QueueSort();
        }

        return changed;
    }

    public bool DockPanel(Control panel, string position)
    {
        var target = FindFirstContainerByPosition(position);
        if (target is null)
        {
            return false;
        }

        if (panel is DockingContainerControl dockContainer)
        {
            dockContainer.SetMeta(NodePropertyMapper.MetaDockSide, Variant.From(target.GetDockSide()));
            dockContainer.Visible = true;
            QueueSort();
            return true;
        }

        var source = FindContainerContaining(panel);
        if (source is not null)
        {
            source.RemoveDockTab(panel);
        }

        var title = panel.HasMeta("sml_ctx_tabTitle")
            ? panel.GetMeta("sml_ctx_tabTitle").AsString()
            : (string.IsNullOrWhiteSpace(panel.Name) ? "Panel" : panel.Name);

        target.AddDockTab(panel, title);
        target.Visible = true;
        QueueSort();
        return true;
    }

    public Window? UndockPanel(Control panel)
    {
        var source = FindContainerContaining(panel);
        if (source is null)
        {
            return null;
        }

        var title = panel.HasMeta("sml_ctx_tabTitle")
            ? panel.GetMeta("sml_ctx_tabTitle").AsString()
            : (string.IsNullOrWhiteSpace(panel.Name) ? "Panel" : panel.Name);

        source.RemoveDockTab(panel);

        var window = new Window
        {
            Name = $"Floating_{panel.Name}",
            Title = string.IsNullOrWhiteSpace(title) ? panel.Name : title,
            InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen,
            Size = new Vector2I(640, 420),
            Visible = false
        };

        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        window.AddChild(panel);

        if (GetTree()?.Root is not null)
        {
            GetTree().Root.AddChild(window);
        }

        window.CloseRequested += () =>
        {
            if (panel.GetParent() == window)
            {
                window.RemoveChild(panel);
            }

            source.AddDockTab(panel, title);
            source.Visible = true;
            QueueSort();
            window.QueueFree();
        };

        window.Show();
        return window;
    }

    public bool IsPanelVisible(Control panel)
    {
        if (panel is DockingContainerControl dock)
        {
            return dock.Visible;
        }

        var owner = FindContainerContaining(panel);
        if (owner is not null)
        {
            return owner.Visible && panel.Visible;
        }

        return panel.Visible;
    }

    public bool TogglePanel(string panelId)
    {
        return IsPanelVisibleById(panelId)
            ? HidePanelById(panelId)
            : ShowPanelById(panelId);
    }

    public bool MapMenuItemToPanel(string menuItemId, string panelId)
    {
        if (string.IsNullOrWhiteSpace(menuItemId) || string.IsNullOrWhiteSpace(panelId))
        {
            return false;
        }

        if (!ContainsPanelId(panelId))
        {
            return false;
        }

        _menuItemToPanel[menuItemId] = panelId;
        NotifyDockingManagersMenuMappingChanged();
        return true;
    }

    public bool UnmapMenuItemToPanel(string menuItemId)
    {
        if (string.IsNullOrWhiteSpace(menuItemId))
        {
            return false;
        }

        var removed = _menuItemToPanel.Remove(menuItemId);
        if (removed)
        {
            NotifyDockingManagersMenuMappingChanged();
        }

        return removed;
    }

    public bool TryResolveMappedPanelId(string menuItemId, out string panelId)
    {
        if (string.IsNullOrWhiteSpace(menuItemId))
        {
            panelId = string.Empty;
            return false;
        }

        return _menuItemToPanel.TryGetValue(menuItemId, out panelId!);
    }

    public Dictionary<string, string> GetMenuItemPanelMappings()
    {
        return new Dictionary<string, string>(_menuItemToPanel, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsMenuItemMappedToPanel(string menuItemId)
    {
        if (string.IsNullOrWhiteSpace(menuItemId)
            || Engine.GetMainLoop() is not SceneTree sceneTree
            || sceneTree.Root is null)
        {
            return false;
        }

        var stack = new Stack<Node>();
        stack.Push(sceneTree.Root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is DockingHostControl host
                && host.TryResolveMappedPanelId(menuItemId, out var panelId)
                && !string.IsNullOrWhiteSpace(panelId))
            {
                return true;
            }

            for (var i = current.GetChildCount() - 1; i >= 0; i--)
            {
                stack.Push(current.GetChild(i));
            }
        }

        return false;
    }

    private void NotifyDockingManagersMenuMappingChanged()
    {
        var tree = GetTree();
        if (tree is null)
        {
            return;
        }

        tree.CallGroup("docking_manager_nodes", "RequestRebuildPanelMenuBindings");
    }

    public bool IsPanelVisibleById(string panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId))
        {
            return false;
        }

        foreach (var container in GetDockContainers())
        {
            if (!container.Visible)
            {
                continue;
            }

            for (var i = 0; i < container.GetTabCount(); i++)
            {
                var panel = container.GetTabControl(i);
                if (panel is null || !HasPanelId(panel, panelId))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    public bool ContainsPanelId(string panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId))
        {
            return false;
        }

        foreach (var container in GetDockContainers())
        {
            for (var i = 0; i < container.GetTabCount(); i++)
            {
                var panel = container.GetTabControl(i);
                if (panel is not null && HasPanelId(panel, panelId))
                {
                    return true;
                }
            }
        }

        return _hiddenPanelsById.ContainsKey(panelId);
    }

    public bool HidePanelById(string panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId))
        {
            return false;
        }

        foreach (var container in GetDockContainers())
        {
            for (var i = 0; i < container.GetTabCount(); i++)
            {
                var panel = container.GetTabControl(i);
                if (panel is null || !HasPanelId(panel, panelId))
                {
                    continue;
                }

                var title = container.GetTabTitle(i);
                _hiddenPanelsById[panelId] = new HiddenPanelState
                {
                    Panel = panel,
                    LastContainer = container,
                    LastDockSide = container.GetDockSide(),
                    Title = string.IsNullOrWhiteSpace(title) ? panelId : title
                };

                container.RemoveDockTab(panel);
                if (container.GetTabCount() == 0)
                {
                    container.Visible = false;
                }

                QueueSort();
                return true;
            }
        }

        return false;
    }

    public bool ShowPanelById(string panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId))
        {
            return false;
        }

        if (!_hiddenPanelsById.TryGetValue(panelId, out var hidden))
        {
            return false;
        }

        if (!GodotObject.IsInstanceValid(hidden.Panel))
        {
            _hiddenPanelsById.Remove(panelId);
            return false;
        }

        DockingContainerControl? target = null;
        if (GodotObject.IsInstanceValid(hidden.LastContainer))
        {
            target = hidden.LastContainer;
        }
        else
        {
            target = FindFirstContainerByPosition(hidden.LastDockSide);
        }

        if (target is null)
        {
            _hiddenPanelsById.Remove(panelId);
            return false;
        }

        target.AddDockTab(hidden.Panel, hidden.Title);
        var index = target.GetTabIndexFromControl(hidden.Panel);
        target.SetCurrentTab(index >= 0 ? index : 0);
        target.Visible = true;

        _hiddenPanelsById.Remove(panelId);
        QueueSort();
        return true;
    }

    public bool IsPanelVisibleByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        foreach (var container in GetDockContainers())
        {
            if (!container.Visible)
            {
                continue;
            }

            for (var i = 0; i < container.GetTabCount(); i++)
            {
                if (!string.Equals(container.GetTabTitle(i), title, StringComparison.Ordinal))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    public bool HidePanelByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        foreach (var container in GetDockContainers())
        {
            for (var i = 0; i < container.GetTabCount(); i++)
            {
                if (!string.Equals(container.GetTabTitle(i), title, StringComparison.Ordinal))
                {
                    continue;
                }

                var panel = container.GetTabControl(i);
                if (panel is null)
                {
                    return false;
                }

                _hiddenPanelsByTitle[title] = new HiddenPanelState
                {
                    Panel = panel,
                    LastContainer = container,
                    LastDockSide = container.GetDockSide(),
                    Title = title
                };

                container.RemoveDockTab(panel);
                if (container.GetTabCount() == 0)
                {
                    container.Visible = false;
                }

                QueueSort();
                return true;
            }
        }

        return false;
    }

    public bool ShowPanelByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        if (!_hiddenPanelsByTitle.TryGetValue(title, out var hidden))
        {
            return false;
        }

        if (!GodotObject.IsInstanceValid(hidden.Panel))
        {
            _hiddenPanelsByTitle.Remove(title);
            return false;
        }

        DockingContainerControl? target = null;
        if (GodotObject.IsInstanceValid(hidden.LastContainer))
        {
            target = hidden.LastContainer;
        }
        else
        {
            target = FindFirstContainerByPosition(hidden.LastDockSide);
        }

        if (target is null)
        {
            _hiddenPanelsByTitle.Remove(title);
            return false;
        }

        target.AddDockTab(hidden.Panel, hidden.Title);
        var index = target.GetTabIndexFromControl(hidden.Panel);
        target.SetCurrentTab(index >= 0 ? index : 0);
        target.Visible = true;

        _hiddenPanelsByTitle.Remove(title);
        QueueSort();
        return true;
    }

    public bool TryExtractPanel(Control panel, out string title)
    {
        title = string.Empty;

        var source = FindContainerContaining(panel);
        if (source is null)
        {
            return false;
        }

        title = panel.HasMeta("sml_ctx_tabTitle")
            ? panel.GetMeta("sml_ctx_tabTitle").AsString()
            : (string.IsNullOrWhiteSpace(panel.Name) ? "Panel" : panel.Name);

        source.RemoveDockTab(panel);
        QueueSort();
        return true;
    }

    public bool ExtractPanel(Control panel)
    {
        return TryExtractPanel(panel, out _);
    }

    public bool SaveLayout(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var state = CaptureLayoutState();
        var path = GetLayoutPath(name);

        try
        {
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("UI", $"Failed to save docking layout '{name}'.", ex);
            return false;
        }
    }

    public bool LoadLayout(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var path = GetLayoutPath(name);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<DockLayoutState>(json);
            if (state is null)
            {
                return false;
            }

            ApplyLayoutState(state);
            return true;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("UI", $"Failed to load docking layout '{name}'.", ex);
            return false;
        }
    }

    public bool ResetLayout()
    {
        if (_defaultLayoutState is null)
        {
            return false;
        }

        ApplyLayoutState(_defaultLayoutState);
        return true;
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
        _rightGapByContainer.Clear();
        _widthLinkedByContainer.Clear();

        var all = new List<LayoutItem>();
        for (var i = 0; i < GetChildCount(); i++)
        {
            if (GetChild(i) is not DockingContainerControl child || !child.Visible)
            {
                continue;
            }

            var side = child.GetDockSideKind();
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
            RebuildHandles([], []);
            return;
        }

        var farLeftColumn = new DockColumn();
        var leftColumn = new DockColumn();
        var rightColumn = new DockColumn();
        var farRightColumn = new DockColumn();
        var centerColumns = new List<DockColumn>();

        foreach (var item in all)
        {
            switch (item.Side)
            {
                case DockingContainerControl.DockSideKind.FarLeft:
                    farLeftColumn.Top = item.Container;
                    farLeftColumn.Width = item.Width;
                    break;
                case DockingContainerControl.DockSideKind.FarLeftBottom:
                    farLeftColumn.Bottom = item.Container;
                    farLeftColumn.Width = item.Width;
                    break;
                case DockingContainerControl.DockSideKind.Left:
                    leftColumn.Top = item.Container;
                    leftColumn.Width = item.Width;
                    break;
                case DockingContainerControl.DockSideKind.LeftBottom:
                    leftColumn.Bottom = item.Container;
                    leftColumn.Width = item.Width;
                    break;
                case DockingContainerControl.DockSideKind.Right:
                    rightColumn.Top = item.Container;
                    rightColumn.Width = item.Width;
                    break;
                case DockingContainerControl.DockSideKind.RightBottom:
                    rightColumn.Bottom = item.Container;
                    rightColumn.Width = item.Width;
                    break;
                case DockingContainerControl.DockSideKind.FarRight:
                    farRightColumn.Top = item.Container;
                    farRightColumn.Width = item.Width;
                    break;
                case DockingContainerControl.DockSideKind.FarRightBottom:
                    farRightColumn.Bottom = item.Container;
                    farRightColumn.Width = item.Width;
                    break;
                default:
                    centerColumns.Add(new DockColumn
                    {
                        Top = item.Container,
                        IsFlex = item.Flex,
                        Width = item.Width
                    });
                    break;
            }
        }

        var gap = 0f;
        if (HasMeta(NodePropertyMapper.MetaDockGap))
        {
            gap = Math.Max(0, GetMeta(NodePropertyMapper.MetaDockGap).AsInt32());
        }

        var endGap = 0f;
        if (HasMeta(NodePropertyMapper.MetaDockEndGap))
        {
            endGap = Math.Max(0, GetMeta(NodePropertyMapper.MetaDockEndGap).AsInt32());
        }

        var widthTotal = Size.X;
        var heightTotal = Size.Y;
        var orderedColumns = new List<DockColumn>();
        if (farLeftColumn.HasVisible)
        {
            orderedColumns.Add(farLeftColumn);
        }

        if (leftColumn.HasVisible)
        {
            orderedColumns.Add(leftColumn);
        }

        foreach (var center in centerColumns)
        {
            if (center.HasVisible)
            {
                orderedColumns.Add(center);
            }
        }

        if (rightColumn.HasVisible)
        {
            orderedColumns.Add(rightColumn);
        }

        if (farRightColumn.HasVisible)
        {
            orderedColumns.Add(farRightColumn);
        }

        var visibleCount = orderedColumns.Count;
        var regularGaps = Math.Max(0, visibleCount - 1) * gap;

        var centerFixed = 0f;
        var centerFlexCount = 0;
        DockColumn? firstFlex = null;
        var nonCenterFixed = 0f;

        foreach (var column in orderedColumns)
        {
            if (centerColumns.Contains(column) && column.IsFlex)
            {
                firstFlex ??= column;
                centerFlexCount++;
                continue;
            }

            if (centerColumns.Contains(column))
            {
                centerFixed += column.Width;
            }
            else
            {
                nonCenterFixed += column.Width;
            }
        }

        if (centerFlexCount > 1)
        {
            GD.PushWarning($"[UI] DockingHost '{Name}' has {centerFlexCount} flex center containers. Only the first will behave as flex.");
            centerFlexCount = 1;
        }

        var rest = widthTotal - nonCenterFixed - centerFixed - regularGaps - endGap;
        var flexWidth = centerFlexCount > 0 ? Math.Max(0, rest / centerFlexCount) : 0f;

        var gapSegments = new List<GapSegment>();
        var verticalGapSegments = new List<VerticalGapSegment>();
        var cursorX = 0f;
        for (var i = 0; i < orderedColumns.Count; i++)
        {
            var column = orderedColumns[i];
            var useFlex = firstFlex is not null && ReferenceEquals(column, firstFlex);
            var itemWidth = useFlex ? flexWidth : column.Width;

            var vgap = LayoutColumn(column, cursorX, itemWidth, heightTotal, gap);
            if (vgap is not null)
            {
                verticalGapSegments.Add(vgap);
            }
            RegisterWidthLinked(column);

            var leftNeighbor = column.PrimaryVisible;
            cursorX += itemWidth;

            var gapWidth = i == orderedColumns.Count - 1 ? endGap : gap;
            if (gapWidth > 0)
            {
                var rect = new Rect2(cursorX, 0, gapWidth, Math.Max(0, heightTotal));
                foreach (var visible in column.VisibleContainers())
                {
                    _rightGapByContainer[visible.GetInstanceId()] = rect;
                }

                gapSegments.Add(new GapSegment
                {
                    Rect = rect,
                    LeftNeighbor = leftNeighbor,
                    RightNeighbor = i < orderedColumns.Count - 1 ? orderedColumns[i + 1].PrimaryVisible : null
                });
                cursorX += gapWidth;
            }
        }

        RebuildHandles(gapSegments, verticalGapSegments);
    }

    private static VerticalGapSegment? LayoutColumn(DockColumn column, float x, float width, float hostHeight, float gap)
    {
        var clampedWidth = Math.Max(0, width);
        var clampedHeight = Math.Max(0, hostHeight);

        var hasTop = column.HasVisibleTop;
        var hasBottom = column.HasVisibleBottom;

        if (hasTop && hasBottom && column.Top is not null && column.Bottom is not null)
        {
            var topHeight = ResolveTopSplitHeight(column.Top, column.Bottom, clampedHeight);
            // Reserve space for the vertical gap handle
            var gapH = Math.Max(0f, gap);
            topHeight = Math.Max(0f, topHeight - gapH / 2f);
            var bottomHeight = Math.Max(0f, clampedHeight - topHeight - gapH);
            var topRect    = new Rect2(x, 0,                      clampedWidth, topHeight);
            var gapRect    = new Rect2(x, topHeight,               clampedWidth, gapH);
            var bottomRect = new Rect2(x, topHeight + gapH,        clampedWidth, bottomHeight);
            column.Top.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
            column.Bottom.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
            column.Top.Position    = topRect.Position;
            column.Top.Size        = topRect.Size;
            column.Bottom.Position = bottomRect.Position;
            column.Bottom.Size     = bottomRect.Size;
            return new VerticalGapSegment
            {
                Rect          = gapRect,
                TopNeighbor    = column.Top,
                BottomNeighbor = column.Bottom
            };
        }

        var single = column.PrimaryVisible;
        if (single is not null)
        {
            single.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
            single.Position = new Vector2(x, 0);
            single.Size = new Vector2(clampedWidth, clampedHeight);
        }

        return null;
    }

    private static float ResolveTopSplitHeight(DockingContainerControl top, DockingContainerControl bottom, float totalHeight)
    {
        var clampedTotal = Math.Max(0f, totalHeight);
        if (clampedTotal <= 0f)
        {
            return 0f;
        }

        var minTop = top.GetMinFixedHeight();
        var minBottom = bottom.GetMinFixedHeight();
        var maxTop = Math.Max(0f, clampedTotal - minBottom);
        var minTopClamped = Math.Min(minTop, maxTop);

        float desiredTop;
        if (top.HasFixedHeight())
        {
            desiredTop = top.GetFixedHeight();
        }
        else if (top.HasHeightPercent())
        {
            desiredTop = clampedTotal * (top.GetHeightPercent() / 100f);
        }
        else if (bottom.HasFixedHeight())
        {
            desiredTop = clampedTotal - bottom.GetFixedHeight();
        }
        else if (bottom.HasHeightPercent())
        {
            desiredTop = clampedTotal * (1f - (bottom.GetHeightPercent() / 100f));
        }
        else
        {
            desiredTop = clampedTotal * 0.5f;
        }

        var topHeight = Mathf.Clamp(desiredTop, minTopClamped, maxTop);
        var bottomHeight = clampedTotal - topHeight;
        if (bottomHeight < minBottom)
        {
            bottomHeight = minBottom;
            topHeight = Math.Max(0f, clampedTotal - bottomHeight);
        }

        return topHeight;
    }

    private void RegisterWidthLinked(DockColumn column)
    {
        var visible = new List<DockingContainerControl>();
        foreach (var item in column.VisibleContainers())
        {
            visible.Add(item);
        }

        if (visible.Count <= 1)
        {
            return;
        }

        foreach (var container in visible)
        {
            _widthLinkedByContainer[container.GetInstanceId()] = visible;
        }
    }

    public bool TryGetRightGapRect(DockingContainerControl container, out Rect2 rect)
    {
        return _rightGapByContainer.TryGetValue(container.GetInstanceId(), out rect);
    }

    private void RepositionVerticalHandles(List<VerticalGapSegment> verticalSegments)
    {
        for (var i = 0; i < _verticalHandles.Count && i < verticalSegments.Count; i++)
        {
            var handle = _verticalHandles[i];
            var seg    = verticalSegments[i];
            if (!GodotObject.IsInstanceValid(handle))
            {
                continue;
            }

            handle.Position = seg.Rect.Position;
            handle.Size     = seg.Rect.Size;
        }
    }

    private void RebuildHandles(List<GapSegment> segments, List<VerticalGapSegment> verticalSegments)
    {
        if (_activeVerticalHandle is not null)
        {
            // Keep active vertical drag handle stable while dragging.
            return;
        }

        if (_activeHandle is not null)
        {
            // During horizontal drag: reposition vertical handles live without rebuilding them.
            RepositionVerticalHandles(verticalSegments);
            return;
        }

        foreach (var handle in _handles)
        {
            if (GodotObject.IsInstanceValid(handle))
            {
                if (handle.GetParent() is not null)
                {
                    handle.GetParent().RemoveChild(handle);
                }

                handle.Free();
            }
        }

        _handles.Clear();
        _activeHandle = null;
        _activeTargetContainer = null;

        // Rebuild vertical (top/bottom) handles
        foreach (var handle in _verticalHandles)
        {
            if (GodotObject.IsInstanceValid(handle))
            {
                if (handle.GetParent() is not null)
                {
                    handle.GetParent().RemoveChild(handle);
                }

                handle.Free();
            }
        }

        _verticalHandles.Clear();
        _activeVerticalHandle = null;
        _activeVerticalTarget = null;

        var interiorGapCount = 0;
        foreach (var segment in segments)
        {
            // One handle per *interior* gap (between two panels).
            if (segment.LeftNeighbor is null || segment.RightNeighbor is null)
            {
                continue;
            }

            interiorGapCount++;

            if (segment.Rect.Size.X <= 0)
            {
                continue;
            }

            // Use full-height gap handle (no split, no menu-button cutout).
            var fullGapRect = new Rect2(segment.Rect.Position.X, segment.Rect.Position.Y, segment.Rect.Size.X, segment.Rect.Size.Y);
            AddResizeHandle(
                leftNeighbor: segment.LeftNeighbor,
                rightNeighbor: segment.RightNeighbor,
                rect: fullGapRect,
                suffix: "Full");
        }

        foreach (var vsegment in verticalSegments)
        {
            if (vsegment.Rect.Size.Y <= 0)
            {
                continue;
            }

            AddVerticalResizeHandle(vsegment.TopNeighbor, vsegment.BottomNeighbor, vsegment.Rect);
        }

        _lastLoggedInteriorGapCount = interiorGapCount;
        _lastLoggedHandleCount = _handles.Count;
    }

    private void AddResizeHandle(DockingContainerControl leftNeighbor, DockingContainerControl rightNeighbor, Rect2 rect, string suffix)
    {
        var handle = new DockResizeHandleControl
        {
            Name = $"DockResizeHandle_{leftNeighbor.Name}_{rightNeighbor.Name}_{suffix}",
            Color = HandleVisibleColor,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.Hsize,
            ZIndex = 900,
            LeftNeighbor = leftNeighbor,
            RightNeighbor = rightNeighbor,
            InputHandler = OnHandleInput
        };

        handle.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        handle.Position = rect.Position;
        handle.Size = rect.Size;

        AddChild(handle);
        _handles.Add(handle);
    }

    private void OnHandleInput(DockResizeHandleControl handle, InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                var localX = mb.Position.X;
                var preferLeft = localX <= (handle.Size.X * 0.5f);
                var gapCenterX = handle.Position.X + (handle.Size.X * 0.5f);
                var hostCenterX = Size.X * 0.5f;
                var preferRightByHostSide = gapCenterX > hostCenterX;

                var left = handle.LeftNeighbor;
                var right = handle.RightNeighbor;

                DockingContainerControl? target = null;
                var usesPositiveDelta = true;

                // Deterministic side-priority by host half:
                // left-half gaps -> left neighbor, right-half gaps -> right neighbor.
                if (!preferRightByHostSide)
                {
                    if (left is not null && !left.IsFlex())
                    {
                        target = left;
                        usesPositiveDelta = true;
                    }
                    else if (right is not null && !right.IsFlex())
                    {
                        target = right;
                        usesPositiveDelta = false;
                    }
                }
                else
                {
                    if (right is not null && !right.IsFlex())
                    {
                        target = right;
                        usesPositiveDelta = false;
                    }
                    else if (left is not null && !left.IsFlex())
                    {
                        target = left;
                        usesPositiveDelta = true;
                    }
                }

                if (target is null)
                {
                    RunnerLogger.Warn(
                        "UI",
                        $"DockingHost '{Name}': drag-start rejected gap='{handle.Name}' (no resizable target).");
                    return;
                }

                _activeHandle = handle;
                _activeTargetContainer = target;
                _activeTargetUsesPositiveDelta = usesPositiveDelta;
                _dragStartMouseX = GetGlobalMousePosition().X;
                _dragStartWidth = target.GetFixedWidth();

                // Hide all current gap handles while dragging to avoid stale-gap visuals.
                foreach (var existingHandle in _handles)
                {
                    if (GodotObject.IsInstanceValid(existingHandle))
                    {
                        existingHandle.Color = HandleHiddenColor;
                    }
                }

                AcceptEvent();
            }
            else if (_activeHandle == handle)
            {
                _activeHandle = null;
                _activeTargetContainer = null;

                foreach (var existingHandle in _handles)
                {
                    if (GodotObject.IsInstanceValid(existingHandle))
                    {
                        existingHandle.Color = HandleVisibleColor;
                    }
                }

                QueueSort();
                AcceptEvent();
            }

            return;
        }

        if (@event is not InputEventMouseMotion || _activeHandle != handle)
        {
            return;
        }

        var mouseX = GetGlobalMousePosition().X;
        var delta = mouseX - _dragStartMouseX;
        if (_activeTargetContainer is null)
        {
            return;
        }

        var proposedWidth = _activeTargetUsesPositiveDelta
            ? _dragStartWidth + delta
            : _dragStartWidth - delta;

        _activeTargetContainer.SetFixedWidth(proposedWidth);
        ApplyLinkedWidth(_activeTargetContainer, _activeTargetContainer.GetFixedWidth());

        QueueSort();
        AcceptEvent();
    }

    private void ApplyLinkedWidth(DockingContainerControl source, float width)
    {
        if (!_widthLinkedByContainer.TryGetValue(source.GetInstanceId(), out var linked))
        {
            return;
        }

        foreach (var container in linked)
        {
            if (ReferenceEquals(container, source))
            {
                continue;
            }

            container.SetFixedWidth(width);
        }
    }

    private float ResolveActiveGapX(DockResizeHandleControl handle, bool usesPositiveDelta, DockingContainerControl activeTarget)
    {
        DockingContainerControl? gapOwner = usesPositiveDelta ? activeTarget : handle.LeftNeighbor;
        if (gapOwner is null)
        {
            return -1f;
        }

        if (_rightGapByContainer.TryGetValue(gapOwner.GetInstanceId(), out var rect))
        {
            return rect.Position.X;
        }

        return -1f;
    }

    private DockLayoutState CaptureLayoutState()
    {
        var containers = GetDockContainers();
        var state = new DockLayoutState();
        foreach (var container in containers)
        {
            var tabs = new List<string>();
            for (var i = 0; i < container.GetTabCount(); i++)
            {
                tabs.Add(container.GetTabTitle(i));
            }

            var currentTitle = string.Empty;
            var currentIndex = container.GetCurrentTab();
            if (currentIndex >= 0 && currentIndex < tabs.Count)
            {
                currentTitle = tabs[currentIndex];
            }

            state.Panels.Add(new DockPanelState
            {
                Key = ResolveContainerKey(container),
                DockSide = container.GetDockSide(),
                Visible = container.Visible,
                FixedWidth = (int)MathF.Round(container.GetFixedWidth()),
                Tabs = tabs,
                CurrentTabTitle = currentTitle
            });
        }

        return state;
    }

    private void ApplyLayoutState(DockLayoutState state)
    {
        var containers = GetDockContainers();
        var byKey = new Dictionary<string, DockingContainerControl>(StringComparer.Ordinal);
        foreach (var container in containers)
        {
            byKey[ResolveContainerKey(container)] = container;
        }

        foreach (var panel in state.Panels)
        {
            if (!byKey.TryGetValue(panel.Key, out var target))
            {
                continue;
            }

            target.SetMeta(NodePropertyMapper.MetaDockSide, Variant.From(panel.DockSide));
            target.SetFixedWidth(panel.FixedWidth);
            target.Visible = panel.Visible;
        }

        var titleMap = new Dictionary<string, (DockingContainerControl Container, Control Control)>(StringComparer.Ordinal);
        foreach (var container in containers)
        {
            for (var i = 0; i < container.GetTabCount(); i++)
            {
                var title = container.GetTabTitle(i);
                var control = container.GetTabControl(i);
                if (string.IsNullOrWhiteSpace(title) || control is null || titleMap.ContainsKey(title))
                {
                    continue;
                }

                titleMap[title] = (container, control);
            }
        }

        foreach (var panel in state.Panels)
        {
            if (!byKey.TryGetValue(panel.Key, out var target))
            {
                continue;
            }

            foreach (var title in panel.Tabs)
            {
                if (!titleMap.TryGetValue(title, out var source))
                {
                    continue;
                }

                if (ReferenceEquals(source.Container, target))
                {
                    continue;
                }

                source.Container.RemoveDockTab(source.Control);
                target.AddDockTab(source.Control, title);
                titleMap[title] = (target, source.Control);
            }

            if (!string.IsNullOrWhiteSpace(panel.CurrentTabTitle))
            {
                for (var i = 0; i < target.GetTabCount(); i++)
                {
                    if (string.Equals(target.GetTabTitle(i), panel.CurrentTabTitle, StringComparison.Ordinal))
                    {
                        target.SetCurrentTab(i);
                        break;
                    }
                }
            }
        }

        QueueSort();
    }

    private List<DockingContainerControl> GetDockContainers()
    {
        var result = new List<DockingContainerControl>();
        for (var i = 0; i < GetChildCount(); i++)
        {
            if (GetChild(i) is DockingContainerControl container)
            {
                result.Add(container);
            }
        }

        return result;
    }

    private DockingContainerControl? FindContainerContaining(Control panel)
    {
        var containers = GetDockContainers();
        foreach (var container in containers)
        {
            for (var i = 0; i < container.GetTabCount(); i++)
            {
                if (!ReferenceEquals(container.GetTabControl(i), panel))
                {
                    continue;
                }

                return container;
            }
        }

        return null;
    }

    private DockingContainerControl? FindFirstContainerByPosition(string position)
    {
        var normalized = NormalizePosition(position);
        var containers = GetDockContainers();
        foreach (var container in containers)
        {
            if (string.Equals(container.GetDockSide(), normalized, StringComparison.OrdinalIgnoreCase))
            {
                return container;
            }
        }

        return null;
    }

    private List<DockingContainerControl> FindContainersByPosition(string position)
    {
        var normalized = NormalizePosition(position);
        var result = new List<DockingContainerControl>();
        var containers = GetDockContainers();
        foreach (var container in containers)
        {
            if (!string.Equals(container.GetDockSide(), normalized, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(container);
        }

        return result;
    }

    private string GetLayoutPath(string name)
    {
        var hostKey = ResolveHostKey();
        var safeName = name.Trim().Replace("/", "_").Replace("\\", "_");
        var relative = $"user://docking/layouts/{hostKey}_{safeName}.json";
        return ProjectSettings.GlobalizePath(relative);
    }

    private string ResolveHostKey()
    {
        if (HasMeta(NodePropertyMapper.MetaId))
        {
            var id = GetMeta(NodePropertyMapper.MetaId).AsString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        return string.IsNullOrWhiteSpace(Name) ? "dockHost" : Name;
    }

    private static string ResolveContainerKey(DockingContainerControl container)
    {
        if (container.HasMeta(NodePropertyMapper.MetaId))
        {
            var id = container.GetMeta(NodePropertyMapper.MetaId).AsString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        return container.Name;
    }

    private static string NormalizePosition(string position)
    {
        return (position ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static bool HasPanelId(Control panel, string panelId)
    {
        if (panel.HasMeta(NodePropertyMapper.MetaId))
        {
            var id = panel.GetMeta(NodePropertyMapper.MetaId).AsString();
            if (string.Equals(id, panelId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return string.Equals(panel.Name, panelId, StringComparison.OrdinalIgnoreCase);
    }
}
