using Godot;
using Runtime.Logging;
using Runtime.UI;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NoCodeDesigner;

public sealed class App : IUiActionModule
{
    // Wird jetzt von Main nach UI-Attach automatisch aufgerufen.
    private async Task OnReadyAsync(UiActionDispatcher dispatcher)
    {
        var tree = await UiRuntimeApi.GetObjectByIdAsync("treeview") as TreeView;
        if (tree is null)
        {
            RunnerLogger.Info("NoCodeDesigner.App", "Kein TreeView mit id 'treeview' gefunden – nichts zu befüllen.");
            return;
        }

        PopulateTreeWithDocsContent(tree);
        UiRuntimeApi.BindTreeViewEvents(tree, dispatcher);
        RunnerLogger.Info("NoCodeDesigner.App", "TreeView 'treeview' wurde mit Inhalten aus /docs befüllt.");
    }

    private static void PopulateTreeWithDocsContent(TreeView tree)
    {
        tree.Clear();
        tree.Columns = 1;

        var docsPath = ResolveDocsPath();
        if (string.IsNullOrWhiteSpace(docsPath) || !Directory.Exists(docsPath))
        {
            var rootFallback = tree.CreateItem();
            rootFallback.SetText(0, "docs (not found)");
            rootFallback.Collapsed = false;
            RunnerLogger.Warn("NoCodeDesigner.App", "Konnte /docs nicht finden. TreeView enthält nur Fallback-Knoten.");
            return;
        }

        var root = tree.CreateItem();
        root.SetText(0, "docs");
        root.Collapsed = false;

        AddDirectoryRecursive(tree, root, docsPath);
    }

    private static void AddDirectoryRecursive(Tree tree, TreeItem parent, string directoryPath)
    {
        foreach (var dir in Directory.GetDirectories(directoryPath))
        {
            var dirName = Path.GetFileName(dir);
            if (string.Equals(dirName, ".DS_Store", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dirItem = tree.CreateItem(parent);
            dirItem.SetText(0, dirName + "/");
            AddDirectoryRecursive(tree, dirItem, dir);
        }

        foreach (var file in Directory.GetFiles(directoryPath))
        {
            var fileName = Path.GetFileName(file);
            if (string.Equals(fileName, ".DS_Store", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileItem = tree.CreateItem(parent);
            fileItem.SetText(0, fileName);
        }
    }

    private static string? ResolveDocsPath()
    {
        var cwd = Directory.GetCurrentDirectory();
        var projectDir = ProjectSettings.GlobalizePath("res://");
        var candidates = new[]
        {
            Path.Combine(projectDir, "..", "docs"),
            Path.Combine(projectDir, "docs"),
            Path.Combine(cwd, "docs"),
            Path.Combine(AppContext.BaseDirectory, "docs"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs")
        };

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (Directory.Exists(full))
            {
                return full;
            }
        }

        return null;
    }

    // Einheitliche Script-Konvention
    private void treeItemSelected(Id treeView, TreeViewItem item)
    {
        RunnerLogger.Info("NoCodeDesigner.App", $"treeItemSelected -> treeView={treeView.Value}, text='{item.Text}'");
    }

    // Einheitliche Script-Konvention
    private void treeItemToggled(Id treeView, TreeViewItem item, bool isOn)
    {
        RunnerLogger.Info("NoCodeDesigner.App", $"treeItemToggled -> treeView={treeView.Value}, text='{item.Text}', isOn={isOn}");
    }
}
