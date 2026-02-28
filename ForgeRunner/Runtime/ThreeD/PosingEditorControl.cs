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
    private Panel?              _bonePanel;
    private Tree?               _boneTree;
    private readonly List<TreeItem?> _boneTreeItems = [];
    private bool                _showBoneTree;

    // ── Angle overlay (shown while dragging a gizmo handle) ───────────────────
    private Label? _angleLabel;

    // ── Camera orbit ──────────────────────────────────────────────────────────
    private float   _yaw           = DefaultYaw;
    private float   _pitch         = DefaultPitch;
    private float   _orbitDistance = DefaultDistance;
    private Vector3 _cameraTarget  = Vector3.Zero;
    private bool    _isRotating;
    private bool    _isPanning;

    // ── Rotation gizmo ────────────────────────────────────────────────────────
    private readonly RotationGizmo3D _gizmo;
    private bool _gizmoDragging;

    // ── Poll-based drag tracking ──────────────────────────────────────────────
    // Motion events on SubViewportContainer are unreliable during left-button drags.
    // We poll GetMousePosition() every frame instead.
    private Vector2 _pollLastMousePos;

    // ── Bone selection ────────────────────────────────────────────────────────
    private int _selectedBoneIdx = -1;

    // ── Joint constraints (bone name → data) ──────────────────────────────────
    private readonly Dictionary<string, JointConstraintData> _constraints
        = new(StringComparer.OrdinalIgnoreCase);

    // ── Pose data (normalized bone name → local rotation quaternion) ──────────
    public Dictionary<string, Quaternion> PoseData { get; } = new(StringComparer.OrdinalIgnoreCase);

    // ── Bone-name normalization ────────────────────────────────────────────────
    /// <summary>
    /// When true, well-known rig prefixes (mixamorig:, mixamo1:, CC_Base_, …)
    /// are stripped from bone names before they appear in PoseData, the bone
    /// tree, and any saved files.  Default: true.
    /// </summary>
    public bool NormalizeNames { get; set; } = true;

    private string NormBone(string raw) =>
        NormalizeNames ? BoneNameNormalizer.Normalize(raw) : raw;

    // ── Scene props (additional static models in the viewport) ────────────────
    public sealed record ScenePropData(string Path, float PosX, float PosY, float PosZ, float RotY, float Scale);
    private readonly List<(Node3D Node, ScenePropData Data)> _sceneProps = [];

    public IReadOnlyList<ScenePropData> SceneProps =>
        _sceneProps.ConvertAll(p => p.Data);

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<string>?             BoneSelected;
    public event Action<string, Quaternion>? PoseChanged;
    public event Action?                     PoseReset;
    public event Action<int, string>?        ScenePropAdded;    // (index, path)
    public event Action<int>?                ScenePropRemoved;  // (index)

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

        _gizmo = new RotationGizmo3D();

        var env = new Godot.Environment
        {
            BackgroundMode     = Godot.Environment.BGMode.Color,
            BackgroundColor    = new Color(0.13f, 0.13f, 0.13f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor  = new Color(0.4f, 0.4f, 0.4f),
        };
        var worldEnv = new WorldEnvironment { Name = "WorldEnv", Environment = env };

        _worldRoot.AddChild(_camera);
        _worldRoot.AddChild(_light);
        _worldRoot.AddChild(_gizmo);
        _worldRoot.AddChild(worldEnv);
        _worldRoot.AddChild(CreateFloorGrid());
        _viewport.AddChild(_worldRoot);
        AddChild(_viewport);

        // Angle label — 2D overlay shown while dragging a gizmo handle.
        _angleLabel = new Label
        {
            Name             = "AngleLabel",
            Visible          = false,
            AnchorLeft       = 0.5f,
            AnchorRight      = 0.5f,
            AnchorTop        = 1.0f,
            AnchorBottom     = 1.0f,
            OffsetLeft       = -80f,
            OffsetRight      = 80f,
            OffsetTop        = -44f,
            OffsetBottom     = -12f,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        AddChild(_angleLabel);

        ResetView();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Button events only. Motion during drag/orbit/pan is polled in _Process
    /// because SubViewportContainer does not reliably deliver left-button motion
    /// events via _GuiInput or _Input.
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
            HandleMouseButton(mb);
    }

    private void HandleMouseButton(InputEventMouseButton mb)
    {
        if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            var (rayOrigin, rayDir) = BuildRay(mb.Position);
            var hitHandle = _gizmo.TryPickHandle(rayOrigin, rayDir);
            if (hitHandle >= 0)
            {
                _gizmo.BeginDrag(hitHandle);
                _gizmoDragging      = true;
                _pollLastMousePos   = GetViewport().GetMousePosition();
                GrabFocus();
            }
            else
            {
                TryPickBone(mb.Position);
            }
            AcceptEvent();
            return;
        }

        if (mb.ButtonIndex == MouseButton.Right)
        {
            _isRotating = mb.Pressed;
            if (_isRotating)
            {
                _pollLastMousePos = GetViewport().GetMousePosition();
                GrabFocus();
            }
            AcceptEvent();
            return;
        }

        if (mb.ButtonIndex == MouseButton.Middle)
        {
            _isPanning = mb.Pressed;
            if (_isPanning)
            {
                _pollLastMousePos = GetViewport().GetMousePosition();
                GrabFocus();
            }
            AcceptEvent();
            return;
        }

        if (!mb.Pressed) return;

        if (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown)
        {
            // Only zoom when the mouse is NOT hovering over the bone-tree panel.
            // If it is, the scroll event leaked up from the Tree hitting its scroll
            // limit — don't hijack it for camera zoom.
            if (_bonePanel is not null && _bonePanel.Visible
                && _bonePanel.GetRect().HasPoint(mb.Position))
            {
                return;
            }

            AdjustCameraDistance(mb.ButtonIndex == MouseButton.WheelUp ? -0.3f : 0.3f);
            AcceptEvent();
        }
    }

    private void FirePoseChangedForSelectedBone()
    {
        if (_skeleton is null || _selectedBoneIdx < 0) return;
        var boneName = NormBone(_skeleton.GetBoneName(_selectedBoneIdx));
        var rot      = _skeleton.GetBonePoseRotation(_selectedBoneIdx);
        PoseData[boneName] = rot;
        PoseChanged?.Invoke(boneName, rot);
    }

    private void UpdateAngleLabel()
    {
        if (_angleLabel is null) return;
        var axisName = _gizmo.DragAxis switch { 0 => "X", 1 => "Y", _ => "Z" };
        var deg      = _gizmo.DragAngleDegrees;
        _angleLabel.Text    = $"{axisName}  {deg:+0.0;-0.0;0.0}°";
        _angleLabel.Visible = true;
    }

    // ── Frame update ──────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        UpdateJointSpherePositions();
        PollDrag();
    }

    /// <summary>
    /// Poll mouse position every frame to drive drag/orbit/pan.
    /// This is more reliable than motion events on SubViewportContainer.
    /// </summary>
    private void PollDrag()
    {
        var mousePos = GetViewport().GetMousePosition();
        var d        = mousePos - _pollLastMousePos;
        _pollLastMousePos = mousePos;

        if (d == Vector2.Zero) return;

        if (_gizmoDragging)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
            {
                _gizmo.EndDrag();
                _gizmoDragging = false;
                if (_angleLabel is not null) _angleLabel.Visible = false;
                return;
            }
            _gizmo.UpdateDrag(d);
            FirePoseChangedForSelectedBone();
            UpdateAngleLabel();
        }
        else if (_isRotating)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Right))
            {
                _isRotating = false;
                return;
            }
            _yaw   -= d.X * 0.01f;
            _pitch  = Mathf.Clamp(_pitch + d.Y * 0.01f, -1.2f, 1.2f);
            ApplyCameraOrbit();
        }
        else if (_isPanning)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Middle))
            {
                _isPanning = false;
                return;
            }
            var basis    = _camera.GlobalTransform.Basis;
            var right    = basis.X.Normalized();
            var up       = basis.Y.Normalized();
            var panScale = _orbitDistance * 0.0015f;
            _cameraTarget += (-right * d.X + up * d.Y) * panScale;
            ApplyCameraOrbit();
        }
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

        _lastLoadedSource = source;
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

        // Stop any embedded animations so they don't override manual bone poses.
        StopAllAnimationPlayers(_modelRoot);

        _skeleton = FindSkeleton(_modelRoot);

        if (_skeleton is null)
        {
            RunnerLogger.Warn("PosingEditor", $"No Skeleton3D found in '{label}'.");
        }
        else
        {
            RunnerLogger.Info("PosingEditor", $"Model loaded from '{label}'. Skeleton: '{_skeleton.Name}', bones: {_skeleton.GetBoneCount()}.");
            BuildJointSpheres();
            if (_showBoneTree || _isExternalBoneTree) PopulateBoneList();
        }
    }

    // ── Joint spheres ─────────────────────────────────────────────────────────

    private bool _jointSpheresVisible = true;

    /// <summary>Show or hide all joint spheres in the 3D viewport.</summary>
    public void SetJointSpheresVisible(bool visible)
    {
        _jointSpheresVisible = visible;
        foreach (var sphere in _jointSpheres)
            sphere.Visible = visible;
    }

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
                Name    = $"JointSphere_{i}",
                Mesh    = mesh,
                Visible = _jointSpheresVisible
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

    // ── Ray helpers ───────────────────────────────────────────────────────────

    /// <summary>Convert a SubViewportContainer mouse position to a world-space ray.</summary>
    private (Vector3 origin, Vector3 dir) BuildRay(Vector2 mousePos)
    {
        var vpSize   = (Vector2)_viewport.Size;
        var ctrlSize = Size;
        var vp       = new Vector2(
            mousePos.X * vpSize.X / ctrlSize.X,
            mousePos.Y * vpSize.Y / ctrlSize.Y);
        return (_camera.ProjectRayOrigin(vp), _camera.ProjectRayNormal(vp));
    }

    // ── Bone picking ──────────────────────────────────────────────────────────

    private void TryPickBone(Vector2 mousePos)
    {
        if (_skeleton is null || _jointSpheres.Count == 0) return;

        var (rayOrigin, rayDir) = BuildRay(mousePos);

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
            var boneName = NormBone(_skeleton.GetBoneName(_selectedBoneIdx));
            SyncBoneListSelection(_selectedBoneIdx);
            BoneSelected?.Invoke(boneName);
            RunnerLogger.Info("PosingEditor", $"Bone selected: '{boneName}' (idx {_selectedBoneIdx}).");

            // Show gizmo on selected bone.
            _gizmo.AttachToBone(_skeleton, _selectedBoneIdx);
            ApplyConstraintsToGizmo(boneName);
        }
        else
        {
            _gizmo.Detach();
        }
    }

    private void ApplyConstraintsToGizmo(string boneName)
    {
        if (_constraints.TryGetValue(boneName, out var c))
            _gizmo.SetConstraints(c.MinX, c.MaxX, c.MinY, c.MaxY, c.MinZ, c.MaxZ);
        else
            _gizmo.SetConstraints(-180f, 180f, -180f, 180f, -180f, 180f);
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

    // When true the bone-tree lives in an external Dock panel, not inside the
    // viewport overlay.  The internal Panel is not created in this case.
    private bool _isExternalBoneTree;

    /// <summary>
    /// Attach a <see cref="Tree"/> that lives outside this control (e.g. in a
    /// DockingContainer) as the bone-list.  The PosingEditor will not create
    /// its own internal overlay panel; the supplied tree is used instead.
    /// Call this before or after model loading — the tree is (re-)populated
    /// whenever a skeleton is available.
    /// </summary>
    public void SetExternalBoneTree(Tree tree)
    {
        // Detach from any previous tree to avoid duplicate event subscriptions.
        if (_boneTree is not null)
            _boneTree.ItemSelected -= OnBoneTreeSelected;

        _isExternalBoneTree = true;
        _boneTree            = tree;
        _boneTree.ItemSelected += OnBoneTreeSelected;

        if (_skeleton is not null) PopulateBoneList();
    }

    public void SetShowBoneTree(bool show)
    {
        _showBoneTree = show;

        if (_isExternalBoneTree)
        {
            // External dock tree: nothing to show/hide here.
            // If the skeleton is already loaded, make sure the tree is populated.
            if (show && _skeleton is not null) PopulateBoneList();
            return;
        }

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
        if (_isExternalBoneTree) return;   // external dock handles the tree

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

        _boneTree = new Tree
        {
            Name             = "BoneTree",
            AnchorLeft       = 0f,
            AnchorRight      = 1.0f,
            AnchorTop        = 0.0f,
            AnchorBottom     = 1.0f,
            OffsetLeft       = 0,
            OffsetRight      = 0,
            OffsetTop        = 0,
            OffsetBottom     = 0,
            Columns          = 1,
            HideRoot         = true,
            SelectMode       = Tree.SelectModeEnum.Single,
        };
        _boneTree.ItemSelected += OnBoneTreeSelected;

        _bonePanel.AddChild(_boneTree);
        AddChild(_bonePanel);
    }

    private void PopulateBoneList()
    {
        if (_boneTree is null || _skeleton is null) return;
        _boneTree.Clear();
        _boneTreeItems.Clear();

        var boneCount = _skeleton.GetBoneCount();
        var root      = _boneTree.CreateItem();   // invisible root (HideRoot = true)

        // Pre-allocate slots so children can reference parents by index.
        for (var i = 0; i < boneCount; i++)
            _boneTreeItems.Add(null);

        // Godot guarantees parents have lower indices than their children.
        for (var i = 0; i < boneCount; i++)
        {
            var parentIdx  = _skeleton.GetBoneParent(i);
            var parentItem = parentIdx >= 0 ? _boneTreeItems[parentIdx] : root;
            var item       = _boneTree.CreateItem(parentItem);
            item.SetText(0, NormBone(_skeleton.GetBoneName(i)));
            // Collapse everything except top-level bones (parentIdx < 0 = direct children
            // of the invisible root, e.g. Hips).  This keeps the tree readable on load:
            // only Hips and its immediate children (Spine, LeftLegUp, RightLegUp, …) are
            // visible; the user expands further levels manually.
            item.Collapsed = parentIdx >= 0;
            _boneTreeItems[i] = item;
        }
    }

    private void OnBoneTreeSelected()
    {
        if (_boneTree is null) return;
        var item = _boneTree.GetSelected();
        if (item is null) return;
        var idx = _boneTreeItems.IndexOf(item);
        if (idx >= 0) SelectBone(idx);
    }

    private void SyncBoneListSelection(int boneIdx)
    {
        if (_boneTree is null) return;
        if (boneIdx < 0 || boneIdx >= _boneTreeItems.Count) return;
        var item = _boneTreeItems[boneIdx];
        if (item is null) return;

        // Expand ancestor chain so the item is visible.
        var parent = item.GetParent();
        while (parent is not null && parent != _boneTree.GetRoot())
        {
            parent.Collapsed = false;
            parent = parent.GetParent();
        }

        _boneTree.SetSelected(item, 0);
        _boneTree.ScrollToItem(item);
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
            // 1. Exact match (most common: pose was saved with same skeleton)
            var idx = _skeleton.FindBone(boneName);

            // 2. Normalised-name match (pose from different rig, same naming base)
            if (idx < 0 && NormalizeNames)
            {
                for (var i = 0; i < _skeleton.GetBoneCount(); i++)
                {
                    if (string.Equals(NormBone(_skeleton.GetBoneName(i)),
                                      boneName, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
            }

            if (idx < 0) continue;
            _skeleton.SetBonePoseRotation(idx, rot);
            PoseData[NormBone(_skeleton.GetBoneName(idx))] = rot;
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

    /// <summary>
    /// Build a flat grid mesh on the XZ plane (Y = 0) to serve as a visual floor.
    /// 20 × 20 cells at 0.5 m each → total 10 m × 10 m.
    /// </summary>
    private static MeshInstance3D CreateFloorGrid()
    {
        const int   cells    = 20;
        const float cellSize = 0.5f;
        const float half     = cells * cellSize * 0.5f;

        var tool = new SurfaceTool();
        tool.Begin(Mesh.PrimitiveType.Lines);

        for (var i = 0; i <= cells; i++)
        {
            var t = -half + i * cellSize;

            // Line parallel to X axis
            tool.AddVertex(new Vector3(-half, 0f, t));
            tool.AddVertex(new Vector3( half, 0f, t));

            // Line parallel to Z axis
            tool.AddVertex(new Vector3(t, 0f, -half));
            tool.AddVertex(new Vector3(t, 0f,  half));
        }

        var arrayMesh = tool.Commit();
        arrayMesh.SurfaceSetMaterial(0, new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.45f, 0.45f, 0.45f),
        });

        return new MeshInstance3D { Name = "FloorGrid", Mesh = arrayMesh };
    }

    private static void StopAllAnimationPlayers(Node root)
    {
        if (root is AnimationPlayer ap)
        {
            RunnerLogger.Info("PosingEditor", $"Stopping AnimationPlayer '{ap.Name}'.");
            ap.Active = false;
            ap.Stop();
        }
        foreach (var child in root.GetChildren())
            StopAllAnimationPlayers(child);
    }

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

    // ── Scene props ───────────────────────────────────────────────────────────

    /// <summary>Add a static prop model to the scene. Returns the prop index.</summary>
    public int AddSceneProp(string path, float posX, float posY, float posZ)
    {
        if (string.IsNullOrWhiteSpace(path)) return -1;

        if (!TryLoadNode3D(path, out var node3D, out var label))
            return -1;

        if (node3D.GetParent() is not null)
            node3D.GetParent().RemoveChild(node3D);

        StopAllAnimationPlayers(node3D);
        _worldRoot.AddChild(node3D);

        var data = new ScenePropData(path, posX, posY, posZ, 0f, 1f);
        ApplyPropTransform(node3D, data);

        var idx = _sceneProps.Count;
        _sceneProps.Add((node3D, data));

        RunnerLogger.Info("PosingEditor", $"Scene prop added: '{label}' at ({posX},{posY},{posZ}).");
        ScenePropAdded?.Invoke(idx, path);
        return idx;
    }

    /// <summary>Remove a scene prop by index.</summary>
    public void RemoveSceneProp(int index)
    {
        if (index < 0 || index >= _sceneProps.Count) return;

        var (node, _) = _sceneProps[index];
        _sceneProps.RemoveAt(index);

        if (node.GetParent() is not null)
            node.GetParent().RemoveChild(node);
        node.QueueFree();

        ScenePropRemoved?.Invoke(index);
    }

    public int    GetScenePropCount()                   => _sceneProps.Count;
    public string GetScenePropPath(int index)            => index >= 0 && index < _sceneProps.Count ? _sceneProps[index].Data.Path : string.Empty;
    public float  GetScenePropPosX(int index)            => index >= 0 && index < _sceneProps.Count ? _sceneProps[index].Data.PosX : 0f;
    public float  GetScenePropPosY(int index)            => index >= 0 && index < _sceneProps.Count ? _sceneProps[index].Data.PosY : 0f;
    public float  GetScenePropPosZ(int index)            => index >= 0 && index < _sceneProps.Count ? _sceneProps[index].Data.PosZ : 0f;

    public void SetScenePropPos(int index, float x, float y, float z)
    {
        if (index < 0 || index >= _sceneProps.Count) return;
        var old  = _sceneProps[index].Data;
        var data = old with { PosX = x, PosY = y, PosZ = z };
        _sceneProps[index] = (_sceneProps[index].Node, data);
        ApplyPropTransform(_sceneProps[index].Node, data);
    }

    private static void ApplyPropTransform(Node3D node, ScenePropData data)
    {
        node.Position = new Vector3(data.PosX, data.PosY, data.PosZ);
        node.RotationDegrees = new Vector3(0f, data.RotY, 0f);
        node.Scale   = new Vector3(data.Scale, data.Scale, data.Scale);
    }

    // ── Project load/save ─────────────────────────────────────────────────────

    /// <summary>
    /// Build a serializable snapshot of the current project state
    /// (scene assets + all keyframes from the supplied timeline).
    /// </summary>
    public AnimationProjectData BuildProjectData(TimelineControl timeline)
    {
        var assets = new List<SceneAssetData>();

        // Primary character
        // We don't store the path internally yet — callers pass it via the
        // project file.  Collect from SceneProps + a placeholder for primary.
        // If _modelRoot exists but we have no stored path, use empty string.
        assets.Add(new SceneAssetData(
            Id:      "character",
            Src:     _lastLoadedSource,
            Primary: true,
            PosX: 0, PosY: 0, PosZ: 0, RotY: 0, Scale: 1));

        for (var i = 0; i < _sceneProps.Count; i++)
        {
            var d = _sceneProps[i].Data;
            assets.Add(new SceneAssetData(
                Id:      $"prop{i}",
                Src:     d.Path,
                Primary: false,
                PosX:    d.PosX,
                PosY:    d.PosY,
                PosZ:    d.PosZ,
                RotY:    d.RotY,
                Scale:   d.Scale));
        }

        // Snapshot keyframes from timeline
        var keyframes = new SortedDictionary<int, Dictionary<string, Quaternion>>();
        var kfCount   = timeline.GetKeyframeCount();
        for (var i = 0; i < kfCount; i++)
        {
            var frame = timeline.GetKeyframeFrameAt(i);
            var pose  = timeline.GetPoseAt(frame);
            if (pose is not null && pose.Count > 0)
                keyframes[frame] = new Dictionary<string, Quaternion>(pose, StringComparer.OrdinalIgnoreCase);
        }

        return new AnimationProjectData(timeline.Fps, timeline.TotalFrames, assets, keyframes);
    }

    /// <summary>
    /// Load a full project from an <see cref="AnimationProjectData"/> snapshot,
    /// populating both this editor and the supplied timeline.
    /// </summary>
    public void LoadProjectData(AnimationProjectData data, TimelineControl timeline)
    {
        // Clear existing state
        ClearSceneProps();
        timeline.ClearAllKeyframes();

        // Load assets
        foreach (var asset in data.SceneAssets)
        {
            if (string.IsNullOrWhiteSpace(asset.Src)) continue;
            if (asset.Primary)
            {
                SetModelSource(asset.Src);
            }
            else
            {
                AddSceneProp(asset.Src, asset.PosX, asset.PosY, asset.PosZ);
            }
        }

        // Restore timeline settings
        timeline.Fps         = data.Fps;
        timeline.TotalFrames = data.TotalFrames;

        // Restore keyframes
        foreach (var (frame, bones) in data.Keyframes)
            timeline.SetKeyframe(frame, bones);
    }

    private void ClearSceneProps()
    {
        for (var i = _sceneProps.Count - 1; i >= 0; i--)
            RemoveSceneProp(i);
    }

    // Track last loaded source so BuildProjectData can persist it
    private string _lastLoadedSource = string.Empty;
}
