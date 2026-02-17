using Godot;
using Runtime.Logging;
using Runtime.Sml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Runtime.UI;

public static class UiRuntimeApi
{
    private static readonly HashSet<ulong> HookedTreeInstances = [];

    /// <summary>
    /// Creates a floating native Godot window from inline SML text.
    /// The window is always-on-top, non-transient (freestanding), and attached to the app scene tree.
    /// </summary>
    public static bool CreateWindowFromSml(string smlText, Action? onClosed = null)
    {
        if (string.IsNullOrWhiteSpace(smlText))
        {
            RunnerLogger.Warn("UI", "CreateWindowFromSml called with empty SML text.");
            return false;
        }

        if (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root is null)
        {
            RunnerLogger.Warn("UI", "CreateWindowFromSml failed: SceneTree root is not available.");
            return false;
        }

        try
        {
            // Force native OS subwindows (not embedded inside main app viewport).
            sceneTree.Root.GuiEmbedSubwindows = false;

            var schema = SmlSchemaFactory.CreateDefault();
            var parser = new SmlParser(smlText, schema);
            var document = parser.ParseDocument();

            foreach (var warning in document.Warnings)
            {
                RunnerLogger.ParserWarning(warning);
            }

            var builder = new SmlUiBuilder(
                new NodeFactoryRegistry(),
                new NodePropertyMapper(),
                animationApi: new Runtime.ThreeD.AnimationControlApi());

            var content = builder.Build(document);

            var floatingWindow = new Window
            {
                Name = content.HasMeta(NodePropertyMapper.MetaId)
                    ? $"FloatingWindow_{content.GetMeta(NodePropertyMapper.MetaId).AsString()}"
                    : "FloatingWindow",
                AlwaysOnTop = true,
                Transient = false,
                InitialPosition = Window.WindowInitialPosition.Absolute,
                Unresizable = false,
                Visible = false
            };

            ApplyWindowProperties(content, floatingWindow);
            PrepareContentForFloatingWindow(content);
            floatingWindow.AddChild(content);
            floatingWindow.CloseRequested += () =>
            {
                try
                {
                    onClosed?.Invoke();
                }
                finally
                {
                    floatingWindow.QueueFree();
                }
            };

            sceneTree.Root.AddChild(floatingWindow);
            floatingWindow.Show();

            RunnerLogger.Info("UI", "Created floating window from SMS via ui.CreateWindow(...). AlwaysOnTop=true.");
            return true;
        }
        catch (Exception ex)
        {
            RunnerLogger.Error("UI", "CreateWindowFromSml failed.", ex);
            return false;
        }
    }

    /// <summary>
    /// Finds an SML node by its unique SML id (Meta: sml_id) in the current scene tree.
    /// Returns null if no node is found.
    /// </summary>
    public static Node? GetObjectById(string smlId)
    {
        if (string.IsNullOrWhiteSpace(smlId))
        {
            return null;
        }

        if (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root is null)
        {
            return null;
        }

        var stack = new Stack<Node>();
        stack.Push(sceneTree.Root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node.HasMeta(NodePropertyMapper.MetaId)
                && string.Equals(node.GetMeta(NodePropertyMapper.MetaId).AsString(), smlId, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            for (var i = node.GetChildCount() - 1; i >= 0; i--)
            {
                stack.Push(node.GetChild(i));
            }
        }

        return null;
    }

    /// <summary>
    /// Waits a few frames for UI attachment and then tries to resolve an SML node by id.
    /// This keeps timing concerns out of app-level code.
    /// </summary>
    public static async Task<Node?> GetObjectByIdAsync(string smlId, int maxFrames = 5)
    {
        if (string.IsNullOrWhiteSpace(smlId))
        {
            return null;
        }

        if (Engine.GetMainLoop() is not SceneTree sceneTree)
        {
            return GetObjectById(smlId);
        }

        // First immediate attempt.
        var node = GetObjectById(smlId);
        if (node is not null)
        {
            return node;
        }

        var frames = Math.Max(1, maxFrames);
        for (var i = 0; i < frames; i++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
            node = GetObjectById(smlId);
            if (node is not null)
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>
    /// Ensures dynamic Tree selections/toggles are forwarded to the dispatcher
    /// using the same actions as SML-built items.
    /// </summary>
    public static void BindTreeEvents(Tree tree, UiActionDispatcher dispatcher)
    {
        var instanceId = tree.GetInstanceId();
        if (!HookedTreeInstances.Add(instanceId))
        {
            return;
        }

        tree.ItemSelected += () =>
        {
            var selected = tree.GetSelected();
            if (selected is null)
            {
                return;
            }

            var sourceId = tree.HasMeta(NodePropertyMapper.MetaId)
                ? tree.GetMeta(NodePropertyMapper.MetaId).AsString()
                : string.Empty;
            var sourceIdValue = tree.HasMeta(NodePropertyMapper.MetaIdValue)
                ? new Id(tree.GetMeta(NodePropertyMapper.MetaIdValue).AsInt32())
                : new Id(0);

            dispatcher.Dispatch(new UiActionContext(
                Source: tree,
                SourceId: sourceId,
                SourceIdValue: sourceIdValue,
                Action: "treeItemSelected",
                Clicked: string.Empty,
                ClickedIdValue: new Id(0),
                ItemId: new Id(0),
                TreeItem: new TreeViewItem
                {
                    Id = 0,
                    Text = selected.GetText(0),
                    Expanded = !selected.Collapsed
                }
            ));
        };

        tree.ButtonClicked += (item, _column, id, _mouseButtonIndex) =>
        {
            if (item is null)
            {
                return;
            }

            var sourceId = tree.HasMeta(NodePropertyMapper.MetaId)
                ? tree.GetMeta(NodePropertyMapper.MetaId).AsString()
                : string.Empty;
            var sourceIdValue = tree.HasMeta(NodePropertyMapper.MetaIdValue)
                ? new Id(tree.GetMeta(NodePropertyMapper.MetaIdValue).AsInt32())
                : new Id(0);

            // Dynamic tree items do not carry built-in toggle model state; dispatch best-effort.
            dispatcher.Dispatch(new UiActionContext(
                Source: tree,
                SourceId: sourceId,
                SourceIdValue: sourceIdValue,
                Action: "treeItemToggled",
                Clicked: string.Empty,
                ClickedIdValue: new Id((int)id),
                BoolValue: null,
                ItemId: new Id(0),
                ToggleIdValue: new ToggleId((int)id),
                TreeItem: new TreeViewItem
                {
                    Id = 0,
                    Text = item.GetText(0),
                    Expanded = !item.Collapsed
                }
            ));
        };

        RunnerLogger.Info("UI", $"Bound dynamic Tree events for '{sourceIdForLog(tree)}'.");
    }

    private static string sourceIdForLog(Tree tree)
    {
        return tree.HasMeta(NodePropertyMapper.MetaId)
            ? tree.GetMeta(NodePropertyMapper.MetaId).AsString()
            : "<without-id>";
    }

    private static void PrepareContentForFloatingWindow(Control content)
    {
        NodePropertyMapper.ApplyFillMaxSize(content);
        content.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        content.SetOffsetsPreset(Control.LayoutPreset.FullRect);
        content.Position = Vector2.Zero;
    }

    private static void ApplyWindowProperties(Control rootControl, Window targetWindow)
    {
        if (rootControl.HasMeta(NodePropertyMapper.MetaWindowTitle))
        {
            targetWindow.Title = rootControl.GetMeta(NodePropertyMapper.MetaWindowTitle).AsString();
        }

        if (rootControl.HasMeta(NodePropertyMapper.MetaWindowSizeX)
            && rootControl.HasMeta(NodePropertyMapper.MetaWindowSizeY))
        {
            var width = Math.Max(1, rootControl.GetMeta(NodePropertyMapper.MetaWindowSizeX).AsInt32());
            var height = Math.Max(1, rootControl.GetMeta(NodePropertyMapper.MetaWindowSizeY).AsInt32());
            targetWindow.Size = new Vector2I(width, height);
        }

        if (rootControl.HasMeta(NodePropertyMapper.MetaWindowPosX)
            && rootControl.HasMeta(NodePropertyMapper.MetaWindowPosY))
        {
            var x = rootControl.GetMeta(NodePropertyMapper.MetaWindowPosX).AsInt32();
            var y = rootControl.GetMeta(NodePropertyMapper.MetaWindowPosY).AsInt32();
            targetWindow.Position = new Vector2I(x, y);
        }

        if (rootControl.HasMeta(NodePropertyMapper.MetaWindowMinSizeX)
            && rootControl.HasMeta(NodePropertyMapper.MetaWindowMinSizeY))
        {
            var minWidth = Math.Max(0, rootControl.GetMeta(NodePropertyMapper.MetaWindowMinSizeX).AsInt32());
            var minHeight = Math.Max(0, rootControl.GetMeta(NodePropertyMapper.MetaWindowMinSizeY).AsInt32());
            targetWindow.MinSize = new Vector2I(minWidth, minHeight);
        }
    }

}
