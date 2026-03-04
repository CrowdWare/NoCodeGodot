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
using System.Globalization;
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
    private int  _selectedPropIdx    = -1;
    private bool _characterSelected;
    private int  _selectedCharacterIdx = -1;

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

    // ── Pose data (character-scoped bone name → local rotation quaternion) ───
    public Dictionary<string, Quaternion> PoseData { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Bone names are kept exactly as they come from the loaded skeleton.
    private static string NormBone(string raw) => raw;

    // ── Scene props (additional static models in the viewport) ────────────────
    public sealed record ScenePropData(
        string Path,
        string Name,
        float PosX, float PosY, float PosZ,
        float RotX, float RotY, float RotZ,
        float ScaleX, float ScaleY, float ScaleZ);

    private sealed class SceneCharacterEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Node3D Node { get; set; } = null!;
        public Skeleton3D Skeleton { get; set; } = null!;
    }

    private readonly List<SceneCharacterEntry> _sceneCharacters = [];
    private int _activeCharacterIdx = -1;
    private int _nextCharacterId = 1;
    private string _projectDirectory = string.Empty;

    private readonly List<(Node3D Node, ScenePropData Data)> _sceneProps = [];
    private readonly Dictionary<string, string> _sceneProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aiPrompt"] = "Keep exact pose and composition, camera framing, body proportions, and left-right orientation. Render photorealistic humans with realistic skin pores, hair strands, and clearly defined abdominal muscles. Match clothing design, colors, and materials from the extra image while preserving visible anatomy where appropriate. Apply only scene style, lighting, mood, and color palette from the style image. Never add UI overlays, gizmos, handles, axes, bone markers, or text.",
        ["aiNegativePrompt"] = string.Empty,
        ["aiStyleImagePath"] = "res:/input/style.jpg",
        ["aiExtraImagePath"] = "res:/input/clothing.jpg",
        ["aiImageModel"] = "grok-imagine-image",
        ["aiVideoModel"] = "grok-imagine-video",
    };

    public IReadOnlyList<ScenePropData> SceneProps =>
        _sceneProps.ConvertAll(p => p.Data);

    public void SetProjectProperty(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _sceneProperties[key] = value ?? string.Empty;
    }

    public string GetProjectProperty(string key, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(key)) return fallback ?? string.Empty;
        return _sceneProperties.TryGetValue(key, out var value) ? value : (fallback ?? string.Empty);
    }

    private Dictionary<string, string> BuildScenePropertiesForSave()
    {
        var props = new Dictionary<string, string>(_sceneProperties, StringComparer.OrdinalIgnoreCase);

        // V1 schema: always persist the complete AI property set.
        if (!props.ContainsKey("aiPrompt"))
            props["aiPrompt"] = "Keep exact pose and composition, camera framing, body proportions, and left-right orientation. Render photorealistic humans with realistic skin pores, hair strands, and clearly defined abdominal muscles. Match clothing design, colors, and materials from the extra image while preserving visible anatomy where appropriate. Apply only scene style, lighting, mood, and color palette from the style image. Never add UI overlays, gizmos, handles, axes, bone markers, or text.";
        if (!props.ContainsKey("aiNegativePrompt"))
            props["aiNegativePrompt"] = string.Empty;
        if (!props.ContainsKey("aiStyleImagePath"))
            props["aiStyleImagePath"] = "res:/input/style.jpg";
        if (!props.ContainsKey("aiExtraImagePath"))
            props["aiExtraImagePath"] = "res:/input/clothing.jpg";
        if (!props.ContainsKey("aiImageModel"))
            props["aiImageModel"] = "grok-imagine-image";
        if (!props.ContainsKey("aiVideoModel"))
            props["aiVideoModel"] = "grok-imagine-video";

        return props;
    }

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<string>?             BoneSelected;
    public event Action<string, Quaternion>? PoseChanged;
    public event Action?                     PoseReset;
    public event Action<int, string>?        ScenePropAdded;    // (index, path)
    public event Action<int>?                ScenePropRemoved;  // (index)
    public event Action<int>?                ObjectSelected;    // (propIdx, -1 = deselect)
    public event Action<int, Vector3>?       ObjectMoved;       // (propIdx, newWorldPos)
    public event Action<string, int>?        ExportProgress;    // (filename, percent 0‥100)
    public event Action<int, string>?        FrameRangeExportFinished; // (written, outputDirectory)

    // ── Async frame-range export state ───────────────────────────────────────
    private bool _frameExportRunning;
    private TimelineControl? _frameExportTimeline;
    private int _frameExportCurrent;
    private int _frameExportEnd;
    private int _frameExportTotal;
    private int _frameExportWritten;
    private string _frameExportOutputDir = string.Empty;
    private bool _frameExportAwaitingCapture;
    private bool _renderOverlaySuppressed;
    private bool _savedJointSpheresVisible;
    private bool _savedRotationGizmoVisible;
    private bool _savedMoveGizmoVisible;
    private bool _savedScaleGizmoVisible;
    private bool _savedAngleLabelVisible;

    // ── Editor mode control ───────────────────────────────────────────────────

    /// <summary>Switch between "pose" (bone editing) and "arrange" (prop transform) modes.</summary>
    public void SetEditorMode(string mode)
    {
        var targetMode = mode.Equals("arrange", StringComparison.OrdinalIgnoreCase)
            ? EditorMode.Arrange : EditorMode.Pose;
        if (targetMode == _editorMode)
            return;

        _editorMode = targetMode;
        _gizmoDragging = _moveGizmoDragging = _scaleGizmoDragging = _rotateGizmoDraggingFreeNode = false;

        if (_editorMode == EditorMode.Pose)
        {
            // Leaving Arrange: detach arrange gizmos but keep selected bone if one
            // was already chosen via the bone tree.
            _moveGizmo.Detach();
            _scaleGizmo.Detach();
            _selectedPropIdx = -1;
            _characterSelected = false;

            if (_skeleton is not null
                && _selectedBoneIdx >= 0
                && _selectedBoneIdx < _skeleton.GetBoneCount())
            {
                _gizmo.AttachToBone(_skeleton, _selectedBoneIdx);
                var boneName = NormBone(_skeleton.GetBoneName(_selectedBoneIdx));
                ApplyConstraintsToGizmo(boneName);
            }
            else
            {
                _gizmo.Detach();
                _selectedBoneIdx = -1;
            }

            return;
        }

        // Switching to Arrange clears bone editing selection.
        _gizmo.Detach();
        _moveGizmo.Detach();
        _scaleGizmo.Detach();
        _selectedBoneIdx = -1;
        _selectedPropIdx = -1;
        _characterSelected = false;
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
        // Re-attach gizmo to currently selected object (swaps gizmo type)
        if (_characterSelected
            && _selectedCharacterIdx >= 0
            && _selectedCharacterIdx < _sceneCharacters.Count)
            AttachArrangeGizmoToNode(_sceneCharacters[_selectedCharacterIdx].Node);
        else if (_selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
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
        var characterId = GetCurrentSkeletonCharacterId();
        var key = BuildBoneKey(characterId, boneName);
        var rot      = _skeleton.GetBonePoseRotation(_selectedBoneIdx);

        // Deduplicate legacy/alias keys for the same edited bone on this character
        // (e.g. mixamorig_LeftArm vs mixamorig1_LeftArm).
        var canonicalEdited = CanonicalBoneAlias(boneName);
        var removeKeys = new List<string>();
        foreach (var existingKey in PoseData.Keys)
        {
            if (string.Equals(existingKey, key, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!TrySplitBoneKey(existingKey, out var scopedId, out var existingBone))
                continue;
            if (!string.Equals(scopedId, characterId, StringComparison.OrdinalIgnoreCase))
                continue;

            var sameBySkeletonIndex = _skeleton.FindBone(existingBone) == _selectedBoneIdx;
            var sameByAlias = string.Equals(CanonicalBoneAlias(existingBone), canonicalEdited, StringComparison.OrdinalIgnoreCase);
            if (sameBySkeletonIndex || sameByAlias)
                removeKeys.Add(existingKey);
        }
        foreach (var removeKey in removeKeys)
            PoseData.Remove(removeKey);

        PoseData[key] = rot;
        PoseChanged?.Invoke(key, rot);
    }

    private static string CanonicalBoneAlias(string boneName)
    {
        if (string.IsNullOrWhiteSpace(boneName))
            return string.Empty;

        var lower = boneName.ToLowerInvariant();
        if (!lower.StartsWith("mixamorig", StringComparison.Ordinal))
            return lower;

        var idx = "mixamorig".Length;
        while (idx < lower.Length && char.IsDigit(lower[idx]))
            idx++;
        if (idx < lower.Length && lower[idx] == '_')
            return lower[(idx + 1)..];

        return lower;
    }

    private void UpdateAngleLabel()
    {
        if (_angleLabel is null) return;
        var axisName = _gizmo.DragAxis switch { 0 => "X", 1 => "Y", _ => "Z" };
        var deg      = _gizmo.DragAngleDegrees;
        _angleLabel.Text    = $"{axisName}  {deg:+0.0;-0.0;0.0}°";
        _angleLabel.Visible = true;
    }

    private void UpdateArrangeLabel()
    {
        if (_angleLabel is null) return;
        string text;
        if (_moveGizmoDragging && _moveGizmo.IsDragging)
        {
            var disp = _moveGizmo.DragTotalDisplacement;
            text = $"{_moveGizmo.DragAxisLabel}  {disp:+0.00;-0.00;0.00}";
        }
        else if (_scaleGizmoDragging && _scaleGizmo.IsDragging)
        {
            var s = _scaleGizmo.DragCurrentScale;
            text = $"{_scaleGizmo.DragAxisLabel}  {s:0.000}×";
        }
        else if (_rotateGizmoDraggingFreeNode && _gizmo.IsDragging)
        {
            var axisName = _gizmo.DragAxis switch { 0 => "X", 1 => "Y", _ => "Z" };
            text = $"{axisName}  {_gizmo.DragAngleDegrees:+0.0;-0.0;0.0}°";
        }
        else return;
        _angleLabel.Text    = text;
        _angleLabel.Visible = true;
    }

    // ── 2D selection overlay ──────────────────────────────────────────────────

    private static readonly Color SelectionBorderColor = new(1f, 0.8f, 0.1f, 0.9f);

    /// <summary>
    /// Draws a yellow 2D bounding-box outline around the currently selected object
    /// (scene prop or character) by projecting its world-space AABB corners onto screen space.
    /// </summary>
    public override void _Draw()
    {
        Node3D? target = null;
        if (_characterSelected
            && _selectedCharacterIdx >= 0
            && _selectedCharacterIdx < _sceneCharacters.Count
            && _sceneCharacters[_selectedCharacterIdx].Node.IsInsideTree())
            target = _sceneCharacters[_selectedCharacterIdx].Node;
        else if (_selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
        {
            var (node, _) = _sceneProps[_selectedPropIdx];
            if (node.IsInsideTree()) target = node;
        }
        if (target is null) return;

        var aabb = GetNodeWorldAabb(target);
        if (aabb.Size == Vector3.Zero) return;

        var vpSize   = (Vector2)_viewport.Size;
        var ctrlSize = Size;
        if (vpSize.X <= 0f || vpSize.Y <= 0f) return;

        var scaleX = ctrlSize.X / vpSize.X;
        var scaleY = ctrlSize.Y / vpSize.Y;

        // 8 corners of the AABB
        var p  = aabb.Position;
        var e  = aabb.End;
        Span<Vector3> corners = stackalloc Vector3[8]
        {
            new(p.X, p.Y, p.Z), new(e.X, p.Y, p.Z),
            new(e.X, e.Y, p.Z), new(p.X, e.Y, p.Z),
            new(p.X, p.Y, e.Z), new(e.X, p.Y, e.Z),
            new(e.X, e.Y, e.Z), new(p.X, e.Y, e.Z),
        };

        // Project to 2D screen coords; skip if any corner is behind the camera
        Span<Vector2> sc = stackalloc Vector2[8];
        var camForward = -_camera.GlobalTransform.Basis.Z;
        for (var i = 0; i < 8; i++)
        {
            var toCorner = corners[i] - _camera.GlobalPosition;
            if (toCorner.Dot(camForward) <= 0f) return; // corner behind camera
            var vp2d = _camera.UnprojectPosition(corners[i]);
            sc[i] = new Vector2(vp2d.X * scaleX, vp2d.Y * scaleY);
        }

        // 12 edges of the box (indices into corners[])
        ReadOnlySpan<(int, int)> edges = stackalloc (int, int)[12]
        {
            (0,1),(1,2),(2,3),(3,0),  // back face
            (4,5),(5,6),(6,7),(7,4),  // front face
            (0,4),(1,5),(2,6),(3,7),  // connecting edges
        };

        foreach (var (a, b) in edges)
            DrawLine(sc[a], sc[b], SelectionBorderColor, 2f);
    }

    // ── Frame update ──────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        TickFrameRangeExport();
        UpdateJointSpherePositions();
        if (_selectedPropIdx >= 0 || _characterSelected) QueueRedraw();
        PollDrag();
    }

    private void TickFrameRangeExport()
    {
        if (!_frameExportRunning || _frameExportTimeline is null)
        {
            return;
        }

        if (_frameExportCurrent > _frameExportEnd)
        {
            FinishFrameRangeExport();
            return;
        }

        if (!_frameExportAwaitingCapture)
        {
            _frameExportTimeline.SetCurrentFrame(_frameExportCurrent);
            var pose = _frameExportTimeline.GetPoseAt(_frameExportCurrent);
            if (pose is not null)
            {
                LoadPose(pose);
            }

            foreach (var character in _sceneCharacters)
            {
                character.Skeleton.ForceUpdateAllBoneTransforms();
                character.Node.ForceUpdateTransform();
            }
            _worldRoot.ForceUpdateTransform();

            // Defer capture to next process tick to ensure this frame is visibly rendered.
            _frameExportAwaitingCapture = true;
            return;
        }

        RenderingServer.ForceDraw();
        RenderingServer.ForceDraw();

        var fileName = "frame_" + _frameExportWritten.ToString("D4", CultureInfo.InvariantCulture) + ".png";
        var outputPath = Path.Combine(_frameExportOutputDir, fileName);
        if (!ExportCurrentFramePng(outputPath))
        {
            RunnerLogger.Warn("PosingEditor", $"Frame range export stopped at frame {_frameExportCurrent}.");
            FinishFrameRangeExport();
            return;
        }

        _frameExportWritten++;
        var percent = (int)Math.Round((_frameExportWritten / (double)Math.Max(1, _frameExportTotal)) * 100.0);
        ExportProgress?.Invoke(fileName, Math.Clamp(percent, 0, 100));

        _frameExportCurrent++;
        _frameExportAwaitingCapture = false;
    }

    private void FinishFrameRangeExport()
    {
        var written = _frameExportWritten;
        var outputDir = _frameExportOutputDir;

        _frameExportRunning = false;
        _frameExportTimeline = null;
        _frameExportCurrent = 0;
        _frameExportEnd = 0;
        _frameExportTotal = 0;
        _frameExportWritten = 0;
        _frameExportAwaitingCapture = false;
        _frameExportOutputDir = string.Empty;
        RestoreOverlaysAfterRender();

        FrameRangeExportFinished?.Invoke(written, outputDir);
    }

    private void SuppressOverlaysForRender()
    {
        if (_renderOverlaySuppressed) return;
        _renderOverlaySuppressed = true;

        _savedJointSpheresVisible = _jointSpheresVisible;
        _savedRotationGizmoVisible = _gizmo.Visible;
        _savedMoveGizmoVisible = _moveGizmo.Visible;
        _savedScaleGizmoVisible = _scaleGizmo.Visible;
        _savedAngleLabelVisible = _angleLabel?.Visible ?? false;

        _gizmoDragging = false;
        _moveGizmoDragging = false;
        _scaleGizmoDragging = false;
        _rotateGizmoDraggingFreeNode = false;

        if (_angleLabel is not null)
            _angleLabel.Visible = false;
        SetJointSpheresVisible(false);
        _gizmo.Visible = false;
        _moveGizmo.Visible = false;
        _scaleGizmo.Visible = false;
    }

    private void RestoreOverlaysAfterRender()
    {
        if (!_renderOverlaySuppressed) return;
        _renderOverlaySuppressed = false;

        SetJointSpheresVisible(_savedJointSpheresVisible);
        _gizmo.Visible = _savedRotationGizmoVisible;
        _moveGizmo.Visible = _savedMoveGizmoVisible;
        _scaleGizmo.Visible = _savedScaleGizmoVisible;
        if (_angleLabel is not null)
            _angleLabel.Visible = _savedAngleLabelVisible;
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
                if (_angleLabel is not null) _angleLabel.Visible = false;
                return;
            }
            const float DepthScale = 0.0018f;
            _moveGizmo.UpdateDrag(_camera, mousePos, mousePos - d, DepthScale);

            // Sync ScenePropData with the node's new position (not needed for character)
            if (!_characterSelected && _selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
            {
                var (node, old) = _sceneProps[_selectedPropIdx];
                var newPos = node.GlobalPosition;
                var updated = old with { PosX = newPos.X, PosY = newPos.Y, PosZ = newPos.Z };
                _sceneProps[_selectedPropIdx] = (node, updated);
                ObjectMoved?.Invoke(_selectedPropIdx, newPos);
            }
            UpdateArrangeLabel();
        }
        else if (_scaleGizmoDragging)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
            {
                _scaleGizmo.EndDrag();
                _scaleGizmoDragging = false;
                if (_angleLabel is not null) _angleLabel.Visible = false;
                return;
            }
            const float DepthScale = 0.0018f;
            _scaleGizmo.UpdateDrag(_camera, mousePos, mousePos - d, DepthScale);

            // Sync ScenePropData with the node's new scale (not needed for character)
            if (!_characterSelected && _selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
            {
                var (node, old) = _sceneProps[_selectedPropIdx];
                var s = node.Scale;
                var updated = old with { ScaleX = s.X, ScaleY = s.Y, ScaleZ = s.Z };
                _sceneProps[_selectedPropIdx] = (node, updated);
                ObjectMoved?.Invoke(_selectedPropIdx, node.GlobalPosition);
            }
            UpdateArrangeLabel();
        }
        else if (_rotateGizmoDraggingFreeNode)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
            {
                _gizmo.EndDrag();
                _rotateGizmoDraggingFreeNode = false;
                if (_angleLabel is not null) _angleLabel.Visible = false;
                return;
            }
            _gizmo.UpdateDrag(d);

            // Sync ScenePropData with the node's new rotation (not needed for character)
            if (!_characterSelected && _selectedPropIdx >= 0 && _selectedPropIdx < _sceneProps.Count)
            {
                var (node, old) = _sceneProps[_selectedPropIdx];
                var euler = node.RotationDegrees;
                var updated = old with { RotX = euler.X, RotY = euler.Y, RotZ = euler.Z };
                _sceneProps[_selectedPropIdx] = (node, updated);
                ObjectMoved?.Invoke(_selectedPropIdx, node.GlobalPosition);
            }
            UpdateArrangeLabel();
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

    private static string NormalizeResPathSyntax(string source)
    {
        if (source.StartsWith("res:/", StringComparison.OrdinalIgnoreCase)
            && !source.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return "res://" + source["res:/".Length..];
        }
        return source;
    }

    public void SetProjectDirectory(string projectDirectory)
    {
        _projectDirectory = string.IsNullOrWhiteSpace(projectDirectory)
            ? string.Empty
            : Path.GetFullPath(projectDirectory);
    }

    private string GetProjectRootForAssets()
    {
        if (!string.IsNullOrWhiteSpace(_projectDirectory))
            return _projectDirectory;
        return ProjectSettings.GlobalizePath("res://");
    }

    private bool TryMapToProjectModelsPath(string sourcePath, out string loadPath, out string storedPath)
    {
        loadPath = sourcePath;
        storedPath = sourcePath;

        if (string.IsNullOrWhiteSpace(sourcePath))
            return false;

        if (sourcePath.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(sourcePath, UriKind.Absolute, out var fileUri))
        {
            sourcePath = fileUri.LocalPath;
        }

        var normalized = NormalizeResPathSyntax(sourcePath);
        var projectRoot = GetProjectRootForAssets();
        var modelsDir = Path.Combine(projectRoot, "assets", "models");
        Directory.CreateDirectory(modelsDir);

        if (normalized.StartsWith("res://assets/models/", StringComparison.OrdinalIgnoreCase))
        {
            var rel = normalized["res://".Length..].Replace('\\', '/');
            storedPath = "res:/" + rel;
            loadPath = Path.Combine(projectRoot, rel.Replace('/', Path.DirectorySeparatorChar));

            // If the target project does not have this model yet, but the same
            // resource exists in the app/project res:// root, copy it over.
            if (!File.Exists(loadPath))
            {
                try
                {
                    var appResRoot = ProjectSettings.GlobalizePath("res://");
                    var appResPath = Path.Combine(appResRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                    var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    if (File.Exists(appResPath) && !string.Equals(appResPath, loadPath, comparison))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(loadPath) ?? modelsDir);
                        File.Copy(appResPath, loadPath, overwrite: false);
                        RunnerLogger.Info("PosingEditor", $"Copied model from app resources into project: '{loadPath}'.");
                    }
                }
                catch (Exception ex)
                {
                    RunnerLogger.Warn("PosingEditor", $"Could not materialize model '{storedPath}' into current project at '{loadPath}'.", ex);
                }
            }
            return true;
        }

        if (Path.IsPathRooted(normalized))
        {
            var full = Path.GetFullPath(normalized);
            var modelsFull = Path.GetFullPath(modelsDir);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var inProjectModels = full.StartsWith(modelsFull + Path.DirectorySeparatorChar, comparison)
                                  || string.Equals(full, modelsFull, comparison);

            var fileName = Path.GetFileName(full);
            if (string.IsNullOrWhiteSpace(fileName))
                return true;

            var targetAbs = Path.Combine(modelsDir, fileName);
            if (!inProjectModels)
            {
                try
                {
                    if (!File.Exists(targetAbs))
                    {
                        File.Copy(full, targetAbs);
                        RunnerLogger.Info("PosingEditor", $"Copied external asset into project: '{targetAbs}'.");
                    }
                }
                catch (Exception ex)
                {
                    RunnerLogger.Warn("PosingEditor", $"Failed to copy external asset '{full}' to '{targetAbs}'. Keeping original path.", ex);
                    return true;
                }
            }

            loadPath = targetAbs;
            storedPath = "res:/assets/models/" + fileName;
            return true;
        }

        return true;
    }

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

        TryMapToProjectModelsPath(source, out var loadPath, out var storedPath);
        if (!TryLoadNode3D(loadPath, out var loaded, out var label))
            return;

        ClearSceneCharacters();
        var characterIndex = AddCharacterNode(loaded, storedPath, 0f, 0f, 0f, characterId: "hero");
        if (characterIndex >= 0)
        {
            RunnerLogger.Info("PosingEditor", $"Model loaded from '{label}'.");
            SelectSceneCharacter(characterIndex);
        }
    }

    private bool TryLoadNode3D(string source, out Node3D node3D, out string label)
    {
        source = NormalizeResPathSyntax(source);
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

    private int AddCharacterNode(Node3D node3D, string path, float posX, float posY, float posZ, string? characterId = null, string? characterName = null)
    {
        if (node3D.GetParent() is not null)
            node3D.GetParent().RemoveChild(node3D);

        // Stop any embedded animations so they don't override manual bone poses.
        StopAllAnimationPlayers(node3D);
        _worldRoot.AddChild(node3D);
        node3D.Position = new Vector3(posX, posY, posZ);
        node3D.RotationDegrees = Vector3.Zero;
        node3D.Scale = Vector3.One;

        var skeleton = FindSkeleton(node3D);
        if (skeleton is null)
        {
            if (node3D.GetParent() is not null)
                node3D.GetParent().RemoveChild(node3D);
            node3D.QueueFree();
            return -1;
        }

        var requestedId = string.IsNullOrWhiteSpace(characterId) ? CreateCharacterId() : characterId!;
        var id = EnsureUniqueCharacterId(requestedId);
        if (!string.Equals(id, requestedId, StringComparison.OrdinalIgnoreCase))
        {
            RunnerLogger.Warn("PosingEditor", $"Duplicate character id '{requestedId}' adjusted to '{id}'.");
        }
        var name = string.IsNullOrWhiteSpace(characterName)
            ? System.IO.Path.GetFileNameWithoutExtension(path)
            : characterName!;
        _sceneCharacters.Add(new SceneCharacterEntry
        {
            Id = id,
            Path = path,
            Name = name,
            Node = node3D,
            Skeleton = skeleton
        });

        var idx = _sceneCharacters.Count - 1;
        ActivateCharacter(idx);
        RunnerLogger.Info("PosingEditor", $"Character added: '{name}' id='{id}', bones={skeleton.GetBoneCount()}.");
        return idx;
    }

    private int AddPropNode(Node3D node3D, string path, float posX, float posY, float posZ)
    {
        if (node3D.GetParent() is not null)
            node3D.GetParent().RemoveChild(node3D);

        StopAllAnimationPlayers(node3D);
        _worldRoot.AddChild(node3D);

        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var data = new ScenePropData(path, name, posX, posY, posZ, 0f, 0f, 0f, 1f, 1f, 1f);
        ApplyPropTransform(node3D, data);

        var idx = _sceneProps.Count;
        _sceneProps.Add((node3D, data));
        ScenePropAdded?.Invoke(idx, path);
        return idx;
    }

    private static bool TryParseGreyboxKind(string path, out string kind)
    {
        kind = string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;
        const string prefix = "builtin:greybox/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        kind = path[prefix.Length..].Trim().ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(kind);
    }

    private static string GreyboxPath(string kind) => $"builtin:greybox/{kind}";

    private static StandardMaterial3D MakeGreyboxMaterial(Color color, bool transparent = false)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = 0.85f,
            Metallic = 0.0f,
            Transparency = transparent
                ? BaseMaterial3D.TransparencyEnum.Alpha
                : BaseMaterial3D.TransparencyEnum.Disabled
        };
    }

    private static MeshInstance3D MakeGreyboxBox(string name, Vector3 size, Color color, Vector3 localPos, bool transparent = false)
    {
        var mesh = new BoxMesh { Size = size };
        mesh.SurfaceSetMaterial(0, MakeGreyboxMaterial(color, transparent));
        return new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            Position = localPos
        };
    }

    private static Node3D CreateGreyboxNode(string kind)
    {
        var root = new Node3D { Name = $"Greybox_{kind}" };

        switch (kind)
        {
            case "wall":
                root.AddChild(MakeGreyboxBox("Wall", new Vector3(4.0f, 2.6f, 0.2f), new Color(0.35f, 0.45f, 0.85f), new Vector3(0f, 1.3f, 0f)));
                break;

            case "door":
                root.AddChild(MakeGreyboxBox("Door", new Vector3(1.0f, 2.1f, 0.12f), new Color(0.45f, 0.30f, 0.16f), new Vector3(0f, 1.05f, 0f)));
                break;

            case "window":
                root.AddChild(MakeGreyboxBox("Frame", new Vector3(1.8f, 1.3f, 0.10f), new Color(0.78f, 0.78f, 0.80f), new Vector3(0f, 1.4f, 0f)));
                root.AddChild(MakeGreyboxBox("Glass", new Vector3(1.45f, 0.95f, 0.03f), new Color(0.45f, 0.70f, 0.95f, 0.45f), new Vector3(0f, 1.4f, 0.04f), transparent: true));
                break;

            case "tree":
            {
                var trunkMesh = new CylinderMesh { TopRadius = 0.16f, BottomRadius = 0.2f, Height = 2.0f };
                trunkMesh.SurfaceSetMaterial(0, MakeGreyboxMaterial(new Color(0.40f, 0.26f, 0.15f)));
                root.AddChild(new MeshInstance3D { Name = "Trunk", Mesh = trunkMesh, Position = new Vector3(0f, 1.0f, 0f) });

                var crownMesh = new SphereMesh { Radius = 0.85f, Height = 1.7f };
                crownMesh.SurfaceSetMaterial(0, MakeGreyboxMaterial(new Color(0.27f, 0.62f, 0.30f)));
                root.AddChild(new MeshInstance3D { Name = "Crown", Mesh = crownMesh, Position = new Vector3(0f, 2.2f, 0f) });
                break;
            }

            default:
                // Fallback = wall to keep UX predictable on unknown kind.
                root.AddChild(MakeGreyboxBox("Wall", new Vector3(4.0f, 2.6f, 0.2f), new Color(0.35f, 0.45f, 0.85f), new Vector3(0f, 1.3f, 0f)));
                break;
        }

        return root;
    }

    public int AddGreyboxItem(string kind, float posX, float posY, float posZ)
    {
        var token = string.IsNullOrWhiteSpace(kind) ? "wall" : kind.Trim().ToLowerInvariant();
        var path = GreyboxPath(token);
        var node3D = CreateGreyboxNode(token);
        var idx = AddPropNode(node3D, path, posX, posY, posZ);
        if (idx >= 0)
            RunnerLogger.Info("PosingEditor", $"Greybox item added: '{token}' at ({posX},{posY},{posZ}).");
        return idx;
    }

    public int AddSceneAsset(string path, float posX, float posY, float posZ)
    {
        if (string.IsNullOrWhiteSpace(path)) return -1;
        TryMapToProjectModelsPath(path, out var loadPath, out var storedPath);
        if (!TryLoadNode3D(loadPath, out var node3D, out var label))
            return -1;

        var skeleton = FindSkeleton(node3D);
        if (skeleton is not null)
        {
            var characterIndex = AddCharacterNode(node3D, storedPath, posX, posY, posZ);
            if (characterIndex >= 0)
            {
                SelectSceneCharacter(characterIndex);
                RunnerLogger.Info("PosingEditor", $"Scene character added from '{label}' at ({posX},{posY},{posZ}).");
                return 1;
            }
            return -1;
        }

        var propIdx = AddPropNode(node3D, storedPath, posX, posY, posZ);
        if (propIdx >= 0)
            RunnerLogger.Info("PosingEditor", $"Scene prop added: '{label}' at ({posX},{posY},{posZ}).");
        return propIdx >= 0 ? 0 : -1;
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
        if (propIdx == _selectedPropIdx && !_characterSelected) return;

        _characterSelected = false;
        _selectedCharacterIdx = -1;
        _selectedPropIdx   = propIdx;

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

        QueueRedraw();
    }

    /// <summary>Attach the correct gizmo to the given prop based on the current ArrangeEditMode.</summary>
    private void AttachArrangeGizmo(int propIdx) =>
        AttachArrangeGizmoToNode(_sceneProps[propIdx].Node);

    private void AttachArrangeGizmoToNode(Node3D node)
    {
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

    /// <summary>Prop/character picking in Arrange mode — selects the hit object and attaches the active gizmo.</summary>
    private void TryPickPropArrange(Vector3 rayOrigin, Vector3 rayDir)
    {
        var bestIdx  = -1;
        var bestCharacterIdx = -1;
        var bestT    = float.MaxValue;

        // Test all characters
        for (var ci = 0; ci < _sceneCharacters.Count; ci++)
        {
            var node = _sceneCharacters[ci].Node;
            if (!node.IsInsideTree()) continue;
            var aabb = GetNodeWorldAabb(node);
            if (aabb.Size != Vector3.Zero && RayAabbIntersect(rayOrigin, rayDir, aabb, out var t))
            {
                if (t < bestT)
                {
                    bestT   = t;
                    bestCharacterIdx = ci;
                    bestIdx = -1;
                }
            }
        }

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
                bestCharacterIdx = -1;
            }
        }

        if (bestCharacterIdx >= 0)
            SelectSceneCharacter(bestCharacterIdx);
        else
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
            // Selecting a bone deselects any prop/character.
            if (_selectedPropIdx >= 0 || _characterSelected)
            {
                _moveGizmo.Detach();
                _scaleGizmo.Detach();
                _selectedPropIdx   = -1;
                _characterSelected = false;
            }

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
        foreach (var character in _sceneCharacters)
        {
            for (var i = 0; i < character.Skeleton.GetBoneCount(); i++)
                character.Skeleton.ResetBonePose(i);
        }
        PoseData.Clear();
        PoseReset?.Invoke();
    }

    public string SavePoseAsSml()
    {
        if (_sceneCharacters.Count == 0) return string.Empty;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Pose {");
        foreach (var (key, rot) in PoseData)
        {
            var euler = rot.GetEuler() * (180f / Mathf.Pi);
            sb.AppendLine($"    Bone {{ name: \"{key}\"; rotX: {euler.X:F1}; rotY: {euler.Y:F1}; rotZ: {euler.Z:F1} }}");
        }
        sb.Append("}");
        return sb.ToString();
    }

    public Dictionary<string, Quaternion> GetPoseDataForCharacter(string characterId)
    {
        var result = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(characterId))
            return result;

        foreach (var (key, rot) in PoseData)
        {
            if (!TrySplitBoneKey(key, out var scopedId, out _))
                continue;
            if (!string.Equals(scopedId, characterId, StringComparison.OrdinalIgnoreCase))
                continue;
            result[key] = rot;
        }
        return result;
    }

    public Dictionary<string, Quaternion> GetPoseDataForActiveCharacter()
    {
        return GetPoseDataForCharacter(GetCurrentSkeletonCharacterId());
    }

    public void LoadPose(Dictionary<string, Quaternion> pose)
    {
        if (_sceneCharacters.Count == 0) return;
        foreach (var (boneKey, rot) in pose)
        {
            Skeleton3D? targetSkeleton = null;
            string boneName;
            string targetCharacterId;
            if (!TrySplitBoneKey(boneKey, out targetCharacterId, out boneName))
            {
                // Legacy unscoped key is ignored intentionally.
                continue;
            }

            for (var ci = 0; ci < _sceneCharacters.Count; ci++)
            {
                if (string.Equals(_sceneCharacters[ci].Id, targetCharacterId, StringComparison.OrdinalIgnoreCase))
                {
                    targetSkeleton = _sceneCharacters[ci].Skeleton;
                    break;
                }
            }

            if (targetSkeleton is null)
                continue;

            var idx = targetSkeleton.FindBone(boneName);
            if (idx < 0) continue;
            targetSkeleton.SetBonePoseRotation(idx, rot);
            var normalized = NormBone(targetSkeleton.GetBoneName(idx));
            var resolvedKey = BuildBoneKey(targetCharacterId, normalized);
            PoseData[resolvedKey] = rot;
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

    private static string BuildBoneKey(string characterId, string boneName) =>
        string.IsNullOrWhiteSpace(characterId) ? boneName : $"{characterId}:{boneName}";

    private static bool TrySplitBoneKey(string key, out string characterId, out string boneName)
    {
        var sep = key.IndexOf(':');
        if (sep <= 0 || sep >= key.Length - 1)
        {
            characterId = string.Empty;
            boneName = key;
            return false;
        }

        characterId = key[..sep];
        boneName = key[(sep + 1)..];
        return true;
    }

    private string GetActiveCharacterIdInternal()
    {
        if (_activeCharacterIdx < 0 || _activeCharacterIdx >= _sceneCharacters.Count)
            return string.Empty;
        return _sceneCharacters[_activeCharacterIdx].Id;
    }

    public string GetActiveCharacterId() => GetActiveCharacterIdInternal();

    private string GetCurrentSkeletonCharacterId()
    {
        if (_skeleton is null)
            return GetActiveCharacterIdInternal();

        for (var i = 0; i < _sceneCharacters.Count; i++)
        {
            if (ReferenceEquals(_sceneCharacters[i].Skeleton, _skeleton))
                return _sceneCharacters[i].Id;
        }

        return GetActiveCharacterIdInternal();
    }

    private bool ActivateCharacter(int index)
    {
        if (index < 0 || index >= _sceneCharacters.Count)
            return false;

        // Character switch must invalidate old bone/gizmo bindings so first
        // bone click on the new character always re-attaches correctly.
        _gizmo.Detach();
        _gizmoDragging = false;
        _rotateGizmoDraggingFreeNode = false;
        _selectedBoneIdx = -1;

        _activeCharacterIdx = index;
        _modelRoot = _sceneCharacters[index].Node;
        _skeleton = _sceneCharacters[index].Skeleton;

        ClearJointSpheres();
        BuildJointSpheres();
        if (_showBoneTree || _isExternalBoneTree) PopulateBoneList();
        return true;
    }

    private void ActivateFirstCharacterOrClear()
    {
        if (_sceneCharacters.Count > 0)
        {
            ActivateCharacter(0);
            return;
        }

        _activeCharacterIdx = -1;
        _modelRoot = null;
        _skeleton = null;
        _selectedBoneIdx = -1;
        ClearJointSpheres();
        if (_showBoneTree || _isExternalBoneTree) PopulateBoneList();
    }

    private string CreateCharacterId() => $"char{_nextCharacterId++}";

    private string EnsureUniqueCharacterId(string requestedId)
    {
        var baseId = string.IsNullOrWhiteSpace(requestedId) ? CreateCharacterId() : requestedId;
        var candidate = baseId;
        var suffix = 1;
        while (ContainsCharacterId(candidate))
        {
            candidate = $"{baseId}_{suffix}";
            suffix++;
        }
        return candidate;
    }

    private bool ContainsCharacterId(string id)
    {
        for (var i = 0; i < _sceneCharacters.Count; i++)
        {
            if (string.Equals(_sceneCharacters[i].Id, id, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ── Scene props ───────────────────────────────────────────────────────────

    /// <summary>Add a static prop model to the scene. Returns the prop index.</summary>
    public int AddSceneProp(string path, float posX, float posY, float posZ)
    {
        if (string.IsNullOrWhiteSpace(path)) return -1;

        if (TryParseGreyboxKind(path, out var greyboxKind))
            return AddGreyboxItem(greyboxKind, posX, posY, posZ);

        TryMapToProjectModelsPath(path, out var loadPath, out var storedPath);
        if (!TryLoadNode3D(loadPath, out var node3D, out var label))
            return -1;
        var idx = AddPropNode(node3D, storedPath, posX, posY, posZ);
        if (idx >= 0)
            RunnerLogger.Info("PosingEditor", $"Scene prop added: '{label}' at ({posX},{posY},{posZ}).");
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

    private void RemoveCharacterByIndex(int index)
    {
        if (index < 0 || index >= _sceneCharacters.Count) return;
        var entry = _sceneCharacters[index];

        _moveGizmo.Detach();
        _scaleGizmo.Detach();
        _gizmo.Detach();

        _selectedPropIdx   = -1;
        _characterSelected = false;
        _selectedCharacterIdx = -1;
        _selectedBoneIdx   = -1;

        // Remove all pose keys for this character.
        var prefix = entry.Id + ":";
        var toRemove = new List<string>();
        foreach (var key in PoseData.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                toRemove.Add(key);
        }
        foreach (var key in toRemove)
            PoseData.Remove(key);

        if (entry.Node.GetParent() is not null)
            entry.Node.GetParent().RemoveChild(entry.Node);
        entry.Node.QueueFree();
        _sceneCharacters.RemoveAt(index);

        if (_activeCharacterIdx == index) _activeCharacterIdx = -1;
        else if (_activeCharacterIdx > index) _activeCharacterIdx--;
        if (_selectedCharacterIdx > index) _selectedCharacterIdx--;

        ActivateFirstCharacterOrClear();

        ObjectSelected?.Invoke(-1);
        QueueRedraw();
        RunnerLogger.Info("PosingEditor", $"Character removed from scene: idx={index}.");
    }

    /// <summary>Remove the active character model from the scene.</summary>
    public void RemoveCharacter()
    {
        if (_activeCharacterIdx < 0) return;
        RemoveCharacterByIndex(_activeCharacterIdx);
    }

    public void RemoveSceneCharacter(int index)
    {
        RemoveCharacterByIndex(index);
    }

    /// <summary>
    /// Select a scene prop by index from external code (e.g. from the scene-asset list in SMS).
    /// Attaches the appropriate arrange gizmo and fires ObjectSelected.
    /// </summary>
    public void SelectSceneProp(int propIdx) => SelectProp(propIdx);

    /// <summary>
    /// Select the active character as the transform target in Arrange mode.
    /// </summary>
    public void SelectCharacter()
    {
        if (_activeCharacterIdx < 0 && _sceneCharacters.Count > 0)
            _activeCharacterIdx = 0;
        SelectSceneCharacter(_activeCharacterIdx);
    }

    public void SelectSceneCharacter(int characterIdx)
    {
        if (characterIdx < 0 || characterIdx >= _sceneCharacters.Count) return;
        if (!ActivateCharacter(characterIdx)) return;
        _characterSelected = true;
        _selectedCharacterIdx = characterIdx;
        _selectedPropIdx   = -1;
        AttachArrangeGizmoToNode(_sceneCharacters[characterIdx].Node);
        ObjectSelected?.Invoke(-1);
        QueueRedraw();
        RunnerLogger.Info("PosingEditor", $"Character selected for transform: idx={characterIdx} id='{_sceneCharacters[characterIdx].Id}'.");
    }

    public int GetSceneCharacterCount() => _sceneCharacters.Count;
    public string GetSceneCharacterId(int i) => i >= 0 && i < _sceneCharacters.Count ? _sceneCharacters[i].Id : string.Empty;
    public string GetSceneCharacterPath(int i) => i >= 0 && i < _sceneCharacters.Count ? _sceneCharacters[i].Path : string.Empty;
    public string GetSceneCharacterName(int i) => i >= 0 && i < _sceneCharacters.Count ? _sceneCharacters[i].Name : string.Empty;

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

        for (var i = 0; i < _sceneCharacters.Count; i++)
        {
            var c = _sceneCharacters[i];
            var pos = c.Node.Position;
            var rot = c.Node.RotationDegrees;
            var scale = c.Node.Scale;
            assets.Add(new SceneAssetData(
                Id:      string.IsNullOrWhiteSpace(c.Id) ? $"char{i}" : c.Id,
                Name:    c.Name,
                Src:     c.Path,
                Primary: true,
                PosX:    pos.X,
                PosY:    pos.Y,
                PosZ:    pos.Z,
                RotX:    rot.X,
                RotY:    rot.Y,
                RotZ:    rot.Z,
                ScaleX:  scale.X,
                ScaleY:  scale.Y,
                ScaleZ:  scale.Z));
        }

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

        return new AnimationProjectData(
            timeline.Fps,
            timeline.TotalFrames,
            assets,
            keyframes,
            BuildScenePropertiesForSave());
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
        SetProjectDirectory(projectDirectory);

        // Clear existing state
        ClearSceneCharacters();
        ClearSceneProps();
        timeline.ClearAllKeyframes();
        _sceneProperties.Clear();
        if (data.SceneProperties is not null)
        {
            foreach (var (key, value) in data.SceneProperties)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                _sceneProperties[key] = value ?? string.Empty;
            }
        }

        // Load assets
        foreach (var asset in data.SceneAssets)
        {
            if (string.IsNullOrWhiteSpace(asset.Src)) continue;
            var resolvedSrc = ResolveProjectPath(asset.Src, projectDirectory);
            if (asset.Primary)
            {
                TryMapToProjectModelsPath(resolvedSrc, out var loadPath, out var storedPath);
                if (!TryLoadNode3D(loadPath, out var node3D, out _))
                    continue;
                var idx = AddCharacterNode(node3D, storedPath, asset.PosX, asset.PosY, asset.PosZ, asset.Id, asset.Name);
                if (idx >= 0)
                {
                    var node = _sceneCharacters[idx].Node;
                    node.RotationDegrees = new Vector3(asset.RotX, asset.RotY, asset.RotZ);
                    node.Scale = new Vector3(asset.ScaleX, asset.ScaleY, asset.ScaleZ);
                }
            }
            else
            {
                var idx = AddSceneProp(resolvedSrc, asset.PosX, asset.PosY, asset.PosZ);
                if (idx >= 0)
                {
                    SetScenePropRot(idx, asset.RotX, asset.RotY, asset.RotZ);
                    if (asset.ScaleX != 1f || asset.ScaleY != 1f || asset.ScaleZ != 1f)
                    {
                        var (node, old) = _sceneProps[idx];
                        var propData = old with { ScaleX = asset.ScaleX, ScaleY = asset.ScaleY, ScaleZ = asset.ScaleZ };
                        _sceneProps[idx] = (node, propData);
                        ApplyPropTransform(node, propData);
                    }
                }
            }
        }

        // Restore timeline settings
        timeline.Fps         = data.Fps;
        timeline.TotalFrames = data.TotalFrames;

        // Restore keyframes
        foreach (var (frame, bones) in data.Keyframes)
            timeline.SetKeyframe(frame, bones);

        if (_sceneCharacters.Count > 0)
            ActivateCharacter(0);

        // Ensure loaded projects do not stay in default T-pose:
        // jump to frame 0 (or the first available keyframe) and apply that pose immediately.
        var initialFrame = 0;
        if (!data.Keyframes.ContainsKey(initialFrame) && data.Keyframes.Count > 0)
        {
            foreach (var frame in data.Keyframes.Keys)
            {
                initialFrame = frame;
                break;
            }
        }

        timeline.SetCurrentFrame(initialFrame);
        var initialPose = timeline.GetPoseAt(initialFrame);
        if (initialPose is not null)
        {
            LoadPose(initialPose);
        }
    }

    /// <summary>
    /// Resolves a source path from a .scene file.
    /// <c>res://relative/path</c> and <c>res:/relative/path</c> are mapped to
    /// <c>{projectDirectory}/relative/path</c>
    /// when a project directory is known; otherwise returned unchanged.
    /// </summary>
    private static string ResolveProjectPath(string src, string projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory)) return src;
        var normalized = NormalizeResPathSyntax(src);
        if (!normalized.StartsWith("res://", StringComparison.OrdinalIgnoreCase)) return src;
        var relative = normalized["res://".Length..];
        return Path.Combine(projectDirectory, relative);
    }

    private void ClearSceneProps()
    {
        for (var i = _sceneProps.Count - 1; i >= 0; i--)
            RemoveSceneProp(i);
    }

    private void ClearSceneCharacters()
    {
        for (var i = _sceneCharacters.Count - 1; i >= 0; i--)
            RemoveCharacterByIndex(i);
        _sceneCharacters.Clear();
        _activeCharacterIdx = -1;
        _selectedCharacterIdx = -1;
        _characterSelected = false;
    }

    // ── GLB Export ────────────────────────────────────────────────────────────

    /// <summary>
    /// Captures the current SubViewport frame and writes it as PNG to <paramref name="path"/>.
    /// Returns true on success, false otherwise.
    /// </summary>
    public bool ExportCurrentFramePng(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            RunnerLogger.Warn("PosingEditor", "ExportCurrentFramePng: output path is empty.");
            return false;
        }

        var restoreOverlays = !_renderOverlaySuppressed;
        if (restoreOverlays)
        {
            SuppressOverlaysForRender();
            RenderingServer.ForceDraw();
            RenderingServer.ForceDraw();
        }

        try
        {
            var image = _viewport.GetTexture()?.GetImage();
            if (image is null || image.IsEmpty())
            {
                RunnerLogger.Warn("PosingEditor", "ExportCurrentFramePng: viewport image is empty.");
                return false;
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var err = image.SavePng(path);
            if (err != Error.Ok)
            {
                RunnerLogger.Warn("PosingEditor", $"ExportCurrentFramePng failed for '{path}' (error {err}).");
                return false;
            }

            RunnerLogger.Info("PosingEditor", $"Current frame exported to '{path}'.");
            return true;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("PosingEditor", $"ExportCurrentFramePng crashed for '{path}'.", ex);
            return false;
        }
        finally
        {
            if (restoreOverlays)
                RestoreOverlaysAfterRender();
        }
    }

    /// <summary>
    /// Exports an inclusive frame range to PNG files (`frame_0000.png`, ...) by
    /// applying timeline poses frame-by-frame and forcing a render before capture.
    /// Returns the number of written frames.
    /// </summary>
    public bool StartExportFrameRangePng(TimelineControl timeline, int frameFrom, int frameTo, string outputDirectory)
    {
        if (_frameExportRunning || timeline is null || string.IsNullOrWhiteSpace(outputDirectory))
        {
            return false;
        }

        var start = Math.Min(frameFrom, frameTo);
        var end = Math.Max(frameFrom, frameTo);
        if (end < start)
        {
            return false;
        }

        Directory.CreateDirectory(outputDirectory);
        foreach (var file in Directory.EnumerateFiles(outputDirectory, "frame_*.png"))
        {
            try { File.Delete(file); } catch { /* best-effort cleanup */ }
        }

        _frameExportTimeline = timeline;
        _frameExportCurrent = start;
        _frameExportEnd = end;
        _frameExportTotal = (end - start) + 1;
        _frameExportWritten = 0;
        _frameExportOutputDir = outputDirectory;
        _frameExportAwaitingCapture = false;
        SuppressOverlaysForRender();
        _frameExportRunning = true;

        return true;
    }

    /// <summary>
    /// Legacy synchronous export; kept for compatibility.
    /// </summary>
    public int ExportFrameRangePng(TimelineControl timeline, int frameFrom, int frameTo, string outputDirectory)
    {
        if (timeline is null || string.IsNullOrWhiteSpace(outputDirectory))
        {
            return 0;
        }

        var start = Math.Min(frameFrom, frameTo);
        var end = Math.Max(frameFrom, frameTo);
        if (end < start)
        {
            return 0;
        }

        Directory.CreateDirectory(outputDirectory);

        var written = 0;
        SuppressOverlaysForRender();
        try
        {
            for (var frame = start; frame <= end; frame++)
            {
                timeline.SetCurrentFrame(frame);
                var pose = timeline.GetPoseAt(frame);
                if (pose is not null)
                {
                    LoadPose(pose);
                }

                // Force transform/skeleton propagation for this frame before rendering.
                foreach (var character in _sceneCharacters)
                {
                    character.Skeleton.ForceUpdateAllBoneTransforms();
                    character.Node.ForceUpdateTransform();
                }
                _worldRoot.ForceUpdateTransform();

                // Force rendering twice to avoid stale texture readback in tight loops.
                RenderingServer.ForceDraw();
                RenderingServer.ForceDraw();

                var fileName = "frame_" + written.ToString("D4", CultureInfo.InvariantCulture) + ".png";
                var outputPath = Path.Combine(outputDirectory, fileName);
                if (!ExportCurrentFramePng(outputPath))
                {
                    RunnerLogger.Warn("PosingEditor", $"ExportFrameRangePng stopped at frame {frame}.");
                    break;
                }

                written++;
            }
        }
        finally
        {
            RestoreOverlaysAfterRender();
        }

        return written;
    }

    /// <summary>
    /// Opens a native Godot export-options dialog (checkboxes for animation + props),
    /// then a file-save dialog, and finally exports to GLB.
    /// </summary>
    public void ShowExportDialog(TimelineControl timeline)
    {
        if (_modelRoot is null || _skeleton is null)
        {
            RunnerLogger.Warn("PosingEditor", "No model loaded — cannot export.");
            return;
        }

        // Options dialog
        var dlg     = new AcceptDialog { Title = "Export as GLB", Size = new Vector2I(300, 140) };
        var vbox    = new VBoxContainer { LayoutMode = 1 };
        var cbAnim  = new CheckBox { Text = "Include Animation", ButtonPressed = true };
        var cbProps = new CheckBox { Text = "Include Props",     ButtonPressed = true };
        vbox.AddChild(cbAnim);
        vbox.AddChild(cbProps);
        dlg.AddChild(vbox);
        AddChild(dlg);
        dlg.Popup();

        dlg.Confirmed += () =>
        {
            var inclAnim  = cbAnim.ButtonPressed;
            var inclProps = cbProps.ButtonPressed;
            dlg.QueueFree();

            var saveDlg = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Access   = FileDialog.AccessEnum.Filesystem,
                Filters  = ["*.glb"],
                Title    = "Export as GLB",
            };
            AddChild(saveDlg);
            saveDlg.Popup();
            saveDlg.FileSelected += async (string savePath) =>
            {
                // Hide immediately (property change is instant; rendering catches up
                // after we await the process frame below).
                saveDlg.Hide();
                saveDlg.QueueFree();

                var filename = System.IO.Path.GetFileName(savePath);
                ExportProgress?.Invoke(filename, 0);

                // CallDeferred runs BEFORE rendering, so we must await at least one
                // rendered frame; otherwise the dialog stays visually open while the
                // main thread is blocked by the export.
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                var opts = new GlbExporter.ExportOptions(inclAnim, inclProps);
                DoExportAsGlb(opts, savePath, timeline);
            };
            saveDlg.Canceled += () => saveDlg.QueueFree();
        };
        dlg.Canceled += () => dlg.QueueFree();
    }

    private void DoExportAsGlb(GlbExporter.ExportOptions options, string path, TimelineControl timeline)
    {
        var filename  = System.IO.Path.GetFileName(path);
        var keyframes = timeline.GetAllKeyframes();
        var props     = _sceneProps.Select(p => (p.Node, p.Data.Name));

        var ctx = GlbExporter.Prepare(_modelRoot!, _skeleton!, keyframes,
            timeline.Fps, timeline.TotalFrames, props, options);

        if (ctx is null)
        {
            if (_modelRoot!.GetParent() is null)
                _worldRoot.AddChild(_modelRoot);
            return;
        }

        GlbExporter.Write(ctx, path);

        // Re-attach modelRoot — Prepare always detaches it.
        if (_modelRoot!.GetParent() is null)
            _worldRoot.AddChild(_modelRoot);

        ExportProgress?.Invoke(filename, 100);
    }
}
