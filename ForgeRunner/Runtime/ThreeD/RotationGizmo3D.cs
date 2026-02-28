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
/// A runtime 3D rotation gizmo rendered inside a SubViewport.
///
/// Three torus rings (one per axis) plus a draggable sphere handle on each ring.
/// Drag a handle to rotate the attached bone on that axis.
/// Rotation is clamped to per-axis min/max limits; the ring turns orange when clamped.
///
/// Rendering: all materials use NoDepthTest = true so the gizmo draws on top of the model.
/// </summary>
public sealed partial class RotationGizmo3D : Node3D
{
    // ── Geometry constants ────────────────────────────────────────────────────
    private const float RingRadius       = 0.38f;   // centre-of-tube radius
    private const float TubeRadius       = 0.018f;  // tube thickness
    private const float HandleRadius     = 0.055f;  // visual sphere radius
    private const float HandlePickRadius = 0.10f;   // ray-sphere hit tolerance
    private const float DragSensitivity  = 0.6f;    // degrees per screen pixel
    private const float D2R              = Mathf.Pi / 180f;

    // ── Colour palette ────────────────────────────────────────────────────────
    private static readonly Color ColRingX   = new(0.95f, 0.20f, 0.20f, 0.75f);
    private static readonly Color ColRingY   = new(0.20f, 0.90f, 0.20f, 0.75f);
    private static readonly Color ColRingZ   = new(0.20f, 0.45f, 1.00f, 0.75f);
    private static readonly Color ColHandle  = new(1.00f, 1.00f, 1.00f, 0.90f);
    private static readonly Color ColClamped = new(1.00f, 0.55f, 0.00f, 0.95f);

    // ── Handle local positions (on their respective rings) ────────────────────
    // X ring lives in YZ plane; handle at top:  (0,  R, 0)
    // Y ring lives in XZ plane; handle at right: (R,  0, 0)
    // Z ring lives in XY plane; handle at left: (-R,  0, 0)  — distinct from Y
    private static readonly Vector3 HandleOffsetX = new(0f,         RingRadius, 0f);
    private static readonly Vector3 HandleOffsetY = new(RingRadius, 0f,         0f);
    private static readonly Vector3 HandleOffsetZ = new(-RingRadius, 0f,        0f);

    // ── Scene nodes ───────────────────────────────────────────────────────────
    private readonly MeshInstance3D     _ringX,    _ringY,    _ringZ;
    private readonly MeshInstance3D     _handleX,  _handleY,  _handleZ;
    private readonly StandardMaterial3D _matRingX, _matRingY, _matRingZ;

    // ── Bone binding ──────────────────────────────────────────────────────────
    private Skeleton3D? _skeleton;
    private int         _boneIdx = -1;

    // ── Constraints (degrees) ─────────────────────────────────────────────────
    private float _minX = -180f, _maxX = 180f;
    private float _minY = -180f, _maxY = 180f;
    private float _minZ = -180f, _maxZ = 180f;

    // ── Drag state ────────────────────────────────────────────────────────────
    private int     _dragAxis         = -1;  // 0=X 1=Y 2=Z  (-1 = not dragging)
    private Vector3 _dragStartEuler;
    private float   _dragAccumulated;

    // ── Active drag axis index → ring material (for colour feedback) ──────────
    private readonly StandardMaterial3D[] _ringMats;

    // ─────────────────────────────────────────────────────────────────────────
    public RotationGizmo3D()
    {
        Name    = "RotationGizmo3D";
        Visible = false;

        _matRingX = MakeRingMat(ColRingX);
        _matRingY = MakeRingMat(ColRingY);
        _matRingZ = MakeRingMat(ColRingZ);
        _ringMats = [_matRingX, _matRingY, _matRingZ];

        // ── Torus rings ───────────────────────────────────────────────────────
        // Godot TorusMesh default: ring in XZ plane, normal along Y.
        // X ring: normal along X → rotate -90° around Z.
        // Y ring: already correct (normal along Y).
        // Z ring: normal along Z → rotate  90° around X.

        _ringX = MakeRing(_matRingX, new Vector3(0f, 0f, -90f));
        _ringY = MakeRing(_matRingY, Vector3.Zero);
        _ringZ = MakeRing(_matRingZ, new Vector3(90f, 0f, 0f));

        // ── Handle spheres ────────────────────────────────────────────────────
        _handleX = MakeHandle(HandleOffsetX);
        _handleY = MakeHandle(HandleOffsetY);
        _handleZ = MakeHandle(HandleOffsetZ);

        AddChild(_ringX);
        AddChild(_ringY);
        AddChild(_ringZ);
        AddChild(_handleX);
        AddChild(_handleY);
        AddChild(_handleZ);
    }

    // ── Frame update ──────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_skeleton is null || _boneIdx < 0 || !_skeleton.IsInsideTree()) return;
        var globalBone = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(_boneIdx);
        GlobalPosition = globalBone.Origin;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Attach the gizmo to a specific bone and show it.</summary>
    public void AttachToBone(Skeleton3D skeleton, int boneIdx)
    {
        _skeleton = skeleton;
        _boneIdx  = boneIdx;
        Visible   = true;
        ResetRingColors();
    }

    /// <summary>Detach and hide the gizmo.</summary>
    public void Detach()
    {
        _skeleton = null;
        _boneIdx  = -1;
        _dragAxis = -1;
        Visible   = false;
    }

    /// <summary>Set rotation limits for all axes (degrees).</summary>
    public void SetConstraints(
        float minX, float maxX,
        float minY, float maxY,
        float minZ, float maxZ)
    {
        _minX = minX; _maxX = maxX;
        _minY = minY; _maxY = maxY;
        _minZ = minZ; _maxZ = maxZ;
    }

    // ── Picking ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Check whether the ray hits one of the three handles.
    /// Returns the axis index (0=X, 1=Y, 2=Z) or -1.
    /// </summary>
    public int TryPickHandle(Vector3 rayOrigin, Vector3 rayDir)
    {
        if (!Visible || !IsInsideTree()) return -1;

        var handleCentres = new[]
        {
            _handleX.GlobalPosition,
            _handleY.GlobalPosition,
            _handleZ.GlobalPosition,
        };

        var best  = -1;
        var bestT = float.MaxValue;

        for (var i = 0; i < 3; i++)
        {
            if (!RaySphereIntersect(rayOrigin, rayDir, handleCentres[i],
                    HandlePickRadius, out var t)) continue;
            if (t < bestT)
            {
                bestT = t;
                best  = i;
            }
        }

        return best;
    }

    // ── Drag ─────────────────────────────────────────────────────────────────

    /// <summary>Begin dragging the given axis handle.</summary>
    public void BeginDrag(int axis)
    {
        if (_skeleton is null || _boneIdx < 0) return;
        _dragAxis       = axis;
        _dragAccumulated = 0f;
        var q = _skeleton.GetBonePoseRotation(_boneIdx);
        _dragStartEuler = q.GetEuler(EulerOrder.Xyz);
    }

    /// <summary>
    /// Feed a screen-space mouse delta.
    /// Returns true if the drag consumed the event.
    /// </summary>
    public bool UpdateDrag(Vector2 screenDelta)
    {
        if (_dragAxis < 0 || _skeleton is null) return false;

        // Axis mapping:  X → mouse Y (up = +X),  Y → mouse X,  Z → mouse X
        var raw = _dragAxis switch
        {
            0 => -screenDelta.Y,
            _ =>  screenDelta.X,
        };

        _dragAccumulated += raw * DragSensitivity * D2R;

        var euler   = _dragStartEuler;
        var clamped = false;

        switch (_dragAxis)
        {
            case 0:
            {
                var want     = euler.X + _dragAccumulated;
                var limited  = Mathf.Clamp(want, _minX * D2R, _maxX * D2R);
                clamped      = Mathf.Abs(want - limited) > 0.001f;
                euler.X      = limited;
                break;
            }
            case 1:
            {
                var want     = euler.Y + _dragAccumulated;
                var limited  = Mathf.Clamp(want, _minY * D2R, _maxY * D2R);
                clamped      = Mathf.Abs(want - limited) > 0.001f;
                euler.Y      = limited;
                break;
            }
            case 2:
            {
                var want     = euler.Z + _dragAccumulated;
                var limited  = Mathf.Clamp(want, _minZ * D2R, _maxZ * D2R);
                clamped      = Mathf.Abs(want - limited) > 0.001f;
                euler.Z      = limited;
                break;
            }
        }

        _skeleton.SetBonePoseRotation(_boneIdx, Quaternion.FromEuler(euler));

        // Visual feedback: ring turns orange when at limit.
        _ringMats[_dragAxis].AlbedoColor = clamped ? ColClamped
            : _dragAxis switch { 0 => ColRingX, 1 => ColRingY, _ => ColRingZ };

        return true;
    }

    /// <summary>Finish the current drag and reset ring colours.</summary>
    public void EndDrag()
    {
        _dragAxis = -1;
        ResetRingColors();
    }

    public bool IsDragging => _dragAxis >= 0;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ResetRingColors()
    {
        _matRingX.AlbedoColor = ColRingX;
        _matRingY.AlbedoColor = ColRingY;
        _matRingZ.AlbedoColor = ColRingZ;
    }

    private static MeshInstance3D MakeRing(StandardMaterial3D mat, Vector3 rotDeg)
    {
        var torus = new TorusMesh
        {
            InnerRadius = RingRadius - TubeRadius,
            OuterRadius = RingRadius + TubeRadius,
            Rings       = 32,
            RingSegments = 12,
        };
        torus.SurfaceSetMaterial(0, mat);

        return new MeshInstance3D
        {
            Mesh            = torus,
            RotationDegrees = rotDeg,
            CastShadow      = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static MeshInstance3D MakeHandle(Vector3 localOffset)
    {
        var sphere = new SphereMesh
        {
            Radius = HandleRadius,
            Height = HandleRadius * 2f,
            RadialSegments = 12,
            Rings          = 6,
        };

        var mat = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor  = ColHandle,
            NoDepthTest  = true,
            CullMode     = BaseMaterial3D.CullModeEnum.Disabled,
        };
        sphere.SurfaceSetMaterial(0, mat);

        return new MeshInstance3D
        {
            Mesh       = sphere,
            Position   = localOffset,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static StandardMaterial3D MakeRingMat(Color color)
    {
        return new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
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
