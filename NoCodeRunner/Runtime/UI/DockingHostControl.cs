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
            RebuildHandles([]);
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
        var cursorX = 0f;
        for (var i = 0; i < orderedColumns.Count; i++)
        {
            var column = orderedColumns[i];
            var useFlex = firstFlex is not null && ReferenceEquals(column, firstFlex);
            var itemWidth = useFlex ? flexWidth : column.Width;

            LayoutColumn(column, cursorX, itemWidth, heightTotal);
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

        RebuildHandles(gapSegments);
    }

    private static void LayoutColumn(DockColumn column, float x, float width, float hostHeight)
    {
        var clampedWidth = Math.Max(0, width);
        var clampedHeight = Math.Max(0, hostHeight);

        var hasTop = column.HasVisibleTop;
        var hasBottom = column.HasVisibleBottom;

        if (hasTop && hasBottom && column.Top is not null && column.Bottom is not null)
        {
            var half = clampedHeight * 0.5f;
            var topRect = new Rect2(x, 0, clampedWidth, half);
            var bottomRect = new Rect2(x, half, clampedWidth, clampedHeight - half);
            column.Top.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
            column.Bottom.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
            column.Top.Position = topRect.Position;
            column.Top.Size = topRect.Size;
            column.Bottom.Position = bottomRect.Position;
            column.Bottom.Size = bottomRect.Size;
            return;
        }

        var single = column.PrimaryVisible;
        if (single is not null)
        {
            single.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
            single.Position = new Vector2(x, 0);
            single.Size = new Vector2(clampedWidth, clampedHeight);
        }
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
            var (topRect, bottomRect) = BuildHandleRects(fullGapRect);
            if (topRect is Rect2 t)
            {
                AddResizeHandle(
                    leftNeighbor: segment.LeftNeighbor,
                    rightNeighbor: segment.RightNeighbor,
                    rect: t,
                    suffix: "Top");
            }

            if (bottomRect is Rect2 b)
            {
                AddResizeHandle(
                    leftNeighbor: segment.LeftNeighbor,
                    rightNeighbor: segment.RightNeighbor,
                    rect: b,
                    suffix: "Bottom");
            }
        }

        _lastLoggedInteriorGapCount = interiorGapCount;
        _lastLoggedHandleCount = _handles.Count;
    }

    private static (Rect2? Top, Rect2? Bottom) BuildHandleRects(Rect2 rect)
    {
        var topInset = Math.Min(HandleTopInset, rect.Size.Y);
        var usableHeight = Math.Max(0f, rect.Size.Y - topInset);
        if (usableHeight <= 0f)
        {
            return (null, null);
        }

        // Split every gap-handle into upper and lower segment so middle area stays clickable for menu buttons.
        // If there is too little height, fall back to one segment in the upper area.
        if (usableHeight < 48f)
        {
            return (new Rect2(rect.Position.X, topInset, rect.Size.X, usableHeight), null);
        }

        var half = usableHeight * 0.5f;
        var separator = Math.Min(28f, half * 0.6f);
        var topHeight = Math.Max(12f, half - (separator * 0.5f));
        var bottomY = rect.Position.Y + topInset + topHeight + separator;
        var bottomHeight = Math.Max(12f, rect.Position.Y + rect.Size.Y - bottomY);

        var topRect = new Rect2(rect.Position.X, rect.Position.Y + topInset, rect.Size.X, topHeight);
        var bottomRect = new Rect2(rect.Position.X, bottomY, rect.Size.X, bottomHeight);
        return (topRect, bottomRect);
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
}
