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

public enum EditorMode      { Pose, Arrange }
public enum ArrangeEditMode { Move, Scale, Rotate }

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

    // ── Translation gizmo (for scene props) ───────────────────────────────────
    private readonly MoveGizmo3D   _moveGizmo;
    private readonly ScaleGizmo3D  _scaleGizmo;
    private bool _moveGizmoDragging;
    private bool _scaleGizmoDragging;
    private bool _rotateGizmoDraggingFreeNode;
    private int  _selectedPropIdx = -1;

    // ── Editor modes ──────────────────────────────────────────────────────────
    private EditorMode      _editorMode      = EditorMode.Pose;
    private ArrangeEditMode _arrangeEditMode = ArrangeEditMode.Move;

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
    public sealed record ScenePropData(
        string Path,
        string Name,
        float PosX, float PosY, float PosZ,
        float RotX, float RotY, float RotZ,
        float ScaleX, float ScaleY, float ScaleZ);
    private readonly List<(Node3D Node, ScenePropData Data)> _sceneProps = [];

    public IReadOnlyList<ScenePropData> SceneProps =>
        _sceneProps.ConvertAll(p => p.Data);

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<string>?             BoneSelected;
    public event Action<string, Quaternion>? PoseChanged;
    public event Action?                     PoseReset;
    public event Action<int, string>?        ScenePropAdded;    // (index, path)
    public event Action<int>?                ScenePropRemoved;  // (index)
    public event Action<int>?                ObjectSelected;    // (propIdx, -1 = deselect)
    public event Action<int, Vector3>?       ObjectMoved;       // (propIdx, newWorldPos)

    // ── Editor mode control ───────────────────────────────────────────────────

    /// <summary>Switch between "pose" (bone editing) and "arrange" (prop transform) modes.</summary>
    public void SetEditorMode(string mode)
    {
        _editorMode = mode.Equals("arrange", StringComparison.OrdinalIgnoreCase)
            ? EditorMode.Arrange : EditorMode.Pose;
        // Clear all gizmos and selections on mode change
        _gizmo.Detach();
        _moveGizmo.Detach();
        _scaleGizmo.Detach();
        _selectedBoneIdx = -1;
        _selectedPropIdx = -1;
        _gizmoDragging = _moveGizmoDragging = _scaleGizmoDragging = _rotateGizmoDraggingFreeNode = false;
    }

    /// <summary>Set the edit sub-mode used in Arrange mode: "move", "scale", or "rotate".</summary>
    public void SetArrangeEditMode(string mode)
    {
        _arrangeEditMode = mode.ToLowerInvariant() switch
        {
            "scale"  => ArrangeEditMode.Scale,
            "rotate" => ArrangeEditMode.Rotate,
            _        => ArrangeEditMode.Move,
        };
        // Re-attach gizmo to currently selected prop (swaps gizmo type)
        if (_selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
            AttachArrangeGizmo(_selectedPropIdx);
    }

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
            HandleInputLocally = true,   // prevents unhandled events from leaking to parent viewport
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

        _gizmo      = new RotationGizmo3D();
        _moveGizmo  = new MoveGizmo3D();
        _scaleGizmo = new ScaleGizmo3D();

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
        _worldRoot.AddChild(_moveGizmo);
        _worldRoot.AddChild(_scaleGizmo);
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
    ///
    /// AcceptEvent() is called unconditionally so that unhandled motion events
    /// are not forwarded into the SubViewport via _push_unhandled_input_internal,
    /// which would cause the "!is_inside_tree()" error after scene loading.
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
            HandleMouseButton(mb);

        // Consume all GUI input to prevent events from leaking into the SubViewport.
        AcceptEvent();
    }

    private void HandleMouseButton(InputEventMouseButton mb)
    {
        if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            var (rayOrigin, rayDir) = BuildRay(mb.Position);

            if (_editorMode == EditorMode.Pose)
            {
                // Pose mode — Priority 1: rotation gizmo handle (bone)
                var hitRotHandle = _gizmo.TryPickHandle(rayOrigin, rayDir);
                if (hitRotHandle >= 0)
                {
                    _gizmo.BeginDrag(hitRotHandle);
                    _gizmoDragging    = true;
                    _pollLastMousePos = GetViewport().GetMousePosition();
                    GrabFocus();
                    AcceptEvent();
                    return;
                }

                // Pose mode — Priority 2: bone picking
                if (TryPickBoneRay(rayOrigin, rayDir))
                {
                    AcceptEvent();
                    return;
                }
            }
            else
            {
                // Arrange mode — Priority 1: active gizmo handle
                int hitHandle = _arrangeEditMode switch
                {
                    ArrangeEditMode.Move   => _moveGizmo.TryPickHandle(rayOrigin, rayDir),
                    ArrangeEditMode.Scale  => _scaleGizmo.TryPickHandle(rayOrigin, rayDir),
                    ArrangeEditMode.Rotate => _gizmo.TryPickHandle(rayOrigin, rayDir),
                    _                      => -1,
                };
                if (hitHandle >= 0)
                {
                    switch (_arrangeEditMode)
                    {
                        case ArrangeEditMode.Move:
                            _moveGizmo.BeginDrag(hitHandle);
                            _moveGizmoDragging = true;
                            break;
                        case ArrangeEditMode.Scale:
                            _scaleGizmo.BeginDrag(hitHandle);
                            _scaleGizmoDragging = true;
                            break;
                        case ArrangeEditMode.Rotate:
                            _gizmo.BeginDrag(hitHandle);
                            _rotateGizmoDraggingFreeNode = true;
                            break;
                    }
                    _pollLastMousePos = GetViewport().GetMousePosition();
                    GrabFocus();
                    AcceptEvent();
                    return;
                }

                // Arrange mode — Priority 2: prop picking
                TryPickPropArrange(rayOrigin, rayDir);
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
        else if (_moveGizmoDragging)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
            {
                _moveGizmo.EndDrag();
                _moveGizmoDragging = false;
                return;
            }
            const float DepthScale = 0.0018f;
            _moveGizmo.UpdateDrag(_camera, mousePos, mousePos - d, DepthScale);

            // Sync ScenePropData with the node's new position
            if (_selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
            {
                var (node, old) = _sceneProps[_selectedPropIdx];
                var newPos = node.GlobalPosition;
                var updated = old with { PosX = newPos.X, PosY = newPos.Y, PosZ = newPos.Z };
                _sceneProps[_selectedPropIdx] = (node, updated);
                ObjectMoved?.Invoke(_selectedPropIdx, newPos);
            }
        }
        else if (_scaleGizmoDragging)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
            {
                _scaleGizmo.EndDrag();
                _scaleGizmoDragging = false;
                return;
            }
            const float DepthScale = 0.0018f;
            _scaleGizmo.UpdateDrag(_camera, mousePos, mousePos - d, DepthScale);

            // Sync ScenePropData with the node's new scale
            if (_selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
            {
                var (node, old) = _sceneProps[_selectedPropIdx];
                var s = node.Scale;
                var updated = old with { ScaleX = s.X, ScaleY = s.Y, ScaleZ = s.Z };
                _sceneProps[_selectedPropIdx] = (node, updated);
                ObjectMoved?.Invoke(_selectedPropIdx, node.GlobalPosition);
            }
        }
        else if (_rotateGizmoDraggingFreeNode)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
            {
                _gizmo.EndDrag();
                _rotateGizmoDraggingFreeNode = false;
                return;
            }
            _gizmo.UpdateDrag(d);

            // Sync ScenePropData with the node's new rotation
            if (_selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
            {
                var (node, old) = _sceneProps[_selectedPropIdx];
                var euler = node.RotationDegrees;
                var updated = old with { RotX = euler.X, RotY = euler.Y, RotZ = euler.Z };
                _sceneProps[_selectedPropIdx] = (node, updated);
                ObjectMoved?.Invoke(_selectedPropIdx, node.GlobalPosition);
            }
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

    private bool _jointSpheresVisible = false;

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

    private bool TryPickBoneRay(Vector3 rayOrigin, Vector3 rayDir)
    {
        if (_skeleton is null || _jointSpheres.Count == 0) return false;

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

        if (bestBone < 0) return false;
        SelectBone(bestBone);
        return true;
    }

    // ── Prop picking ──────────────────────────────────────────────────────────

    private void SelectProp(int propIdx)
    {
        if (propIdx == _selectedPropIdx) return;

        _selectedPropIdx = propIdx;

        if (propIdx >= 0 && propIdx < _sceneProps.Count)
        {
            AttachArrangeGizmo(propIdx);
            ObjectSelected?.Invoke(propIdx);
            RunnerLogger.Info("PosingEditor", $"Prop selected: idx={propIdx}.");
        }
        else
        {
            _moveGizmo.Detach();
            _scaleGizmo.Detach();
            _gizmo.Detach();
            if (propIdx == -1)
                ObjectSelected?.Invoke(-1);
        }
    }

    /// <summary>Attach the correct gizmo to the given prop based on the current ArrangeEditMode.</summary>
    private void AttachArrangeGizmo(int propIdx)
    {
        var (node, _) = _sceneProps[propIdx];
        _moveGizmo.Detach();
        _scaleGizmo.Detach();
        _gizmo.Detach();
        switch (_arrangeEditMode)
        {
            case ArrangeEditMode.Move:   _moveGizmo.AttachToNode(node);  break;
            case ArrangeEditMode.Scale:  _scaleGizmo.AttachToNode(node); break;
            case ArrangeEditMode.Rotate: _gizmo.AttachToFreeNode(node);  break;
        }
    }

    /// <summary>Prop picking in Arrange mode — selects the prop and attaches the active gizmo.</summary>
    private void TryPickPropArrange(Vector3 rayOrigin, Vector3 rayDir)
    {
        var bestIdx = -1;
        var bestT   = float.MaxValue;

        for (var i = 0; i < _sceneProps.Count; i++)
        {
            var (node, _) = _sceneProps[i];
            if (!node.IsInsideTree()) continue;

            var aabb = GetNodeWorldAabb(node);
            if (aabb.Size == Vector3.Zero) continue;

            if (RayAabbIntersect(rayOrigin, rayDir, aabb, out var t) && t < bestT)
            {
                bestT   = t;
                bestIdx = i;
            }
        }

        SelectProp(bestIdx);
    }

    /// <summary>Compute a world-space AABB for a node by summing all MeshInstance3D children.</summary>
    private static Aabb GetNodeWorldAabb(Node3D node)
    {
        var result = new Aabb();
        var first  = true;

        CollectMeshAabb(node, ref result, ref first);
        return result;
    }

    private static void CollectMeshAabb(Node node, ref Aabb result, ref bool first)
    {
        if (node is MeshInstance3D mi && mi.Mesh is not null)
        {
            var localAabb  = mi.Mesh.GetAabb();
            var worldAabb  = mi.GlobalTransform * localAabb;
            if (first) { result = worldAabb; first = false; }
            else        result = result.Merge(worldAabb);
        }

        foreach (var child in node.GetChildren())
            CollectMeshAabb(child, ref result, ref first);
    }

    /// <summary>Slab-based ray vs. AABB intersection test. Returns true if hit, with distance t.</summary>
    private static bool RayAabbIntersect(Vector3 origin, Vector3 dir, Aabb aabb, out float t)
    {
        t = float.MaxValue;
        var tMin = float.MinValue;
        var tMax = float.MaxValue;
        var end  = aabb.Position + aabb.Size;

        for (var axis = 0; axis < 3; axis++)
        {
            var o  = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            var d  = axis == 0 ? dir.X    : axis == 1 ? dir.Y    : dir.Z;
            var lo = axis == 0 ? aabb.Position.X : axis == 1 ? aabb.Position.Y : aabb.Position.Z;
            var hi = axis == 0 ? end.X  : axis == 1 ? end.Y  : end.Z;

            if (Mathf.Abs(d) < 1e-8f)
            {
                if (o < lo || o > hi) return false;
                continue;
            }

            var t1 = (lo - o) / d;
            var t2 = (hi - o) / d;
            if (t1 > t2) { (t1, t2) = (t2, t1); }
            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax) return false;
        }

        t = tMin > 0f ? tMin : tMax;
        return t > 0f;
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
            // Selecting a bone deselects any prop.
            if (_selectedPropIdx >= 0) { _moveGizmo.Detach(); _selectedPropIdx = -1; }

            SetSphereColor(_selectedBoneIdx, SphereColorSelected);
            var boneName = NormBone(_skeleton.GetBoneName(_selectedBoneIdx));
            SyncBoneListSelection(_selectedBoneIdx);
            BoneSelected?.Invoke(boneName);
            RunnerLogger.Info("PosingEditor", $"Bone selected: '{boneName}' (idx {_selectedBoneIdx}).");

            // Show rotation gizmo on selected bone.
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

        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var data = new ScenePropData(path, name, posX, posY, posZ, 0f, 0f, 0f, 1f, 1f, 1f);
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

        // Deselect move gizmo if it was on the removed prop.
        if (_selectedPropIdx == index)
        {
            _moveGizmo.Detach();
            _selectedPropIdx = -1;
        }

        var (node, _) = _sceneProps[index];
        _sceneProps.RemoveAt(index);

        // Keep _selectedPropIdx valid after removal.
        if (_selectedPropIdx > index) _selectedPropIdx--;

        if (node.GetParent() is not null)
            node.GetParent().RemoveChild(node);
        node.QueueFree();

        ScenePropRemoved?.Invoke(index);
    }

    public int    GetScenePropCount()       => _sceneProps.Count;
    public string GetScenePropPath(int i)   => i >= 0 && i < _sceneProps.Count ? _sceneProps[i].Data.Path : string.Empty;
    public string GetScenePropName(int i)   => i >= 0 && i < _sceneProps.Count ? _sceneProps[i].Data.Name : string.Empty;
    public Vector3 GetScenePropPos(int i)   => i >= 0 && i < _sceneProps.Count ? _sceneProps[i].Node.GlobalPosition : Vector3.Zero;
    public Vector3 GetScenePropRot(int i)   => i >= 0 && i < _sceneProps.Count ? _sceneProps[i].Node.RotationDegrees : Vector3.Zero;

    public void SetScenePropPos(int index, float x, float y, float z)
    {
        if (index < 0 || index >= _sceneProps.Count) return;
        var (node, old) = _sceneProps[index];
        var data = old with { PosX = x, PosY = y, PosZ = z };
        _sceneProps[index] = (node, data);
        ApplyPropTransform(node, data);
    }

    public void SetScenePropRot(int index, float x, float y, float z)
    {
        if (index < 0 || index >= _sceneProps.Count) return;
        var (node, old) = _sceneProps[index];
        var data = old with { RotX = x, RotY = y, RotZ = z };
        _sceneProps[index] = (node, data);
        ApplyPropTransform(node, data);
    }

    private static void ApplyPropTransform(Node3D node, ScenePropData data)
    {
        node.Position        = new Vector3(data.PosX, data.PosY, data.PosZ);
        node.RotationDegrees = new Vector3(data.RotX, data.RotY, data.RotZ);
        node.Scale           = new Vector3(data.ScaleX, data.ScaleY, data.ScaleZ);
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
        assets.Add(new SceneAssetData(
            Id:      "hero",
            Name:    System.IO.Path.GetFileNameWithoutExtension(_lastLoadedSource),
            Src:     _lastLoadedSource,
            Primary: true,
            PosX: 0, PosY: 0, PosZ: 0, RotX: 0, RotY: 0, RotZ: 0, ScaleX: 1, ScaleY: 1, ScaleZ: 1));

        for (var i = 0; i < _sceneProps.Count; i++)
        {
            var d   = _sceneProps[i].Data;
            var pos = _sceneProps[i].Node.GlobalPosition;
            var rot = _sceneProps[i].Node.RotationDegrees;
            assets.Add(new SceneAssetData(
                Id:      $"prop{i}",
                Name:    d.Name,
                Src:     d.Path,
                Primary: false,
                PosX:    pos.X,
                PosY:    pos.Y,
                PosZ:    pos.Z,
                RotX:    rot.X,
                RotY:    rot.Y,
                RotZ:    rot.Z,
                ScaleX:  d.ScaleX,
                ScaleY:  d.ScaleY,
                ScaleZ:  d.ScaleZ));
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
    /// <param name="data">Project data to restore.</param>
    /// <param name="timeline">Timeline to populate with keyframes.</param>
    /// <param name="projectDirectory">
    /// Filesystem directory of the .scene file. When set, <c>res://</c> paths in
    /// the file are resolved relative to this directory instead of Godot's resource system.
    /// </param>
    public void LoadProjectData(AnimationProjectData data, TimelineControl timeline, string projectDirectory = "")
    {
        // Clear existing state
        ClearSceneProps();
        timeline.ClearAllKeyframes();

        // Load assets
        foreach (var asset in data.SceneAssets)
        {
            if (string.IsNullOrWhiteSpace(asset.Src)) continue;
            var resolvedSrc = ResolveProjectPath(asset.Src, projectDirectory);
            if (asset.Primary)
            {
                SetModelSource(resolvedSrc);
            }
            else
            {
                var idx = AddSceneProp(resolvedSrc, asset.PosX, asset.PosY, asset.PosZ);
                if (idx >= 0)
                    SetScenePropRot(idx, asset.RotX, asset.RotY, asset.RotZ);
            }
        }

        // Restore timeline settings
        timeline.Fps         = data.Fps;
        timeline.TotalFrames = data.TotalFrames;

        // Restore keyframes
        foreach (var (frame, bones) in data.Keyframes)
            timeline.SetKeyframe(frame, bones);
    }

    /// <summary>
    /// Resolves a source path from a .scene file.
    /// <c>res://relative/path</c> is mapped to <c>{projectDirectory}/relative/path</c>
    /// when a project directory is known; otherwise returned unchanged.
    /// </summary>
    private static string ResolveProjectPath(string src, string projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory)) return src;
        if (!src.StartsWith("res://", StringComparison.OrdinalIgnoreCase)) return src;
        var relative = src["res://".Length..];
        return Path.Combine(projectDirectory, relative);
    }

    private void ClearSceneProps()
    {
        for (var i = _sceneProps.Count - 1; i >= 0; i--)
            RemoveSceneProp(i);
    }

    // Track last loaded source so BuildProjectData can persist it
    private string _lastLoadedSource = string.Empty;
}
