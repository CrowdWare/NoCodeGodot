using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Runtime.ThreeD;

public sealed partial class Viewport3DControl : SubViewportContainer
{
    private const float DefaultYaw = 0f;
    private const float DefaultPitch = 0.43f;
    private const float DefaultDistance = 3.85f;
    private const float MinDistance = 1.0f;
    private const float MaxDistance = 20.0f;

    private readonly SubViewport _viewport;
    private readonly Node3D _worldRoot;
    private readonly Camera3D _camera;
    private readonly DirectionalLight3D _light;

    private Node3D? _modelRoot;
    private AnimationPlayer? _animationPlayer;
    private string? _pendingAutoplayAnimation;
    private readonly List<AnimationPlayer> _animationPlayers = [];
    private readonly List<AnimationTree> _animationTrees = [];
    private float _yaw = DefaultYaw;
    private float _pitch = DefaultPitch;
    private float _orbitDistance = DefaultDistance;
    private Vector3 _cameraTarget = Vector3.Zero;
    private bool _isRotating;
    private bool _isPanning;

    public AnimationControlApi? AnimationApi { get; set; }
    public string SmlId { get; set; } = string.Empty;
    public bool PlayFirstAnimationOnLoad { get; set; }
    public string DefaultAnimation { get; set; } = string.Empty;

    public Viewport3DControl()
    {
        Stretch = true;
        CustomMinimumSize = new Vector2(640, 360);

        _viewport = new SubViewport
        {
            Name = "Viewport",
            Size = new Vector2I(1280, 720),
            HandleInputLocally = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };

        _worldRoot = new Node3D { Name = "WorldRoot" };
        _camera = new Camera3D
        {
            Name = "Camera",
            Position = new Vector3(0, 1.6f, 3.5f)
        };
        _camera.LookAtFromPosition(_camera.Position, Vector3.Zero, Vector3.Up);

        _light = new DirectionalLight3D
        {
            Name = "Light",
            Position = new Vector3(0, 3, 0),
            RotationDegrees = new Vector3(-45, 35, 0)
        };

        _worldRoot.AddChild(_camera);
        _worldRoot.AddChild(_light);
        _viewport.AddChild(_worldRoot);
        AddChild(_viewport);

        TreeEntered += OnTreeEntered;
        ResetView();
    }

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

    public void SetModelSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            RunnerLogger.Warn("3D", "Model source is empty.");
            return;
        }

        if (source.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out var fileUri))
            {
                source = fileUri.LocalPath;
            }
        }

        if (Path.IsPathRooted(source))
        {
            if (!File.Exists(source))
            {
                RunnerLogger.Warn("3D", $"Model file does not exist: '{source}'.");
                return;
            }

            if (TryLoadAbsoluteGltf(source, out var absoluteNode))
            {
                AttachLoadedModel(absoluteNode, source);
                return;
            }

            RunnerLogger.Warn("3D", $"Model source '{source}' is an absolute file path and could not be loaded as .glb/.gltf at runtime.");
            return;
        }

        if (!source.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            && !source.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            RunnerLogger.Warn("3D", $"Unsupported model source '{source}'. Use res:// or user://.");
            return;
        }

        Resource? resource = GD.Load<Resource>(source);
        if (resource is null)
        {
            RunnerLogger.Warn("3D", $"Could not load 3D resource '{source}'.");
            return;
        }

        Node? instance = resource is PackedScene scene
            ? scene.Instantiate()
            : null;

        if (instance is not Node3D node3DFromPackedScene)
        {
            RunnerLogger.Warn("3D", $"Resource '{source}' is not a 3D scene (expected imported .glb/.gltf PackedScene).");
            return;
        }

        AttachLoadedModel(node3DFromPackedScene, source);
    }

    private bool TryLoadAbsoluteGltf(string absolutePath, out Node3D node)
    {
        node = null!;

        var extension = Path.GetExtension(absolutePath).ToLowerInvariant();
        if (extension is not (".glb" or ".gltf"))
        {
            return false;
        }

        var gltfState = new GltfState();
        var gltfDocument = new GltfDocument();
        var appendError = gltfDocument.AppendFromFile(absolutePath, gltfState);
        if (appendError != Error.Ok)
        {
            RunnerLogger.Warn("3D", $"GLTF append failed for '{absolutePath}' with error {appendError}.");
            return false;
        }

        var generated = gltfDocument.GenerateScene(gltfState);
        if (generated is not Node3D generatedNode)
        {
            RunnerLogger.Warn("3D", $"GLTF scene generation failed for '{absolutePath}'.");
            return false;
        }

        node = generatedNode;
        return true;
    }

    private void AttachLoadedModel(Node3D node3D, string sourceLabel)
    {
        if (node3D.GetParent() is not null)
        {
            node3D.GetParent().RemoveChild(node3D);
        }

        if (_modelRoot is not null)
        {
            _worldRoot.RemoveChild(_modelRoot);
            _modelRoot.QueueFree();
            _modelRoot = null;
            _animationPlayer = null;
            _animationPlayers.Clear();
            _animationTrees.Clear();
        }

        _modelRoot = node3D;
        _modelRoot.Name = "ModelRoot";
        _worldRoot.AddChild(_modelRoot);

        CollectAnimationPlayers(_modelRoot, _animationPlayers);
        CollectAnimationTrees(_modelRoot, _animationTrees);

        _animationPlayer = FindBestAnimationPlayer(_animationPlayers);
        if (_animationPlayer is null || _animationPlayers.Count == 0)
        {
            RunnerLogger.Warn("3D", $"Model loaded from '{sourceLabel}', but no usable AnimationPlayer with animations was found.");
            return;
        }

        foreach (var tree in _animationTrees)
        {
            if (tree.Active)
            {
                tree.Active = false;
                RunnerLogger.Info("3D", $"Disabled active AnimationTree '{tree.Name}' to allow direct AnimationPlayer playback.");
            }
        }

        foreach (var player in _animationPlayers)
        {
            player.PlaybackActive = true;
        }

        var playerId = string.IsNullOrWhiteSpace(SmlId) ? Name.ToString() : SmlId;
        AnimationApi?.Register(playerId, _animationPlayer);

        var animations = new List<string>();
        foreach (string animationName in _animationPlayer.GetAnimationList())
        {
            animations.Add(animationName);
        }

        RunnerLogger.Info("3D", $"Model loaded from '{sourceLabel}'. AnimationPlayers={_animationPlayers.Count}, AnimationTrees={_animationTrees.Count}, primary='{_animationPlayer.Name}', animations: {string.Join(", ", animations)}");

        if (PlayFirstAnimationOnLoad && animations.Count > 0)
        {
            QueuePendingAutoplayFromCurrentPlayer();
        }
    }

    public void SetPlayFirstAnimationOnLoad(bool enabled)
    {
        PlayFirstAnimationOnLoad = enabled;
        if (enabled)
        {
            QueuePendingAutoplayFromCurrentPlayer();
        }
    }

    public void SetDefaultAnimation(string animationName)
    {
        DefaultAnimation = animationName;
        if (PlayFirstAnimationOnLoad)
        {
            QueuePendingAutoplayFromCurrentPlayer();
        }
    }

    private void OnTreeEntered()
    {
        TryStartPendingAutoplay();
    }

    private void TryStartPendingAutoplay()
    {
        if (string.IsNullOrWhiteSpace(_pendingAutoplayAnimation) || _animationPlayer is null)
        {
            return;
        }

        if (!IsInsideTree() || !_animationPlayer.IsInsideTree())
        {
            CallDeferred(nameof(TryStartPendingAutoplay));
            return;
        }

        PlayPendingAutoplay();
    }

    private void QueuePendingAutoplayFromCurrentPlayer()
    {
        if (_animationPlayer is null)
        {
            return;
        }

        _pendingAutoplayAnimation = null;
        if (!string.IsNullOrWhiteSpace(DefaultAnimation) && HasAnimationOnAnyPlayer(DefaultAnimation))
        {
            _pendingAutoplayAnimation = DefaultAnimation;
        }
        else
        {
            var animationList = _animationPlayer.GetAnimationList();
            if (animationList.Length > 0)
            {
                _pendingAutoplayAnimation = animationList[0];
            }
        }

        if (!string.IsNullOrWhiteSpace(_pendingAutoplayAnimation))
        {
            CallDeferred(nameof(TryStartPendingAutoplay));
        }
    }

    private void PlayPendingAutoplay()
    {
        if (_animationPlayer is null || string.IsNullOrWhiteSpace(_pendingAutoplayAnimation))
        {
            return;
        }

        var animationName = _pendingAutoplayAnimation;
        _pendingAutoplayAnimation = null;

        var playedOnAny = false;
        foreach (var player in _animationPlayers)
        {
            if (!player.HasAnimation(animationName))
            {
                continue;
            }

            player.Play(animationName);
            RunnerLogger.Info("3D", $"Autoplay started: '{animationName}' on AnimationPlayer '{player.Name}'.");
            playedOnAny = true;
        }

        if (!playedOnAny)
        {
            RunnerLogger.Warn("3D", $"Autoplay animation '{animationName}' not found on any AnimationPlayer.");
            return;
        }
    }

    public void SetCameraDistance(float z)
    {
        _orbitDistance = Mathf.Clamp(Mathf.Abs(z), MinDistance, MaxDistance);
        ApplyCameraOrbit();
    }

    public void AdjustCameraDistance(float delta)
    {
        _orbitDistance = Mathf.Clamp(_orbitDistance + delta, MinDistance, MaxDistance);
        ApplyCameraOrbit();
    }

    public void ResetView()
    {
        _yaw = DefaultYaw;
        _pitch = DefaultPitch;
        _orbitDistance = DefaultDistance;
        _cameraTarget = Vector3.Zero;
        ApplyCameraOrbit();
    }

    public void SetLightEnergy(float energy)
    {
        _light.LightEnergy = energy;
    }

    private bool HasAnimationOnAnyPlayer(string animationName)
    {
        foreach (var player in _animationPlayers)
        {
            if (player.HasAnimation(animationName))
            {
                return true;
            }
        }

        return false;
    }

    private static AnimationPlayer? FindBestAnimationPlayer(List<AnimationPlayer> players)
    {
        if (players.Count == 0)
        {
            return null;
        }

        // Prefer players that actually contain animations.
        foreach (var player in players)
        {
            if (player.GetAnimationList().Length > 0)
            {
                return player;
            }
        }

        // Fallback: return first player found so callers can still register/log.
        return players[0];
    }

    private static void CollectAnimationPlayers(Node node, List<AnimationPlayer> players)
    {
        if (node is AnimationPlayer player)
        {
            players.Add(player);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectAnimationPlayers(child, players);
        }
    }

    private static void CollectAnimationTrees(Node node, List<AnimationTree> trees)
    {
        if (node is AnimationTree tree)
        {
            trees.Add(tree);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectAnimationTrees(child, trees);
        }
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            _isRotating = mouseButton.Pressed;
            if (_isRotating)
            {
                GrabFocus();
            }
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Middle)
        {
            _isPanning = mouseButton.Pressed;
            if (_isPanning)
            {
                GrabFocus();
            }
            AcceptEvent();
            return;
        }

        if (!mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            AdjustCameraDistance(-0.35f);
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
            AdjustCameraDistance(0.35f);
            AcceptEvent();
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        if (_isRotating)
        {
            _yaw -= mouseMotion.Relative.X * 0.01f;
            _pitch = Mathf.Clamp(_pitch + mouseMotion.Relative.Y * 0.01f, -1.2f, 1.2f);
            ApplyCameraOrbit();
            AcceptEvent();
            return;
        }

        if (_isPanning)
        {
            PanCamera(mouseMotion.Relative);
            AcceptEvent();
        }
    }

    private void PanCamera(Vector2 relative)
    {
        var basis = _camera.GlobalTransform.Basis;
        var right = basis.X.Normalized();
        var up = basis.Y.Normalized();
        var panFactor = _orbitDistance * 0.0015f;

        _cameraTarget += (-right * relative.X + up * relative.Y) * panFactor;
        ApplyCameraOrbit();
    }

    private void ApplyCameraOrbit()
    {
        var cosPitch = Mathf.Cos(_pitch);
        var sinPitch = Mathf.Sin(_pitch);
        var sinYaw = Mathf.Sin(_yaw);
        var cosYaw = Mathf.Cos(_yaw);

        var position = new Vector3(
            _cameraTarget.X + _orbitDistance * cosPitch * sinYaw,
            _cameraTarget.Y + _orbitDistance * sinPitch,
            _cameraTarget.Z + _orbitDistance * cosPitch * cosYaw
        );

        _camera.Position = position;
        _camera.LookAtFromPosition(position, _cameraTarget, Vector3.Up);
    }
}
