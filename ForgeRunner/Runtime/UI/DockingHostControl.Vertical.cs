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

namespace Runtime.UI;

public sealed partial class DockingHostControl
{
    private sealed class VerticalGapSegment
    {
        public required Rect2 Rect;
        public required DockingContainerControl TopNeighbor;
        public required DockingContainerControl BottomNeighbor;
    }

    private readonly List<DockResizeHandleControl> _verticalHandles = [];
    private DockResizeHandleControl? _activeVerticalHandle;
    private DockingContainerControl? _activeVerticalTarget;
    private float _dragStartMouseY;
    private float _dragStartHeight;

    private void AddVerticalResizeHandle(DockingContainerControl topNeighbor, DockingContainerControl bottomNeighbor, Rect2 rect)
    {
        var handle = new DockResizeHandleControl
        {
            Name = $"DockVResizeHandle_{topNeighbor.Name}_{bottomNeighbor.Name}",
            Color = HandleVisibleColor,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.Vsize,
            ZIndex = 900,
            LeftNeighbor  = topNeighbor,
            RightNeighbor = bottomNeighbor,
            InputHandler  = OnVerticalHandleInput
        };

        handle.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        handle.Position = rect.Position;
        handle.Size     = rect.Size;

        AddChild(handle);
        _verticalHandles.Add(handle);
    }

    private void OnVerticalHandleInput(DockResizeHandleControl handle, InputEvent @event)
    {
        var topContainer    = handle.LeftNeighbor;
        var bottomContainer = handle.RightNeighbor;

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                if (topContainer is null || bottomContainer is null)
                {
                    return;
                }

                _activeVerticalHandle = handle;
                _activeVerticalTarget = topContainer;
                _dragStartMouseY  = GetGlobalMousePosition().Y;
                _dragStartHeight  = topContainer.Size.Y;

                foreach (var h in _verticalHandles)
                {
                    if (GodotObject.IsInstanceValid(h))
                    {
                        h.Color = HandleHiddenColor;
                    }
                }

                AcceptEvent();
            }
            else if (_activeVerticalHandle == handle)
            {
                _activeVerticalHandle = null;
                _activeVerticalTarget = null;

                foreach (var h in _verticalHandles)
                {
                    if (GodotObject.IsInstanceValid(h))
                    {
                        h.Color = HandleVisibleColor;
                    }
                }

                QueueSort();
                AcceptEvent();
            }

            return;
        }

        if (@event is not InputEventMouseMotion || _activeVerticalHandle != handle)
        {
            return;
        }

        if (_activeVerticalTarget is null || bottomContainer is null)
        {
            return;
        }

        var mouseY = GetGlobalMousePosition().Y;
        var delta  = mouseY - _dragStartMouseY;
        var totalH = _activeVerticalTarget.Size.Y + bottomContainer.Size.Y;
        var minBottom = bottomContainer.GetMinFixedHeight() > 0 ? bottomContainer.GetMinFixedHeight() : 40f;
        var minTop    = _activeVerticalTarget.GetMinFixedHeight() > 0 ? _activeVerticalTarget.GetMinFixedHeight() : 40f;

        var newTopHeight = Mathf.Clamp(_dragStartHeight + delta, minTop, Math.Max(minTop, totalH - minBottom));

        _activeVerticalTarget.SetFixedHeight(newTopHeight);
        QueueSort();
        AcceptEvent();
    }
}
