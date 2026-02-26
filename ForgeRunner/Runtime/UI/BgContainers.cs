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
/// VBoxContainer with optional background drawn via _Draw(),
/// enabling bgColor/borderColor/borderWidth/borderRadius support.
/// </summary>
public partial class BgVBoxContainer : VBoxContainer
{
    private StyleBoxFlat? _bgStyle;

    public void SetBgStyle(StyleBoxFlat style)
    {
        _bgStyle = style;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_bgStyle != null)
            DrawStyleBox(_bgStyle, new Rect2(Vector2.Zero, Size));
    }
}

/// <summary>
/// HBoxContainer with optional background drawn via _Draw(),
/// enabling bgColor/borderColor/borderWidth/borderRadius support.
/// </summary>
public partial class BgHBoxContainer : HBoxContainer
{
    private StyleBoxFlat? _bgStyle;

    public void SetBgStyle(StyleBoxFlat style)
    {
        _bgStyle = style;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_bgStyle != null)
            DrawStyleBox(_bgStyle, new Rect2(Vector2.Zero, Size));
    }
}

/// <summary>
/// Control with optional background drawn via _Draw(),
/// enabling bgColor/borderColor/borderWidth/borderRadius support.
/// </summary>
public partial class BgControl : Control
{
    private StyleBoxFlat? _bgStyle;

    public void SetBgStyle(StyleBoxFlat style)
    {
        _bgStyle = style;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_bgStyle != null)
            DrawStyleBox(_bgStyle, new Rect2(Vector2.Zero, Size));
    }
}
