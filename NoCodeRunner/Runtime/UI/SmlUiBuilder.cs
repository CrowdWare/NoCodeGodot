using Godot;
using Runtime.Logging;
using Runtime.Sml;
using Runtime.ThreeD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
            RunnerLogger.ParserWarning(warning);
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

        control.SetMeta(NodePropertyMapper.MetaNodeName, Variant.From(node.Name));

        foreach (var (propertyName, value) in node.Properties)
        {
            if (TryApplyWindowScalingMetadata(control, node.Name, propertyName, value))
            {
                continue;
            }

            _propertyMapper.Apply(control, propertyName, value, _resolveAssetPath);
        }

        if (control is CodeEdit && !node.TryGetProperty("font", out _) && !node.TryGetProperty("fontSource", out _))
        {
            _propertyMapper.Apply(control, "font", SmlValue.FromString("appres:/Anonymous.ttf"), _resolveAssetPath);
        }

        ApplyDefaultLayoutMode(control, node.Name);

        if (!node.TryGetProperty("fillMaxSize", out _) && ShouldFillMaxSizeByDefault(node.Name))
        {
            NodePropertyMapper.ApplyFillMaxSize(control);
        }

        if (control is Viewport3DControl viewport3D)
        {
            var viewportIdValue = GetMetaId(control, NodePropertyMapper.MetaIdValue);
            if (viewportIdValue.IsSet)
            {
                _viewportsById[viewportIdValue.Value.ToString()] = viewport3D;
            }
        }

        if (control is Tree treeControl && string.Equals(node.Name, "TreeView", StringComparison.OrdinalIgnoreCase))
        {
            BuildTreeViewItems(treeControl, node);
        }
        else
        {
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
        }

        BindInteractions(control);

        if (IsScrollable(control))
        {
            control = WrapWithScrollContainer(control, node.Name);
        }

        if (HasPadding(control))
        {
            control = WrapWithPaddingContainer(control, node.Name);
        }

        return control;
    }

    private static void ApplyDefaultLayoutMode(Control control, string nodeName)
    {
        if (control.HasMeta(NodePropertyMapper.MetaLayoutMode))
        {
            return;
        }

        var mode = nodeName.Equals("Page", StringComparison.OrdinalIgnoreCase)
                   || nodeName.Equals("Column", StringComparison.OrdinalIgnoreCase)
                   || nodeName.Equals("Row", StringComparison.OrdinalIgnoreCase)
                   || nodeName.Equals("Markdown", StringComparison.OrdinalIgnoreCase)
                   || nodeName.Equals("CodeEdit", StringComparison.OrdinalIgnoreCase)
                   || nodeName.Equals("Box", StringComparison.OrdinalIgnoreCase)
            ? "document"
            : "app";

        control.SetMeta(NodePropertyMapper.MetaLayoutMode, Variant.From(mode));
    }

    private static bool IsScrollable(Control control)
    {
        return control.HasMeta(NodePropertyMapper.MetaScrollable)
               && control.GetMeta(NodePropertyMapper.MetaScrollable).AsBool();
    }

    private static bool HasPadding(Control control)
    {
        return control.HasMeta(NodePropertyMapper.MetaPaddingTop)
               || control.HasMeta(NodePropertyMapper.MetaPaddingRight)
               || control.HasMeta(NodePropertyMapper.MetaPaddingBottom)
               || control.HasMeta(NodePropertyMapper.MetaPaddingLeft);
    }

    private static Control WrapWithPaddingContainer(Control content, string nodeName)
    {
        var top = content.HasMeta(NodePropertyMapper.MetaPaddingTop)
            ? content.GetMeta(NodePropertyMapper.MetaPaddingTop).AsInt32()
            : 0;
        var right = content.HasMeta(NodePropertyMapper.MetaPaddingRight)
            ? content.GetMeta(NodePropertyMapper.MetaPaddingRight).AsInt32()
            : 0;
        var bottom = content.HasMeta(NodePropertyMapper.MetaPaddingBottom)
            ? content.GetMeta(NodePropertyMapper.MetaPaddingBottom).AsInt32()
            : 0;
        var left = content.HasMeta(NodePropertyMapper.MetaPaddingLeft)
            ? content.GetMeta(NodePropertyMapper.MetaPaddingLeft).AsInt32()
            : 0;

        var margin = new MarginContainer
        {
            Name = $"{nodeName}Padding"
        };

        margin.AddThemeConstantOverride("margin_top", top);
        margin.AddThemeConstantOverride("margin_right", right);
        margin.AddThemeConstantOverride("margin_bottom", bottom);
        margin.AddThemeConstantOverride("margin_left", left);

        margin.SetMeta(NodePropertyMapper.MetaNodeName, Variant.From("PaddingContainer"));
        if (content.HasMeta(NodePropertyMapper.MetaLayoutMode))
        {
            margin.SetMeta(NodePropertyMapper.MetaLayoutMode, content.GetMeta(NodePropertyMapper.MetaLayoutMode));
        }

        if (content.GetParent() is not null)
        {
            content.GetParent()?.RemoveChild(content);
        }

        NodePropertyMapper.ApplyFillMaxSize(margin);
        NodePropertyMapper.ApplyFillMaxSize(content);
        content.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        content.SetOffsetsPreset(Control.LayoutPreset.FullRect);

        margin.AddChild(content);
        return margin;
    }

    private static Control WrapWithScrollContainer(Control content, string nodeName)
    {
        var scroll = new DocumentScrollContainer
        {
            Name = $"{nodeName}Scroll"
        };

        scroll.SetMeta(NodePropertyMapper.MetaNodeName, Variant.From("ScrollContainer"));
        scroll.SetMeta(NodePropertyMapper.MetaLayoutMode, Variant.From("document"));
        scroll.ConfigureFromSmlMeta(content);

        if (content.GetParent() is not null)
        {
            content.GetParent()?.RemoveChild(content);
        }

        NodePropertyMapper.ApplyFillMaxSize(scroll);
        NodePropertyMapper.ApplyFillMaxSize(content);
        content.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        content.SetOffsetsPreset(Control.LayoutPreset.FullRect);

        scroll.AddChild(content);
        return scroll;
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
                    SourceIdValue: GetMetaId(control, NodePropertyMapper.MetaIdValue),
                    Action: action,
                    Clicked: target,
                    ClickedIdValue: GetMetaId(control, NodePropertyMapper.MetaClickedIdValue)
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
                    SourceIdValue: GetMetaId(control, NodePropertyMapper.MetaIdValue),
                    Action: action,
                    Clicked: target,
                    ClickedIdValue: GetMetaId(control, NodePropertyMapper.MetaClickedIdValue),
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

        _actionDispatcher.RegisterActionHandler("perspectiveNear", ctx => SetViewportCameraDistance(ctx.ClickedIdValue, 2f));
        _actionDispatcher.RegisterActionHandler("perspectiveDefault", ctx => SetViewportCameraDistance(ctx.ClickedIdValue, 4f));
        _actionDispatcher.RegisterActionHandler("perspectiveFar", ctx => SetViewportCameraDistance(ctx.ClickedIdValue, 7f));
        _actionDispatcher.RegisterActionHandler("zoomIn", ctx => AdjustViewportCameraDistance(ctx.ClickedIdValue, -0.6f));
        _actionDispatcher.RegisterActionHandler("zoomOut", ctx => AdjustViewportCameraDistance(ctx.ClickedIdValue, 0.6f));
        _actionDispatcher.RegisterActionHandler("cameraReset", ctx => ResetViewportCamera(ctx.ClickedIdValue));
    }

    private void SetViewportCameraDistance(Id viewportId, float distance)
    {
        if (!viewportId.IsSet || !_viewportsById.TryGetValue(viewportId.Value.ToString(), out var viewport))
        {
            RunnerLogger.Warn("UI", $"Perspective action target '{viewportId.Value}' not found.");
            return;
        }

        viewport.SetCameraDistance(distance);
    }

    private void AdjustViewportCameraDistance(Id viewportId, float delta)
    {
        if (!viewportId.IsSet || !_viewportsById.TryGetValue(viewportId.Value.ToString(), out var viewport))
        {
            RunnerLogger.Warn("UI", $"Zoom action target '{viewportId.Value}' not found.");
            return;
        }

        viewport.AdjustCameraDistance(delta);
    }

    private void ResetViewportCamera(Id viewportId)
    {
        if (!viewportId.IsSet || !_viewportsById.TryGetValue(viewportId.Value.ToString(), out var viewport))
        {
            RunnerLogger.Warn("UI", $"Camera reset action target '{viewportId.Value}' not found.");
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

    private static Id GetMetaId(Control control, string key)
    {
        if (!control.HasMeta(key))
        {
            return new Id(0);
        }

        return new Id(control.GetMeta(key).AsInt32());
    }

    private static bool ShouldFillMaxSizeByDefault(string nodeName)
    {
        return nodeName.Equals("Window", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Page", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Panel", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Column", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("CodeEdit", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Markdown", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Box", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Tabs", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("Tab", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("DockSpace", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("DockPanel", StringComparison.OrdinalIgnoreCase)
               || nodeName.Equals("TreeView", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLowercaseMetaNode(SmlNode node)
    {
        return !string.IsNullOrWhiteSpace(node.Name) && char.IsLower(node.Name[0]);
    }

    private void BuildTreeViewItems(Tree tree, SmlNode treeNode)
    {
        tree.Columns = 1;
        tree.HideRoot = GetMetaBool(tree, NodePropertyMapper.MetaTreeHideRoot, defaultValue: true);
        var showGuides = GetMetaBool(tree, NodePropertyMapper.MetaTreeShowGuides, defaultValue: true);
        tree.HideFolding = false;
        tree.AddThemeConstantOverride("draw_guides", showGuides ? 1 : 0);

        if (tree.HasMeta(NodePropertyMapper.MetaTreeIndent))
        {
            var indent = Math.Max(0, tree.GetMeta(NodePropertyMapper.MetaTreeIndent).AsInt32());
            tree.AddThemeConstantOverride("item_margin", indent);
        }

        if (tree.HasMeta(NodePropertyMapper.MetaTreeRowHeight))
        {
            var rowHeight = Math.Max(1, tree.GetMeta(NodePropertyMapper.MetaTreeRowHeight).AsInt32());
            tree.AddThemeConstantOverride("v_separation", rowHeight);
        }

        var rootItem = tree.CreateItem();
        var mappedItems = new Dictionary<TreeItem, TreeViewItem>();
        var seenIds = new HashSet<int>();

        foreach (var child in treeNode.Children)
        {
            if (IsLowercaseMetaNode(child))
            {
                continue;
            }

            if (!string.Equals(child.Name, "Item", StringComparison.OrdinalIgnoreCase))
            {
                throw new SmlParseException($"TreeView supports only Item children (and lowercase meta nodes). Found '{child.Name}' at line {child.Line}.");
            }

            var item = BuildTreeItemRecursive(tree, rootItem, child, mappedItems, seenIds);
            if (item is null)
            {
                continue;
            }
        }

        tree.ItemSelected += () =>
        {
            var selected = tree.GetSelected();
            if (selected is null || !mappedItems.TryGetValue(selected, out var selectedModel))
            {
                return;
            }

            var selectedId = new Id(selectedModel.Id);
            var treeId = GetMetaString(tree, NodePropertyMapper.MetaId);
            _actionDispatcher.Dispatch(new UiActionContext(
                Source: tree,
                SourceId: treeId,
                SourceIdValue: GetMetaId(tree, NodePropertyMapper.MetaIdValue),
                Action: "treeItemSelected",
                Clicked: string.Empty,
                ClickedIdValue: new Id(0),
                ItemId: selectedId,
                TreeItem: selectedModel
            ));

            var handled = false;

            if (!string.IsNullOrWhiteSpace(treeId))
            {
                handled = TryInvokeTreeSelectionHandler(tree, $"{treeId}ItemSelected", selectedId, selectedModel);
            }

            if (!handled)
            {
                TryInvokeTreeSelectionHandler(tree, "treeViewItemSelected", selectedId, selectedModel);
            }
        };

        tree.ButtonClicked += (item, _column, id, _mouseButtonIndex) =>
        {
            if (item is null || !mappedItems.TryGetValue(item, out var selectedModel))
            {
                return;
            }

            var toggleIndex = -1;
            TreeViewToggle? selectedToggle = null;
            for (var i = 0; i < selectedModel.Toggles.Count; i++)
            {
                if (selectedModel.Toggles[i].ToggleId.Value != (int)id)
                {
                    continue;
                }

                selectedToggle = selectedModel.Toggles[i];
                toggleIndex = i;
                break;
            }

            if (selectedToggle is null)
            {
                return;
            }

            selectedToggle.State = !selectedToggle.State;
            var texture = ResolveToggleTexture(selectedToggle);
            if (texture is not null && toggleIndex >= 0)
            {
                item.SetButton(0, toggleIndex, texture);
            }

            var selectedId = new Id(selectedModel.Id);
            var treeId = GetMetaString(tree, NodePropertyMapper.MetaId);
            _actionDispatcher.Dispatch(new UiActionContext(
                Source: tree,
                SourceId: treeId,
                SourceIdValue: GetMetaId(tree, NodePropertyMapper.MetaIdValue),
                Action: "treeItemToggle",
                Clicked: selectedToggle.Name,
                ClickedIdValue: new Id(selectedToggle.ToggleId.Value),
                BoolValue: selectedToggle.State,
                ItemId: selectedId,
                ToggleIdValue: selectedToggle.ToggleId,
                TreeItem: selectedModel
            ));

            var handled = false;

            if (!string.IsNullOrWhiteSpace(treeId))
            {
                handled = TryInvokeTreeToggleHandler(tree, $"{treeId}ItemToggle", selectedId, selectedModel, selectedToggle.ToggleId, selectedToggle.State);
            }

            if (!handled)
            {
                TryInvokeTreeToggleHandler(tree, "treeViewItemToggle", selectedId, selectedModel, selectedToggle.ToggleId, selectedToggle.State);
            }
        };
    }

    private TreeViewItem? BuildTreeItemRecursive(
        Tree tree,
        TreeItem parent,
        SmlNode itemNode,
        Dictionary<TreeItem, TreeViewItem> mappedItems,
        HashSet<int> seenIds)
    {
        if (!string.Equals(itemNode.Name, "Item", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var id = 0;
        if (itemNode.TryGetProperty("id", out var idValue))
        {
            var idRaw = idValue.AsStringOrThrow("id");
            if (!int.TryParse(idRaw, out id))
            {
                throw new SmlParseException($"TreeView Item.id must be numeric when provided. Found '{idRaw}' at line {itemNode.Line}.");
            }

            if (id != 0 && !seenIds.Add(id))
            {
                throw new SmlParseException($"Duplicate TreeView Item.id '{id}' in the same TreeView scope (line {itemNode.Line}).");
            }
        }

        var text = itemNode.GetRequiredProperty("text").AsStringOrThrow("text");
        var icon = itemNode.TryGetProperty("icon", out var iconValue)
            ? iconValue.AsStringOrThrow("icon")
            : null;
        var expanded = itemNode.TryGetProperty("expanded", out var expandedValue)
            ? expandedValue.AsBoolOrThrow("expanded")
            : false;

        SmlNode? dataNode = null;
        var dataBlocks = 0;
        var toggles = new List<TreeViewToggle>();

        foreach (var child in itemNode.Children)
        {
            if (string.Equals(child.Name, "Item", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsLowercaseMetaNode(child))
            {
                if (!string.Equals(child.Name, "data", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                dataBlocks++;
                if (dataBlocks > 1)
                {
                    throw new SmlParseException($"TreeView Item at line {itemNode.Line} may contain at most one data{{}} block.");
                }

                if (child.Children.Count != 1)
                {
                    throw new SmlParseException($"TreeView data{{}} at line {child.Line} must contain exactly one root node.");
                }

                dataNode = child.Children[0];
                continue;
            }

            if (string.Equals(child.Name, "Toggle", StringComparison.OrdinalIgnoreCase))
            {
                var toggleName = child.GetRequiredProperty("id").AsStringOrThrow("id");
                var toggleId = new ToggleId(IdRuntimeScope.GetOrCreate(toggleName).Value);
                var imageOn = child.GetRequiredProperty("imageOn").AsStringOrThrow("imageOn");
                var imageOff = child.GetRequiredProperty("imageOff").AsStringOrThrow("imageOff");
                var state = child.TryGetProperty("state", out var stateValue)
                    ? stateValue.AsBoolOrThrow("state")
                    : true;

                toggles.Add(new TreeViewToggle
                {
                    ToggleId = toggleId,
                    Name = toggleName,
                    State = state,
                    ImageOn = imageOn,
                    ImageOff = imageOff
                });
                continue;
            }

            throw new SmlParseException($"TreeView Item supports only Item/Toggle children and lowercase meta nodes. Found '{child.Name}' at line {child.Line}.");
        }

        var uiItem = tree.CreateItem(parent);
        uiItem.SetText(0, text);
        uiItem.Collapsed = !expanded;

        var model = new TreeViewItem
        {
            Id = id,
            Text = text,
            Icon = icon,
            Expanded = expanded,
            Data = dataNode
        };

        if (!string.IsNullOrWhiteSpace(icon))
        {
            var iconPath = _resolveAssetPath is null ? icon : _resolveAssetPath(icon);
            var iconTexture = LoadTexture2D(iconPath, $"tree item '{text}' icon");
            if (iconTexture is not null)
            {
                uiItem.SetIcon(0, iconTexture);
            }
            else
            {
                RunnerLogger.Warn("UI", $"Could not load tree item icon '{iconPath}' for item '{text}'.");
            }
        }

        foreach (var toggle in toggles)
        {
            model.Toggles.Add(toggle);
            var texture = ResolveToggleTexture(toggle);
            if (texture is not null)
            {
                uiItem.AddButton(0, texture, toggle.ToggleId.Value, false, toggle.Name);
            }
        }

        mappedItems[uiItem] = model;

        foreach (var child in itemNode.Children)
        {
            if (!string.Equals(child.Name, "Item", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var childModel = BuildTreeItemRecursive(tree, uiItem, child, mappedItems, seenIds);
            if (childModel is not null)
            {
                model.Children.Add(childModel);
            }
        }

        return model;
    }

    private static bool TryInvokeTreeSelectionHandler(Tree sourceTree, string methodName, Id id, TreeViewItem item)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return false;
        }

        var sceneTree = sourceTree.GetTree();
        if (sceneTree?.Root is null)
        {
            return false;
        }

        var stack = new Stack<Node>();
        stack.Push(sceneTree.Root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (TryInvokeMethod(node, methodName, id, item))
            {
                return true;
            }

            for (var i = node.GetChildCount() - 1; i >= 0; i--)
            {
                stack.Push(node.GetChild(i));
            }
        }

        return false;
    }

    private static bool TryInvokeMethod(Node target, string methodName, Id id, TreeViewItem item)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase,
            binder: null,
            types: [typeof(Id), typeof(TreeViewItem)],
            modifiers: null);

        if (method is null)
        {
            return false;
        }

        try
        {
            method.Invoke(target, [id, item]);
            return true;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("UI", $"TreeView selection handler '{methodName}' on '{target.Name}' threw exception.", ex);
            return true;
        }
    }

    private static bool TryInvokeTreeToggleHandler(Tree sourceTree, string methodName, Id id, TreeViewItem item, ToggleId toggleId, bool isOn)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return false;
        }

        var sceneTree = sourceTree.GetTree();
        if (sceneTree?.Root is null)
        {
            return false;
        }

        var stack = new Stack<Node>();
        stack.Push(sceneTree.Root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            var method = node.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase,
                binder: null,
                types: [typeof(Id), typeof(TreeViewItem), typeof(ToggleId), typeof(bool)],
                modifiers: null);

            if (method is not null)
            {
                try
                {
                    method.Invoke(node, [id, item, toggleId, isOn]);
                }
                catch (Exception ex)
                {
                    RunnerLogger.Warn("UI", $"TreeView toggle handler '{methodName}' on '{node.Name}' threw exception.", ex);
                }

                return true;
            }

            for (var i = node.GetChildCount() - 1; i >= 0; i--)
            {
                stack.Push(node.GetChild(i));
            }
        }

        return false;
    }

    private Texture2D? ResolveToggleTexture(TreeViewToggle toggle)
    {
        var source = toggle.State ? toggle.ImageOn : toggle.ImageOff;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var resolved = _resolveAssetPath is null ? source : _resolveAssetPath(source);
        var texture = LoadTexture2D(resolved, $"toggle '{toggle.Name}'");
        if (texture is null)
        {
            RunnerLogger.Warn("UI", $"Could not load toggle image '{resolved}' for toggle '{toggle.Name}'.");
        }

        return texture;
    }

    private static Texture2D? LoadTexture2D(string source, string context)
    {
        if (source.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            || source.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            return GD.Load<Texture2D>(source);
        }

        string absolutePath;
        if (source.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(source, UriKind.Absolute, out var fileUri) || !fileUri.IsFile)
            {
                RunnerLogger.Warn("UI", $"Invalid file URI '{source}' for {context}.");
                return null;
            }

            absolutePath = fileUri.LocalPath;
        }
        else if (Path.IsPathRooted(source))
        {
            absolutePath = source;
        }
        else
        {
            RunnerLogger.Warn("UI", $"Texture source '{source}' for {context} is not loadable. Use res://, user://, file:// or absolute path.");
            return null;
        }

        var image = new Image();
        var error = image.Load(absolutePath);
        if (error != Error.Ok)
        {
            RunnerLogger.Warn("UI", $"Could not load image file '{absolutePath}' for {context} (error: {error}).");
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static bool GetMetaBool(Control control, string key, bool defaultValue)
    {
        if (!control.HasMeta(key))
        {
            return defaultValue;
        }

        return control.GetMeta(key).AsBool();
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
