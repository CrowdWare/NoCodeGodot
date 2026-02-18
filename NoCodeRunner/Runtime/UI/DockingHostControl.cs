using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;

namespace Runtime.UI;

public sealed partial class DockingHostControl : Container
{
    private const float HandleTopInset = 30f;
    private const float DefaultHandleWidth = 6f;
    private static readonly Color HandleVisibleColor = new(1f, 1f, 1f, 0.18f);
    private static readonly Color HandleHiddenColor = new(1f, 1f, 1f, 0f);

    private sealed class LayoutItem
    {
        public required DockingContainerControl Container;
        public required string Side;
        public required bool Flex;
        public required float Width;
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
    private readonly List<DockResizeHandleControl> _handles = [];
    private int _lastLoggedHandleCount = -1;
    private int _lastLoggedInteriorGapCount = -1;
    private DockResizeHandleControl? _activeHandle;
    private DockingContainerControl? _activeTargetContainer;
    private bool _activeTargetUsesPositiveDelta;
    private float _dragStartMouseX;
    private float _dragStartWidth;
    private int _lastLoggedDragWidth = -1;

    public bool IsHandleDragActive => _activeHandle is not null;

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
            RebuildHandles([]);
            return;
        }

        var left = new List<LayoutItem>();
        var farLeft = new List<LayoutItem>();
        var center = new List<LayoutItem>();
        var right = new List<LayoutItem>();
        var farRight = new List<LayoutItem>();

        foreach (var item in all)
        {
            switch (item.Side)
            {
                case "farleft":
                    farLeft.Add(item);
                    break;
                case "left":
                    left.Add(item);
                    break;
                case "farright":
                    farRight.Add(item);
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

        var endGap = gap;
        if (HasMeta(NodePropertyMapper.MetaDockEndGap))
        {
            endGap = Math.Max(0, GetMeta(NodePropertyMapper.MetaDockEndGap).AsInt32());
        }

        var widthTotal = Size.X;
        var heightTotal = Size.Y;
        var visibleCount = all.Count;
        var regularGaps = Math.Max(0, visibleCount - 1) * gap;

        var leftFixed = 0f;
        foreach (var item in farLeft)
        {
            leftFixed += item.Width;
        }
        foreach (var item in left)
        {
            leftFixed += item.Width;
        }

        var rightFixed = 0f;
        foreach (var item in right)
        {
            rightFixed += item.Width;
        }
        foreach (var item in farRight)
        {
            rightFixed += item.Width;
        }

        var centerFixed = 0f;
        var centerFlexCount = 0;
        LayoutItem? firstFlex = null;
        foreach (var item in center)
        {
            if (item.Flex)
            {
                firstFlex ??= item;
                centerFlexCount++;
            }
            else
            {
                centerFixed += item.Width;
            }
        }

        if (centerFlexCount > 1)
        {
            GD.PushWarning($"[UI] DockingHost '{Name}' has {centerFlexCount} flex center containers. Only the first will behave as flex.");
            centerFlexCount = 1;
        }

        var rest = widthTotal - leftFixed - rightFixed - centerFixed - regularGaps - endGap;
        var flexWidth = centerFlexCount > 0 ? Math.Max(0, rest / centerFlexCount) : 0f;

        var ordered = new List<LayoutItem>(all.Count);
        ordered.AddRange(farLeft);
        ordered.AddRange(left);
        ordered.AddRange(center);
        ordered.AddRange(right);
        ordered.AddRange(farRight);

        var gapSegments = new List<GapSegment>();
        var cursorX = 0f;
        for (var i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];
            var useFlex = firstFlex is not null && ReferenceEquals(item, firstFlex);
            var itemWidth = useFlex ? flexWidth : item.Width;
            FitChildInRect(item.Container, new Rect2(cursorX, 0, Math.Max(0, itemWidth), Math.Max(0, heightTotal)));
            cursorX += itemWidth;

            var gapWidth = i == ordered.Count - 1 ? endGap : gap;
            if (gapWidth > 0)
            {
                var rect = new Rect2(cursorX, 0, gapWidth, Math.Max(0, heightTotal));
                _rightGapByContainer[item.Container.GetInstanceId()] = rect;
                gapSegments.Add(new GapSegment
                {
                    Rect = rect,
                    LeftNeighbor = item.Container,
                    RightNeighbor = i < ordered.Count - 1 ? ordered[i + 1].Container : null
                });
                cursorX += gapWidth;
            }
        }

        RebuildHandles(gapSegments);
    }

    public bool TryGetRightGapRect(DockingContainerControl container, out Rect2 rect)
    {
        return _rightGapByContainer.TryGetValue(container.GetInstanceId(), out rect);
    }

    private void RebuildHandles(List<GapSegment> segments)
    {
        if (_activeHandle is not null)
        {
            // Keep active drag handle stable while dragging.
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

            // Use full gap width as click area so left/right decision is stable.
            var fullGapRect = new Rect2(segment.Rect.Position.X, segment.Rect.Position.Y, segment.Rect.Size.X, segment.Rect.Size.Y);
            AddResizeHandle(
                leftNeighbor: segment.LeftNeighbor,
                rightNeighbor: segment.RightNeighbor,
                rect: BuildHandleRect(fullGapRect));
        }

        LogHandleCountIfChanged(interiorGapCount, _handles.Count);
    }

    private void LogHandleCountIfChanged(int interiorGapCount, int handleCount)
    {
        if (_lastLoggedInteriorGapCount == interiorGapCount && _lastLoggedHandleCount == handleCount)
        {
            return;
        }

        _lastLoggedInteriorGapCount = interiorGapCount;
        _lastLoggedHandleCount = handleCount;
        RunnerLogger.Info("UI", $"DockingHost '{Name}': interiorGaps={interiorGapCount}, handles={handleCount}");
    }

    private static Rect2 BuildHandleRect(Rect2 rect)
    {
        var top = Math.Min(HandleTopInset, rect.Size.Y);
        var height = Math.Max(0f, rect.Size.Y - top);
        return new Rect2(rect.Position.X, top, rect.Size.X, height);
    }

    private void AddResizeHandle(DockingContainerControl leftNeighbor, DockingContainerControl rightNeighbor, Rect2 rect)
    {
        var handle = new DockResizeHandleControl
        {
            Name = $"DockResizeHandle_{leftNeighbor.Name}_{rightNeighbor.Name}",
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

                RunnerLogger.Info(
                    "UI",
                    $"DockingHost '{Name}': drag-click gap='{handle.Name}', localX={localX:F1}, handleW={handle.Size.X:F1}, preferLeft={preferLeft}, gapCenterX={gapCenterX:F1}, hostCenterX={hostCenterX:F1}, preferRightByHostSide={preferRightByHostSide}, left='{left?.Name}', right='{right?.Name}'");

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
                _lastLoggedDragWidth = -1;

                // Hide all current gap handles while dragging to avoid stale-gap visuals.
                foreach (var existingHandle in _handles)
                {
                    if (GodotObject.IsInstanceValid(existingHandle))
                    {
                        existingHandle.Color = HandleHiddenColor;
                    }
                }

                RunnerLogger.Info(
                    "UI",
                    $"DockingHost '{Name}': drag-start gap='{handle.Name}', target='{target.Name}', mode={(usesPositiveDelta ? "left-priority" : "right-priority")}, startWidth={_dragStartWidth:F1}");
                AcceptEvent();
            }
            else if (_activeHandle == handle)
            {
                if (_activeTargetContainer is not null)
                {
                    var gapX = ResolveActiveGapX(handle, _activeTargetUsesPositiveDelta, _activeTargetContainer);
                    RunnerLogger.Info(
                        "UI",
                        $"DockingHost '{Name}': drag-end gap='{handle.Name}', target='{_activeTargetContainer.Name}', width={_activeTargetContainer.GetFixedWidth():F1}, gapX={gapX:F1}");
                }

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
        var currentWidthInt = (int)MathF.Round(_activeTargetContainer.GetFixedWidth());
        if (currentWidthInt != _lastLoggedDragWidth)
        {
            _lastLoggedDragWidth = currentWidthInt;
            var gapX = ResolveActiveGapX(handle, _activeTargetUsesPositiveDelta, _activeTargetContainer);
            RunnerLogger.Info(
                "UI",
                $"DockingHost '{Name}': dragging gap='{handle.Name}', target='{_activeTargetContainer.Name}', mouseX={mouseX:F1}, width={_activeTargetContainer.GetFixedWidth():F1}, gapX={gapX:F1}");
        }

        QueueSort();
        AcceptEvent();
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
}
