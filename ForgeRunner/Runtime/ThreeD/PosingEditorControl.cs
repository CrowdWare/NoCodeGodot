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
using System.IO;

namespace Runtime.ThreeD;

/// <summary>
/// A self-contained posing tool that combines:
/// - A 3D viewport with orbit camera
/// - Pickable joint spheres at every Skeleton3D bone position
/// - An optional bone-tree overlay panel
/// - JointConstraint data collection (used by RotationGizmo3D in a later pass)
///
/// SML usage:
/// <code>
/// PosingEditor {
///     src: "res://assets/models/demo.glb"
///     showBoneTree: true
///     JointConstraint { bone: "RightKnee"; minX: -140; maxX: 0 }
/// }
/// </code>
/// </summary>
public sealed partial class PosingEditorControl : SubViewportContainer
{
    // ── Camera defaults ──────────────────────────────────────────────────────
    private const float DefaultYaw      = 0f;
    private const float DefaultPitch    = 0.35f;
    private const float DefaultDistance = 3.5f;
    private const float MinDistance     = 0.5f;
    private const float MaxDistance     = 20f;

    // ── Joint sphere visual settings ─────────────────────────────────────────
    private const float SphereRadius        = 0.04f;
    private const float PickRayRadius       = 0.12f;   // world-space hit tolerance
    private static readonly Color SphereColorNormal   = new(0.4f, 0.8f, 1.0f, 0.55f);
    private static readonly Color SphereColorSelected = new(1.0f, 0.7f, 0.1f, 0.9f);

    // ── 3D scene ─────────────────────────────────────────────────────────────
    private readonly SubViewport         _viewport;
    private readonly Node3D              _worldRoot;
    private readonly Camera3D            _camera;
    private readonly DirectionalLight3D  _light;

    private Node3D?    _modelRoot;
    private Skeleton3D? _skeleton;
    private readonly List<MeshInstance3D> _jointSpheres = [];

    // ── Bone tree overlay ─────────────────────────────────────────────────────
    private Panel?    _bonePanel;
    private ItemList? _boneList;
    private bool      _showBoneTree;

    // ── Camera orbit ──────────────────────────────────────────────────────────
    private float   _yaw           = DefaultYaw;
    private float   _pitch         = DefaultPitch;
    private float   _orbitDistance = DefaultDistance;
    private Vector3 _cameraTarget  = Vector3.Zero;
    private bool    _isRotating;
    private bool    _isPanning;

    // ── Bone selection ────────────────────────────────────────────────────────
    private int _selectedBoneIdx = -1;

    // ── Joint constraints (bone name → data) ──────────────────────────────────
    private readonly Dictionary<string, JointConstraintData> _constraints
        = new(StringComparer.OrdinalIgnoreCase);

    // ── Pose data (bone name → local rotation quaternion) ────────────────────
    public Dictionary<string, Quaternion> PoseData { get; } = new(StringComparer.OrdinalIgnoreCase);

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<string>?             BoneSelected;
    public event Action<string, Quaternion>? PoseChanged;
    public event Action?                     PoseReset;

    // ─────────────────────────────────────────────────────────────────────────
    public PosingEditorControl()
    {
        Stretch = true;
        CustomMinimumSize = new Vector2(640, 480);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical   = SizeFlags.ExpandFill;

        _viewport = new SubViewport
        {
            Name = "Viewport",
            Size = new Vector2I(1280, 720),
            HandleInputLocally = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            TransparentBg = false
        };

        _worldRoot = new Node3D { Name = "WorldRoot" };

        _camera = new Camera3D { Name = "Camera" };

        _light = new DirectionalLight3D
        {
            Name  = "Light",
            RotationDegrees = new Vector3(-45f, 35f, 0f),
            LightEnergy = 1.2f
        };

        _worldRoot.AddChild(_camera);
        _worldRoot.AddChild(_light);
        _viewport.AddChild(_worldRoot);
        AddChild(_viewport);

        ResetView();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                HandleMouseButton(mouseButton);
                break;
            case InputEventMouseMotion mouseMotion:
                HandleMouseMotion(mouseMotion);
                break;
        }
    }

    private void HandleMouseButton(InputEventMouseButton mb)
    {
        if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            TryPickBone(mb.Position);
            AcceptEvent();
            return;
        }

        if (mb.ButtonIndex == MouseButton.Right)
        {
            _isRotating = mb.Pressed;
            if (_isRotating) GrabFocus();
            AcceptEvent();
            return;
        }

        if (mb.ButtonIndex == MouseButton.Middle)
        {
            _isPanning = mb.Pressed;
            if (_isPanning) GrabFocus();
            AcceptEvent();
            return;
        }

        if (!mb.Pressed) return;

        if (mb.ButtonIndex == MouseButton.WheelUp)
        {
            AdjustCameraDistance(-0.3f);
            AcceptEvent();
            return;
        }

        if (mb.ButtonIndex == MouseButton.WheelDown)
        {
            AdjustCameraDistance(0.3f);
            AcceptEvent();
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mm)
    {
        if (_isRotating)
        {
            _yaw   -= mm.Relative.X * 0.01f;
            _pitch  = Mathf.Clamp(_pitch + mm.Relative.Y * 0.01f, -1.2f, 1.2f);
            ApplyCameraOrbit();
            AcceptEvent();
            return;
        }

        if (_isPanning)
        {
            var basis    = _camera.GlobalTransform.Basis;
            var right    = basis.X.Normalized();
            var up       = basis.Y.Normalized();
            var panScale = _orbitDistance * 0.0015f;
            _cameraTarget += (-right * mm.Relative.X + up * mm.Relative.Y) * panScale;
            ApplyCameraOrbit();
            AcceptEvent();
        }
    }

    // ── Frame update ──────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        UpdateJointSpherePositions();
    }

    private void UpdateJointSpherePositions()
    {
        if (_skeleton is null || !_skeleton.IsInsideTree()) return;

        var boneCount = _skeleton.GetBoneCount();
        for (var i = 0; i < _jointSpheres.Count && i < boneCount; i++)
        {
            if (!_jointSpheres[i].IsInsideTree()) continue;
            var globalBone = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(i);
            _jointSpheres[i].GlobalTransform = new Transform3D(Basis.Identity, globalBone.Origin);
        }
    }

    // ── Model loading ─────────────────────────────────────────────────────────

    public void SetModelSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            RunnerLogger.Warn("PosingEditor", "Model source is empty.");
            return;
        }

        if (source.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(source, UriKind.Absolute, out var fileUri))
        {
            source = fileUri.LocalPath;
        }

        if (!TryLoadNode3D(source, out var loaded, out var label))
            return;

        AttachModel(loaded, label);
    }

    private bool TryLoadNode3D(string source, out Node3D node3D, out string label)
    {
        label = source;
        node3D = null!;

        if (Path.IsPathRooted(source))
        {
            if (!File.Exists(source))
            {
                RunnerLogger.Warn("PosingEditor", $"File not found: '{source}'.");
                return false;
            }

            return TryLoadAbsoluteGltf(source, out node3D);
        }

        if (!source.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            && !source.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            RunnerLogger.Warn("PosingEditor", $"Unsupported source '{source}'. Use res:// or user://.");
            return false;
        }

        var resource = GD.Load<Resource>(source);
        if (resource is not PackedScene scene)
        {
            RunnerLogger.Warn("PosingEditor", $"Could not load packed scene '{source}'.");
            return false;
        }

        var instance = scene.Instantiate();
        if (instance is not Node3D n)
        {
            RunnerLogger.Warn("PosingEditor", $"'{source}' is not a 3D scene.");
            return false;
        }

        node3D = n;
        return true;
    }

    private static bool TryLoadAbsoluteGltf(string path, out Node3D node)
    {
        node = null!;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is not (".glb" or ".gltf")) return false;

        var state = new GltfState();
        var doc   = new GltfDocument();
        if (doc.AppendFromFile(path, state) != Error.Ok) return false;

        var generated = doc.GenerateScene(state);
        if (generated is not Node3D n) return false;

        node = n;
        return true;
    }

    private void AttachModel(Node3D node3D, string label)
    {
        if (node3D.GetParent() is not null)
            node3D.GetParent().RemoveChild(node3D);

        if (_modelRoot is not null)
        {
            ClearJointSpheres();
            _worldRoot.RemoveChild(_modelRoot);
            _modelRoot.QueueFree();
            _modelRoot = null;
            _skeleton  = null;
        }

        _modelRoot = node3D;
        _modelRoot.Name = "ModelRoot";
        _worldRoot.AddChild(_modelRoot);

        _skeleton = FindSkeleton(_modelRoot);

        if (_skeleton is null)
        {
            RunnerLogger.Warn("PosingEditor", $"No Skeleton3D found in '{label}'.");
        }
        else
        {
            RunnerLogger.Info("PosingEditor", $"Model loaded from '{label}'. Skeleton: '{_skeleton.Name}', bones: {_skeleton.GetBoneCount()}.");
            BuildJointSpheres();
            if (_showBoneTree) PopulateBoneList();
        }
    }

    // ── Joint spheres ─────────────────────────────────────────────────────────

    private void BuildJointSpheres()
    {
        if (_skeleton is null) return;

        var mesh = new SphereMesh
        {
            Radius = SphereRadius,
            Height = SphereRadius * 2f
        };

        var mat = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor  = SphereColorNormal,
            CullMode     = BaseMaterial3D.CullModeEnum.Disabled
        };
        mesh.SurfaceSetMaterial(0, mat);

        for (var i = 0; i < _skeleton.GetBoneCount(); i++)
        {
            var sphere = new MeshInstance3D
            {
                Name = $"JointSphere_{i}",
                Mesh = mesh
            };
            _worldRoot.AddChild(sphere);
            _jointSpheres.Add(sphere);
        }
    }

    private void ClearJointSpheres()
    {
        foreach (var sphere in _jointSpheres)
        {
            if (sphere.GetParent() is not null)
                sphere.GetParent().RemoveChild(sphere);
            sphere.QueueFree();
        }
        _jointSpheres.Clear();
    }

    // ── Bone picking ──────────────────────────────────────────────────────────

    private void TryPickBone(Vector2 mousePos)
    {
        if (_skeleton is null || _jointSpheres.Count == 0) return;

        // Map mouse position from SubViewportContainer to SubViewport coordinates.
        var vpSize   = (Vector2)_viewport.Size;
        var ctrlSize = Size;
        var vp       = new Vector2(
            mousePos.X * vpSize.X / ctrlSize.X,
            mousePos.Y * vpSize.Y / ctrlSize.Y);

        var rayOrigin = _camera.ProjectRayOrigin(vp);
        var rayDir    = _camera.ProjectRayNormal(vp);

        var bestBone = -1;
        var bestT    = float.MaxValue;

        for (var i = 0; i < _jointSpheres.Count; i++)
        {
            var center  = _jointSpheres[i].GlobalPosition;
            var oc      = rayOrigin - center;
            var b       = oc.Dot(rayDir);
            var c       = oc.Dot(oc) - PickRayRadius * PickRayRadius;
            var discriminant = b * b - c;
            if (discriminant < 0f) continue;

            var t = -b - Mathf.Sqrt(discriminant);
            if (t < 0f) t = -b + Mathf.Sqrt(discriminant);
            if (t > 0f && t < bestT)
            {
                bestT    = t;
                bestBone = i;
            }
        }

        SelectBone(bestBone);
    }

    private void SelectBone(int boneIdx)
    {
        if (_skeleton is null) return;
        if (boneIdx == _selectedBoneIdx) return;

        // Reset old sphere colour.
        if (_selectedBoneIdx >= 0 && _selectedBoneIdx < _jointSpheres.Count)
            SetSphereColor(_selectedBoneIdx, SphereColorNormal);

        _selectedBoneIdx = boneIdx;

        if (_selectedBoneIdx >= 0 && _selectedBoneIdx < _jointSpheres.Count)
        {
            SetSphereColor(_selectedBoneIdx, SphereColorSelected);
            var boneName = _skeleton.GetBoneName(_selectedBoneIdx);
            SyncBoneListSelection(_selectedBoneIdx);
            BoneSelected?.Invoke(boneName);
            RunnerLogger.Info("PosingEditor", $"Bone selected: '{boneName}' (idx {_selectedBoneIdx}).");
        }
    }

    private void SetSphereColor(int idx, Color color)
    {
        if (_jointSpheres[idx].Mesh is SphereMesh sm
            && sm.SurfaceGetMaterial(0) is StandardMaterial3D mat)
        {
            mat.AlbedoColor = color;
        }
    }

    // ── Bone tree overlay ─────────────────────────────────────────────────────

    public void SetShowBoneTree(bool show)
    {
        _showBoneTree = show;

        if (show)
        {
            EnsureBonePanel();
            if (_skeleton is not null) PopulateBoneList();
        }
        else if (_bonePanel is not null)
        {
            _bonePanel.Visible = false;
        }
    }

    private void EnsureBonePanel()
    {
        if (_bonePanel is not null)
        {
            _bonePanel.Visible = true;
            return;
        }

        _bonePanel = new Panel
        {
            Name = "BonePanel",
            // Anchor to right 25 % of the SubViewportContainer.
            AnchorLeft   = 0.75f,
            AnchorRight  = 1.0f,
            AnchorTop    = 0.0f,
            AnchorBottom = 1.0f,
            OffsetLeft   = 0,
            OffsetRight  = 0,
            OffsetTop    = 0,
            OffsetBottom = 0
        };

        _boneList = new ItemList
        {
            Name = "BoneList",
            AnchorLeft   = 0f,
            AnchorRight  = 1.0f,
            AnchorTop    = 0.0f,
            AnchorBottom = 1.0f,
            OffsetLeft   = 4,
            OffsetRight  = -4,
            OffsetTop    = 4,
            OffsetBottom = -4
        };
        _boneList.ItemSelected += OnBoneListItemSelected;

        _bonePanel.AddChild(_boneList);
        AddChild(_bonePanel);
    }

    private void PopulateBoneList()
    {
        if (_boneList is null || _skeleton is null) return;
        _boneList.Clear();

        for (var i = 0; i < _skeleton.GetBoneCount(); i++)
        {
            var name   = _skeleton.GetBoneName(i);
            var parent = _skeleton.GetBoneParent(i);
            var indent = BuildBoneIndent(i);
            _boneList.AddItem($"{indent}{name}");
        }
    }

    private string BuildBoneIndent(int boneIdx)
    {
        if (_skeleton is null) return string.Empty;
        var depth = 0;
        var current = _skeleton.GetBoneParent(boneIdx);
        while (current >= 0)
        {
            depth++;
            current = _skeleton.GetBoneParent(current);
        }
        return new string(' ', depth * 2);
    }

    private void OnBoneListItemSelected(long idx)
    {
        SelectBone((int)idx);
    }

    private void SyncBoneListSelection(int boneIdx)
    {
        if (_boneList is null) return;
        _boneList.Select(boneIdx);
    }

    // ── Constraints ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by SmlUiBuilder after building all children.
    /// Collects JointConstraintNode children, stores their data, then removes them.
    /// </summary>
    public void FinalizeConstraints()
    {
        var toRemove = new List<JointConstraintNode>();

        foreach (var child in GetChildren())
        {
            if (child is JointConstraintNode jc)
            {
                if (!string.IsNullOrWhiteSpace(jc.BoneName))
                    _constraints[jc.BoneName] = jc.ToData();
                toRemove.Add(jc);
            }
        }

        foreach (var jc in toRemove)
        {
            RemoveChild(jc);
            jc.QueueFree();
        }

        if (_constraints.Count > 0)
            RunnerLogger.Info("PosingEditor", $"Loaded {_constraints.Count} joint constraint(s).");
    }

    public IReadOnlyDictionary<string, JointConstraintData> Constraints => _constraints;

    // ── Pose API ──────────────────────────────────────────────────────────────

    public void ResetPose()
    {
        if (_skeleton is null) return;
        for (var i = 0; i < _skeleton.GetBoneCount(); i++)
            _skeleton.ResetBonePose(i);
        PoseData.Clear();
        PoseReset?.Invoke();
    }

    public string SavePoseAsSml()
    {
        if (_skeleton is null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Pose {");
        foreach (var (boneName, rot) in PoseData)
        {
            var euler = rot.GetEuler() * (180f / Mathf.Pi);
            sb.AppendLine($"    Bone {{ name: \"{boneName}\"; rotX: {euler.X:F1}; rotY: {euler.Y:F1}; rotZ: {euler.Z:F1} }}");
        }
        sb.Append("}");
        return sb.ToString();
    }

    public void LoadPose(Dictionary<string, Quaternion> pose)
    {
        if (_skeleton is null) return;
        foreach (var (boneName, rot) in pose)
        {
            var idx = _skeleton.FindBone(boneName);
            if (idx < 0) continue;
            _skeleton.SetBonePoseRotation(idx, rot);
            PoseData[boneName] = rot;
        }
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    public void ResetView()
    {
        _yaw           = DefaultYaw;
        _pitch         = DefaultPitch;
        _orbitDistance = DefaultDistance;
        _cameraTarget  = Vector3.Zero;
        ApplyCameraOrbit();
    }

    public void SetCameraDistance(float distance)
    {
        _orbitDistance = Mathf.Clamp(Mathf.Abs(distance), MinDistance, MaxDistance);
        ApplyCameraOrbit();
    }

    public void AdjustCameraDistance(float delta)
    {
        _orbitDistance = Mathf.Clamp(_orbitDistance + delta, MinDistance, MaxDistance);
        ApplyCameraOrbit();
    }

    private void ApplyCameraOrbit()
    {
        var cosPitch = Mathf.Cos(_pitch);
        var sinPitch = Mathf.Sin(_pitch);
        var sinYaw   = Mathf.Sin(_yaw);
        var cosYaw   = Mathf.Cos(_yaw);

        var pos = new Vector3(
            _cameraTarget.X + _orbitDistance * cosPitch * sinYaw,
            _cameraTarget.Y + _orbitDistance * sinPitch,
            _cameraTarget.Z + _orbitDistance * cosPitch * cosYaw);

        _camera.Position = pos;
        _camera.LookAtFromPosition(pos, _cameraTarget, Vector3.Up);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Skeleton3D? FindSkeleton(Node root)
    {
        if (root is Skeleton3D sk) return sk;
        foreach (var child in root.GetChildren())
        {
            var found = FindSkeleton(child);
            if (found is not null) return found;
        }
        return null;
    }
}
