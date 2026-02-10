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
        _camera.LookAt(Vector3.Zero, Vector3.Up);

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
        }

        _modelRoot = node3D;
        _modelRoot.Name = "ModelRoot";
        _worldRoot.AddChild(_modelRoot);

        _animationPlayer = FindAnimationPlayer(_modelRoot);
        if (_animationPlayer is null)
        {
            RunnerLogger.Info("3D", $"Model loaded from '{source}'. No AnimationPlayer found.");
            return;
        }

        var playerId = string.IsNullOrWhiteSpace(SmlId) ? Name.ToString() : SmlId;
        AnimationApi?.Register(playerId, _animationPlayer);

        var animations = new List<string>();
        foreach (string animationName in _animationPlayer.GetAnimationList())
        {
            animations.Add(animationName);
        }

        RunnerLogger.Info("3D", $"Model loaded from '{source}'. Animations: {string.Join(", ", animations)}");

        if (PlayFirstAnimationOnLoad && animations.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(DefaultAnimation) && _animationPlayer.HasAnimation(DefaultAnimation))
            {
                _animationPlayer.Play(DefaultAnimation);
            }
            else
            {
                _animationPlayer.Play(animations[0]);
            }
        }
    }

    public void SetCameraDistance(float z)
    {
        _camera.Position = new Vector3(_camera.Position.X, _camera.Position.Y, z);
        _camera.LookAt(Vector3.Zero, Vector3.Up);
    }

    public void SetLightEnergy(float energy)
    {
        _light.LightEnergy = energy;
    }

    private static AnimationPlayer? FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer player)
        {
            return player;
        }

        foreach (Node child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
