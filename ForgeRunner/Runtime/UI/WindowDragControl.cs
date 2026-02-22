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

namespace Runtime.UI;

/// <summary>
/// Caption-like drag surface for frameless/extend-to-title layouts.
/// - Left click starts native window drag.
/// - Left double-click toggles maximize/restore.
/// </summary>
public sealed partial class WindowDragControl : Panel
{
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;

        // Keep drag surface visually transparent / borderless.
        AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton
            || mouseButton.ButtonIndex != MouseButton.Left
            || !mouseButton.Pressed)
        {
            return;
        }

        var hostWindow = GetWindow();
        if (hostWindow is null)
        {
            return;
        }

        if (mouseButton.DoubleClick)
        {
            ToggleMaximizeRestore(hostWindow);
            AcceptEvent();
            return;
        }

        hostWindow.StartDrag();
        AcceptEvent();
    }

    private static void ToggleMaximizeRestore(Window window)
    {
        if (window.Mode == Window.ModeEnum.Maximized)
        {
            window.Mode = Window.ModeEnum.Windowed;
            return;
        }

        if (window.Mode == Window.ModeEnum.Windowed)
        {
            window.Mode = Window.ModeEnum.Maximized;
        }
    }
}
