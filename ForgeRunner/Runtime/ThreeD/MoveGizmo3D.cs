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
/// A runtime 3D translation gizmo — three colored arrows (X=red, Y=green, Z=blue)
/// rendered on top of everything. Drag an arrow tip to translate the attached <see cref="Node3D"/>.
///
/// Architecture mirrors <see cref="RotationGizmo3D"/>: call <see cref="AttachToNode"/> to show,
/// <see cref="TryPickHandle"/> + <see cref="BeginDrag"/> / <see cref="UpdateDrag"/> / <see cref="EndDrag"/>
/// to drive interaction.
/// </summary>
public sealed partial class MoveGizmo3D : Node3D
{
    // ── Geometry constants ────────────────────────────────────────────────────
    private const float ShaftLength      = 0.40f;   // cylinder length
    private const float ShaftRadius      = 0.012f;  // cylinder radius
    private const float ConeHeight       = 0.14f;   // arrow head height
    private const float ConeRadius       = 0.035f;  // arrow head base radius
    private const float HandlePickRadius = 0.12f;   // ray-sphere hit tolerance for cone tip

    // ── Colour palette ────────────────────────────────────────────────────────
    private static readonly Color ColAxisX      = new(0.95f, 0.20f, 0.20f, 1.0f);
    private static readonly Color ColAxisY      = new(0.20f, 0.90f, 0.20f, 1.0f);
    private static readonly Color ColAxisZ      = new(0.20f, 0.50f, 1.00f, 1.0f);
    private static readonly Color ColHighlight  = new(1.00f, 0.85f, 0.00f, 1.0f);

    // Cone tip positions (world-local offsets from gizmo origin).
    // X arrow points along +X, Y along +Y, Z along -Z (Godot forward).
    private static readonly Vector3 TipOffsetX = new( ShaftLength + ConeHeight, 0f,  0f);
    private static readonly Vector3 TipOffsetY = new(0f,  ShaftLength + ConeHeight,  0f);
    private static readonly Vector3 TipOffsetZ = new(0f,  0f, -(ShaftLength + ConeHeight));

    // ── Scene nodes ───────────────────────────────────────────────────────────
    private readonly MeshInstance3D      _shaftX,  _shaftY,  _shaftZ;
    private readonly MeshInstance3D      _coneX,   _coneY,   _coneZ;
    private readonly StandardMaterial3D  _matX,    _matY,    _matZ;

    // ── Target binding ────────────────────────────────────────────────────────
    private Node3D? _target;

    // ── Drag state ────────────────────────────────────────────────────────────
    private int     _dragAxis      = -1;    // 0=X 1=Y 2=Z  (-1 = idle)
    private Vector3 _dragStartPos;          // target position when drag began

    // ─────────────────────────────────────────────────────────────────────────
    public MoveGizmo3D()
    {
        Name    = "MoveGizmo3D";
        Visible = false;

        _matX = MakeArrowMat(ColAxisX);
        _matY = MakeArrowMat(ColAxisY);
        _matZ = MakeArrowMat(ColAxisZ);

        // Shafts
        _shaftX = MakeShaft(_matX, new Vector3(0f, 0f, 90f));   // cylinder along +X
        _shaftY = MakeShaft(_matY, Vector3.Zero);                 // cylinder along +Y (default)
        _shaftZ = MakeShaft(_matZ, new Vector3(90f, 0f, 0f));    // cylinder along -Z

        // Cones (arrow heads)
        _coneX = MakeCone(_matX, TipOffsetX, new Vector3(0f, 0f, -90f));  // points +X
        _coneY = MakeCone(_matY, TipOffsetY, Vector3.Zero);                // points +Y (default)
        _coneZ = MakeCone(_matZ, TipOffsetZ, new Vector3(90f, 0f, 0f));    // points -Z

        AddChild(_shaftX); AddChild(_shaftY); AddChild(_shaftZ);
        AddChild(_coneX);  AddChild(_coneY);  AddChild(_coneZ);
    }

    // ── Frame update ──────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_target is null || !_target.IsInsideTree()) return;
        GlobalPosition = _target.GlobalPosition;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Attach the gizmo to a <see cref="Node3D"/> and show it.</summary>
    public void AttachToNode(Node3D target)
    {
        _target  = target;
        Visible  = true;
        ResetColors();
    }

    /// <summary>Detach and hide the gizmo.</summary>
    public void Detach()
    {
        _target   = null;
        _dragAxis = -1;
        Visible   = false;
    }

    /// <summary>
    /// Ray-test against the three cone tips.
    /// Returns the axis index (0=X, 1=Y, 2=Z) or -1.
    /// </summary>
    public int TryPickHandle(Vector3 rayOrigin, Vector3 rayDir)
    {
        if (!Visible) return -1;

        var cones = new[] { _coneX, _coneY, _coneZ };
        var best  = -1;
        var bestT = float.MaxValue;

        for (var i = 0; i < 3; i++)
        {
            var center = cones[i].IsInsideTree()
                ? cones[i].GlobalPosition
                : GlobalPosition + (i switch { 0 => TipOffsetX, 1 => TipOffsetY, _ => TipOffsetZ });

            if (!RaySphereIntersect(rayOrigin, rayDir, center, HandlePickRadius, out var t))
                continue;
            if (t < bestT)
            {
                bestT = t;
                best  = i;
            }
        }

        return best;
    }

    // ── Drag ─────────────────────────────────────────────────────────────────

    /// <summary>Record the drag start state.</summary>
    public void BeginDrag(int axis)
    {
        if (_target is null) return;
        _dragAxis     = axis;
        _dragStartPos = _target.GlobalPosition;
        HighlightAxis(axis);
    }

    /// <summary>
    /// Translate the target by projecting the mouse delta onto the drag axis.
    /// Returns the position delta actually applied.
    /// </summary>
    public Vector3 UpdateDrag(Camera3D camera, Vector2 mousePos, Vector2 lastMousePos, float depthScale)
    {
        if (_dragAxis < 0 || _target is null || camera is null) return Vector3.Zero;

        // World-space axis direction
        var worldAxis = _dragAxis switch
        {
            0 => Vector3.Right,   // +X
            1 => Vector3.Up,      // +Y
            _ => Vector3.Back,    // -Z  (Godot forward = -Z)
        };

        // Project axis endpoints to screen space
        var basePos    = _target.GlobalPosition;
        var axisEndPos = basePos + worldAxis;

        var screenBase = camera.UnprojectPosition(basePos);
        var screenEnd  = camera.UnprojectPosition(axisEndPos);
        var screenAxis = (screenEnd - screenBase);

        var screenLen = screenAxis.Length();
        if (screenLen < 0.01f) return Vector3.Zero;
        screenAxis /= screenLen;

        // Mouse delta projected onto screen axis
        var mouseDelta = mousePos - lastMousePos;
        var scalarMove = mouseDelta.Dot(screenAxis);

        // Scale: map screen pixels to world units using camera depth
        var depth    = camera.GlobalPosition.DistanceTo(basePos);
        var worldUnitsPerPixel = depth * depthScale;
        var delta = worldAxis * (scalarMove * worldUnitsPerPixel);

        _target.GlobalPosition += delta;
        return delta;
    }

    /// <summary>Finish the current drag.</summary>
    public void EndDrag()
    {
        _dragAxis = -1;
        ResetColors();
    }

    public bool IsDragging => _dragAxis >= 0;
    public int  DragAxis   => _dragAxis;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ResetColors()
    {
        _matX.AlbedoColor = ColAxisX;
        _matY.AlbedoColor = ColAxisY;
        _matZ.AlbedoColor = ColAxisZ;
    }

    private void HighlightAxis(int axis)
    {
        ResetColors();
        var mat = axis switch { 0 => _matX, 1 => _matY, _ => _matZ };
        mat.AlbedoColor = ColHighlight;
    }

    private static MeshInstance3D MakeShaft(StandardMaterial3D mat, Vector3 rotDeg)
    {
        var cyl = new CylinderMesh
        {
            TopRadius    = ShaftRadius,
            BottomRadius = ShaftRadius,
            Height       = ShaftLength,
            RadialSegments = 8,
        };
        cyl.SurfaceSetMaterial(0, mat);

        // Cylinder default is along Y. Offset by half height along Y, then rotate.
        return new MeshInstance3D
        {
            Mesh            = cyl,
            // Centre of shaft at half shaft length along local +Y before rotation.
            Position        = new Vector3(0f, ShaftLength * 0.5f, 0f),
            RotationDegrees = rotDeg,
            CastShadow      = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static MeshInstance3D MakeCone(StandardMaterial3D mat, Vector3 localOffset, Vector3 rotDeg)
    {
        var cone = new CylinderMesh
        {
            TopRadius    = 0f,
            BottomRadius = ConeRadius,
            Height       = ConeHeight,
            RadialSegments = 10,
        };
        cone.SurfaceSetMaterial(0, mat);

        return new MeshInstance3D
        {
            Mesh            = cone,
            Position        = localOffset,
            RotationDegrees = rotDeg,
            CastShadow      = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static StandardMaterial3D MakeArrowMat(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor  = color,
            NoDepthTest  = true,
            CullMode     = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }

    private static bool RaySphereIntersect(
        Vector3 origin, Vector3 dir, Vector3 center, float radius, out float t)
    {
        var oc = origin - center;
        var b  = oc.Dot(dir);
        var c  = oc.Dot(oc) - radius * radius;
        var d  = b * b - c;
        if (d < 0f) { t = 0f; return false; }
        var sq = Mathf.Sqrt(d);
        t = -b - sq;
        if (t < 0f) t = -b + sq;
        return t > 0f;
    }
}
