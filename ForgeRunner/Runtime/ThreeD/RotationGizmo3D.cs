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
using System;

namespace Runtime.ThreeD;

/// <summary>
/// A runtime 3D rotation gizmo rendered inside a SubViewport.
///
/// Thin rotation rings (one per axis), sphere-guide rings, and draggable trapezoid handles.
/// Drag a handle to rotate the attached bone on that axis.
/// Rotation is clamped to per-axis min/max limits; the ring turns orange when clamped.
///
/// Rendering: all materials use NoDepthTest = true so the gizmo draws on top of the model.
/// </summary>
public sealed partial class RotationGizmo3D : Node3D
{
    // ── Geometry constants ────────────────────────────────────────────────────
    // Colored rotation arcs live on the inner (gray) guide radius,
    // so arc endpoints visually terminate on that circle.
    private const float RingRadius       = 0.35f;
    private const float GuideGrayRadius  = 0.35f;
    private const float GuideBlueRadius  = 0.50f;
    private const bool  EnableOuterBlueGuideRing = false;
    private const int   AxisRingSteps    = 96;
    private const float HandleRingDiag   = RingRadius * 0.70710677f; // R / sqrt(2) = 45° on ring
    private const float AxisLineLength   = GuideGrayRadius * 0.98f;
    private const float PivotRadius      = 0.018f;
    private const float HandleDiamondLength = 0.060f;
    private const float HandleDiamondWidth  = 0.028f;
    private const float HandleDiamondDepth  = 0.018f;
    private const float HandlePickRadius = 0.14f;   // ray-sphere hit tolerance (generous for UX)
    private const float DragSensitivity  = 0.6f;    // degrees per screen pixel
    private const float D2R              = Mathf.Pi / 180f;

    // ── Colour palette ────────────────────────────────────────────────────────
    private static readonly Color ColRingX   = new(0.95f, 0.20f, 0.20f, 0.90f);
    private static readonly Color ColRingY   = new(0.20f, 0.90f, 0.20f, 0.90f);
    private static readonly Color ColRingZ   = new(0.20f, 0.50f, 1.00f, 0.90f);
    private static readonly Color ColGuideBlue = new(0.26f, 0.62f, 1.00f, 0.48f);
    private static readonly Color ColGuideGray = new(0.76f, 0.78f, 0.82f, 0.36f);
    private static readonly Color ColPivot     = new(0.96f, 0.96f, 0.98f, 0.92f);
    private static readonly Color ColClamped = new(1.00f, 0.55f, 0.00f, 0.95f);

    // ── Handle local positions — on their rings, spread to avoid overlap ──────
    // Handle positions — all on the front face, clearly separated in screen space:
    // X ring (YZ plane): front  → (0,  0, +R)   12 o'clock on that ring from the camera
    // Y ring (XZ plane): right  → (+R, 0,  0)   3 o'clock on the ground ring
    // Z ring (XY plane): top    → (0, +R,  0)   top of the vertical ring
    private static readonly Vector3 HandleOffsetX = new(0f,             -HandleRingDiag,  HandleRingDiag);
    private static readonly Vector3 HandleOffsetY = new(HandleRingDiag,  0f,              HandleRingDiag);
    private static readonly Vector3 HandleOffsetZ = new(HandleRingDiag,  HandleRingDiag,  0f);

    // ── Scene nodes ───────────────────────────────────────────────────────────
    private readonly MeshInstance3D     _ringX,    _ringY,    _ringZ;
    private readonly MeshInstance3D?    _guideRingBlue;
    private readonly MeshInstance3D     _guideRingGray;
    private readonly MeshInstance3D     _axisX, _axisY, _axisZ;
    private readonly MeshInstance3D     _pivotMarker;
    private readonly MeshInstance3D     _handleX,  _handleY,  _handleZ;
    private readonly StandardMaterial3D _matRingX, _matRingY, _matRingZ;

    // ── Bone binding ──────────────────────────────────────────────────────────
    private Skeleton3D? _skeleton;
    private int         _boneIdx  = -1;

    // ── Free-node binding (Arrange/Rotate mode) ───────────────────────────────
    private Node3D?     _freeNode = null;
    private bool        _useLocalSpace;

    // ── Constraints (degrees) ─────────────────────────────────────────────────
    private float _minX = -180f, _maxX = 180f;
    private float _minY = -180f, _maxY = 180f;
    private float _minZ = -180f, _maxZ = 180f;

    // ── Drag state ────────────────────────────────────────────────────────────
    private int        _dragAxis        = -1;   // 0=X 1=Y 2=Z  (-1 = not dragging)
    private Quaternion _dragStartPose;           // bone pose at drag start
    private Quaternion _dragPreRot;              // parentWorldRot * restRot (fixed at drag start)
    private Vector3    _dragWorldAxis = Vector3.Right;
    private float      _dragAccumulated;

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

        _ringX = MakeRingArcXY(_matRingX, new Vector3(0f, 90f, 0f), RingRadius, 0f, 360f, AxisRingSteps);
        _ringY = MakeRingArcXY(_matRingY, new Vector3(90f, 0f, 0f), RingRadius, 0f, 360f, AxisRingSteps);
        _ringZ = MakeRingArcXY(_matRingZ, Vector3.Zero, RingRadius, 0f, 360f, AxisRingSteps);
        _guideRingBlue = EnableOuterBlueGuideRing
            ? MakeRingArcXY(MakeRingMat(ColGuideBlue), Vector3.Zero, GuideBlueRadius, 0f, 360f, 96)
            : null;
        _guideRingGray = MakeRingArcXY(MakeRingMat(ColGuideGray), Vector3.Zero, GuideGrayRadius, 0f, 360f, 96);
        _axisX = MakeAxisLine(ColRingX, Vector3.Left * AxisLineLength, Vector3.Right * AxisLineLength);
        _axisY = MakeAxisLine(ColRingY, Vector3.Down * AxisLineLength, Vector3.Up * AxisLineLength);
        _axisZ = MakeAxisLine(ColRingZ, Vector3.Forward * AxisLineLength, Vector3.Back * AxisLineLength);
        _pivotMarker = MakePivotMarker();

        // ── Diamond handles (same axis colours) ──────────────────────────────
        _handleX = MakeHandle(HandleOffsetX, new Vector3(90f, 0f, 45f), ColRingX);
        _handleY = MakeHandle(HandleOffsetY, new Vector3(0f, 0f, -45f), ColRingY);
        _handleZ = MakeHandle(HandleOffsetZ, new Vector3(0f, 0f, 45f), ColRingZ);

        AddChild(_ringX);
        AddChild(_ringY);
        AddChild(_ringZ);
        if (_guideRingBlue is not null)
            AddChild(_guideRingBlue);
        AddChild(_guideRingGray);
        AddChild(_axisX);
        AddChild(_axisY);
        AddChild(_axisZ);
        AddChild(_pivotMarker);
        AddChild(_handleX);
        AddChild(_handleY);
        AddChild(_handleZ);
    }

    // ── Frame update ──────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        var camera = GetViewport()?.GetCamera3D();

        if (_freeNode is not null && _freeNode.IsInsideTree())
        {
            GlobalPosition = _freeNode.GlobalPosition;
            GlobalBasis = _useLocalSpace ? _freeNode.GlobalBasis.Orthonormalized() : Basis.Identity;
            UpdateCameraFacingGuideRings(camera);
            return;
        }
        if (_skeleton is null || _boneIdx < 0 || !_skeleton.IsInsideTree()) return;
        var globalBone = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(_boneIdx);
        GlobalPosition = globalBone.Origin;
        GlobalBasis = Basis.Identity;
        UpdateCameraFacingGuideRings(camera);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Attach the gizmo to a specific bone and show it.</summary>
    public void AttachToBone(Skeleton3D skeleton, int boneIdx)
    {
        _freeNode = null;
        _skeleton = skeleton;
        _boneIdx  = boneIdx;
        Visible   = true;
        ResetRingColors();
    }

    /// <summary>Attach the gizmo to a free <see cref="Node3D"/> (Arrange/Rotate mode) and show it.</summary>
    public void AttachToFreeNode(Node3D node)
    {
        _skeleton = null;
        _boneIdx  = -1;
        _freeNode = node;
        // No joint constraints on scene objects
        _minX = _minY = _minZ = -360f;
        _maxX = _maxY = _maxZ =  360f;
        Visible = true;
        ResetRingColors();
    }

    /// <summary>Set transform space: "world" or "local" (free-node arrange mode).</summary>
    public void SetTransformSpace(string space)
    {
        _useLocalSpace = string.Equals(space, "local", StringComparison.OrdinalIgnoreCase);
        if (_freeNode is not null && _freeNode.IsInsideTree())
        {
            GlobalBasis = _useLocalSpace ? _freeNode.GlobalBasis.Orthonormalized() : Basis.Identity;
        }
    }

    /// <summary>Detach and hide the gizmo.</summary>
    public void Detach()
    {
        _skeleton = null;
        _boneIdx  = -1;
        _freeNode = null;
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
        if (!Visible) return -1;

        var handleNodes = new[] { _handleX, _handleY, _handleZ };

        var best  = -1;
        var bestT = float.MaxValue;

        for (var i = 0; i < 3; i++)
        {
            var center = handleNodes[i].IsInsideTree()
                ? handleNodes[i].GlobalPosition
                : GlobalPosition + handleNodes[i].Position;   // fallback before tree enter

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

    /// <summary>Begin dragging the given axis handle.</summary>
    public void BeginDrag(int axis)
    {
        _dragAxis        = axis;
        _dragAccumulated = 0f;

        if (_freeNode is not null)
        {
            // Free-node mode: world = local (top-level node in worldRoot)
            _dragStartPose = _freeNode.Quaternion;
            _dragPreRot    = Quaternion.Identity;
            _dragWorldAxis = ResolveWorldAxis(axis, _freeNode);
            return;
        }

        if (_skeleton is null || _boneIdx < 0 || !_skeleton.IsInsideTree()) return;
        _dragStartPose   = _skeleton.GetBonePoseRotation(_boneIdx);
        _dragWorldAxis = ResolveWorldAxis(axis, null);

        // _dragPreRot = parentWorldRot * restRot
        // Derived from: boneGlobalRot = preRot * poseRot  →  preRot = boneGlobalRot * poseRot⁻¹
        var boneGlobal   = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(_boneIdx);
        var boneWorldRot = boneGlobal.Basis.GetRotationQuaternion();
        _dragPreRot = (boneWorldRot * _dragStartPose.Inverse()).Normalized();
    }

    /// <summary>
    /// Feed a screen-space mouse delta.
    /// Rotates the bone around the world-space axis that the dragged ring represents,
    /// so rings always correspond to the correct world axis regardless of bone orientation.
    /// Returns true if the drag consumed the event.
    /// </summary>
    public bool UpdateDrag(Vector2 screenDelta)
    {
        if (_dragAxis < 0) return false;
        if (_skeleton is null && _freeNode is null) return false;

        // Screen-space convention per handle position:
        //   X handle at (0,0,+R) front: drag DOWN  → positive X rotation
        //   Y handle at (+R,0,0) right: drag RIGHT → positive Y rotation
        //   Z handle at (0,+R,0) top:   drag LEFT  → positive Z rotation
        var raw = _dragAxis switch
        {
            0 =>  screenDelta.Y,
            1 =>  screenDelta.X,
            _ => -screenDelta.X,
        };

        _dragAccumulated += raw * DragSensitivity * D2R;

        // World-space delta rotation.
        var worldDelta = new Quaternion(_dragWorldAxis, _dragAccumulated);

        // Convert to bone-pose space:  newPose = preRot⁻¹ * worldDelta * preRot * startPose
        var newPose = (_dragPreRot.Inverse() * worldDelta * _dragPreRot * _dragStartPose).Normalized();

        // Apply per-axis constraints by clamping euler components of the result.
        var e = newPose.GetEuler(EulerOrder.Xyz);
        var ex = Mathf.Clamp(e.X, _minX * D2R, _maxX * D2R);
        var ey = Mathf.Clamp(e.Y, _minY * D2R, _maxY * D2R);
        var ez = Mathf.Clamp(e.Z, _minZ * D2R, _maxZ * D2R);
        var clamped = Mathf.Abs(ex - e.X) > 0.001f
                   || Mathf.Abs(ey - e.Y) > 0.001f
                   || Mathf.Abs(ez - e.Z) > 0.001f;
        if (clamped) newPose = Quaternion.FromEuler(new Vector3(ex, ey, ez));

        if (_freeNode is not null)
            _freeNode.Quaternion = newPose.Normalized();
        else
            _skeleton!.SetBonePoseRotation(_boneIdx, newPose);

        // Visual feedback: ring turns orange when clamped.
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

    public bool  IsDragging        => _dragAxis >= 0;
    public float DragAngleDegrees  => _dragAccumulated * (180f / Mathf.Pi);
    public int   DragAxis          => _dragAxis;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector3 ResolveWorldAxis(int axis, Node3D? localNode)
    {
        var baseAxis = axis switch
        {
            0 => Vector3.Right,
            1 => Vector3.Up,
            _ => Vector3.Back,
        };

        if (!_useLocalSpace || localNode is null)
        {
            return baseAxis;
        }

        var transformed = localNode.GlobalBasis * baseAxis;
        return transformed.LengthSquared() > 0.000001f ? transformed.Normalized() : baseAxis;
    }

    private void UpdateCameraFacingGuideRings(Camera3D? camera)
    {
        if (camera is null)
        {
            return;
        }

        var facingBasis = camera.GlobalBasis.Orthonormalized();
        if (_guideRingBlue is not null && GodotObject.IsInstanceValid(_guideRingBlue))
            _guideRingBlue.GlobalBasis = facingBasis;
        _guideRingGray.GlobalBasis = facingBasis;
    }

    private void ResetRingColors()
    {
        _matRingX.AlbedoColor = ColRingX;
        _matRingY.AlbedoColor = ColRingY;
        _matRingZ.AlbedoColor = ColRingZ;
    }

    private static MeshInstance3D MakeRingArcXY(
        StandardMaterial3D mat,
        Vector3 rotDeg,
        float radius,
        float startDeg,
        float endDeg,
        int steps)
    {
        var mesh = new ImmediateMesh();
        var start = startDeg * D2R;
        var end = endDeg * D2R;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, mat);
        for (var i = 0; i < steps; i++)
        {
            var t0 = i / (float)steps;
            var t1 = (i + 1) / (float)steps;
            var a0 = Mathf.Lerp(start, end, t0);
            var a1 = Mathf.Lerp(start, end, t1);
            mesh.SurfaceAddVertex(new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0f));
            mesh.SurfaceAddVertex(new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0f));
        }
        mesh.SurfaceEnd();

        return new MeshInstance3D
        {
            Mesh            = mesh,
            RotationDegrees = rotDeg,
            CastShadow      = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static MeshInstance3D MakeAxisLine(Color color, Vector3 from, Vector3 to)
    {
        var mat = MakeRingMat(color);
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, mat);
        mesh.SurfaceAddVertex(from);
        mesh.SurfaceAddVertex(to);
        mesh.SurfaceEnd();

        return new MeshInstance3D
        {
            Mesh = mesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static MeshInstance3D MakePivotMarker()
    {
        var sphere = new SphereMesh
        {
            Radius = PivotRadius,
            Height = PivotRadius * 2f,
            RadialSegments = 10,
            Rings = 6,
        };
        sphere.SurfaceSetMaterial(0, MakeRingMat(ColPivot));

        return new MeshInstance3D
        {
            Mesh = sphere,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static MeshInstance3D MakeHandle(Vector3 localOffset, Vector3 rotDeg, Color color)
    {
        var box = new BoxMesh
        {
            Size = new Vector3(HandleDiamondWidth, HandleDiamondLength, HandleDiamondDepth),
        };

        var mat = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor  = color,
            NoDepthTest  = true,
            CullMode     = BaseMaterial3D.CullModeEnum.Disabled,
            ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        box.SurfaceSetMaterial(0, mat);

        return new MeshInstance3D
        {
            Mesh       = box,
            Position   = localOffset,
            RotationDegrees = rotDeg,
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
            ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
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
