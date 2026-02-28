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

namespace Runtime.ThreeD;

/// <summary>
/// Data record for a single joint rotation constraint.
/// </summary>
public record struct JointConstraintData(
    string BoneName,
    float MinX, float MaxX,
    float MinY, float MaxY,
    float MinZ, float MaxZ);

/// <summary>
/// Invisible marker node that holds joint constraint data.
/// Used as a child of PosingEditorControl in SML:
/// <code>
/// JointConstraint { bone: "RightKnee"; minX: -140; maxX: 0 }
/// </code>
/// The PosingEditorControl collects and removes these nodes after build.
/// </summary>
public sealed partial class JointConstraintNode : Control
{
    public string BoneName { get; set; } = string.Empty;
    public float MinX { get; set; } = -180f;
    public float MaxX { get; set; } = 180f;
    public float MinY { get; set; } = -180f;
    public float MaxY { get; set; } = 180f;
    public float MinZ { get; set; } = -180f;
    public float MaxZ { get; set; } = 180f;

    public JointConstraintNode()
    {
        Visible = false;
        CustomMinimumSize = Vector2.Zero;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public JointConstraintData ToData() =>
        new(BoneName, MinX, MaxX, MinY, MaxY, MinZ, MaxZ);
}
