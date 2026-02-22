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

using Runtime.Sml;
using System.Collections.Generic;

namespace Runtime.UI;

public sealed class TreeViewItem
{
    public int Id { get; init; }
    public required string Text { get; init; }
    public string? Icon { get; init; }
    public bool Expanded { get; init; }
    public SmlNode? Data { get; init; }
    public List<TreeViewItem> Children { get; } = [];
    public List<TreeViewToggle> Toggles { get; } = [];
}

public sealed class TreeViewToggle
{
    public required ToggleId ToggleId { get; init; }
    public required string Name { get; init; }
    public bool State { get; set; }
    public required string ImageOn { get; init; }
    public required string ImageOff { get; init; }
}
