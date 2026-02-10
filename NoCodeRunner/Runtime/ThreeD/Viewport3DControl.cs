using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;

namespace Runtime.ThreeD;

public sealed partial class Viewport3DControl : SubViewportContainer
{
    private readonly SubViewport _viewport;
    private readonly Node3D _worldRoot;
    private readonly Camera3D _camera;
    private readonly DirectionalLight3D _light;

    private Node3D? _modelRoot;
    private AnimationPlayer? _animationPlayer;
    private string? _pendingAutoplayAnimation;
    private readonly List<AnimationPlayer> _animationPlayers = [];
    private readonly List<AnimationTree> _animationTrees = [];

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
    }

    public void SetModelSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            RunnerLogger.Warn("3D", "Model source is empty.");
            return;
        }

        Resource? resource = GD.Load<Resource>(source);
        if (resource is null)
        {
            RunnerLogger.Warn("3D", $"Could not load 3D resource '{source}'.");
            return;
        }

        Node? instance = resource switch
        {
            PackedScene scene => scene.Instantiate(),
            _ => null
        };

        if (instance is not Node3D node3D)
        {
            RunnerLogger.Warn("3D", $"Resource '{source}' is not a 3D scene (expected imported .glb/.gltf PackedScene).");
            return;
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
            RunnerLogger.Warn("3D", $"Model loaded from '{source}', but no usable AnimationPlayer with animations was found.");
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

        RunnerLogger.Info("3D", $"Model loaded from '{source}'. AnimationPlayers={_animationPlayers.Count}, AnimationTrees={_animationTrees.Count}, primary='{_animationPlayer.Name}', animations: {string.Join(", ", animations)}");

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
        _camera.Position = new Vector3(_camera.Position.X, _camera.Position.Y, z);
        _camera.LookAtFromPosition(_camera.Position, Vector3.Zero, Vector3.Up);
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
}
