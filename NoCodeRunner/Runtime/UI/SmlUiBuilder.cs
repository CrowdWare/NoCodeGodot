using Godot;
using Runtime.Generated;
using Runtime.Logging;
using Runtime.Sml;
using Runtime.ThreeD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        var ui = BuildNodeRecursive(rootNode, parentNodeName: null);
        var built = ui ?? BuildFallback($"Could not build root node '{rootNode.Name}'.");
        ApplyAnchorsFromMetaRecursively(built);

        var enableDockingManager = built.HasMeta(NodePropertyMapper.MetaEnableDockingManager)
            && built.GetMeta(NodePropertyMapper.MetaEnableDockingManager).AsBool();
        var hasDockingHost = ContainsDockingHost(built);
        var legacyDockingTabCount = CountDragRearrangeTabContainers(built);
        RunnerLogger.Info("UI", $"Docking activation check: explicit={enableDockingManager}, host={hasDockingHost}, legacyTabs={legacyDockingTabCount}.");
        if (enableDockingManager || hasDockingHost)
        {
            DockingManagerRuntime.AttachIfNeeded(built);
        }

        return built;
    }

    private static bool ContainsDockingHost(Control root)
    {
        var stack = new Stack<Control>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is DockingHostControl)
            {
                return true;
            }

            for (var i = current.GetChildCount() - 1; i >= 0; i--)
            {
                if (current.GetChild(i) is Control childControl)
                {
                    stack.Push(childControl);
                }
            }
        }

        return false;
    }

    private static int CountDragRearrangeTabContainers(Control root)
    {
        var count = 0;
        var stack = new Stack<Control>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is TabContainer tabs && tabs.DragToRearrangeEnabled)
            {
                count++;
            }

            for (var i = current.GetChildCount() - 1; i >= 0; i--)
            {
                if (current.GetChild(i) is Control childControl)
                {
                    stack.Push(childControl);
                }
            }
        }

        return count;
    }

    private static void ApplyAnchorsFromMetaRecursively(Control control)
    {
        var hasLeft = control.HasMeta(NodePropertyMapper.MetaAnchorLeft) && control.GetMeta(NodePropertyMapper.MetaAnchorLeft).AsBool();
        var hasRight = control.HasMeta(NodePropertyMapper.MetaAnchorRight) && control.GetMeta(NodePropertyMapper.MetaAnchorRight).AsBool();
        var hasTop = control.HasMeta(NodePropertyMapper.MetaAnchorTop) && control.GetMeta(NodePropertyMapper.MetaAnchorTop).AsBool();
        var hasBottom = control.HasMeta(NodePropertyMapper.MetaAnchorBottom) && control.GetMeta(NodePropertyMapper.MetaAnchorBottom).AsBool();

        // Horizontal semantics:
        // - left+right => stretch horizontally (0..1)
        // - left only  => pin to left edge   (0..0)
        // - right only => pin to right edge  (1..1)
        if (hasLeft && hasRight)
        {
            control.SetAnchor(Side.Left, 0f, true);
            control.SetAnchor(Side.Right, 1f, true);
        }
        else if (hasRight)
        {
            control.SetAnchor(Side.Left, 1f, true);
            control.SetAnchor(Side.Right, 1f, true);
        }
        else if (hasLeft)
        {
            control.SetAnchor(Side.Left, 0f, true);
            control.SetAnchor(Side.Right, 0f, true);
        }

        // Vertical semantics:
        // - top+bottom => stretch vertically (0..1)
        // - top only   => pin to top edge    (0..0)
        // - bottom only=> pin to bottom edge (1..1)
        if (hasTop && hasBottom)
        {
            control.SetAnchor(Side.Top, 0f, true);
            control.SetAnchor(Side.Bottom, 1f, true);
        }
        else if (hasBottom)
        {
            control.SetAnchor(Side.Top, 1f, true);
            control.SetAnchor(Side.Bottom, 1f, true);
        }
        else if (hasTop)
        {
            control.SetAnchor(Side.Top, 0f, true);
            control.SetAnchor(Side.Bottom, 0f, true);
        }

        for (var i = 0; i < control.GetChildCount(); i++)
        {
            if (control.GetChild(i) is Control childControl)
            {
                ApplyAnchorsFromMetaRecursively(childControl);
            }
        }
    }

    private Control? BuildNodeRecursive(SmlNode node, string? parentNodeName)
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

        foreach (var (propertyName, value) in node.Properties
                     .OrderBy(kvp => node.PropertyLines.TryGetValue(kvp.Key, out var line) ? line : int.MaxValue)
                     .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (TryApplyContextProperty(control, parentNodeName, node.Name, propertyName, value))
            {
                continue;
            }

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

        if (control is Viewport3DControl viewport3D)
        {
            var viewportIdValue = GetMetaId(control, NodePropertyMapper.MetaIdValue);
            if (viewportIdValue.IsSet)
            {
                _viewportsById[viewportIdValue.Value.ToString()] = viewport3D;
            }
        }

        if (string.Equals(node.Name, "MenuBar", StringComparison.OrdinalIgnoreCase))
        {
            BuildMenuBar(control, node);
        }

        if (control is Tree treeControl)
        {
            BuildTreeViewItems(treeControl, node);
        }
        else if (string.Equals(node.Name, "MenuBar", StringComparison.OrdinalIgnoreCase))
        {
            // Menu children are consumed by BuildMenuBar.
        }
        else
        {
            foreach (var child in node.Children)
            {
                var childControl = BuildNodeRecursive(child, node.Name);
                if (childControl is not null)
                {
                    if (control is DockingContainerControl dockingContainer)
                    {
                        var tabTitle = childControl.HasMeta(BuildContextMetaKey("tabTitle"))
                            ? childControl.GetMeta(BuildContextMetaKey("tabTitle")).AsString()
                            : child.TryGetProperty("title", out var titleValue)
                                ? titleValue.AsStringOrThrow("title")
                                : child.TryGetProperty("label", out var labelValue)
                                    ? labelValue.AsStringOrThrow("label")
                                    : child.Name;

                        dockingContainer.AddDockTab(childControl, tabTitle);
                    }
                    else
                    {
                        control.AddChild(childControl);

                        if (control is TabContainer tabs)
                        {
                            var tabIndex = tabs.GetTabIdxFromControl(childControl);
                            var tabTitle = childControl.HasMeta(BuildContextMetaKey("tabTitle"))
                                ? childControl.GetMeta(BuildContextMetaKey("tabTitle")).AsString()
                                : child.TryGetProperty("title", out var titleValue)
                                    ? titleValue.AsStringOrThrow("title")
                                    : child.TryGetProperty("label", out var labelValue)
                                        ? labelValue.AsStringOrThrow("label")
                                        : child.Name;
                            tabs.SetTabTitle(tabIndex, tabTitle);
                        }
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

    private void BuildMenuBar(Control menuBarControl, SmlNode menuBarNode)
    {
        var defaults = SchemaLayoutDefaults.MenuBar;

        // Ensure app-layout runtime places the menu bar at the top when no explicit values are provided.
        if (!menuBarControl.HasMeta(NodePropertyMapper.MetaX))
        {
            menuBarControl.SetMeta(NodePropertyMapper.MetaX, Variant.From(defaults.X));
        }
        if (!menuBarControl.HasMeta(NodePropertyMapper.MetaY))
        {
            menuBarControl.SetMeta(NodePropertyMapper.MetaY, Variant.From(defaults.Y));
        }
        if (!menuBarControl.HasMeta(NodePropertyMapper.MetaHeight))
        {
            menuBarControl.SetMeta(NodePropertyMapper.MetaHeight, Variant.From(defaults.Height));
        }
        if (!menuBarControl.HasMeta(NodePropertyMapper.MetaAnchorLeft))
        {
            menuBarControl.SetMeta(NodePropertyMapper.MetaAnchorLeft, Variant.From(defaults.AnchorLeft));
        }
        if (!menuBarControl.HasMeta(NodePropertyMapper.MetaAnchorRight))
        {
            menuBarControl.SetMeta(NodePropertyMapper.MetaAnchorRight, Variant.From(defaults.AnchorRight));
        }
        if (!menuBarControl.HasMeta(NodePropertyMapper.MetaAnchorTop))
        {
            menuBarControl.SetMeta(NodePropertyMapper.MetaAnchorTop, Variant.From(defaults.AnchorTop));
        }

        menuBarControl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        menuBarControl.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        menuBarControl.CustomMinimumSize = new Vector2(menuBarControl.CustomMinimumSize.X, Math.Max(menuBarControl.CustomMinimumSize.Y, defaults.MinHeight));
        menuBarControl.ZIndex = defaults.ZIndex;

        var preferGlobalMenu = false;
        if (menuBarControl.HasMeta(NodePropertyMapper.MetaMenuPreferGlobal))
        {
            preferGlobalMenu = menuBarControl.GetMeta(NodePropertyMapper.MetaMenuPreferGlobal).AsBool();
        }

        var isMacOs = string.Equals(OS.GetName(), "macOS", StringComparison.OrdinalIgnoreCase);

        if (menuBarControl is MenuBar nativeMenuBar)
        {
            nativeMenuBar.PreferGlobalMenu = isMacOs && preferGlobalMenu;

            foreach (var menuNode in EnumerateMenuNodes(menuBarNode))
            {
                AddNativeMenuPopup(nativeMenuBar, menuNode, isMacOs && preferGlobalMenu);
            }
        }
        else if (menuBarControl is BoxContainer menuBar)
        {
            foreach (var menuNode in EnumerateMenuNodes(menuBarNode))
            {
                var menuId = menuNode.TryGetProperty("id", out var menuIdValue)
                    ? menuIdValue.AsStringOrThrow("id")
                    : string.Empty;
                var title = menuNode.TryGetProperty("title", out var titleValue)
                    ? titleValue.AsStringOrThrow("title")
                    : (string.IsNullOrWhiteSpace(menuId) ? "Menu" : menuId);

                var button = new MenuButton
                {
                    Text = title,
                    Name = string.IsNullOrWhiteSpace(menuId) ? $"Menu_{title}" : $"Menu_{menuId}"
                };

                if (!string.IsNullOrWhiteSpace(menuId))
                {
                    button.SetMeta(NodePropertyMapper.MetaId, menuId);
                    var menuIdRuntime = IdRuntimeScope.GetOrCreate(menuId);
                    button.SetMeta(NodePropertyMapper.MetaIdValue, menuIdRuntime.Value);
                }

                PopulateMenuButton(button, menuNode, menuId, isMacOs && preferGlobalMenu);
                menuBar.AddChild(button);
            }
        }

        if (menuBarControl.GetParent() is Control parent)
        {
            parent.MoveChild(menuBarControl, parent.GetChildCount() - 1);
        }
    }

    private void AddNativeMenuPopup(MenuBar menuBar, SmlNode menuNode, bool globalMacMenuMode)
    {
        var menuId = menuNode.TryGetProperty("id", out var menuIdValue)
            ? menuIdValue.AsStringOrThrow("id")
            : string.Empty;

        if (globalMacMenuMode && string.Equals(menuId, "appMenu", StringComparison.OrdinalIgnoreCase))
        {
            // Defer merge until the native menu is fully available on macOS.
            ScheduleMacAppMenuMerge(menuBar, menuNode, menuId, attemptsLeft: 6);
            return;
        }

        var title = menuNode.TryGetProperty("title", out var titleValue)
            ? titleValue.AsStringOrThrow("title")
            : (string.IsNullOrWhiteSpace(menuId) ? "Menu" : menuId);

        var popup = new PopupMenu
        {
            Name = string.IsNullOrWhiteSpace(title) ? (string.IsNullOrWhiteSpace(menuId) ? "Menu" : menuId) : title
        };

        if (!string.IsNullOrWhiteSpace(menuId))
        {
            popup.SetMeta(NodePropertyMapper.MetaId, menuId);
            var menuIdRuntime = IdRuntimeScope.GetOrCreate(menuId);
            popup.SetMeta(NodePropertyMapper.MetaIdValue, menuIdRuntime.Value);
        }

        PopulatePopupMenu(popup, menuBar, menuNode, menuId, globalMacMenuMode);
        menuBar.AddChild(popup);
    }

    private void PopulateMenuButton(MenuButton button, SmlNode menuNode, string menuId, bool globalMacMenuMode)
    {
        var popup = button.GetPopup();
        if (!string.IsNullOrWhiteSpace(menuId))
        {
            popup.SetMeta(NodePropertyMapper.MetaId, menuId);
            var menuIdRuntime = IdRuntimeScope.GetOrCreate(menuId);
            popup.SetMeta(NodePropertyMapper.MetaIdValue, menuIdRuntime.Value);
        }
        PopulatePopupMenu(popup, button, menuNode, menuId, globalMacMenuMode);
    }

    private void PopulatePopupMenu(PopupMenu popup, Control dispatchSource, SmlNode menuNode, string menuId, bool globalMacMenuMode)
    {
        popup.Clear();

        var actionMap = new Dictionary<long, (string MenuId, string ItemId)>();
        var hasSettingsItem = false;
        string? appMenuAboutText = null;
        string? appMenuQuitText = null;
        long itemIdSeed = 1;

        foreach (var child in menuNode.Children)
        {
            if (string.Equals(child.Name, "Separator", StringComparison.OrdinalIgnoreCase))
            {
                popup.AddSeparator();
                continue;
            }

            if (!IsMenuEntryNode(child.Name))
            {
                continue;
            }

            var text = child.TryGetProperty("text", out var textValue)
                ? textValue.AsStringOrThrow("text")
                : "Menu Item";
            var itemId = child.TryGetProperty("id", out var itemIdValue)
                ? itemIdValue.AsStringOrThrow("id")
                : $"item_{itemIdSeed}";
            var isCheckItemNode = string.Equals(child.Name, "CheckItem", StringComparison.OrdinalIgnoreCase);
            var isChecked = isCheckItemNode;
            if (child.TryGetProperty("checked", out var checkedValue))
            {
                isChecked = checkedValue.AsBoolOrThrow("checked");
            }
            else if (child.TryGetProperty("isChecked", out var isCheckedValue))
            {
                // Legacy alias
                isChecked = isCheckedValue.AsBoolOrThrow("isChecked");
            }

            if (string.IsNullOrWhiteSpace(text) && string.Equals(itemId, "about", StringComparison.OrdinalIgnoreCase))
            {
                text = "About";
            }
            else if (string.IsNullOrWhiteSpace(text) && string.Equals(itemId, "quit", StringComparison.OrdinalIgnoreCase))
            {
                text = "Quit";
            }
            else if (string.IsNullOrWhiteSpace(text))
            {
                text = itemId;
            }

            if (string.Equals(itemId, "settings", StringComparison.OrdinalIgnoreCase))
            {
                hasSettingsItem = true;
            }

            if (globalMacMenuMode
                && string.Equals(menuId, "appMenu", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(itemId, "about", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(itemId, "quit", StringComparison.OrdinalIgnoreCase)))
            {
                if (string.Equals(itemId, "about", StringComparison.OrdinalIgnoreCase))
                {
                    appMenuAboutText = text;
                }
                else
                {
                    appMenuQuitText = text;
                }

                // On macOS global menu, About/Quit are system entries and should be merged/replaced,
                // not duplicated as extra items.
                continue;
            }

            popup.AddItem(text, (int)itemIdSeed);
            var createdIndex = popup.ItemCount - 1;
            popup.SetItemMetadata(createdIndex, Variant.From(itemId));
            if (isCheckItemNode || isChecked)
            {
                popup.SetItemAsCheckable(createdIndex, true);
                popup.SetItemChecked(createdIndex, isChecked);
            }
            actionMap[itemIdSeed] = (menuId, itemId);
            itemIdSeed++;
        }

        if (globalMacMenuMode
            && string.Equals(menuId, "appMenu", StringComparison.OrdinalIgnoreCase)
            && !hasSettingsItem)
        {
            popup.AddSeparator();
            popup.AddItem("Settings", (int)itemIdSeed);
            var createdIndex = popup.ItemCount - 1;
            popup.SetItemMetadata(createdIndex, Variant.From("settings"));
            actionMap[itemIdSeed] = (menuId, "settings");
        }

        if (globalMacMenuMode && string.Equals(menuId, "appMenu", StringComparison.OrdinalIgnoreCase))
        {
            MergeMacSystemAppMenu(
                dispatchSource,
                menuId,
                appMenuAboutText,
                settingsText: null,
                appMenuQuitText,
                hasSettings: hasSettingsItem);
        }

        popup.IdPressed += id =>
        {
            if (!actionMap.TryGetValue(id, out var mapped))
            {
                return;
            }

            var sourceId = string.IsNullOrWhiteSpace(mapped.MenuId) ? "menu" : mapped.MenuId;
            var clickedId = mapped.ItemId;
            _actionDispatcher.Dispatch(new UiActionContext(
                Source: dispatchSource,
                SourceId: sourceId,
                SourceIdValue: IdRuntimeScope.GetOrCreate(sourceId),
                Action: "menuItemSelected",
                Clicked: clickedId,
                ClickedIdValue: IdRuntimeScope.GetOrCreate(clickedId)
            ));
        };
    }

    private void MergeMacSystemAppMenuFromSml(Control dispatchSource, SmlNode menuNode, string menuId, int attemptsLeft)
    {
        string? aboutText = null;
        string? settingsText = null;
        string? quitText = null;
        var hasSettings = false;

        foreach (var child in menuNode.Children)
        {
            if (!IsMenuEntryNode(child.Name))
            {
                continue;
            }

            var itemId = child.TryGetProperty("id", out var itemIdValue)
                ? itemIdValue.AsStringOrThrow("id")
                : string.Empty;
            var text = child.TryGetProperty("text", out var textValue)
                ? textValue.AsStringOrThrow("text")
                : string.Empty;

            if (string.Equals(itemId, "about", StringComparison.OrdinalIgnoreCase))
            {
                aboutText = text;
            }
            else if (string.Equals(itemId, "settings", StringComparison.OrdinalIgnoreCase))
            {
                settingsText = text;
                hasSettings = true;
            }
            else if (string.Equals(itemId, "quit", StringComparison.OrdinalIgnoreCase))
            {
                quitText = text;
            }
        }

        var merged = MergeMacSystemAppMenu(dispatchSource, menuId, aboutText, settingsText, quitText, hasSettings);
        if (!merged && attemptsLeft > 0)
        {
            ScheduleMacAppMenuMerge(dispatchSource, menuNode, menuId, attemptsLeft - 1);
        }
    }

    private void ScheduleMacAppMenuMerge(Control dispatchSource, SmlNode menuNode, string menuId, int attemptsLeft)
    {
        if (!GodotObject.IsInstanceValid(dispatchSource))
        {
            return;
        }

        if (!dispatchSource.IsInsideTree())
        {
            if (attemptsLeft <= 0)
            {
                return;
            }

            void OnTreeEntered()
            {
                dispatchSource.TreeEntered -= OnTreeEntered;
                ScheduleMacAppMenuMerge(dispatchSource, menuNode, menuId, attemptsLeft);
            }

            dispatchSource.TreeEntered += OnTreeEntered;
            return;
        }

        var tree = dispatchSource.GetTree();
        if (tree is null)
        {
            return;
        }

        var timer = tree.CreateTimer(0.05);
        timer.Timeout += () => MergeMacSystemAppMenuFromSml(dispatchSource, menuNode, menuId, attemptsLeft);
    }

    private bool MergeMacSystemAppMenu(Control dispatchSource, string menuId, string? aboutText, string? settingsText, string? quitText, bool hasSettings)
    {
        if (!string.Equals(OS.GetName(), "macOS", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Engine.HasSingleton("NativeMenu"))
        {
            return false;
        }

        var singleton = Engine.GetSingleton("NativeMenu");
        if (singleton is not NativeMenuInstance nativeMenu)
        {
            return false;
        }

        if (!nativeMenu.HasSystemMenu(NativeMenu.SystemMenus.ApplicationMenuId))
        {
            return false;
        }

        var appMenuRid = nativeMenu.GetSystemMenu(NativeMenu.SystemMenus.ApplicationMenuId);
        FindMacSystemAppMenuIndices(nativeMenu, appMenuRid, out var aboutIndex, out var settingsIndex, out var quitIndex);

        if (aboutIndex >= 0)
        {
            if (!string.IsNullOrWhiteSpace(aboutText))
            {
                nativeMenu.SetItemText(appMenuRid, aboutIndex, aboutText);
            }

            nativeMenu.SetItemTag(appMenuRid, aboutIndex, Variant.From("about"));
            nativeMenu.SetItemCallback(appMenuRid, aboutIndex, Callable.From((Variant _unused) =>
            {
                DispatchMenuSelection(dispatchSource, menuId, "about");
            }));
        }

        if (hasSettings && settingsIndex < 0)
        {
            var settingsLabel = string.IsNullOrWhiteSpace(settingsText) ? "Settings" : settingsText;
            var desiredIndex = aboutIndex >= 0
                ? aboutIndex + 1
                : (quitIndex >= 0 ? quitIndex : nativeMenu.GetItemCount(appMenuRid));

            var inserted = TryInsertNativeMenuItem(nativeMenu, appMenuRid, settingsLabel, desiredIndex);
            if (!inserted)
            {
                RunnerLogger.Warn("UI", "Could not inject Settings item into macOS app menu (NativeMenu insert API unavailable).");
            }

            // Re-scan after potential insertion.
            FindMacSystemAppMenuIndices(nativeMenu, appMenuRid, out aboutIndex, out settingsIndex, out quitIndex);
        }

        if (hasSettings && settingsIndex >= 0)
        {
            if (!string.IsNullOrWhiteSpace(settingsText))
            {
                nativeMenu.SetItemText(appMenuRid, settingsIndex, settingsText);
            }

            nativeMenu.SetItemTag(appMenuRid, settingsIndex, Variant.From("settings"));
            nativeMenu.SetItemCallback(appMenuRid, settingsIndex, Callable.From((Variant _unused) =>
            {
                DispatchMenuSelection(dispatchSource, menuId, "settings");
            }));
        }

        if (quitIndex >= 0)
        {
            if (!string.IsNullOrWhiteSpace(quitText))
            {
                nativeMenu.SetItemText(appMenuRid, quitIndex, quitText);
            }

            nativeMenu.SetItemTag(appMenuRid, quitIndex, Variant.From("quit"));
            nativeMenu.SetItemCallback(appMenuRid, quitIndex, Callable.From((Variant _unused) =>
            {
                DispatchMenuSelection(dispatchSource, menuId, "quit");
            }));
        }

        return aboutIndex >= 0 || settingsIndex >= 0 || quitIndex >= 0;
    }

    private static IEnumerable<SmlNode> EnumerateMenuNodes(SmlNode menuBarNode)
    {
        foreach (var child in menuBarNode.Children)
        {
            if (string.Equals(child.Name, "PopupMenu", StringComparison.OrdinalIgnoreCase))
            {
                yield return child;
            }
        }
    }

    private static void FindMacSystemAppMenuIndices(NativeMenuInstance nativeMenu, Rid appMenuRid, out int aboutIndex, out int settingsIndex, out int quitIndex)
    {
        var itemCount = nativeMenu.GetItemCount(appMenuRid);
        aboutIndex = -1;
        settingsIndex = -1;
        quitIndex = -1;

        for (var i = 0; i < itemCount; i++)
        {
            var text = nativeMenu.GetItemText(appMenuRid, i);
            if (aboutIndex < 0 && text.StartsWith("About", StringComparison.OrdinalIgnoreCase))
            {
                aboutIndex = i;
            }

            if (settingsIndex < 0 && IsLikelySettingsText(text))
            {
                settingsIndex = i;
            }

            if (quitIndex < 0 && text.StartsWith("Quit", StringComparison.OrdinalIgnoreCase))
            {
                quitIndex = i;
            }
        }
    }

    private static bool IsLikelySettingsText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.StartsWith("Settings", StringComparison.OrdinalIgnoreCase)
               || text.StartsWith("Preferences", StringComparison.OrdinalIgnoreCase)
               || text.StartsWith("Einstellungen", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryInsertNativeMenuItem(NativeMenuInstance nativeMenu, Rid appMenuRid, string text, int atIndex)
    {
        var addItemMethods = nativeMenu
            .GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => string.Equals(m.Name, "AddItem", StringComparison.Ordinal)
                        || string.Equals(m.Name, "add_item", StringComparison.OrdinalIgnoreCase))
            .Select(m => new { Method = m, Params = m.GetParameters() })
            .Where(x => x.Params.Length >= 2
                        && x.Params[0].ParameterType == typeof(Rid)
                        && x.Params[1].ParameterType == typeof(string))
            .OrderByDescending(x => x.Params.Count(p => p.Name is not null && p.Name.Contains("index", StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(x => x.Params.Length)
            .ToList();

        foreach (var candidate in addItemMethods)
        {
            try
            {
                var args = new object?[candidate.Params.Length];
                args[0] = appMenuRid;
                args[1] = text;

                for (var i = 2; i < candidate.Params.Length; i++)
                {
                    var parameter = candidate.Params[i];
                    var parameterType = parameter.ParameterType;

                    if (parameter.Name is not null && parameter.Name.Contains("index", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = atIndex;
                        continue;
                    }

                    args[i] = parameterType switch
                    {
                        var t when t == typeof(int) => -1,
                        var t when t == typeof(long) => -1L,
                        var t when t == typeof(bool) => false,
                        var t when t == typeof(string) => string.Empty,
                        var t when t == typeof(Callable) => new Callable(),
                        _ when parameter.HasDefaultValue => parameter.DefaultValue,
                        _ => parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null
                    };
                }

                candidate.Method.Invoke(nativeMenu, args);
                return true;
            }
            catch
            {
                // Try next overload.
            }
        }

        return false;
    }

    private static bool IsMenuEntryNode(string nodeName)
    {
        return string.Equals(nodeName, "Item", StringComparison.OrdinalIgnoreCase)
               || string.Equals(nodeName, "CheckItem", StringComparison.OrdinalIgnoreCase);
    }

    private void DispatchMenuSelection(Control source, string menuId, string itemId)
    {
        _actionDispatcher.Dispatch(new UiActionContext(
            Source: source,
            SourceId: menuId,
            SourceIdValue: IdRuntimeScope.GetOrCreate(menuId),
            Action: "menuItemSelected",
            Clicked: itemId,
            ClickedIdValue: IdRuntimeScope.GetOrCreate(itemId)
        ));
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

        if (content.GetParent() is not null)
        {
            content.GetParent()?.RemoveChild(content);
        }

        margin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
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
        scroll.ConfigureFromSmlMeta(content);

        if (content.GetParent() is not null)
        {
            content.GetParent()?.RemoveChild(content);
        }

        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
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

    private static bool TryApplyContextProperty(
        Control control,
        string? parentNodeName,
        string childNodeName,
        string propertyName,
        SmlValue value)
    {
        if (string.IsNullOrWhiteSpace(parentNodeName))
        {
            return false;
        }

        if (!SchemaContextProperties.TryGet(parentNodeName, childNodeName, propertyName, out var def))
        {
            return false;
        }

        var metaKey = BuildContextMetaKey(def.TargetMeta);
        control.SetMeta(metaKey, ToVariant(value, propertyName, def.ValueType));
        return true;
    }

    private static string BuildContextMetaKey(string targetMeta)
        => "sml_ctx_" + targetMeta;

    private static Variant ToVariant(SmlValue value, string propertyName, string valueType)
    {
        return valueType.ToLowerInvariant() switch
        {
            "string" => Variant.From(value.AsStringOrThrow(propertyName)),
            "bool" => Variant.From(value.AsBoolOrThrow(propertyName)),
            "int" => Variant.From(value.AsIntOrThrow(propertyName)),
            "float" => Variant.From((float)value.AsDoubleOrThrow(propertyName)),
            _ => Variant.From(value.AsStringOrThrow(propertyName))
        };
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
