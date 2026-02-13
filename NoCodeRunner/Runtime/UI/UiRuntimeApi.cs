using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Runtime.UI;

public static class UiRuntimeApi
{
    private static readonly HashSet<ulong> HookedTreeInstances = [];

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
    /// Ensures dynamic TreeView selections/toggles are forwarded to the dispatcher
    /// using the same actions as SML-built items.
    /// </summary>
    public static void BindTreeViewEvents(TreeView tree, UiActionDispatcher dispatcher)
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

        RunnerLogger.Info("UI", $"Bound dynamic TreeView events for '{sourceIdForLog(tree)}'.");
    }

    private static string sourceIdForLog(TreeView tree)
    {
        return tree.HasMeta(NodePropertyMapper.MetaId)
            ? tree.GetMeta(NodePropertyMapper.MetaId).AsString()
            : "<without-id>";
    }
}
