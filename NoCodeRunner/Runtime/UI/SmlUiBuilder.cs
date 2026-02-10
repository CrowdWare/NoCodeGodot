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
    private readonly Func<string, string>? _resolveAssetPath;
    private readonly AnimationControlApi _animationApi;
    private readonly UiActionDispatcher _actionDispatcher;
    private readonly Dictionary<string, Viewport3DControl> _viewportsById = new(StringComparer.OrdinalIgnoreCase);

    public SmlUiBuilder(
        NodeFactoryRegistry registry,
        NodePropertyMapper propertyMapper,
        AnimationControlApi animationApi,
        Func<string, string>? resolveAssetPath = null)
    {
        _registry = registry;
        _propertyMapper = propertyMapper;
        _animationApi = animationApi;
        _resolveAssetPath = resolveAssetPath;
        _actionDispatcher = new UiActionDispatcher();
        RegisterDefaultActionHandlers();
    }

    public UiActionDispatcher Actions => _actionDispatcher;

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

        if (control is Viewport3DControl viewport3DForInit)
        {
            // Must be set before property mapping so `id`/`model` handlers can register animations.
            viewport3DForInit.AnimationApi = _animationApi;
        }

        foreach (var (propertyName, value) in node.Properties)
        {
            if (TryApplyWindowScalingMetadata(control, node.Name, propertyName, value))
            {
                continue;
            }

            _propertyMapper.Apply(control, propertyName, value, _resolveAssetPath);
        }

        if (!node.TryGetProperty("fillMaxSize", out _) && ShouldFillMaxSizeByDefault(node.Name))
        {
            NodePropertyMapper.ApplyFillMaxSize(control);
        }

        if (control is Viewport3DControl viewport3D)
        {
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

                if (string.IsNullOrWhiteSpace(id)
                    && string.IsNullOrWhiteSpace(action)
                    && string.IsNullOrWhiteSpace(target))
                {
                    // Ignore framework/internal buttons that have no SML metadata.
                    return;
                }

                RunnerLogger.Info("UI", $"Button pressed: id='{id}', action='{action}', target='{target}'");

                _actionDispatcher.Dispatch(new UiActionContext(
                    Source: control,
                    SourceId: id,
                    Action: action,
                    Clicked: target
                ));
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
                _actionDispatcher.Dispatch(new UiActionContext(
                    Source: control,
                    SourceId: GetMetaString(control, NodePropertyMapper.MetaId),
                    Action: action,
                    Clicked: target,
                    NumericValue: normalized
                ));
            };
        }
    }

    private void RegisterDefaultActionHandlers()
    {
        _actionDispatcher.SetPageHandler(path =>
        {
            // Hook for Main/runtime navigation integration.
            RunnerLogger.Warn("UI", $"page action requested ('{path}') but runtime page navigation is not wired yet.");
        });

        _actionDispatcher.RegisterActionHandler("closeQuery", ctx =>
        {
            ctx.Source.GetTree()?.Quit();
        });

        _actionDispatcher.RegisterActionHandler("animPlay", ctx =>
        {
            if (string.IsNullOrWhiteSpace(ctx.Clicked))
            {
                RunnerLogger.Warn("UI", "animPlay requires clicked target id.");
                return;
            }

            var animations = _animationApi.ListAnimations(ctx.Clicked);
            if (animations.Count == 0)
            {
                RunnerLogger.Warn("UI", $"animPlay: no animations available for target '{ctx.Clicked}'.");
                return;
            }

            _animationApi.Play(ctx.Clicked, animations[0]);
        });

        _actionDispatcher.RegisterActionHandler("animStop", ctx =>
        {
            if (string.IsNullOrWhiteSpace(ctx.Clicked))
            {
                RunnerLogger.Warn("UI", "animStop requires clicked target id.");
                return;
            }

            _animationApi.Stop(ctx.Clicked);
        });

        _actionDispatcher.RegisterActionHandler("animRewind", ctx =>
        {
            if (string.IsNullOrWhiteSpace(ctx.Clicked))
            {
                RunnerLogger.Warn("UI", "animRewind requires clicked target id.");
                return;
            }

            _animationApi.Rewind(ctx.Clicked);
        });

        _actionDispatcher.RegisterActionHandler("animScrub", ctx =>
        {
            if (string.IsNullOrWhiteSpace(ctx.Clicked))
            {
                RunnerLogger.Warn("UI", "animScrub requires clicked target id.");
                return;
            }

            if (ctx.NumericValue is null)
            {
                RunnerLogger.Warn("UI", "animScrub requires numeric value.");
                return;
            }

            _animationApi.SeekNormalized(ctx.Clicked, (float)ctx.NumericValue.Value);
        });

        _actionDispatcher.RegisterActionHandler("perspectiveNear", ctx => SetViewportCameraDistance(ctx.Clicked, 2f));
        _actionDispatcher.RegisterActionHandler("perspectiveDefault", ctx => SetViewportCameraDistance(ctx.Clicked, 4f));
        _actionDispatcher.RegisterActionHandler("perspectiveFar", ctx => SetViewportCameraDistance(ctx.Clicked, 7f));
        _actionDispatcher.RegisterActionHandler("zoomIn", ctx => AdjustViewportCameraDistance(ctx.Clicked, -0.6f));
        _actionDispatcher.RegisterActionHandler("zoomOut", ctx => AdjustViewportCameraDistance(ctx.Clicked, 0.6f));
        _actionDispatcher.RegisterActionHandler("cameraReset", ctx => ResetViewportCamera(ctx.Clicked));
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
