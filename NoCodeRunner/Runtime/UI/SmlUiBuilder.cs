using Godot;
using Runtime.Logging;
using Runtime.Sml;
using Runtime.ThreeD;
using System;
using System.Collections.Generic;

namespace Runtime.UI;

public sealed class SmlUiBuilder
{
    public const string MetaScalingMode = "sml_windowScalingMode";
    public const string MetaDesignSizeX = "sml_windowDesignSizeX";
    public const string MetaDesignSizeY = "sml_windowDesignSizeY";

    private readonly NodeFactoryRegistry _registry;
    private readonly NodePropertyMapper _propertyMapper;
    private readonly AnimationControlApi _animationApi;
    private readonly Dictionary<string, Viewport3DControl> _viewportsById = new(StringComparer.OrdinalIgnoreCase);

    public SmlUiBuilder(NodeFactoryRegistry registry, NodePropertyMapper propertyMapper, AnimationControlApi animationApi)
    {
        _registry = registry;
        _propertyMapper = propertyMapper;
        _animationApi = animationApi;
    }

    public Control Build(SmlDocument document)
    {
        if (document.Roots.Count == 0)
        {
            return BuildFallback("Empty SML document");
        }

        foreach (var warning in document.Warnings)
        {
            RunnerLogger.Warn("UI", warning);
        }

        _viewportsById.Clear();

        var rootNode = document.Roots[0];
        var ui = BuildNodeRecursive(rootNode);
        return ui ?? BuildFallback($"Could not build root node '{rootNode.Name}'.");
    }

    private Control? BuildNodeRecursive(SmlNode node)
    {
        if (!_registry.TryCreate(node.Name, out var control))
        {
            RunnerLogger.Warn("UI", $"No factory registered for '{node.Name}'. Node skipped.");
            return null;
        }

        foreach (var (propertyName, value) in node.Properties)
        {
            if (TryApplyWindowScalingMetadata(control, node.Name, propertyName, value))
            {
                continue;
            }

            _propertyMapper.Apply(control, propertyName, value);
        }

        if (!node.TryGetProperty("fillMaxSize", out _) && ShouldFillMaxSizeByDefault(node.Name))
        {
            NodePropertyMapper.ApplyFillMaxSize(control);
        }

        if (control is Viewport3DControl viewport3D)
        {
            viewport3D.AnimationApi = _animationApi;

            var viewportId = GetMetaString(control, NodePropertyMapper.MetaId);
            if (!string.IsNullOrWhiteSpace(viewportId))
            {
                _viewportsById[viewportId] = viewport3D;
            }
        }

        foreach (var child in node.Children)
        {
            var childControl = BuildNodeRecursive(child);
            if (childControl is not null)
            {
                control.AddChild(childControl);

                if (control is TabContainer tabs && string.Equals(child.Name, "Tab", StringComparison.OrdinalIgnoreCase))
                {
                    var tabIndex = tabs.GetTabIdxFromControl(childControl);
                    var tabTitle = child.TryGetProperty("title", out var titleValue)
                        ? titleValue.AsStringOrThrow("title")
                        : child.TryGetProperty("label", out var labelValue)
                            ? labelValue.AsStringOrThrow("label")
                            : child.Name;
                    tabs.SetTabTitle(tabIndex, tabTitle);
                }
            }
        }

        BindInteractions(control);

        return control;
    }

    private static bool TryApplyWindowScalingMetadata(Control control, string nodeName, string propertyName, SmlValue value)
    {
        if (!nodeName.Equals("Window", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        switch (propertyName.ToLowerInvariant())
        {
            case "scaling":
                control.SetMeta(MetaScalingMode, Variant.From(value.AsStringOrThrow(propertyName)));
                return true;

            case "designsize":
                var designSize = value.AsVec2iOrThrow(propertyName);
                control.SetMeta(MetaDesignSizeX, Variant.From(designSize.X));
                control.SetMeta(MetaDesignSizeY, Variant.From(designSize.Y));
                return true;

            default:
                return false;
        }
    }

    private void BindInteractions(Control control)
    {
        if (control is Button button)
        {
            button.Pressed += () =>
            {
                var id = GetMetaString(control, NodePropertyMapper.MetaId);
                var action = GetMetaString(control, NodePropertyMapper.MetaAction);
                var target = GetMetaString(control, NodePropertyMapper.MetaClicked);

                RunnerLogger.Info("UI", $"Button pressed: id='{id}', action='{action}', target='{target}'");

                if (string.Equals(action, "closeQuery", StringComparison.OrdinalIgnoreCase))
                {
                    var tree = button.GetTree();
                    tree?.Quit();
                    return;
                }

                HandleViewportAction(action, target);
            };
        }

        if (control is TextEdit textEdit)
        {
            textEdit.TextChanged += () =>
            {
                var id = GetMetaString(control, NodePropertyMapper.MetaId);
                RunnerLogger.Info("UI", $"TextEdit changed: id='{id}', length={textEdit.Text.Length}");
            };
        }

        if (control is HSlider slider)
        {
            slider.ValueChanged += value =>
            {
                var action = GetMetaString(control, NodePropertyMapper.MetaAction);
                if (!string.Equals(action, "animScrub", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var target = GetMetaString(control, NodePropertyMapper.MetaClicked);
                if (string.IsNullOrWhiteSpace(target))
                {
                    RunnerLogger.Warn("UI", "Slider action 'animScrub' requires 'clicked' target id (Viewport3D id).");
                    return;
                }

                var normalized = (float)(value / 100.0);
                _animationApi.SeekNormalized(target, normalized);
            };
        }
    }

    private void HandleViewportAction(string action, string targetId)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            return;
        }

        switch (action)
        {
            case "animPlay":
            {
                var animations = _animationApi.ListAnimations(targetId);
                if (animations.Count == 0)
                {
                    RunnerLogger.Warn("UI", $"animPlay: no animations available for target '{targetId}'.");
                    return;
                }

                _animationApi.Play(targetId, animations[0]);
                return;
            }

            case "animStop":
                _animationApi.Stop(targetId);
                return;

            case "animRewind":
                _animationApi.Rewind(targetId);
                return;

            case "perspectiveNear":
                SetViewportCameraDistance(targetId, 2f);
                return;

            case "perspectiveDefault":
                SetViewportCameraDistance(targetId, 4f);
                return;

            case "perspectiveFar":
                SetViewportCameraDistance(targetId, 7f);
                return;

            case "zoomIn":
                AdjustViewportCameraDistance(targetId, -0.6f);
                return;

            case "zoomOut":
                AdjustViewportCameraDistance(targetId, 0.6f);
                return;

            case "cameraReset":
                ResetViewportCamera(targetId);
                return;
        }
    }

    private void SetViewportCameraDistance(string viewportId, float distance)
    {
        if (!_viewportsById.TryGetValue(viewportId, out var viewport))
        {
            RunnerLogger.Warn("UI", $"Perspective action target '{viewportId}' not found.");
            return;
        }

        viewport.SetCameraDistance(distance);
    }

    private void AdjustViewportCameraDistance(string viewportId, float delta)
    {
        if (!_viewportsById.TryGetValue(viewportId, out var viewport))
        {
            RunnerLogger.Warn("UI", $"Zoom action target '{viewportId}' not found.");
            return;
        }

        viewport.AdjustCameraDistance(delta);
    }

    private void ResetViewportCamera(string viewportId)
    {
        if (!_viewportsById.TryGetValue(viewportId, out var viewport))
        {
            RunnerLogger.Warn("UI", $"Camera reset action target '{viewportId}' not found.");
            return;
        }

        viewport.ResetView();
    }

    private static string GetMetaString(Control control, string key)
    {
        if (!control.HasMeta(key))
        {
            return string.Empty;
        }

        return control.GetMeta(key).AsString();
    }

    private static bool ShouldFillMaxSizeByDefault(string nodeName)
    {
        return nodeName.Equals("Window", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Column", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Box", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Tabs", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Tab", StringComparison.OrdinalIgnoreCase);
    }

    private static Control BuildFallback(string message)
    {
        var root = new VBoxContainer();
        root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        root.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        root.AddChild(new Label
        {
            Text = "NoCodeRunner UI fallback",
            HorizontalAlignment = HorizontalAlignment.Center
        });

        root.AddChild(new Label
        {
            Text = message,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        return root;
    }
}
